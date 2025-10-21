using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SigSharp.Utils;
using SigSharp.Utils.Perf;

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

    internal GatedLatch<SignalTracker>? AccessLatch { get; private set; }
    internal SemaphoreSlim? AccessLock { get; private set; }

    protected internal readonly ILogger Logger;
    
    private readonly ConditionalWeakTable<SignalNode, object> _referencedBy = [];

    // Lock the referencedBy because of minimizing it's internal allocations (ensure no multiple enumerators cause copies)
    private readonly Lock _refLock = new();
    
    // TODO: Use weakset
    //private readonly WeakSet<SignalNode> _referencedBy = new();
    
    private volatile uint _version = 1;
    private volatile bool _disposing;

    internal Lock RequestLock { get; } = new();
    internal ConcurrentHashSet<SignalTracker> Waiters { get; } = [];
    internal ConcurrentHashSet<SignalTracker> LockedBy { get; } = [];
    
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
            _referencedBy.Clear();
        }

        this.NewVersion();
        
        this.AccessLatch?.Dispose();
        this.AccessLatch = null;
        this.AccessLock?.Dispose();
        this.AccessLock = null;
        
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
        this.AccessLatch?.Dispose();
        this.AccessLatch = null;
        this.AccessLock?.Dispose();
        this.AccessLock = null;

        this.AccessStrategy = accessStrategy;
        
        switch (accessStrategy)
        {
            case SignalAccessStrategy.Optimistic:
            case SignalAccessStrategy.Unrestricted:
                break;
            case SignalAccessStrategy.ExclusiveLock:
                this.AccessLock = new SemaphoreSlim(1);
                break;
            case SignalAccessStrategy.PreemptiveLock:
                this.AccessLatch = new GatedLatch<SignalTracker>();
                break;
            default:                                   throw new ArgumentOutOfRangeException(nameof(accessStrategy), accessStrategy, null);
        }
    }
    
    protected virtual void ReferenceChanged(SignalNode refNode) { }
    protected virtual void ReferenceDisposed(SignalNode refNode) { }

    protected void WithEachReferencing<TState>(TState state, Action<TState, SignalNode> action)
    {
        lock (_refLock)
        {
            foreach (var (node, _) in _referencedBy)
            {
                action(state, node);
            }
        }
    }
    
    protected void WithEachReferencing(Action<SignalNode> action)
    {
        lock (_refLock)
        {
            foreach (var (node, _) in _referencedBy)
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
            _referencedBy.AddOrUpdate(node, EmptyObject);
        }
    }

    public void RemoveReferencedBy(SignalNode node)
    {
        lock (_refLock)
        {
            _referencedBy.RemoveSafe(node);
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
            foreach (var (node, _) in _referencedBy)
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
}