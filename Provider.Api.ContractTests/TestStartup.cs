namespace Provider.Api.ContractTests;

public class TestStartup
{
    private readonly Startup _realStartup;

    public TestStartup(IConfiguration configuration)
    {
        _realStartup = new Startup(configuration);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        _realStartup.ConfigureServices(services);
        services.AddControllers(options =>
        {
            options.Filters.Add<QueryStringValidatorFilter>();
        });
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseMiddleware<ProviderStateMiddleware>();
        _realStartup.Configure(app, env);
    }
}