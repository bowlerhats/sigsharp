using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SigSharp.Utils;

internal readonly record struct SignalEffectFunctor(
    Action AsAction = null,
    Func<ValueTask> AsValueTask = null
)
{
    public bool IsAction => this.AsAction is not null;
    public bool IsValueTask => this.AsValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask;

    [DebuggerStepThrough]
    public async ValueTask Invoke()
    {
        if (this.IsAction)
        {
            this.AsAction();
            return;
        }

        if (this.IsValueTask)
        {
            await this.AsValueTask();
        }
    }
    
    public static SignalEffectFunctor Of(Action action)
    {
        return new SignalEffectFunctor(action);
    }

    public static SignalEffectFunctor Of(Func<ValueTask> func)
    {
        return new SignalEffectFunctor(null, func);
    }
}

internal readonly record struct SignalEffectFunctor<TState>(
    Action<TState> AsAction = null,
    Func<TState, ValueTask> AsValueTask = null
)
{
    public bool IsAction => this.AsAction is not null;
    public bool IsValueTask => this.AsValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask;

    [DebuggerStepThrough]
    public async ValueTask Invoke(TState state)
    {
        if (this.IsAction)
        {
            this.AsAction(state);
            return;
        }

        if (this.IsValueTask)
        {
            await this.AsValueTask(state);
        }
    }
    
    public static SignalEffectFunctor<TState> Of(Action<TState> action)
    {
        return new SignalEffectFunctor<TState>(action);
    }

    public static SignalEffectFunctor<TState> Of(Func<TState, ValueTask> func)
    {
        return new SignalEffectFunctor<TState>(null, func);
    }
}