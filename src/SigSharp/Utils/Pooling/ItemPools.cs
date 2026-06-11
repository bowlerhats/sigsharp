using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.Utils.Pooling;

public class SignalItemPools
{
    public static SignalItemPools Instance { get; } = new();

    internal static ISignalItemPool<SignalTracker> TrackerPool => Instance._trackerPool;
    internal static ISignalItemPool<ConcurrentHashSet<SignalTracker>> WaiterPool => Instance._waiterPool;
    internal static ISignalItemPool<ConcurrentHashSet<SignalTracker>> LockedByPool => Instance._lockedByPool;
    internal static ISignalItemPool<ConditionalWeakTable<SignalNode, object>> ReferencedByPool => Instance._referencedByPool;
    internal static ISignalItemPool<GatedLatch<SignalTracker>> AccessLatchPool => Instance._accessLatchPool;

    private readonly ISignalItemPool<SignalTracker> _trackerPool
        = new ItemPool<SignalTracker>();

    private readonly ISignalItemPool<ConcurrentHashSet<SignalTracker>> _waiterPool
        = new ItemPool<ConcurrentHashSet<SignalTracker>>();
    
    private readonly ISignalItemPool<ConcurrentHashSet<SignalTracker>> _lockedByPool
        = new ItemPool<ConcurrentHashSet<SignalTracker>>();

    private readonly ISignalItemPool<ConditionalWeakTable<SignalNode, object>> _referencedByPool
        = new ItemPool<ConditionalWeakTable<SignalNode, object>>();

    private readonly ISignalItemPool<GatedLatch<SignalTracker>> _accessLatchPool
        = new ItemPool<GatedLatch<SignalTracker>>();
}