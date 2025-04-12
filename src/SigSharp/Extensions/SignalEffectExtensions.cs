using System;
using System.Threading;
using System.Threading.Tasks;

namespace SigSharp;

public static class SignalEffectExtensions
{
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        return Signals.Effect(func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<SignalEffectResult> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<ValueTask> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<ValueTask<SignalEffectResult>> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action<TAnchor> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, SignalEffectResult> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, SignalEffectResult> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, SignalEffectResult> func,
        SignalEffectOptions effectOptions = null,
        string name = null,
        CancellationToken stopToken = default
    )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken);
    }
}