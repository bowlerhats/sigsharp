using System;
using System.Collections.Generic;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp.TrackerStores;

public sealed class ConcurrentTrackerStore : ITrackerStore
{
    public IEnumerable<SignalNode> Tracked => _tracked;
    
    public bool IsDisposed { get; private set; }
    
    private readonly ConcurrentHashSet<SignalNode> _tracked = [];

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
        
        return _tracked.Contains(node);
    }

    public void Track(SignalNode node)
    {
        this.CheckDisposed();
        
        _tracked.Add(node);
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