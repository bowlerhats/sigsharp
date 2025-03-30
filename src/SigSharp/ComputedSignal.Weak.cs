using System;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public class WeakComputedSignal<T, TState> : ComputedSignal<T>
    where TState: class
{
    private WeakReference<TState> _state;
    private ComputedFunctor<T, TState> _calcFunctor;
    private ComputedFunctor<Signal<T>, TState> _wrappingFunctor;
    
    internal WeakComputedSignal(
        SignalGroup group,
        TState state,
        ComputedFunctor<T, TState> calcFunctor,
        ComputedFunctor<Signal<T>, TState> wrappingFunctor,
        ComputedSignalOptions opts = null
    ) : base(group, default, opts)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        _state = new WeakReference<TState>(state);
        
        _calcFunctor = calcFunctor;
        _wrappingFunctor = wrappingFunctor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state = null;

            _calcFunctor = default;
            _wrappingFunctor = default;
        }
        
        base.Dispose(disposing);
    }

    protected override async ValueTask<T> CalcValue()
    {
        if (_state is null || !_state.TryGetTarget(out var state))
            return default;

        if (_wrappingFunctor.IsValid)
            return (await _wrappingFunctor.Invoke(state)).Get();

        return await _calcFunctor.Invoke(state);
    }
}