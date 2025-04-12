using System;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static partial class Signals
{
    public static ComputedSignal<T> Computed<T, TState>(
        TState state,
        Func<TState, T> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(group);
        
        return new ComputedSignal<T, TState>(group, state, ComputedFunctor<T, TState>.Of(func), opts, name);
    }
    
    public static ComputedSignal<T> Computed<T, TState>(
        TState state,
        Func<TState, ValueTask<T>> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(group);
        
        return new ComputedSignal<T, TState>(group, state, ComputedFunctor<T, TState>.Of(func), opts, name);
    }
    
    public static ComputedSignal<T> Computed<T>(
        Func<T> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(group);

        return new ComputedSignal<T>(group, ComputedFunctor<T>.Of(func), opts, name);
    }
    
    public static ComputedSignal<T> Computed<T>(
        Func<ValueTask<T>> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(group);

        return new ComputedSignal<T>(group, ComputedFunctor<T>.Of(func), opts, name);
    }
    
    public static ComputedSignal<T> Computed<T>(
        Func<Signal<T>> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(group);
        
        return Computed(func, static f => f().Get(), group, opts, name);
    }
    
    public static ComputedSignal<T> Computed<T>(
        Func<ValueTask<Signal<T>>> func,
        SignalGroup group,
        ComputedSignalOptions opts = null,
        string name = null
        )
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(func);
        
        return Computed(func, static async f => (await f()).Get(), group, opts, name);
    }
}