using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Provider.Database;
using Respawn;
using Testcontainers.PostgreSql;

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