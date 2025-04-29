using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

internal sealed class WeakNamedTrackerStore : INamedTrackerStore
{
    private readonly ConcurrentDictionary<ComputedSignalId, WeakReference<SignalNode>> _tracked = [];

    private bool _disposed;
    
    public void Dispose()
    {
        this.Clear();
        _disposed = true;
    }

    public T? LookupComputed<T>(ComputedSignalId id)
        where T : SignalNode
    {
        this.CheckDisposed();
        
        var nodeRef = _tracked.GetValueOrDefault(id);

        if (nodeRef is null)
            return null;
        
        if (!nodeRef.TryGetTarget(out var node))
        {
            _tracked.Remove(id, out _);

            return null;
        }

        if (node is T signalNode)
        {
            return signalNode;
        }
        
        if (node is not null)
            throw new SignalException("Existing compute node's type is different then expected");

        return null;
    }

    public void Clear()
    {
        this.CheckDisposed();
        
        _tracked.Clear();
    }
    
    public void Track(SignalNode node, ComputedSignalId id)
    {
        this.CheckDisposed();

        _tracked[id] = new WeakReference<SignalNode>(node);
    }
    
    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}