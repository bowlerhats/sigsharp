using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ExplicitCallerInfoArgument

namespace SigSharp;

public static class SignalEffectExtensions
{
    private static string ComposeName<TAnchor>(string? cm, int lineNumber, string? cexp, SignalGroup group, TAnchor anchor)
    {
        return $"{Signals.ComposeName(cm, lineNumber, cexp, group)} | for '{anchor?.GetType().Name ?? typeof(TAnchor).Name}'";
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);

        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);

        return Signals.Effect(func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<SignalEffectResult> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<ValueTask> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<ValueTask<SignalEffectResult>> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Action<TAnchor> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor>(
        this TAnchor anchor,
        Func<TAnchor, SignalEffectResult> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(anchor, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect Effect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, SignalEffectResult> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.Effect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Action<TState> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
        )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
    
    public static SignalEffect WeakEffect<TAnchor, TState>(
        this TAnchor anchor,
        TState state,
        Func<TState, SignalEffectResult> func,
        SignalEffectOptions? effectOptions = null,
        string? name = null,
        CancellationToken stopToken = default,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(func))]string? cexp = null
    )
        where TAnchor: class
        where TState: class
    {
        ArgumentNullException.ThrowIfNull(anchor);
        ArgumentNullException.ThrowIfNull(func);
        
        var group = SignalGroup.Of(anchor, SignalGroupOptions.Defaults);
        
        name ??= ComposeName(cm, lineNumber, cexp, group, anchor);
        
        return Signals.WeakEffect(state, func, group, effectOptions, name, stopToken, cm, lineNumber, cexp);
    }
}