using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public sealed class WeakTrackerStore : ITrackerStore
{
    public IEnumerable<SignalNode> Tracked => _tracked.Select(static d => d.Key);
    
    public bool IsDisposed { get; private set; }
    
    private readonly ConditionalWeakTable<SignalNode, object> _tracked = [];

    public void Dispose()
    {
        if (this.IsDisposed)
            return;
        
        _tracked.Clear();
        
        this.IsDisposed = true;
    }

    public void Clear()
    {
        _tracked.Clear();
    }

    public bool Contains(SignalNode node)
    {
        this.CheckDisposed();

        return _tracked.TryGetValue(node, out _);
    }

    public void Track(SignalNode node)
    {
        this.CheckDisposed();
        
        _tracked.Add(node, SignalNode.EmptyObject);
    }
    
    public void UnTrack(SignalNode node)
    {
        this.CheckDisposed();
        
        _tracked.Remove(node);
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(this.IsDisposed, this);
    }
}