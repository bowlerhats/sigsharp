using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public static partial class Signals
{
    internal static string ComposeName(string? cm, int lineNumber, string? cexp, SignalGroup group)
    {
        cexp ??= "???";
        if (cexp.Length > 30)
        {
            cexp = String.Concat(cexp.AsSpan(0, 30), "...");
        }
        
        return $"{group.Name} -> {cm ?? "???"}:{lineNumber}:{{{cexp}}}";
    }
    
    public static SignalEffect Effect(
        Action effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null
        )
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect(
        Func<SignalEffectResult> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect(
        Func<ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect(
        Func<ValueTask<SignalEffectResult>> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect(
            group,
            SignalEffectFunctor.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }

    public static SignalEffect Effect<TState>(
        TState state,
        Action<TState> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect<TState>(
        TState state,
        Func<TState, SignalEffectResult> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect<TState>(
        TState state,
        Func<TState, ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect Effect<TState>(
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new SignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Action<TState> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Func<TState, SignalEffectResult> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Func<TState, ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
    
    public static SignalEffect WeakEffect<TState>(
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(effectFunction);
        ArgumentNullException.ThrowIfNull(group);
        
        name ??= ComposeName(cm, lineNumber, cexp, group);
        
        return new WeakSignalEffect<TState>(
            group,
            state,
            SignalEffectFunctor<TState>.Of(effectFunction),
            name,
            opts,
            stopToken: stopToken);
    }
}