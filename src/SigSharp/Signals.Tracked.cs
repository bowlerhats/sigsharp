using System;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp;

public static partial class Signals
{
    public static void Tracked(Action action)
    {
        Tracked(action, Unity);
    }
    
    public static T Tracked<T>(Func<T> func)
    {
        return Tracked(func, Unity);
    }
    
    public static ValueTask Tracked(Func<ValueTask> action)
    {
        return Tracked(action, Unity);
    }
    
    public static ValueTask<T> Tracked<T>(Func<ValueTask<T>> func)
    {
        return Tracked(func, Unity);
    }
    
    internal static void Tracked(Action action, Func<SignalTracker, SignalTracker> setup)
    {
        var tracker = setup(SignalTracker.Push(false));
        try
        {
            action();
        }
        finally
        {
            SignalTracker.Pop(tracker);
        }
    }
    
    internal static T Tracked<T>(Func<T> func, Func<SignalTracker, SignalTracker> setup)
    {
        var tracker = setup(SignalTracker.Push(false));
        try
        {
            return func();
        }
        finally
        {
            SignalTracker.Pop(tracker);
        }
    }
    
    internal static async ValueTask Tracked(Func<ValueTask> action, Func<SignalTracker, SignalTracker> setup)
    {
        var tracker = setup(SignalTracker.Push(false));
        try
        {
            await action();
        }
        finally
        {
            SignalTracker.Pop(tracker);
        }
    }
    
    internal static async ValueTask<T> Tracked<T>(Func<ValueTask<T>> func, Func<SignalTracker, SignalTracker> setup)
    {
        var tracker = setup(SignalTracker.Push(false));
        try
        {
            return await func();
        }
        finally
        {
            SignalTracker.Pop(tracker);
        }
    }

    private static SignalTracker Unity(SignalTracker tracker)
    {
        return tracker;
    }
}