using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SigSharp.Utils;
using SigSharp.Utils.Perf;
using SigSharp.Utils.Pooling;

namespace SigSharp.Nodes;

public abstract class SignalNode : IDisposable, IAsyncDisposable
{
    internal static readonly object EmptyObject = new();
    private static ulong _ids;

    public static IEqualityComparer<T> AsGenericComparer<T>(IEqualityComparer? comparer)
    {
        return comparer switch
        {
            IEqualityComparer<T> genericComparer => genericComparer,
            _ => EqualityComparer<T>.Default
        };
    }

    public ulong NodeId { get; } = Interlocked.Increment(ref _ids);
    
    public bool IsDisposing => _disposing;
    public bool IsDisposed { get; private set; }
    
    public virtual bool DisposedBySignalGroup { get; protected set; }
    
    public bool IsSuspended { get; private set; }
    public bool WouldBeTracked { get; private set; }
    
    public bool IsTrackable { get; }
    
    public bool IsDirty { get; private set; }
    protected bool IsShadowDirty { get; set; }

    public SignalAccessStrategy AccessStrategy { get; private set; }
        = SignalAccessStrategy.Unrestricted;
    
    public string? Name { get; set; }

    public uint Version => _version;

    protected internal readonly ILogger Logger;

    internal SemaphoreSlim? AccessLock { get; private set; }
    internal GatedLatch<SignalTracker>? AccessLatch { get; private set; }

    private ConditionalWeakTable<SignalNode, object>? _referencedBy;

    // Lock the referencedBy because of minimizing it's internal allocations (ensure no multiple enumerators cause copies)
    // Can be removed in the future when we have a good WeakSet implementation
    private readonly Lock _refLock = new();
    
    // TODO: Use weakset
    //private readonly WeakSet<SignalNode> _referencedBy = new();
    
    private volatile uint _version = 1;
    private volatile bool _disposing;

    internal Lock RequestLock { get; } = new();

    private SpinLock _trackingSetsLock = new(false);

    private ConcurrentHashSet<SignalTracker>? _waiters;
    internal ConcurrentHashSet<SignalTracker>? Waiters => _waiters;

    private ConcurrentHashSet<SignalTracker>? _lockedBy;
    internal ConcurrentHashSet<SignalTracker>? LockedBy => _lockedBy;
    
    protected SignalNode(
        bool isTrackable,
        bool initiallyDirty,
        string? name)
    {
        this.Name = name;
        
        Logger = Signals.Options.Logging.CreateLogger(this.GetType());

        this.IsTrackable = isTrackable;
        
        this.IsDirty = initiallyDirty;
        this.IsShadowDirty = initiallyDirty;
        
        this.MarkTracked();
        this.RequestAccess();
        
        Perf.Increment("signal.node.count");
    }

    protected virtual ValueTask DisposeAsyncCore()
    {
        this.WithEachReferencing(this, static (@this, node) =>
            {
                if (!node.IsDisposed)
                    node.ReferenceDisposed(@this);
            });

        lock (_refLock)
        {
            if (_referencedBy is not null)
            {
                _referencedBy.Clear();
                SignalItemPools.ReferencedByPool.Return(_referencedBy);
                _referencedBy = null;
            }
        }

        this.NewVersion();

        this.ForceReturnAccessLatch();
        
        this.AccessLock?.Dispose();
        this.AccessLock = null;
        
        this.ReturnWaiter();
        this.ReturnLockedBy();

        Perf.Decrement("signal.node.count");
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        var wasDisposing = Interlocked.CompareExchange(ref _disposing, true, false);
        if (!wasDisposing && _disposing)
        {
            try
            {
                this.RequestUpdate();
            }
            catch (Exception)
            {
                // ignore
            }
            
            await this.DisposeAsyncCore();
            
            GC.SuppressFinalize(this);
        }
        
        this.IsDisposed = true;
    }

    public void Dispose()
    {
        var wasDisposing = Interlocked.CompareExchange(ref _disposing, true, false);
        if (!wasDisposing && _disposing)
        {
            try
            {
                this.RequestUpdate();
            }
            catch (Exception)
            {
                // ignore
            }
            
            var disposer = this.DisposeAsyncCore();
            if (!disposer.IsCompletedSuccessfully)
            {
                disposer.AsTask().GetAwaiter().GetResult();
            }
            
            GC.SuppressFinalize(this);
        }

        this.IsDisposed = true;
    }

    protected void SetAccessStrategy(SignalAccessStrategy accessStrategy)
    {
        this.ForceReturnAccessLatch();
        
        this.AccessLock?.Dispose();
        this.AccessLock = null;

        this.AccessStrategy = accessStrategy;

        switch (accessStrategy)
        {
            case SignalAccessStrategy.PreemptiveLock:
            case SignalAccessStrategy.Optimistic:
            case SignalAccessStrategy.Unrestricted:
                break;
            case SignalAccessStrategy.ExclusiveLock:
                this.AccessLock = new SemaphoreSlim(1);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(accessStrategy), accessStrategy, null);
        }
    }
    
    protected virtual void ReferenceChanged(SignalNode refNode) { }
    protected virtual void ReferenceDisposed(SignalNode refNode) { }

    protected void WithEachReferencing<TState>(TState state, Action<TState, SignalNode> action)
    {
        lock (_refLock)
        {
            var refBy = _referencedBy;

            if (refBy is null)
                return;
            
            foreach (var (node, _) in refBy)
            {
                action(state, node);
            }
        }
    }
    
    protected void WithEachReferencing(Action<SignalNode> action)
    {
        lock (_refLock)
        {
            var refBy = _referencedBy;

            if (refBy is null)
                return;
            
            foreach (var (node, _) in refBy)
            {
                action(node);
            }
        }
    }

    protected uint NewVersion()
    {
        var nextVersion = Interlocked.Increment(ref _version);

        return nextVersion >= Int32.MaxValue
            ? Interlocked.Exchange(ref _version, 1)
            : nextVersion;
    }

    public void Changed()
    {
        if (this.IsDisposing)
            return;
        
        this.NewVersion();
        
        SignalTracker.Current?.TrackChanged(this);
        
        this.WithEachReferencing(this, static (@this, node) => node.ReferenceChanged(@this));
    }

    public void MarkTracked()
    {
        if (!this.IsTrackable || this.IsDisposing)
            return;
        
        if (this.IsSuspended)
        {
            this.WouldBeTracked |= SignalTracker.Current is not null;
        }
        else
        {
            SignalTracker.Current?.Track(this);
        }
    }

    public virtual SignalSuspender Suspend(bool disposing = false)
    {
        this.IsSuspended = true;
        return new SignalSuspender(this, disposing);
    }

    public virtual void Resume()
    {
        if (!this.IsSuspended)
            return;
        
        this.IsSuspended = false;
        
        if (this.WouldBeTracked)
        {
            this.MarkTracked();
        }
    }

    public void AddReferencedBy(SignalNode node)
    {
        lock (_refLock)
        {
            _referencedBy ??= SignalItemPools.ReferencedByPool.Rent();
            _referencedBy.AddOrUpdate(node, EmptyObject);
        }
    }

    public void RemoveReferencedBy(SignalNode node)
    {
        lock (_refLock)
        {
            if (_referencedBy is null)
                return;
            
            _referencedBy.RemoveSafe(node);

            if (!_referencedBy.Any())
            {
                var refBy = _referencedBy;
                _referencedBy = null;
                
                refBy.Clear();
                SignalItemPools.ReferencedByPool.Return(refBy);
            }
        }
    }
    
    public virtual void MarkDirty()
    {
        if (this.IsDirty || this.IsDisposing)
            return;
        
        this.IsDirty = true;
        this.IsShadowDirty = true;
        
        this.OnDirty();
    }
    
    public virtual void MarkPristine()
    {
        if (this.IsDisposed)
            return;
        
        this.IsDirty = false;
        this.IsShadowDirty = false;
    }

    internal void AddWaiter(SignalTracker tracker)
    {
        if (this.IsDisposing)
            return;
        
        ConcurrentHashSet<SignalTracker>? rented = null;
        
        while (true)
        {
            rented ??= _waiters is null ? SignalItemPools.WaiterPool.Rent() : null;
            
            var lockTaken = false;
            try
            {
                _trackingSetsLock.Enter(ref lockTaken);

                if (_waiters is not null)
                {
                    _waiters.Add(tracker);
                    break;
                }
                
                if (rented is null)
                    continue;

                _waiters = rented;
                rented = null;
                _waiters.Add(tracker);

                break;
            }
            finally
            {
                if (lockTaken)
                    _trackingSetsLock.Exit(false);
            }
        }
        
        if (rented is not null)
        {
            SignalItemPools.WaiterPool.Return(rented);
        }
    }
    
    internal void RemoveWaiter(SignalTracker tracker)
    {
        ConcurrentHashSet<SignalTracker>? waiters = _waiters;
        
        waiters?.Remove(tracker);
        
        if (waiters is null || waiters.Count > 0)
            return;
        
        this.ReturnWaiter();
    }

    private void ReturnWaiter()
    {
        ConcurrentHashSet<SignalTracker>? toReturn = null;
        
        var lockTaken = false;
        try
        {
            _trackingSetsLock.Enter(ref lockTaken);
            
            if (_waiters is not null)
            {
                toReturn = _waiters;
                _waiters = null;
            }
        }
        finally
        {
            if (lockTaken)
                _trackingSetsLock.Exit(false);
        }

        if (toReturn is not null)
        {
            toReturn.Clear();
            SignalItemPools.WaiterPool.Return(toReturn);
        }
    }

    internal void AddLockedBy(SignalTracker tracker)
    {
        if (this.IsDisposing)
            return;
        
        ConcurrentHashSet<SignalTracker>? rented = null;
        
        while (true)
        {
            rented ??= _lockedBy is null ? SignalItemPools.LockedByPool.Rent() : null;
            
            var lockTaken = false;
            try
            {
                _trackingSetsLock.Enter(ref lockTaken);

                if (_lockedBy is not null)
                {
                    _lockedBy.Add(tracker);
                    break;
                }
                
                if (rented is null)
                    continue;

                _lockedBy = rented;
                rented = null;
                
                _lockedBy.Add(tracker);

                break;
            }
            finally
            {
                if (lockTaken)
                    _trackingSetsLock.Exit(false);
            }
        }
        
        if (rented is not null)
        {
            SignalItemPools.LockedByPool.Return(rented);
        }
    }

    internal void RemoveLockedBy(SignalTracker tracker)
    {
        ConcurrentHashSet<SignalTracker>? lockedBy = _lockedBy;
        
        lockedBy?.Remove(tracker);
        
        if (lockedBy is null || lockedBy.Count > 0)
            return;

        this.ReturnLockedBy();
    }

    private void ReturnLockedBy()
    {
        ConcurrentHashSet<SignalTracker>? toReturn = null;
        
        var lockTaken = false;
        try
        {
            _trackingSetsLock.Enter(ref lockTaken);
            
            if (_lockedBy is not null)
            {
                toReturn = _lockedBy;
                _lockedBy = null;
            }
        }
        finally
        {
            if (lockTaken)
                _trackingSetsLock.Exit(false);
        }

        if (toReturn is not null)
        {
            toReturn.Clear();
            SignalItemPools.LockedByPool.Return(toReturn);
        }
    }
    
    protected virtual void OnDirty()
    {
        if (this.IsDisposing)
            return;
        
        this.WithEachReferencing(static node =>
                {
                    if (!node.IsDirty)
                    {
                        node.MarkDirty();
                    }
                }
            );
    }

    protected void CheckDisposed()
    {
        SignalDisposedException.ThrowIf(this.IsDisposed, this);
    }

    protected bool IsCycliclyReferenced()
    {
        return this.IsCycliclyReferenced(this, []);
    }

    private bool IsCycliclyReferenced(SignalNode searchNode, HashSet<SignalNode> visited)
    {
        if (visited.Contains(searchNode))
            return true;

        lock (_refLock)
        {
            var refBy = _referencedBy;

            if (refBy is null)
                return false;
            
            foreach (var (node, _) in refBy)
            {
                visited.Add(node);

                if (node.IsCycliclyReferenced(searchNode, visited))
                {
                    return true;
                }

                visited.Remove(node);
            }
        }

        return false;
    }

    protected void RequestAccess()
    {
        if (this.IsDisposing || this.AccessStrategy == SignalAccessStrategy.Unrestricted)
            return;

        var tracker = SignalTracker.Current;
        if (tracker is not null)
        {
            tracker.RequestAccess(this);

            return;
        }
        
        var phantomTracker = SignalTracker.Push(true, this);
        try
        {
            phantomTracker.RequestAccess(this);
        }
        finally
        {
            SignalTracker.Pop(phantomTracker);
        }
    }
    
    protected internal void RequestUpdate()
    {
        if (this.IsDisposing || this.AccessStrategy == SignalAccessStrategy.Unrestricted)
            return;

        var tracker = SignalTracker.Current;
        if (tracker is not null)
        {
            tracker.RequestUpdate(this);

            return;
        }
        
        var phantomTracker = SignalTracker.Push(true, this);
        try
        {
            phantomTracker.RequestUpdate(this);
        }
        finally
        {
            SignalTracker.Pop(phantomTracker);
        }
    }

    public override string ToString()
    {
        return $"{this.GetType().Name}({this.NodeId}):{this.Name ?? "Anonymous"}";
    }

    internal bool TryGetOrCreateAccessLatch([MaybeNullWhen(false)]out GatedLatch<SignalTracker> accessLatch)
    {
        accessLatch = null;
        
        if (this.IsDisposing)
            return false;

        accessLatch = this.AccessLatch;
        if (accessLatch is not null)
            return true;

        GatedLatch<SignalTracker>? rented = null;
        while (true)
        {
             rented ??= this.AccessLatch is null
                ? SignalItemPools.AccessLatchPool.Rent()
                : null;
            
            var lockTaken = false;
            try
            {
                _trackingSetsLock.Enter(ref lockTaken);

                accessLatch = this.AccessLatch;

                if (accessLatch is not null)
                    break;
            
                if (accessLatch is null && rented is not null)
                {
                    this.AccessLatch = rented;
                    accessLatch = rented;
                    rented = null;
                    break;
                }
            }
            finally
            {
                if (lockTaken)
                    _trackingSetsLock.Exit(false);
            }
        }

        if (rented is not null)
        {
            SignalItemPools.AccessLatchPool.Return(rented);
        }

        return true;
    }

    internal void TryFreeAccessLatch()
    {
        GatedLatch<SignalTracker>? toReturn = null;
        
        var lockTaken = false;
        try
        {
            _trackingSetsLock.Enter(ref lockTaken);
            
            if (this.AccessLatch is { IsEmpty: true })
            {
                toReturn = this.AccessLatch;
                this.AccessLatch = null;
            }
        }
        finally
        {
            if (lockTaken)
                _trackingSetsLock.Exit(false);
        }

        if (toReturn is not null)
        {
            toReturn.Reset();
            SignalItemPools.AccessLatchPool.Return(toReturn);
        }
    }

    private void ForceReturnAccessLatch()
    {
        var latch = this.AccessLatch;
        if (latch is not null)
        {
            this.AccessLatch = null;
            latch.Reset();
            SignalItemPools.AccessLatchPool.Return(latch);
        }
    }
}