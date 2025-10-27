using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory data store
var products = new List<Product>
{
    new() { Id = 1, Name = "Product A", Price = 29.99m, Stock = 100 },
    new() { Id = 2, Name = "Product B", Price = 39.99m, Stock = 50 },
    new() { Id = 3, Name = "Product C", Price = 49.99m, Stock = 75 }
};

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Products API endpoints
app.MapGet("/api/products", () => Results.Ok(products));

app.MapGet("/api/products/{id}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is not null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/api/products", (Product product) =>
{
    product.Id = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;
    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

// Logging for debugging
app.Logger.LogInformation("Products Service starting on port 8080");

app.Run("http://*:8080");

record Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

