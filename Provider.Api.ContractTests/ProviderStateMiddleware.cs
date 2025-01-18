using System.Net;
using System.Text;
using System.Text.Json;
using Provider.Database;

namespace Provider.Api.ContractTests;

public record PossibleParameters(int? ProductId, string? ProductName, decimal? ProducePrice);
public record ProviderState(string Action, PossibleParameters Params, string State);

public class ProviderStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Func<Task> _resetDatabase;
    private readonly Dictionary<string, Func<int, string, decimal, Task>> _providerStateSetups;
    private ProductContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    
    public ProviderStateMiddleware(RequestDelegate next, Func<Task> resetDatabase)
    {
        _next = next;
        _resetDatabase = resetDatabase;
        _providerStateSetups = new Dictionary<string, Func<int, string, decimal, Task>>
        {
            {
                "A product with id 1 exists",
                async (productId, productName, productPrice) => await MockData(productId, productName, productPrice)
            },
            {
                "Products exist",
                async (productId, productName, productPrice) => await MockData(1, "A cool product", 10.5m)
            },
            {
                "A product exists",
                async (productId, productName, productPrice) => await MockData(1, "A cool product", 10.5m)
            }
        };
    }
    
    public async Task Invoke(HttpContext context, ProductContext dbContext)
    {
        if (context.Request.Path.Value == "/provider-states")
        {
            _dbContext = dbContext;
            await HandleProviderStateRequest(context);
            await context.Response.WriteAsync(string.Empty);
        }
        else
        {
            await _next(context);
        }
    }
    
    private async Task HandleProviderStateRequest(HttpContext context)
    {
        await _resetDatabase();
        
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        if (context.Request.Method.ToUpper() != HttpMethod.Post.ToString().ToUpper()) { return; }
        
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync();
        }
        
        var providerState = JsonSerializer.Deserialize<ProviderState>(body, _jsonOptions);
        if (!string.IsNullOrEmpty(providerState?.State))
        {
            var actionExists = _providerStateSetups.TryGetValue(providerState.State, out var dataSetupAction);
            if (!actionExists) { return; }

            await dataSetupAction!.Invoke(
                providerState.Params.ProductId ?? 1,
                providerState.Params.productName ?? "A cool product",
                providerState.Params.ProducePrice ?? 10.5m
            );
        }
    }
    
    private async Task MockData(int id, string productName, decimal price)
    {
        await _dbContext.AddAsync(new Product
        {
            Id = id,
            Name = productName,
            Price = price,
            Location = "Cool Store #12345"
        });
        await _dbContext.SaveChangesAsync();
    }
}