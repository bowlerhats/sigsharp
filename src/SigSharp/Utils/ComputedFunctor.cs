using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SigSharp.Utils;

internal readonly record struct ComputedFunctor<T>(
    Func<T>? AsAction = null,
    Func<ValueTask<T>>? AsValueTask = null
)
{
    public bool IsAction => this.AsAction is not null;
    public bool IsValueTask => this.AsValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask;

    [DebuggerStepThrough]
    public T InvokeSyncOnly()
    {
        return this.IsAction ? this.AsAction!() : default!;
    }
    
    [DebuggerStepThrough]
    public async ValueTask<T> InvokeAsync()
    {
        if (this.IsAction)
        {
            return this.AsAction!();
        }

        if (this.IsValueTask)
        {
            return await this.AsValueTask!();
        }

        return default!;
    }
    
    public static ComputedFunctor<T> Of(Func<T> action)
    {
        return new ComputedFunctor<T>(action);
    }

    public static ComputedFunctor<T> Of(Func<ValueTask<T>> func)
    {
        return new ComputedFunctor<T>(null, func);
    }
}

internal readonly record struct ComputedFunctor<T, TState>(
    Func<TState, T>? AsAction = null,
    Func<TState, ValueTask<T>>? AsValueTask = null
)
{
    public bool IsAction => this.AsAction is not null;
    public bool IsValueTask => this.AsValueTask is not null;

    public bool IsValid => this.IsAction || this.IsValueTask;

    [DebuggerStepThrough]
    public T InvokeSyncOnly(TState state)
    {
        return this.IsAction ? this.AsAction!(state) : default!;
    }
    
    [DebuggerStepThrough]
    public ValueTask<T> InvokeAsync(TState state)
    {
        if (this.IsAction)
        {
            var res = this.AsAction!(state);

            return ValueTask.FromResult(res);
        }

        if (this.IsValueTask)
        {
            return this.AsValueTask!(state);
        }

        return ValueTask.FromResult<T>(default!);
    }
    
    public static ComputedFunctor<T, TState> Of(Func<TState, T>? action)
    {
        return new ComputedFunctor<T, TState>(action);
    }

    public static ComputedFunctor<T, TState> Of(Func<TState, ValueTask<T>>? func)
    {
        return new ComputedFunctor<T, TState>(null, func);
    }
}