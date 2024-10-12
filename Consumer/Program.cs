using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Consumer;

public class Program
{
    public static readonly HttpHeader[] ProductClientRequestHeaders = { new("Accept", "application/json") };
    
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddHttpClient<IProductClient, ProductClient>(client =>
                {
                    client.BaseAddress = new Uri("http://example.com");
                    client.AddRequestHeaders(ProductClientRequestHeaders);
                });
            })
            .Build()
            .RunAsync();
    }
}

public record HttpHeader(string Key, string Value);

public static class ServiceExtensions
{
    public static void AddRequestHeaders(this HttpClient client, IEnumerable<HttpHeader> headers)
    {
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }
}