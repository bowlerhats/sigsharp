using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SigSharp.Utils;
using SigSharp.Utils.Perf;

namespace SigSharp.Nodes;

internal sealed partial class SignalTracker
{
    internal ConcurrentHashSet<SignalNode> Tracked { get; } = [];
    internal ConcurrentHashSet<SignalNode> Changed { get; } = [];
    internal ConcurrentHashSet<SignalEffect> Effects { get; } = [];
    internal ConcurrentHashSet<SignalNode> Locked { get; } = [];
    internal ConcurrentHashSet<SignalNode> Waited { get; } = [];
    internal ConcurrentHashSet<SignalNode> Latched { get; } = [];
    internal ConcurrentHashSet<SignalTracker> Children { get; } = [];
    
    internal Dictionary<SignalNode, uint> Versions { get; } = [];
    private SpinLock _versionsLock = new(false); 
    
    internal bool IsReadonly => _isReadonly || (_parent?.IsReadonly ?? false);

    internal bool AcceptEffects => _collectEffects || (_parent?.AcceptEffects ?? false);

    internal bool IsTracking => _isTracking;

    internal SignalTracker? Parent => _parent;

    internal SignalTracker Root => _parent?.Root ?? this;
    
    internal bool IsRoot => this.Root == this;
    
    private SignalEffect? RootEffect => this.Root._contextNode as SignalEffect;
    
    private static long _trackerIdCount = 1;
    private long _trackerId = 1;
    
    private SignalTracker? _parent;
    private bool _isReadonly;

    private bool _isTracking = true;
    private bool _isChangeTracking;
    private bool _breaksTracking;

    private bool _recursive;
    private bool _collectEffects;

    private SignalNode? _contextNode;

    internal SignalTracker Readonly(bool @readonly = true)
    {
        _isReadonly = @readonly;

        return this;
    }

    internal SignalTracker DisableTracking()
    {
        _isTracking = false;
        _isChangeTracking = false;

        return this;
    }

    internal SignalTracker EnableTracking(bool enabled = true)
    {
        _isTracking = enabled;

        return this;
    }

    internal SignalTracker BreaksTracking(bool breaksTracking = true)
    {
        _breaksTracking = breaksTracking;

        return this;
    }

    internal SignalTracker EnableChangeTracking(bool enabled = true)
    {
        _isChangeTracking = enabled;

        return this;
    }

    internal SignalTracker Recursive(bool recursive = true)
    {
        _recursive = recursive;

        return this;
    }

    internal SignalTracker CollectEffects(bool collectEffects = true)
    {
        _collectEffects = collectEffects;
    
        return this;
    }

    internal void PostEffect(SignalEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        if (_collectEffects)
        {
            this.Effects.Add(effect);
        }

        if (_parent?.AcceptEffects ?? false)
        {
            _parent?.PostEffect(effect);
        }
    }

    internal void Track(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        foreach (var tracker in ObjectWalker.Walk(this, static tracker => tracker._parent))
        {
            if (tracker._breaksTracking)
                break;
            
            if (!tracker._isTracking)
                continue;

            if (tracker._recursive || tracker == this)
            {
                tracker.Tracked.Add(node);
            }
        }
    }

    internal HashSet<SignalNode> GatherLocked(HashSet<SignalNode>? collector = null)
    {
        collector ??= new HashSet<SignalNode>(16);
        
        foreach (var tracker in ObjectWalker.Walk(this, static d => d.Parent))
        {
            foreach (var signalNode in tracker.Locked)
            {
                collector.Add(signalNode);
            }
        }

        foreach (var child in this.Children)
        {
            child.GatherLocked(collector);
        }

        return collector;
    }

    internal HashSet<SignalNode> GatherWaited(HashSet<SignalNode>? collector = null)
    {
        collector ??= new HashSet<SignalNode>(16);
        
        foreach (var tracker in ObjectWalker.Walk(this, static d => d.Parent))
        {
            foreach (var signalNode in tracker.Waited)
            {
                collector.Add(signalNode);
            }
        }

        foreach (var child in this.Children)
        {
            child.GatherWaited(collector);
        }

        return collector;
    }

    public void RequestAccess(SignalNode node)
    {
        if (node.AccessStrategy == SignalAccessStrategy.Unrestricted)
            return;
        
        Perf.MonoIncrement(node, "signal.tracker.access.requests.all");
        
        if (this.HoldsLock(node, true))
            return;
        
        switch (node.AccessStrategy)
        {
            case SignalAccessStrategy.Unrestricted:
                return;
            
            case SignalAccessStrategy.ExclusiveLock:
                Perf.MonoIncrement(node, "signal.tracker.access.requests.exclusive_locks");
                
                this.ExclusiveLock(node);
                break;
            
            case SignalAccessStrategy.PreemptiveLock:
                Perf.MonoIncrement(node, "signal.tracker.access.requests.preemptive_locks");
                
                this.PreEmptiveAccess(node);
                break;
            case SignalAccessStrategy.Optimistic:
                Perf.MonoIncrement(node, "signal.tracker.access.requests.optimistic");
                
                this.OptimisticAccess(node);
                break;
            default:                                   throw new ArgumentOutOfRangeException();
        }
    }

    public void RequestUpdate(SignalNode node)
    {
        if (node.AccessStrategy == SignalAccessStrategy.Unrestricted)
            return;
        
        Perf.MonoIncrement(node, "signal.tracker.update.requests.all");
        
        if (this.HoldsLock(node, true))
            return;
        
        switch (node.AccessStrategy)
        {
            case SignalAccessStrategy.Unrestricted:
                return;
            
            case SignalAccessStrategy.ExclusiveLock:
                Perf.MonoIncrement(node, "signal.tracker.update.requests.exclusive_locks");
                
                this.ExclusiveLock(node);
                break;
            
            case SignalAccessStrategy.PreemptiveLock:
                Perf.MonoIncrement(node, "signal.tracker.update.requests.preemptive_locks");
                
                this.PreEmptiveUpdate(node);
                break;
            case SignalAccessStrategy.Optimistic:
                Perf.MonoIncrement(node, "signal.tracker.update.requests.optimistic");
                
                this.OptimisticUpdate(node);
                break;
            default:                                   throw new ArgumentOutOfRangeException();
        }
    }

    private void RegisterLock(SignalNode node)
    {
        foreach (var tracker in ObjectWalker.Walk(this, static tracker => tracker._parent))
        {
            tracker.Locked.Add(node);
            node.LockedBy.Add(tracker);
        }
    }

    private void ExclusiveLock(SignalNode node)
    {
        var accessLock = node.AccessLock;
        if (accessLock is null)
            return;
        
        Debugger.NotifyOfCrossThreadDependency();
        
        using var activity = node.StartActivity();

        lock (node.RequestLock)
        {
            if (this.HoldsLock(node, true))
                return;
            
            if (accessLock.CurrentCount > 0)
            {
                if (accessLock.Wait(TimeSpan.FromMilliseconds(10)))
                {
                    this.RegisterLock(node);

                    return;
                }
            }

            if (node.IsDisposing)
                return;
        }

        if (!this.WaitAccess(node, TimeSpan.FromSeconds(1)))
            return;
        
        this.RegisterLock(node);
    }

    private bool WaitAccess(SignalNode node, TimeSpan timeout)
    {
        var accessLock = node.AccessLock;

        if (accessLock is null)
            return false;

        try
        {
            node.Waiters.Add(this);
            this.Waited.Add(node);
            
            while (!accessLock.Wait(timeout))
            {
                node.Event("AccessLockTimeout");
                
                if (node.IsDisposing)
                    return false;
                
                if (this.HoldsLock(node, true))
                    return true;

                if (node.LockedBy.HasAny)
                {
                    if (this.IsDeadlocked(node))
                    {
                        throw new SignalDeadlockedException(node);
                    }
                }
            }

            return !node.IsDisposing;
        }
        finally
        {
            node.Waiters.Remove(this);
            this.Waited.Remove(node);
        }
    }

    internal bool IsInEffect(SignalEffect? searchEffect = null)
    {
        var isSearching = searchEffect is not null;

        foreach (var tracker in ObjectWalker.Walk(this, static d => d._parent))
        {
            if (!isSearching)
            {
                if (tracker._contextNode is SignalEffect)
                    return true;
            }
            else if (searchEffect == tracker._contextNode)
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsDeadlocked(SignalNode checkNode)
    {
        HashSet<SignalNode> visitedNodes = [];
        Queue<SignalNode> lockedNodes = [];
        
        this.GatherLocked().ForEach(lockedNodes.Enqueue);

        while (lockedNodes.TryDequeue(out var lockedNode))
        {
            if (!visitedNodes.Add(lockedNode))
                continue;
            
            foreach (var waiterTrack in lockedNode.Waiters)
            {
                if (waiterTrack.HoldsLock(checkNode, true))
                    return true;
                
                var sublock = waiterTrack.GatherLocked();
                sublock.ForEach(lockedNodes.Enqueue);
            }
        }

        return false;
    }

    private void OptimisticAccess(SignalNode node)
    {
        this.CheckVersion(node);
    }

    private void OptimisticUpdate(SignalNode node)
    {
        lock (node.RequestLock)
        {
            if (!node.IsDirty)
            {
                this.CheckVersion(node);
                return;
            }

            if (node.LockedBy.HasAny)
            {
                this.YieldWhenYoungerThan(node);

                throw new SignalDeadlockedException(node);
            }

            this.RegisterLock(node);
        }
    }
    
    private void PreEmptiveAccess(SignalNode node)
    {
        var accessLatch = node.AccessLatch;
        if (accessLatch is null)
            return;

        if (this.HasLatched(node))
        {
            this.YieldWhenYoungerThan(node);
            
            this.CheckVersion(node);
            
            return;
        }
        
        if (!accessLatch.Acquire(this.Root, TimeSpan.FromMilliseconds(10)))
        {
            if (node.IsDisposing)
                return;

            using var waitMeasure = Perf.MeasureTime(node, "signal.wait.access.acquire_latch");
            
            try
            {
                node.Waiters.Add(this);
                this.Waited.Add(node);

                while (!accessLatch.Acquire(this.Root, TimeSpan.FromMilliseconds(300)))
                {
                    if (node.IsDisposing)
                        return;
                    
                    if (accessLatch.ClosedBy == this.Root)
                        return;
                    
                    if (this.HoldsLock(node, true))
                        return;
                    
                    this.YieldWhenYoungerThan(node);
                    
                    lock (node.RequestLock)
                    {
                        if (accessLatch.IsGateClosed)
                        {
                            
                            this.Preempt(node, accessLatch.ClosedBy);
                        }
                    }

                    if (this.IsDeadlocked(node))
                    {
                        throw new SignalDeadlockedException(node);
                    }
                }
            }
            finally
            {
                node.Waiters.Remove(this);
                this.Waited.Remove(node);
            }
        }

        this.Root.Latched.Add(node);

        this.CheckVersion(node);
    }
    
    private void PreEmptiveUpdate(SignalNode node)
    {
        var accessLatch = node.AccessLatch;
        if (accessLatch is null)
            return;

        this.YieldWhenYoungerThan(node);

        lock (node.RequestLock)
        {
            if (accessLatch.IsGateClosed)
            {
                if (accessLatch.ClosedBy != this.Root)
                {
                    this.Preempt(node, accessLatch.ClosedBy);
                }
            }

            accessLatch.CloseGate(this.Root);

            if (accessLatch.ClosedBy != this.Root)
            {
                this.Preempt(node, accessLatch.ClosedBy);
            }
            
        }

        var wasLatching = accessLatch.HasLatch(this.Root);
        if (wasLatching)
            accessLatch.Release(this.Root);

        if (!accessLatch.Wait(TimeSpan.FromMilliseconds(20)))
        {
            this.YieldWhenYoungerThan(node);
            
            using var waitMeasure = Perf.MeasureTime(node, "signal.wait.update.wait_latch");

            try
            {
                node.Waiters.Add(this);
                this.Waited.Add(node);

                while (!accessLatch.Wait(TimeSpan.FromMilliseconds(300)))
                {
                    if (node.IsDisposing || this.HoldsLock(node, true))
                    {
                        return;
                    }
                    
                    this.YieldWhenYoungerThan(node);

                    if (this.IsDeadlocked(node))
                    {
                        throw new SignalDeadlockedException(node);
                    }
                }
            }
            finally
            {
                node.Waiters.Remove(this);
                this.Waited.Remove(node);
            }
        }

        this.RegisterLock(node);
    }

    private void YieldWhenYoungerThan(SignalNode node)
    {
        if (node.LockedBy.IsEmpty)
            return;

        SignalTracker? minLocker = null;

        foreach (var lockingTracker in node.LockedBy)
        {
            minLocker ??= lockingTracker;
            
            foreach (var lTracker in ObjectWalker.Walk(lockingTracker, static d => d.Parent))
            {
                if (!lTracker.AcceptEffects)
                    continue;

                if (lTracker._trackerId < minLocker._trackerId)
                    minLocker = lTracker;
            }
        }

        if (minLocker is { AcceptEffects: true } && minLocker._trackerId < _trackerId)
        {
            this.Preempt(node, minLocker);
        }
    }

    private void Preempt(SignalNode node, SignalTracker? scheduleTarget)
    {
        if (this.RootEffect is not null && scheduleTarget is not null)
        {
            scheduleTarget.PostEffect(this.RootEffect);
                
            throw new SignalPreemptedException(node) { IsRescheduled = true };
        }

        throw new SignalPreemptedException(node);
    }

    private void CheckVersion(SignalNode node)
    {
        // Order of checks is important!
        if (!this.IsRoot)
            this.Root.CheckVersion(node);
        
        var lockTaken = false;
        try
        {
            _versionsLock.Enter(ref lockTaken);
            
            if (!this.Versions.TryGetValue(node, out var version))
            {
                version = node.Version;
                this.Versions.Add(node, version);
            }
            
            if (version != node.Version)
            {
                throw new SignalVersionChangedException(node, version, node.Version);
            }
        }
        finally
        {
            if (lockTaken)
            {
                _versionsLock.Exit(false);
            }
        }
    }

    private bool HasLatched(SignalNode node)
    {
        return this.Root.Latched.Contains(node);
    }

    internal bool HoldsLock(SignalNode node, bool recursive = false)
    {
        if (!recursive)
            return this.Locked.Contains(node);

        foreach (var tracker in ObjectWalker.Walk(this, static tracker => tracker._parent))
        {
            if (tracker.Locked.Contains(node))
                return true;
        }

        return false;
    }

    public bool HasTracked(SignalNode node, bool recursive = false)
    {
        if (!recursive)
            return this.Tracked.Contains(node);

        foreach (var tracker in ObjectWalker.Walk(this, static tracker => tracker._parent))
        {
            if (tracker.Tracked.Contains(node))
                return true;
        }

        return false;
    }

    internal void TrackChanged(SignalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        
        if (_isChangeTracking)
        {
            this.Changed.Add(node);
        }
        
        if (_parent is null)
            return;
        
        foreach (var tracker in ObjectWalker.Walk(_parent, static tracker => tracker._parent))
        {
            if (tracker is { _isChangeTracking: true, _recursive: true })
            {
                tracker.Changed.Add(node);
            }
        }
    }

    private SignalTracker Reset()
    {
        this.ReleaseLocks();

        _parent?.Children.Remove(this);
        this.Children.Clear();
        
        this.Tracked.Clear();
        this.Changed.Clear();
        this.Effects.Clear();
        this.Versions.Clear();
        this.Waited.Clear();
        this.Latched.Clear();
        
        _parent = null;

        _isReadonly = false;
        _isTracking = true;
        _recursive = false;
        _isChangeTracking = false;
        _collectEffects = false;
        _breaksTracking = false;
        
        return this;
    }

    private SignalTracker Init(SignalNode? contextNode, SignalTracker? parent)
    {
        this.Reset();

        _trackerId = Interlocked.Increment(ref _trackerIdCount);
        
        _contextNode = contextNode;
        
        _parent = parent;
        parent?.Children.Add(this);
        
        return this;
    }

    private void ReleaseLocks()
    {
        foreach (var node in this.Locked)
        {
            this.Locked.Remove(node);
            node.LockedBy.Remove(this);

            try
            {
                if (this.IsRoot)
                {
                    switch (node.AccessStrategy)
                    {
                        case SignalAccessStrategy.Optimistic:
                        case SignalAccessStrategy.Unrestricted: break;
                        case SignalAccessStrategy.ExclusiveLock:
                            node.AccessLock?.Release();
                            break;
                        case SignalAccessStrategy.PreemptiveLock:
                            node.AccessLatch?.ReleaseGate(this.Root);
                            break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        
        this.Locked.Clear();
        
        if (this.IsRoot)
        {
            foreach (var node in this.Latched)
            {
                node.AccessLatch?.Release(this);
            }
            
            this.Latched.Clear();
        }
    }

    public override string ToString()
    {
        return _contextNode is null
            ? "Tracker:Anonymous"
            : $"Tracker:{_contextNode.GetType().Name}:{_contextNode}";
    }
}