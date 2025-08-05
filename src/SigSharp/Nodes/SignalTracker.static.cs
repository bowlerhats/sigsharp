using System;
using System.Threading;
using SigSharp.Utils;

namespace SigSharp.Nodes;

internal partial class SignalTracker
{
    public static SignalTracker? Current => CurrentTracker.Value;

    public static bool IsReadonlyContext => Current?.IsReadonly ?? false;

    private static ItemPool<SignalTracker> TrackerPool { get; } = new();
    
    private static readonly AsyncLocal<SignalTracker?> CurrentTracker = new();

    internal static SignalTracker Push(bool expectEmpty)
    {
        var parent = CurrentTracker.Value;

        if (expectEmpty && parent is not null)
        {
            throw new ArgumentException("Expected empty signal tracker", nameof(expectEmpty));
        }

        var tracker = TrackerPool.Rent().Init(parent);

        CurrentTracker.Value = tracker;

        return tracker;
    }

    internal static void Pop(SignalTracker expected)
    {
        var tracker = Current;
        if (tracker is not null)
        {
            CurrentTracker.Value = tracker._parent;

            tracker = tracker.Reset().DisableTracking();

            TrackerPool.Return(tracker);
        }

        if (tracker != expected)
            throw new ArgumentOutOfRangeException(nameof(expected), "Popped tracker is not the expected one");
    }

    internal static SignalTracker? ReplaceWith(SignalTracker? newTracker)
    {
        var current = CurrentTracker.Value;
        CurrentTracker.Value = newTracker;

        return current;
    }
}