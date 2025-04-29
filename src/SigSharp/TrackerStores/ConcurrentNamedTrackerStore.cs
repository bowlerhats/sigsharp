using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SigSharp.Nodes;

namespace SigSharp.TrackerStores;

internal sealed class ConcurrentNamedTrackerStore : INamedTrackerStore
{
    private readonly ConcurrentDictionary<ComputedSignalId, SignalNode> _tracked = [];

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
        
        var node = _tracked.GetValueOrDefault(id);
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

        _tracked[id] = node;
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}