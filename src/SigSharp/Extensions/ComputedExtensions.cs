using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static class ComputedExtensions
{
    #region Factories
    
    private static ComputedSignal<T> CreateComputed<T, TAnchor>(
        TAnchor anchor,
        Func<T> func,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), cm!, lineNumber, opts)
            : group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), name, 0, opts);

        return signal;
    }
    
    private static ComputedSignal<T> CreateComputedWithState<T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, T> func,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), cm!, lineNumber, opts)
            : group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), name, 0, opts);
        
        return signal;
    }
    
    private static ComputedSignal<T> CreateWeakComputed<T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, T>? func,
        Func<TState, Signal<T>>? wrappedFunc,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
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
                opts
                )
            : group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                name,
                0,
                opts
                );
        
        return signal;
    }
    
    private static ComputedSignal<T> CreateWeakComputed<T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>>? func,
        Func<TState, ValueTask<Signal<T>>>? wrappedFunc,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
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
                opts
                )
            : group.GetOrCreateWeakComputed(
                state,
                ComputedFunctor<T, TState>.Of(func),
                ComputedFunctor<Signal<T>, TState>.Of(wrappedFunc),
                name,
                0,
                opts
                );
        
        return signal;
    }
    
    #endregion
    
    #region Async factories
    
    private static ComputedSignal<T> CreateComputed<T, TAnchor>(
        TAnchor anchor,
        Func<ValueTask<T>> func,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), cm!, lineNumber, opts)
            : group.GetOrCreateComputed(ComputedFunctor<T>.Of(func), name, 0, opts);

        return signal;
    }
    
    private static ComputedSignal<T> CreateComputedWithState<T, TAnchor, TState>(
        TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        string? name,
        string? cm,
        int lineNumber,
        ComputedSignalOptions? opts)
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        if (name is null)
            ArgumentException.ThrowIfNullOrWhiteSpace(cm);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        var signal = name is null
            ? group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), cm!, lineNumber, opts)
            : group.GetOrCreateComputed(state, ComputedFunctor<T, TState>.Of(func), name, 0, opts);
        
        return signal;
    }
    
    #endregion
    
    #region Regular compute
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<T> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputed(anchor, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, T> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(anchor, anchor, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T Computed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, T> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(anchor, state, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T WeakComputed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, T> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
        where TState: class
    {
        return CreateWeakComputed(anchor, state, func, null, name, cm, lineNumber, opts).Get();
    }
    
    #endregion

    #region Signal unwrap
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<Signal<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(anchor, func, static d => d().Get(), name, cm, lineNumber, opts).Get();
    }
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, Signal<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(
            anchor,
            (func, anchor),
            static d => d.func(d.anchor).Get(),
            name,
            cm,
            lineNumber,
            opts
            ).Get();
    }
    
    public static T Computed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, Signal<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class, IDisposable
    {
        return CreateComputedWithState(
            anchor,
            (func, state),
            static d => d.func(d.state).Get(),
            name,
            cm,
            lineNumber,
            opts
            ).Get();
    }
    
    public static T WeakComputed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, Signal<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class, IDisposable
        where TState: class
    {
        return CreateWeakComputed(anchor, state, null, func, name, cm, lineNumber, opts).Get();
    }
    
    #endregion
    
    #region Async unwrap
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<ValueTask<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputed(anchor, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T Computed<T, TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(anchor, anchor, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T Computed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
    {
        return CreateComputedWithState(anchor, state, func, name, cm, lineNumber, opts).Get();
    }
    
    public static T WeakComputed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<T>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
        where TState: class
    {
        return CreateWeakComputed<T, TAnchor, TState>(anchor, state, func, null, name, cm, lineNumber, opts).Get();
    }
    
    public static T WeakComputed<T, TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<Signal<T>>> func,
        ComputedSignalOptions? opts = null,
        string? name = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0)
        where TAnchor: class
        where TState: class
    {
        return CreateWeakComputed(anchor, state, null, func, name, cm, lineNumber, opts).Get();
    }
    
    #endregion
}