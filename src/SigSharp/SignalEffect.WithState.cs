using System;
using System.Threading;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public sealed class SignalEffect<TState> : SignalEffect
{
    private TState? _state;

    private SignalEffectFunctor<TState> _stateEffectFunctor;
    
    internal SignalEffect(
        SignalGroup group,
        TState state,
        SignalEffectFunctor<TState> effectFunctor,
        string? name = null,
        SignalEffectOptions? options = null,
        CancellationToken stopToken = default)
        : base(group, default, name, options, true, stopToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        _state = state;

        _stateEffectFunctor = effectFunctor;

        this.AutoStart();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state = default;
            _stateEffectFunctor = default;
        }
        
        base.Dispose(disposing);
    }

    protected override async ValueTask<SignalEffectResult> InvokeRunnerFunc()
    {
        if (_state is null || !_stateEffectFunctor.IsValid)
        {
            this.StopAutoRun();

            return SignalEffectResult.Stop();
        }

        return await _stateEffectFunctor.Invoke(_state);
    }
}