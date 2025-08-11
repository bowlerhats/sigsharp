using SigSharp.TrackerStores;
using SigSharp.Utils;

namespace SigSharp.Nodes;

public abstract class TrackingSignalNode : ReactiveNode
{
    public bool HasTracking => _store?.HasAny ?? false;
    public bool IsDirty { get; private set; } = true;
    
    private ITrackerStore? _store;
    
    private bool _trackingDisabled;
    
    protected TrackingSignalNode(SignalGroup group, bool isTrackable, string? name)
        :base(group, isTrackable, name)
    {
        _store = new ConcurrentTrackerStore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _store?.Clear();
            _store?.Dispose();
            _store = null!;
        }
        
        base.Dispose(disposing);
    }

    protected override void ReferenceChanged(SignalNode refNode)
    {
        if (this.IsDisposed || _trackingDisabled || refNode.IsDisposed)
            return;
        
        this.MarkDirty();
        
        base.ReferenceChanged(refNode);
    }
    
    protected override void ReferenceDisposed(SignalNode refNode)
    {
        if (this.IsDisposed || _trackingDisabled)
            return;

        if (_store?.Contains(refNode) ?? false)
        {
            _store.UnTrack(refNode);
            
            this.MarkDirty();
        }

        base.ReferenceDisposed(refNode);
    }

    protected virtual void OnDirty()
    {
        if (this.IsDisposed)
            return;
        
        this.WithEachReferencing(static node =>
                {
                    if (node is TrackingSignalNode { IsDirty: false } trackingNode)
                    {
                        trackingNode.MarkDirty();
                    }
                }
            );
    }

    protected void DisableTracking()
    {
        _trackingDisabled = true;
    }
    
    protected void ChangeStoreTo(ITrackerStore newStore)
    {
        newStore.Clear();
        
        _store?.WithEach(newStore, static (store, node) => store.Track(node));
        
        var oldStore = _store;
        _store = newStore;
        
        oldStore?.Dispose();
    }

    public void MarkDirty()
    {
        if (this.IsDirty || this.IsDisposed)
            return;
        
        this.IsDirty = true;
        
        this.OnDirty();
    }

    public void MarkPristine()
    {
        if (this.IsDisposed)
            return;
        
        this.IsDirty = false;
    }

    public void Track(SignalNode node)
    {
        if (_trackingDisabled || node == this || node.IsDisposed)
            return;
        
        node.AddReferencedBy(this);
        _store!.Track(node);
    }

    public void UnTrack(SignalNode node)
    {
        if (_trackingDisabled || node == this)
            return;
        
        node.RemoveReferencedBy(this);
        _store!.UnTrack(node);
    }

    public void ClearTracked()
    {
        _store?.WithEach(this, static (@this, signalNode) =>
        {
            signalNode.RemoveReferencedBy(@this);
        });
        
        _store?.Clear();
    }
    
    internal SignalTracker StartTrack(bool expectEmpty)
    {
        this.CheckDisposed();
        
        return SignalTracker.Push(expectEmpty);
    }

    internal void EndTrack(SignalTracker tracker)
    {
        try
        {
            this.CheckDisposed();
            
            this.UpdateTrackerStore(tracker.Tracked);
        }
        finally
        {
            SignalTracker.Pop(tracker);
        }
    }

    private void UpdateTrackerStore(ConcurrentHashSet<SignalNode> nodes)
    {
        if (_trackingDisabled)
            return;
        
        _store?.WithEach((this, nodes), static (state, trackedNode) =>
        {
            if (trackedNode.IsDisposed || !state.nodes.Contains(trackedNode))
            {
                state.Item1.UnTrack(trackedNode);
            }
        });
        
        foreach (var node in nodes)
        {
            if (node.IsDisposed || (_store?.Contains(node) ?? false))
                continue;

            this.Track(node);
        }
    }
}