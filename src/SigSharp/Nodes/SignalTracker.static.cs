using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SigSharp.Utils;
using SigSharp.Utils.Perf;

namespace SigSharp.Nodes;

internal partial class SignalTracker
{
    public static SignalTracker? Current => CurrentTracker.Value;

    public static bool IsReadonlyContext => Current?.IsReadonly ?? false;

    internal static ItemPool<SignalTracker> TrackerPool { get; } = new();
    
    private static readonly AsyncLocal<SignalTracker?> CurrentTracker = new();

    internal static SignalTracker Push(bool expectEmpty, SignalNode? contextNode = null)
    {
        var parent = CurrentTracker.Value;

        if (expectEmpty && parent is not null)
        {
            throw new ArgumentException("Expected empty signal tracker", nameof(expectEmpty));
        }

        var tracker = TrackerPool.Rent().Init(contextNode, parent);

        CurrentTracker.Value = tracker;
        
        Perf.Increment("signal.tracker.active_count");

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
        
        Perf.Decrement("signal.tracker.active_count");

        if (tracker != expected)
            throw new ArgumentOutOfRangeException(nameof(expected), "Popped tracker is not the expected one");
    }

    internal static SignalTracker? ReplaceWith(SignalTracker? newTracker)
    {
        var current = CurrentTracker.Value;
        CurrentTracker.Value = newTracker;

        return current;
    }

    internal static HashSet<SignalTracker> FindAllLockingOrWaitingTrackers()
    {
        var res = new HashSet<SignalTracker>(32);
        
        SignalGroup[] groups = SignalGroup.GetAllGroups().ToArray();
        foreach (var group in groups)
        {
            if (group.IsDisposing)
                continue;
            
            group.MemberStore.WithEach(
                res,
                static (r, node) =>
                    {
                        if (node.IsDisposing || !node.IsTrackable)
                            return;

                        foreach (var lockTracker in node.LockedBy)
                        {
                            foreach (var tracker in ObjectWalker.Walk(lockTracker, static d => d._parent))
                            {
                                r.Add(tracker);
                            }
                        }
                        
                        foreach (var lockTracker in node.Waiters)
                        {
                            foreach (var tracker in ObjectWalker.Walk(lockTracker, static d => d._parent))
                            {
                                r.Add(tracker);
                            }
                        }
                    }
                );
        }

        return res;
    }
}