using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Provider.Database;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace Provider.Api.ContractTests;

public class ProductApiFixture : IDisposable, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().WithCleanUp(true).Build();
    private Respawner _respawner = null!;
    private IHost _server = null!;
    private DbConnection _connection = null!;
    public ProductContext Db { get; private set; } = null!;
    public Uri PactServerUri { get; } = new ("http://localhost:26404");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _server = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseUrls(PactServerUri.ToString());
                builder.UseStartup<TestStartup>();
            })
            .ConfigureServices(services =>
            {
                services.ReplaceDbContext<ProductContext>(_container.GetConnectionString());
                services.AddSingleton<Func<Task>>(_ => ResetDatabase);
            })
            .Build();

        Db = _server.Services.CreateScope().ServiceProvider.GetRequiredService<ProductContext>();
        _connection = Db.Database.GetDbConnection();
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });

        _server.Start();
    }

    public async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _container.DisposeAsync();
    }
    
    public void Dispose()
    {
        _server.Dispose();
    }

    private async Task ResetDatabase()
    {
        await _respawner.ResetAsync(_connection);
    }
}

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

public static class ServiceProviderExtensions
{
    public static IServiceCollection ReplaceDbContext<T>(this IServiceCollection services, string connectionString) where T : DbContext
    {
        services.RemoveAll<DbContextOptions<T>>();
        services.AddDbContext<T>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        var scope = services.BuildServiceProvider().CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductContext>();
        dbContext.Database.EnsureCreated();

        return services;
    }
}