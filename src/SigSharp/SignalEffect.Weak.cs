using System;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public sealed class WeakSignalEffect<TState> : SignalEffect
    where TState: class
{
    private WeakReference<TState> _state;
    private SignalEffectFunctor<TState> _stateEffectFunctor;
    
    internal WeakSignalEffect(
        SignalGroup group,
        TState state,
        SignalEffectFunctor<TState> effectFunctor,
        string? name = null,
        SignalEffectOptions? options = null,
        CancellationToken stopToken = default)
        : base(group, default, name, options, true, stopToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        _state = new WeakReference<TState>(state);

        _stateEffectFunctor = effectFunctor;
        
        this.AutoStart();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stateEffectFunctor = default;
        }
        
        base.Dispose(disposing);
    }
    
    protected override async ValueTask<SignalEffectResult> InvokeRunnerFunc()
    {
        if (!_stateEffectFunctor.IsValid || !_state.TryGetTarget(out var state))
        {
            this.StopAutoRun();

            return SignalEffectResult.Stop();
        }

        var effectResult = await _stateEffectFunctor.Invoke(state);
        
        return await this.HandleEffectResult(effectResult);
    }
}