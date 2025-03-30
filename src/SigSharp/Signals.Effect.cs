using System;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static partial class Signals
{
    public static SignalEffect Effect(
        Action effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect(
        Func<ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }

    public static SignalEffect Effect<TState>(
        TState state,
        Action<TState> effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect<TState>(
        TState state,
        Func<TState, ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Action<TState> effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Func<TState, ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions opts = null,
        CancellationToken stopToken = default)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            opts,
            stopToken: stopToken);
    }
}