using System.Threading.Tasks;
using SigSharp.TrackerStores;
using SigSharp.Utils;

namespace SigSharp.Nodes;

public abstract class TrackingSignalNode : ReactiveNode
{
    public bool HasTracking => _store?.HasAny ?? false;
    
    private ITrackerStore? _store;
    
    private bool _trackingDisabled;
    
    protected TrackingSignalNode(SignalGroup group, bool isTrackable, bool initiallyDirty, string? name)
        :base(group, isTrackable, initiallyDirty, name)
    {
        _store = new ConcurrentTrackerStore();
    }

    protected override ValueTask DisposeAsyncCore()
    {
        _store?.Clear();
        _store?.Dispose();
        _store = null!;
        
        return base.DisposeAsyncCore();
    }

    protected override void ReferenceChanged(SignalNode refNode)
    {
        if (this.IsDisposing || _trackingDisabled || refNode.IsDisposing)
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

    public void Track(SignalNode node)
    {
        if (_trackingDisabled || node == this || node.IsDisposing)
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
        
        return SignalTracker.Push(expectEmpty, this);
    }

    internal void EndTrack(SignalTracker tracker)
    {
        try
        {
            this.CheckDisposed();

            if (!tracker.IsTracking)
                return;
            
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
            if (trackedNode.IsDisposing || !state.nodes.Contains(trackedNode))
            {
                state.Item1.UnTrack(trackedNode);
            }
        });
        
        foreach (var node in nodes)
        {
            if (node.IsDisposing || (_store?.Contains(node) ?? false))
                continue;

            this.Track(node);
        }
    }
}