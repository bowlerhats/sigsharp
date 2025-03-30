using System;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static class SignalEffectExtensions
{
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<ValueTask> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect(group, SignalEffectFunctor.Of(func), effectOptions, stopToken: stopToken);
        
        return effect;
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect(group, SignalEffectFunctor.Of(func), effectOptions, stopToken: stopToken);
        
        return effect;
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect<TAnchor>(
            group,
            anchor,
            SignalEffectFunctor<TAnchor>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action<TAnchor> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect<TAnchor>(
            group,
            anchor,
            SignalEffectFunctor<TAnchor>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions effectOptions = null,
        CancellationToken stopToken = default
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        var effect = new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(func),
            effectOptions,
            stopToken: stopToken
            );
        
        return effect;
    }
}