using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

public sealed class WeakTrackerStore : ITrackerStore
{
    public IEnumerable<SignalNode> Tracked => _tracked.Select(static d => d.Key);

    public bool HasAny => _tracked.Any();
    
    private readonly ConditionalWeakTable<SignalNode, object> _tracked = [];
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
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}