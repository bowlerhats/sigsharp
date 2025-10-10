using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using SigSharp.Utils;

namespace SigSharp;

public class SignalDefaultValueProvider
{
    public static SignalDefaultValueProvider DefaultInstance { get; } = new();

    private readonly ConcurrentDictionary<Type, object?> _valueCache = [];
    private readonly ConcurrentHashSet<Type> _nonConstructable = [];
    
    public bool TryGetCached<T>(out T? value, Type? type = null)
    {
        type ??= typeof(T);

        if (_valueCache.TryGetValue(type, out var cached))
        {
            value = cached is null ? default : (T)cached;

            return true;
        }

        value = default;
        return false;
    }

    public bool AddToCache<T>(Type type, T instance)
    {
        return _valueCache.TryAdd(type, instance);
    }

    public void ClearCache()
    {
        _valueCache.Clear();
    }

    public virtual T? GetDefaultValue<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]
        T>(T? fallback)
    {
        if (!TypeUtils.IsNullableByDefault<T>())
            return default;

        var type = typeof(T);

        if (this.TryGetCached<T>(out var cached, type))
            return cached;

        if (_nonConstructable.Contains(type))
            return fallback;

        var isCollection = type
            .GetInterfaces()
            .Any(d => d.IsGenericType && d.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (isCollection)
        {
            try
            {
                var instance = Activator.CreateInstance<T>();

                this.AddToCache(type, instance);

                return instance;
            }
            catch (Exception)
            {
                _nonConstructable.Add(type);
            }
        }

        return fallback;
    }
}