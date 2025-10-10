using System;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp;

public static partial class Signals
{
    public static void Untracked(Action action)
    {
        var tracker = SignalTracker.Current is null
            ? null
            : SignalTracker.Push(false).BreaksTracking();
        try
        {
            action();
        }
        finally
        {
            if (tracker is not null)
                SignalTracker.Pop(tracker);
        }
    }
    
    public static T Untracked<T>(Func<T> func)
    {
        var tracker = SignalTracker.Current is null
            ? null
            : SignalTracker.Push(false).BreaksTracking();
        try
        {
            return func();
        }
        finally
        {
            if (tracker is not null)
                SignalTracker.Pop(tracker);
        }
    }
    
    public static async ValueTask Untracked(Func<ValueTask> action)
    {
        var tracker = SignalTracker.Current is null
            ? null
            : SignalTracker.Push(false).BreaksTracking();
        try
        {
            await action();
        }
        finally
        {
            if (tracker is not null)
                SignalTracker.Pop(tracker);
        }
    }
    
    public static async ValueTask<T> Untracked<T>(Func<ValueTask<T>> func)
    {
        var tracker = SignalTracker.Current is null
            ? null
            : SignalTracker.Push(false).BreaksTracking();
        try
        {
            return await func();
        }
        finally
        {
            if (tracker is not null)
                SignalTracker.Pop(tracker);
        }
    }
    
    public static void Detached(Action action)
    {
        var tracker = SignalTracker.ReplaceWith(null);
        try
        {
            action();
        }
        finally
        {
            if (tracker is not null)
            {
                SignalTracker.ReplaceWith(tracker);
            }
        }
    }
    
    public static T Detached<T>(Func<T> func)
    {
        var tracker = SignalTracker.ReplaceWith(null);
        try
        {
            return func();
        }
        finally
        {
            if (tracker is not null)
            {
                SignalTracker.ReplaceWith(tracker);
            }
        }
    }
    
    public static async ValueTask Detached(Func<ValueTask> action)
    {
        var tracker = SignalTracker.ReplaceWith(null);
        try
        {
            await action();
        }
        finally
        {
            if (tracker is not null)
            {
                SignalTracker.ReplaceWith(tracker);
            }
        }
    }
    
    public static async ValueTask<T> Detached<T>(Func<ValueTask<T>> func)
    {
        var tracker = SignalTracker.ReplaceWith(null);
        try
        {
            return await func();
        }
        finally
        {
            if (tracker is not null)
            {
                SignalTracker.ReplaceWith(tracker);
            }
        }
    }
}