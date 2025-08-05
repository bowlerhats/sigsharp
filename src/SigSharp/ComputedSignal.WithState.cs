using System;
using System.Threading.Tasks;
using SigSharp.Utils;

namespace SigSharp;

public class ComputedSignal<T, TState> : ComputedSignal<T>
{
    private TState? _state;
    private ComputedFunctor<T, TState> _stateFunctor;
    
    internal ComputedSignal(
        SignalGroup group,
        TState state,
        ComputedFunctor<T, TState> functor,
        ComputedSignalOptions? opts = null,
        string? name = null
    ) : base(group, default, opts, name)
    {
        ArgumentNullException.ThrowIfNull(state);
        
        _stateFunctor = functor;
        
        _state = state;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state = default;
            _stateFunctor = default;
        }
        
        base.Dispose(disposing);
    }

    protected override T CalcValueSyncOnly()
    {
        if (_state is null || !_stateFunctor.IsValid)
            return default!;

        return _stateFunctor.InvokeSyncOnly(_state);
    }

    protected override async ValueTask<T> CalcValueAsync()
    {
        if (_state is null || !_stateFunctor.IsValid)
            return default!;

        return await _stateFunctor.InvokeAsync(_state);
    }
    
    public static implicit operator T(ComputedSignal<T, TState> sig)
    {
        ArgumentNullException.ThrowIfNull(sig);
        return sig.Get();
    }
}