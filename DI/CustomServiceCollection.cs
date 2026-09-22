namespace web_app_scratch.DI;

public class CustomServiceCollection
{
    private readonly List<ServiceDescriptor> _services = [];

    public void AddTransient<TServiceType, TImplementationType>()
    {
        _services.Add(new ServiceDescriptor(
            typeof(TServiceType),
            typeof(TImplementationType),
            ServiceLifetime.Transient
        ));
    }

    public void AddScoped<TServiceType, TImplementationType>()
    {
        _services.Add(new ServiceDescriptor(
            typeof(TServiceType),
            typeof(TImplementationType),
        ServiceLifetime.Scoped
        ));
    }

    public void AddSingleton<TServiceType, TImplementationType>()
    {
        _services.Add(new ServiceDescriptor(
            typeof(TServiceType),
            typeof(TImplementationType),
        ServiceLifetime.Singleton
        ));
    }

    public ServiceProvider BuildServiceProvider()
    {
        return new ServiceProvider(_services.AsReadOnly());
    }
}