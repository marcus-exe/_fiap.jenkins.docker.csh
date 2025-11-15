using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Get products service URL from environment
var productsUrl = builder.Configuration["PRODUCTS_URL"] ?? "http://products:8080";

// JWT Configuration (should match Products service for inter-service communication)
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "YourSuperSecretKeyForJWTTokenGenerationThatShouldBeAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "ProductsService";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "ProductsService";

// Validate JWT secret length
if (jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT_SECRET must be at least 32 characters long for security.");
}

var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// In-memory data store
var orders = new List<Order>
{
    new() { Id = 1, CustomerName = "John Doe", ProductId = 1, Quantity = 2, Status = "Completed" },
    new() { Id = 2, CustomerName = "Jane Smith", ProductId = 2, Quantity = 1, Status = "Pending" }
};

// In-memory user store with hashed passwords (for demo purposes)
// In production, use a database with proper password hashing
var users = new Dictionary<string, User>
{
    // Passwords are hashed using BCrypt
    // Default passwords: admin123, user123
    { "admin", new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 12) } },
    { "user", new User { Username = "user", PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123", workFactor: 12) } }
};

// Simple rate limiting dictionary (in production, use Redis or dedicated rate limiting library)
var loginAttempts = new Dictionary<string, (int attempts, DateTime resetTime)>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint (public)
app.MapGet("/health", async (HttpClient httpClient) =>
{
    try
    {
        // Optionally check if products service is reachable
        var response = await httpClient.GetAsync($"{productsUrl}/health");
        return Results.Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            productsService = response.IsSuccessStatusCode ? "reachable" : "unreachable"
        });
    }
    catch
    {
        return Results.Ok(new 
        { 
            status = "healthy", 
            timestamp = DateTime.UtcNow,
            productsService = "unreachable"
        });
    }
});

// Login endpoint (public) with rate limiting and validation
app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    // Input validation
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    // Simple rate limiting (5 attempts per 15 minutes per username)
    var rateLimitKey = $"{request.Username}";
    if (loginAttempts.TryGetValue(rateLimitKey, out var attempts))
    {
        if (attempts.resetTime > DateTime.UtcNow)
        {
            if (attempts.attempts >= 5)
            {
                return Results.StatusCode(429); // Too Many Requests
            }
            loginAttempts[rateLimitKey] = (attempts.attempts + 1, attempts.resetTime);
        }
        else
        {
            loginAttempts[rateLimitKey] = (1, DateTime.UtcNow.AddMinutes(15));
        }
    }
    else
    {
        loginAttempts[rateLimitKey] = (1, DateTime.UtcNow.AddMinutes(15));
    }

    // Validate user exists
    if (!users.TryGetValue(request.Username, out var user))
    {
        // Don't reveal if user exists (security best practice)
        return Results.Unauthorized();
    }

    // Verify password using BCrypt
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    // Reset rate limit on successful login
    loginAttempts.Remove(rateLimitKey);

    // Generate JWT token
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, request.Username),
        new Claim(ClaimTypes.NameIdentifier, request.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { token = tokenString, expiresIn = 3600 });
})
.WithName("Login");

// Orders API endpoints (protected)
var ordersGroup = app.MapGroup("/api/orders").RequireAuthorization();

ordersGroup.MapGet("/", () => Results.Ok(orders));

ordersGroup.MapGet("/{id}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { error = "Order ID must be greater than 0." });
    }

    var order = orders.FirstOrDefault(o => o.Id == id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

ordersGroup.MapPost("/", async (Order order, HttpClient httpClient, HttpContext httpContext) =>
{
    // Input validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(order);
    
    if (!Validator.TryValidateObject(order, validationContext, validationResults, true))
    {
        return Results.BadRequest(new { errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Additional business validation
    if (order.Quantity <= 0)
    {
        return Results.BadRequest(new { error = "Quantity must be greater than 0." });
    }

    if (order.ProductId <= 0)
    {
        return Results.BadRequest(new { error = "Product ID must be greater than 0." });
    }

    // Get the JWT token from the current request to forward to Products service
    var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    
    // Verify product exists by calling products service with JWT token
    try
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{productsUrl}/api/products/{order.ProductId}");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        
        var productResponse = await httpClient.SendAsync(request);
        if (!productResponse.IsSuccessStatusCode)
        {
            return Results.BadRequest(new { error = "Product not found" });
        }
    }
    catch
    {
        return Results.BadRequest(new { error = "Cannot reach products service" });
    }

    order.Id = orders.Count > 0 ? orders.Max(o => o.Id) + 1 : 1;
    order.Status = "Pending";
    orders.Add(order);
    return Results.Created($"/api/orders/{order.Id}", order);
});

app.Logger.LogInformation("Orders Service starting on port 8080");
app.Logger.LogInformation($"Products Service URL configured as: {productsUrl}");

app.Run("http://*:8080");

record Order
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;
    
    [Range(1, int.MaxValue, ErrorMessage = "Product ID must be greater than 0.")]
    public int ProductId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
    
    public string Status { get; set; } = "Pending";
    public int Id { get; set; }
}

record LoginRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
