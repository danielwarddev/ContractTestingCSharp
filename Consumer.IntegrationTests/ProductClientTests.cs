using System.Net;
using FluentAssertions;
using PactNet;

namespace Consumer.IntegrationTests;

public class ProductClientTests
{
    private readonly HttpClient _httpClient;
    private readonly ProductClient _productClient;
    private readonly IPactBuilderV4 _pactBuilder = Pact
        .V4("My Consumer Service", "Product API", new PactConfig())
        .WithHttpInteractions();

    public ProductClientTests()
    {
        _httpClient = new HttpClient();
        _httpClient.AddRequestHeaders(Program.ProductClientRequestHeaders);
        _productClient = new ProductClient(_httpClient);
    }
    
    [Fact]
    public async Task Api_Returns_All_Products()
    {
        var expectedProducts = new [] { new Product(1, "A cool product", 10.50m, "Cool Store #12345") };
        
        _pactBuilder
            .UponReceiving("A GET request to retrieve all products")
            .Given("Products exist")
            .WithRequest(HttpMethod.Get, "/products")
            .WithHeaders(Program.ProductClientRequestHeaders)
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(expectedProducts);
        
        await _pactBuilder.VerifyAsync(async context =>
        {
            _httpClient.BaseAddress = context.MockServerUri;
            var actualProducts = await _productClient.GetAllProducts();
            actualProducts.Should().BeEquivalentTo(expectedProducts);
        });
    }
    
    /*[Fact]
    public async Task Api_Returns_All_Products_With_Name()
    {
        var expectedProducts = new [] { new Product(1, "A cool product", 10.50m, "Cool Store #12345") };
        
        _pactBuilder
            .UponReceiving("A GET request to retrieve all products with given id and name")
            .Given("A product exists")
            .WithRequest(HttpMethod.Get, "/products")
            .WithQuery("name", "A cool product")
            .WithHeaders(Program.ProductClientRequestHeaders)
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(expectedProducts);
        
        await _pactBuilder.VerifyAsync(async context =>
        {
            _httpClient.BaseAddress = context.MockServerUri;
            var actualProducts = await _productClient.GetProductsByName("A cool product");
            actualProducts.Should().BeEquivalentTo(expectedProducts);
        });
    }*/
    
    [Fact]
    public async Task When_Product_Exists_Then_Api_Returns_Product()
    {
        var expectedProduct = new Product(1, "Cool product", 10.50m, "Cool Store #12345");
        
        _pactBuilder
            .UponReceiving("A GET request to retrieve a product")
            .Given("A product with id 1 exists", new Dictionary<string, string>
            {
                { "productId", "1" },
                { "productName", "Cool product" }
            })
            .WithRequest(HttpMethod.Get, "/product/1")
            .WithHeaders(Program.ProductClientRequestHeaders)
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(expectedProduct);
        
        await _pactBuilder.VerifyAsync(async context =>
        {
            _httpClient.BaseAddress = context.MockServerUri;
            var actualProduct = await _productClient.GetProduct(1);
            actualProduct.Should().BeEquivalentTo(expectedProduct);
        });
    }
    
    [Fact]
    public async Task When_Product_Does_Not_Exist_Then_Api_Returns_404()
    {
        _pactBuilder
            .UponReceiving("A GET request to retrieve a product")
            .Given("There is not a product with id 1")
            .WithRequest(HttpMethod.Get, "/product/1")
            .WithHeaders(Program.ProductClientRequestHeaders)
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);
        
        await _pactBuilder.VerifyAsync(async context =>
        {
            _httpClient.BaseAddress = context.MockServerUri;
            var actualProduct = await _productClient.GetProduct(1);
            actualProduct.Should().BeNull();
        });
    }
}

public static class PactExtensions
{
    public static IRequestBuilderV4 WithHeaders(this IRequestBuilderV4 builder, IEnumerable<HttpHeader> headers)
    {
        foreach (var header in headers)
        {
            builder = builder.WithHeader(header.Key, header.Value);
        }

        return builder;
    }
}