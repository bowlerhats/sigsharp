using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static class ComputedExtensions
{
    internal static readonly AsyncLocal<bool> CapturePluck = new();
    
    #region Factories
    
    private static ComputedSignal<T>? CreateComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        TAnchor anchor,
        Func<T> func,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), cm!, lineNumber, optsBuilder)
            : group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), name, 0, optsBuilder);

        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }

        return signal;
    }
    
    private static ComputedSignal<T>? CreateComputedWithState<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, T> func,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), cm!, lineNumber, optsBuilder)
            : group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), name, 0, optsBuilder);
        
        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }
        
        return signal;
    }
    
    private static ComputedSignal<T>? CreateWeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, T>? func,
        Func<TState, Signal<T>>? wrappedFunc,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                cm!,
                lineNumber,
                optsBuilder
                )
            : group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                name,
                0,
                optsBuilder
                );
        
        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }
        
        return signal;
    }
    
    private static ComputedSignal<T>? CreateWeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>>? func,
        Func<TState, ValueTask<Signal<T>>>? wrappedFunc,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                cm!,
                lineNumber,
                optsBuilder
                )
            : group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                name,
                0,
                optsBuilder
                );
        
        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }
        
        return signal;
    }
    
    #endregion
    
    #region Async factories
    
    private static ComputedSignal<T>? CreateComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        TAnchor anchor,
        Func<ValueTask<T>> func,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), cm!, lineNumber, optsBuilder)
            : group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), name, 0, optsBuilder);
        
        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }

        return signal;
    }
    
    private static ComputedSignal<T>? CreateComputedWithState<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        string? name,
        string? cm,
        int lineNumber,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), cm!, lineNumber, optsBuilder)
            : group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), name, 0, optsBuilder);
        
        if (signal is not null && CapturePluck.Value)
        {
            throw new SignalPluckException<T>(signal);
        }
        
        return signal;
    }
    
    #endregion
    
    #region Regular compute
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        this TAnchor anchor,
        Func<T> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputed(anchor, func, name, cm, lineNumber, optsBuilder);

        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;
        
        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }

    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]
        T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, T> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0
        )
        where TAnchor : class
    {
        var computed = CreateComputedWithState(anchor, anchor, func, name, cm, lineNumber, optsBuilder);

        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }

    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]
        T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, T> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0
        )
        where TAnchor : class
    {
        var computed = CreateComputedWithState(anchor, state, func, name, cm, lineNumber, optsBuilder);

        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }

    public static T WeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]
        T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, T> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0
        )
        where TAnchor : class
        where TState : class
    {
        var computed = CreateWeakComputed(anchor, state, func, null, name, cm, lineNumber, optsBuilder);

        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }

    #endregion

    #region Signal unwrap
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        this TAnchor anchor,
        Func<Signal<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputedWithState(anchor, func, static d => d().Get(), name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, Signal<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputedWithState(
            anchor,
            (func, anchor),
            static d => d.func(d.anchor).Get(),
            name,
            cm,
            lineNumber,
            optsBuilder
            );
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, Signal<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class, IDisposable
    {
        var computed = CreateComputedWithState(
            anchor,
            (func, state),
            static d => d.func(d.state).Get(),
            name,
            cm,
            lineNumber,
            optsBuilder
            );
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T WeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, Signal<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class, IDisposable
        where TState: class
    {
        var computed = CreateWeakComputed(anchor, state, null, func, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    #endregion
    
    #region Async unwrap
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        this TAnchor anchor,
        Func<ValueTask<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputed(anchor, func, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputedWithState(anchor, anchor, func, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T Computed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        var computed = CreateComputedWithState(anchor, state, func, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T WeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
        where TState: class
    {
        var computed = CreateWeakComputed<T, TAnchor, TState>(anchor, state, func, null, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    public static T WeakComputed<[DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.Interfaces
            )]T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<Signal<T>>> func,
        Func<ComputedSignalOptions, ComputedSignalOptions>? optsBuilder = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
        where TState: class
    {
        var computed = CreateWeakComputed(anchor, state, null, func, name, cm, lineNumber, optsBuilder);
        
        if (computed is not null)
            return computed.Get();

        var opts = optsBuilder?.Invoke(ComputedSignalOptions.Defaults) ?? ComputedSignalOptions.Defaults;

        return opts.DefaultValueProvider.GetDefaultValue<T>(default)!;
    }
    
    #endregion
}