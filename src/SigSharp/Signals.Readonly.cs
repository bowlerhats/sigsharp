using System;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp;

public static partial class Signals
{
    public static void Readonly(Action action)
    {
        var tracker = SignalTracker.Current;
        if (tracker?.IsReadonly ?? false)
        {
            action();
            return;
        }

        Tracked(action, AsReadonly);
    }
    
    public static T Readonly<T>(Func<T> func)
    {
        var tracker = SignalTracker.Current;
        if (tracker?.IsReadonly ?? false)
        {
            return func();
        }

        return Tracked(func, AsReadonly);
    }
    
    public static ValueTask Readonly(Func<ValueTask> action)
    {
        var tracker = SignalTracker.Current;
        if (tracker?.IsReadonly ?? false)
        {
            return action();
        }

        return Tracked(action, AsReadonly);
    }
    
    public static ValueTask<T> Readonly<T>(Func<ValueTask<T>> func)
    {
        var tracker = SignalTracker.Current;
        if (tracker?.IsReadonly ?? false)
        {
            return func();
        }

        return Tracked(func, AsReadonly);
    }

    private static SignalTracker AsReadonly(SignalTracker tracker)
    {
        return tracker.Readonly();
    }
}