using System.Net;
using System.Text;
using System.Text.Json;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit.Abstractions;

namespace Provider.Api.ContractTests;

public class EmployeeGeneratorTests : IDisposable, IAsyncLifetime
{
    private readonly EmployeeGenerator _employeeGenerator = new();
    private readonly PactVerifier _pactVerifier;
    private readonly string _pactPath;
    private WebApplication _server;
    private readonly string _providerServerUri = "http://localhost:26405";

    public EmployeeGeneratorTests(ITestOutputHelper outputHelper)
    {
        var config = new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Debug,
            Outputters = new List<IOutput> { new XunitOutput(outputHelper), new ConsoleOutput() }
        };
        
        _pactPath = Path.Combine("Pacts", "Employee Message Consumer-Employee Message Publisher.json");
        _pactVerifier = new PactVerifier("Employee Message Publisher", config);
    }
    
    [Fact]
    public void Verify_EmployeeService_Pact_Is_Honored()
    {
        _pactVerifier
            .WithMessages(scenarios =>
            {
                scenarios.Add(
                    "An employee event",
                    // This should create a business object the same way as the real app
                    () => _employeeGenerator.CreateEmployee()
                );
            })
            .WithFileSource(new FileInfo(_pactPath))
            .Verify();
    }
    
    [Fact]
    public void Verify_EmployeeService_Pact_Is_Honored_With_Provider_States()
    {
        _pactVerifier
            .WithMessages(scenarios =>
            {
                scenarios.Add(
                    "An employee event",
                    // This should create a business object the same way as the real app
                    () => _employeeGenerator.CreateEmployee()
                );
            })
            .WithFileSource(new FileInfo(_pactPath))
            .WithProviderStateUrl(new Uri($"{_providerServerUri}/provider-states"))
            .Verify();
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(_providerServerUri);
        _server = builder.Build();
        
        _server.MapPost("/provider-states", async context =>
        {
            string body;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }
            var providerState = JsonSerializer.Deserialize<ProviderState>(body)!;

            if (providerState.State == "some provider state")
            {
                // Do some setup with the state here
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(String.Empty);
        });
        
        await _server.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _server.StopAsync();
    }
    
    public void Dispose()
    {
        _pactVerifier.Dispose();
    }
}