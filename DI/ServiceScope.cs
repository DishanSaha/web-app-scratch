namespace web_app_scratch.DI;

public interface IServiceScope : IDisposable
{
    ServiceProvider ServiceProvider { get; }
}

public class ServiceScope : IServiceScope
{
    private readonly ServiceProvider _serviceProvider;
    public ServiceProvider ServiceProvider => _serviceProvider;

    public ServiceScope(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public void Dispose()
    {
        _serviceProvider.DisposeScopedInstance();
    }
}
