using System;
using System.Collections.Generic;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp.TrackerStores;

public sealed class ConcurrentTrackerStore : ITrackerStore
{
    public IEnumerable<SignalNode> Tracked => _tracked;
    
    private readonly ConcurrentHashSet<SignalNode> _tracked = [];
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;
        
        _tracked.Clear();

        _disposed = true;
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
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}