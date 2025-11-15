using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Configuration
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
var products = new List<Product>
{
    new() { Id = 1, Name = "Product A", Price = 29.99m, Stock = 100 },
    new() { Id = 2, Name = "Product B", Price = 39.99m, Stock = 50 },
    new() { Id = 3, Name = "Product C", Price = 49.99m, Stock = 75 }
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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Login endpoint (public) with rate limiting and validation
app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    // Input validation
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    // Simple rate limiting (5 attempts per 15 minutes per IP/username)
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

// Products API endpoints (protected)
var productsGroup = app.MapGroup("/api/products").RequireAuthorization();

productsGroup.MapGet("/", () => Results.Ok(products));

productsGroup.MapGet("/{id}", (int id) =>
{
    if (id <= 0)
    {
        return Results.BadRequest(new { error = "Product ID must be greater than 0." });
    }

    var product = products.FirstOrDefault(p => p.Id == id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

productsGroup.MapPost("/", (Product product) =>
{
    // Input validation
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(product);
    
    if (!Validator.TryValidateObject(product, validationContext, validationResults, true))
    {
        return Results.BadRequest(new { errors = validationResults.Select(v => v.ErrorMessage) });
    }

    // Additional business validation
    if (product.Price < 0)
    {
        return Results.BadRequest(new { error = "Price cannot be negative." });
    }

    if (product.Stock < 0)
    {
        return Results.BadRequest(new { error = "Stock cannot be negative." });
    }

    product.Id = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;
    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

// Logging for debugging
app.Logger.LogInformation("Products Service starting on port 8080");

app.Run("http://*:8080");

record Product
{
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
    public decimal Price { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number.")]
    public int Stock { get; set; }
    
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
