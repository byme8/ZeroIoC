﻿using System;

namespace ZeroIoC;

public interface IInstanceResolver : IDisposable
{
    object Resolve(IZeroIoCResolver resolver);
    object Resolve(IZeroIoCResolver resolver, IOverrides overrides);

    IInstanceResolver Duplicate();
}

public interface ICreator<T>
{
    T Create(IZeroIoCResolver resolver);
    T Create(IZeroIoCResolver resolver, IOverrides overrides);
}
    
public sealed class TransientResolver : IInstanceResolver
{
    private readonly Func<IZeroIoCResolver, object> _activator;

    public TransientResolver(Func<IZeroIoCResolver, object> activator)
    {
        _activator = activator;
    }

    public object Resolve(IZeroIoCResolver resolver)
    {
        return _activator(resolver);
    }

    public object Resolve(IZeroIoCResolver resolver, IOverrides overrides)
    {
        return _activator(resolver);
    }

    public IInstanceResolver Duplicate()
    {
        return new TransientResolver(_activator);
    }

    public void Dispose()
    {
    }
}

public sealed class TransientResolver<TCreator, TType> : IInstanceResolver
    where TCreator : struct, ICreator<TType>
{
    public object Resolve(IZeroIoCResolver resolver)
    {
        return default(TCreator).Create(resolver);
    }

    public object Resolve(IZeroIoCResolver resolver, IOverrides overrides)
    {
        return default(TCreator).Create(resolver, overrides);
    }

    public IInstanceResolver Duplicate()
    {
        return new TransientResolver<TCreator, TType>();
    }

    public void Dispose()
    {
    }
}


public sealed class SingletonResolver<TCreator, TType> : IInstanceResolver
    where TCreator : struct, ICreator<TType>
{
    private object @object = new object();
    private object _cache;
    private bool _disposed;
    private Func<IZeroIoCResolver, object> _resolve;
    private Func<IZeroIoCResolver, IOverrides, object> _resolveOverride;

    public SingletonResolver()
    {
        _cache = null;
        _disposed = false;

        _resolve = ResolveInternal;
        _resolveOverride = ResolveInternalOverride;
    }

    public object Resolve(IZeroIoCResolver resolver)
    {
        return _resolve(resolver);
    }

    public object Resolve(IZeroIoCResolver resolver, IOverrides overrides)
    {
        return _resolveOverride(resolver, overrides);
    }

    public IInstanceResolver Duplicate()
    {
        return new SingletonResolver<TCreator, TType>();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("Instance resolver was disposed. It may happen because the scope was disposed.");
        }

        if (_cache != null && _cache is IDisposable disposable)
        {
            _disposed = true;
            disposable.Dispose();
        }
    }

    private object ResolveInternal(IZeroIoCResolver resolver)
    {
        lock (@object)
        {
            if (_cache != null)
            {
                return _cache;
            }

            var creator = default(TCreator);
            _cache = creator.Create(resolver);
            _resolve = o => _cache;
            _resolveOverride = (o, oo) => _cache;;
            return _cache;
        }
    }
        
    private object ResolveInternalOverride(IZeroIoCResolver resolver, IOverrides overrides)
    {
        lock (@object)
        {
            if (_cache != null)
            {
                return _cache;
            }

            var creator = default(TCreator);
            _cache = creator.Create(resolver, overrides);
            _resolve = o => _cache;
            _resolveOverride = (o, oo) => _cache;;
            return _cache;
        }
    }
}

public sealed class SingletonResolver : IInstanceResolver
{
    private readonly Func<IZeroIoCResolver, object> _activator;
    private object _cache;
    private bool _disposed;
    private Func<IZeroIoCResolver, object> _resolve;

    public SingletonResolver(Func<IZeroIoCResolver, object> activator)
    {
        _activator = activator;
        _cache = null;
        _disposed = false;

        _resolve = ResolveInternal;
    }

    public object Resolve(IZeroIoCResolver resolver)
    {
        return _resolve(resolver);
    }

    public object Resolve(IZeroIoCResolver resolver, IOverrides overrides)
    {
        return _resolve(resolver);
    }

    public IInstanceResolver Duplicate()
    {
        return new SingletonResolver(_activator);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException("Instance resolver was disposed. It may happen because the scope was disposed.");
        }

        if (_cache != null && _cache is IDisposable disposable)
        {
            _disposed = true;
            disposable.Dispose();
        }
    }

    private object ResolveInternal(IZeroIoCResolver resolver)
    {
        lock (_activator)
        {
            if (_cache != null)
            {
                return _cache;
            }

            _cache = _activator(resolver);
            _resolve = GetCached;
            return _cache;
        }
    }

    private object GetCached(IZeroIoCResolver resolver)
    {
        return _cache;
    }
}