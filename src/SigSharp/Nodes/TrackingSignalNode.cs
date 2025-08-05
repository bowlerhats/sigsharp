using System.Collections.Generic;
using System.Linq;
using SigSharp.TrackerStores;
using SigSharp.Utils;

namespace SigSharp.Nodes;

public abstract class TrackingSignalNode : ReactiveNode
{
    public bool HasTracking => _store.Tracked.Any();
    public bool IsDirty { get; private set; } = true;
    
    public IEnumerable<SignalNode> Tracked => _store.Tracked;

    private ITrackerStore _store;
    
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
            _store.Clear();
            _store.Dispose();
            _store = null!;
        }
        
        base.Dispose(disposing);
    }

    protected override void ReferenceChanged(SignalNode refNode)
    {
        if (this.IsDisposed || _trackingDisabled)
            return;

        if (_store.Contains(refNode))
        {
            if (refNode.IsDisposed)
            {
                _store.UnTrack(refNode);
            }
            
            this.MarkDirty();
        }

        base.ReferenceChanged(refNode);
    }
    
    protected override void ReferenceDisposed(SignalNode refNode)
    {
        if (this.IsDisposed || _trackingDisabled)
            return;

        if (_store.Contains(refNode))
        {
            _store.UnTrack(refNode);
            
            this.MarkDirty();
        }

        base.ReferenceDisposed(refNode);
    }
    
    protected virtual void OnDirty() { }

    protected void DisableTracking()
    {
        _trackingDisabled = true;
    }
    
    protected void ChangeStoreTo(ITrackerStore newStore)
    {
        newStore.Clear();
        
        _store.Tracked.ForEach(newStore.Track);
        var oldStore = _store;
        _store = newStore;
        
        oldStore.Dispose();
    }

    public void MarkDirty()
    {
        if (this.IsDirty || this.IsDisposed)
            return;
        
        this.IsDirty = true;
        
        this.Changed();
        
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
        _store.Track(node);
    }

    public void UnTrack(SignalNode node)
    {
        if (_trackingDisabled || node == this)
            return;
        
        node.RemoveReferencedBy(this);
        _store.UnTrack(node);
    }

    public void ClearTracked()
    {
        foreach (var node in _store.Tracked)
        {
            node.RemoveReferencedBy(this);
        }
        
        _store.Clear();
    }
    
    internal SignalTracker StartTrack(bool expecEmpty)
    {
        this.CheckDisposed();

        return SignalTracker.Push(expecEmpty);
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

    private void UpdateTrackerStore(IReadOnlyCollection<SignalNode> nodes)
    {
        if (_trackingDisabled)
            return;

        foreach (var trackedNode in _store.Tracked)
        {
            if (trackedNode.IsDisposed || !nodes.Contains(trackedNode))
            {
                trackedNode.RemoveReferencedBy(this);
                _store.UnTrack(trackedNode);
            }
        }
        
        foreach (var node in nodes.Except(_store.Tracked))
        {
            if (node.IsDisposed)
                continue;
            
            node.AddReferencedBy(this);
            _store.Track(node);
        }
    }
}