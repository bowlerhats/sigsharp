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
        SignalEffectOptions options = null,
        CancellationToken stopToken = default)
        : base(group, default, options, true, stopToken)
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
            _state = null;
            _stateEffectFunctor = default;
        }
        
        base.Dispose(disposing);
    }
    
    protected override async ValueTask InvokeRunnerFunc()
    {
        if (_state is null || !_stateEffectFunctor.IsValid || !_state.TryGetTarget(out var state))
        {
            this.StopAutoRun();

            return;
        }

        await _stateEffectFunctor.Invoke(state);
    }
}