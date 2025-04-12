using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SigSharp.Utils;

internal readonly record struct SignalEffectFunctor(
    Action AsAction = null,
    Func<SignalEffectResult> AsReturnAction = null,
    Func<ValueTask> AsValueTask = null,
    Func<ValueTask<SignalEffectResult>> AsReturnValueTask = null 
)
{
    private bool IsAction => this.AsAction is not null;
    private bool IsReturnAction => this.AsReturnAction is not null;
    private bool IsValueTask => this.AsValueTask is not null;
    private bool IsReturnValueTask => this.AsReturnValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask || this.IsReturnAction || this.IsReturnValueTask;

    [DebuggerStepThrough]
    public async ValueTask<SignalEffectResult> Invoke()
    {
        if (this.IsAction)
        {
            this.AsAction();
        }
        else if (this.IsReturnAction)
        {
            return this.AsReturnAction();
        }
        else if (this.IsValueTask)
        {
            await this.AsValueTask();
        }
        else if (this.IsReturnValueTask)
        {
            return await this.AsReturnValueTask();
        }
        else
        {
            return SignalEffectResult.Stop();
        }
        
        return SignalEffectResult.Ok();
    }
    
    public static SignalEffectFunctor Of(Action action)
    {
        return new SignalEffectFunctor(action);
    }
    
    public static SignalEffectFunctor Of(Func<SignalEffectResult> action)
    {
        return new SignalEffectFunctor(AsReturnAction: action);
    }

    public static SignalEffectFunctor Of(Func<ValueTask> func)
    {
        return new SignalEffectFunctor(AsValueTask: func);
    }
    
    public static SignalEffectFunctor Of(Func<ValueTask<SignalEffectResult>> func)
    {
        return new SignalEffectFunctor(AsReturnValueTask: func);
    }
}

internal readonly record struct SignalEffectFunctor<TState>(
    Action<TState> AsAction = null,
    Func<TState, SignalEffectResult> AsReturnAction = null,
    Func<TState, ValueTask> AsValueTask = null,
    Func<TState, ValueTask<SignalEffectResult>> AsReturnValueTask = null
)
{
    public bool IsAction => this.AsAction is not null;
    public bool IsReturnAction => this.AsReturnAction is not null;
    public bool IsValueTask => this.AsValueTask is not null;
    public bool IsReturnValueTask => this.AsReturnValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask || this.IsReturnAction || this.IsReturnValueTask;

    [DebuggerStepThrough]
    public async ValueTask<SignalEffectResult> Invoke(TState state)
    {
        if (this.IsAction)
        {
            this.AsAction(state);
        }
        else if (this.IsReturnAction)
        {
            return this.AsReturnAction(state);
        }
        else if (this.IsValueTask)
        {
            await this.AsValueTask(state);
        }
        else if (this.IsReturnValueTask)
        {
            return await this.AsReturnValueTask(state);
        }
        else
        {
            return SignalEffectResult.Stop();
        }
        
        return SignalEffectResult.Ok();
    }
    
    public static SignalEffectFunctor<TState> Of(Action<TState> action)
    {
        return new SignalEffectFunctor<TState>(action);
    }
    
    public static SignalEffectFunctor<TState> Of(Func<TState, SignalEffectResult> action)
    {
        return new SignalEffectFunctor<TState>(AsReturnAction: action);
    }

    public static SignalEffectFunctor<TState> Of(Func<TState, ValueTask> func)
    {
        return new SignalEffectFunctor<TState>(AsValueTask: func);
    }
    
    public static SignalEffectFunctor<TState> Of(Func<TState, ValueTask<SignalEffectResult>> func)
    {
        return new SignalEffectFunctor<TState>(AsReturnValueTask: func);
    }
}