﻿using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Provider.Database;

namespace Provider.Api.ContractTests;

public class ProviderState
{
    [property: JsonPropertyName("action")]
    public string Action { get; init; } = null!;
    [property: JsonPropertyName("params")]
    public Dictionary<string, string> Params { get; init; } = null!;
    [property: JsonPropertyName("state")]
    public string State { get; init; } = null!;
}

public class ProviderStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Func<Task> _resetDatabase;
    private readonly IDictionary<string, Action> _providerStates;
    private ProductContext _dbContext;
    
    public ProviderStateMiddleware(RequestDelegate next, Func<Task> resetDatabase)
    {
        _next = next;
        _resetDatabase = resetDatabase;
        _providerStates = new Dictionary<string, Action>
        {
            {
                "A product with id 1 exists",
                () => MockData(1)
            },
            {
                "A product exists",
                () => MockData(1)
            },
            {
                "Products exist",
                () => MockData(1)
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
        
        var providerState = JsonSerializer.Deserialize<ProviderState>(body);
        if (!string.IsNullOrEmpty(providerState?.State))
        {
            var actionExists = _providerStates.TryGetValue(providerState.State, out var dataSetupAction);
            if (!actionExists) { return; }
            dataSetupAction!.Invoke();
        }
    }
    
    private void MockData(int id)
    {
        _dbContext.Add(new Product
        {
            Id = id,
            Name = "A cool product",
            Price = 10.5,
            Location = "Cool Store #12345"
        });
        _dbContext.SaveChanges();
    }
}