using System.Dynamic;

namespace web_app_scratch.DI;

public class ServiceProvider
{
    private readonly IReadOnlyList<ServiceDescriptor> _serviceDescriptors;
    private readonly Dictionary<Type, object>? _scopedCache;
    private readonly Dictionary<Type, object>? _singletonCache;

    // root provider
    public ServiceProvider(IReadOnlyList<ServiceDescriptor> serviceDescriptors)
    {
        _serviceDescriptors = serviceDescriptors;
        _scopedCache = null;
        _singletonCache = new();
    }

    // scope provider
    public ServiceProvider(
      IReadOnlyList<ServiceDescriptor> serviceDescriptors,
      bool isScope,
      Dictionary<Type, object> singletonCache
    )
    {
        _serviceDescriptors = serviceDescriptors;
        _scopedCache = isScope ? [] : null;
        _singletonCache = singletonCache;
    }
    public T GetRequiredService<T>() => (T)GetService(typeof(T));

    public object GetService(Type serviceType)
    {
        var descriptor = _serviceDescriptors.FirstOrDefault(x => x.ServiceType == serviceType)
        ?? throw new Exception($"Service of Type {serviceType.Name} is not registered");

        return descriptor.Lifetime switch
        {
            ServiceLifetime.Transient => CreateInstance(descriptor.ImplementationType),
            ServiceLifetime.Singleton => CreateInstance(descriptor.ImplementationType),
            ServiceLifetime.Scoped => CreateInstance(descriptor.ImplementationType),
            _ => throw new NotImplementedException()
        };
    }

    public object CreateInstance(Type implementationType)
    {
        var firstConstructor = implementationType.GetConstructors().FirstOrDefault()
        ?? throw new Exception($"No public constructor fount for type{implementationType.Name}");

        var deps = firstConstructor.GetParameters().
        Select(p => GetService(p.ParameterType)).ToArray();

        return Activator.CreateInstance(implementationType, deps);
    }


    private object CreateSingletonInstance(ServiceDescriptor descriptor)
    {
        if (_singletonCache.TryGetValue(descriptor.ServiceType, out var instance))
            return instance;

        instance = CreateInstance(descriptor.ImplementationType);
        _singletonCache[descriptor.ServiceType] = instance;
        return instance;
    }

    public object CreateScopedInstance(ServiceDescriptor descriptor)
    {
        if (_scopedCache == null)
        {
            throw new InvalidOperationException("Cannot resolve scoped service from root provider");
        }
        if (_scopedCache.TryGetValue(descriptor.ServiceType, out var instance))
            return instance;
        instance = CreateInstance(descriptor.ImplementationType);
        _scopedCache[descriptor.ServiceType] = instance;
        return instance;
    }

    public ServiceScope CreateScope()
    {
        var scopeProvider = new ServiceProvider(_serviceDescriptors, true, _singletonCache);
        return new ServiceScope(scopeProvider);
    }

    public void DisposeScopedInstance()
    {
        if (_scopedCache == null) return;

        foreach (var instance in _scopedCache.Values)
        {
            if (instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _scopedCache.Clear();
        }
    }
}


// GetRequiredService<T>()
//           ↓
//      GetService()
//           ↓
//    Find Descriptor
//           ↓
//     Check Lifetime
//       /     |      \
//      /      |       \
// Transient Singleton Scoped
//    ↓          ↓        ↓
// New Object   Cache    Scope Cache
//              ↓          ↓
//           Same App   Same Scope