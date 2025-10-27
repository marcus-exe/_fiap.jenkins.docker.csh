using System.Net.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Get products service URL from environment
var productsUrl = builder.Configuration["PRODUCTS_URL"] ?? "http://products:8080";

// In-memory data store
var orders = new List<Order>
{
    new() { Id = 1, CustomerName = "John Doe", ProductId = 1, Quantity = 2, Status = "Completed" },
    new() { Id = 2, CustomerName = "Jane Smith", ProductId = 2, Quantity = 1, Status = "Pending" }
};

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Health check endpoint
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

// Orders API endpoints
app.MapGet("/api/orders", () => Results.Ok(orders));

app.MapGet("/api/orders/{id}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.MapPost("/api/orders", async (Order order, HttpClient httpClient) =>
{
    // Verify product exists by calling products service
    try
    {
        var productResponse = await httpClient.GetAsync($"{productsUrl}/api/products/{order.ProductId}");
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
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending";
}

