using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit.Abstractions;

namespace Provider.Api.ContractTests;

public class ProductControllerTests : IClassFixture<ProductApiFixture>
{
    private readonly ProductApiFixture _apiFixture;
    private readonly ITestOutputHelper _outputHelper;
    
    public ProductControllerTests(ProductApiFixture apiFixture, ITestOutputHelper outputHelper)
    {
        _apiFixture = apiFixture;
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public void Verify_MyService_Pact_Is_Honored()
    {
        var config = new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Debug,
            Outputters = new List<IOutput> { new XunitOutput(_outputHelper), new ConsoleOutput() }
        };

        var pactPath = Path.Combine("Pacts", "My Consumer Service-Product API.json");
        var pactVerifier = new PactVerifier("Product API", config);

        pactVerifier
            .WithHttpEndpoint(_apiFixture.PactServerUri)
            .WithFileSource(new FileInfo(pactPath))
            .WithProviderStateUrl(new Uri(_apiFixture.PactServerUri, "/provider-states"))
            .WithFilter("x A GET request to retrieve all products")
            .Verify();
    }
}