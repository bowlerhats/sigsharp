using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ExplicitCallerInfoArgument

namespace SigSharp;

public static partial class Signals
{
    public static async ValueTask RunDetachedToCompletion(Func<ValueTask> action, TimeSpan? rerunDelay = null)
    {
        rerunDelay ??= TimeSpan.FromMilliseconds(300);
        
        while (true)
        {
            try
            {
                await Detached(action);

                break;
            }
            catch (SignalException)
            {
                // ignore
            }
            
            await Task.Delay(rerunDelay.Value);
        }
    }

    public static SignalEffect RunOnce<TState>(
        TState state,
        Func<TState, ValueTask<SignalEffectResult>> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        TimeSpan? retryDelay = null,
        Func<Exception, ValueTask<SignalEffectResult?>>? errorHandler = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        return Effect(
            (state, effectFunction, retryDelay, errorHandler, stopToken),
            static async ValueTask<SignalEffectResult>(s) =>
                {
                    var defResult = SignalEffectResult.Reschedule();
                    try
                    {
                        defResult = await s.effectFunction(s.state);

                        if (defResult is { ShouldReschedule: false, ShouldDestroy: false })
                            return SignalEffectResult.Destroy();
                    }
                    catch (Exception exc)
                    {
                        if (s.errorHandler is not null)
                        {
                            try
                            {
                                SignalEffectResult? handlerResult = await s.errorHandler(exc);
                                if (handlerResult.HasValue && (handlerResult.Value.ShouldReschedule || handlerResult.Value.ShouldDestroy))
                                {
                                    defResult = handlerResult.Value;
                                }
                            }
                            catch (Exception)
                            {
                                return SignalEffectResult.Destroy();
                            }
                        }
                    }
                    
                    if (defResult.ShouldReschedule && s.retryDelay.HasValue)
                    {
                        await Task.Delay(s.retryDelay.Value, s.stopToken);
                    }
                    
                    return defResult;
                },
            group, opts, name, stopToken, cm, lineNumber, cexp
            );
    }
    
    public static SignalEffect RunOnce<TState>(
        TState state,
        Func<TState, ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        TimeSpan? retryDelay = null,
        Func<Exception, ValueTask<SignalEffectResult?>>? errorHandler = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        return RunOnce(
            (state, effectFunction),
            static async s =>
                {
                    await s.effectFunction(s.state);

                    return SignalEffectResult.Destroy();
                },
            group,
            opts, name, stopToken,
            retryDelay, errorHandler,
            cm, lineNumber, cexp
            );
    }
    
    public static SignalEffect RunOnce(
        Func<ValueTask> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        TimeSpan? retryDelay = null,
        Func<Exception, ValueTask<SignalEffectResult?>>? errorHandler = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        return RunOnce(
            effectFunction,
            static async fn =>
                {
                    await fn();

                    return SignalEffectResult.Destroy();
                },
            group,
            opts, name, stopToken,
            retryDelay, errorHandler,
            cm, lineNumber, cexp
            );
    }
    
    public static SignalEffect RunOnce(
        Func<ValueTask<SignalEffectResult>> effectFunction,
        SignalGroup group,
        SignalEffectOptions? opts = null,
        string? name = null,
        CancellationToken stopToken = default,
        TimeSpan? retryDelay = null,
        Func<Exception, ValueTask<SignalEffectResult?>>? errorHandler = null,
        [CallerMemberName] string? cm = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerArgumentExpression(nameof(effectFunction))]string? cexp = null)
    {
        return RunOnce(
            effectFunction,
            static fn => fn(),
            group,
            opts, name, stopToken,
            retryDelay, errorHandler,
            cm, lineNumber, cexp
            );
    }
}