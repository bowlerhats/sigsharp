using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp.TrackerStores;

public sealed class ConcurrentTrackerStore : ITrackerStore
{
    public bool HasAny => _tracked.HasAny;

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

    public bool Track(SignalNode node)
    {
        this.CheckDisposed();
        
        return _tracked.Add(node);
    }
    
    public void UnTrack(SignalNode node)
    {
        this.CheckDisposed();
        
        _tracked.Remove(node);
    }

    public void WithEach(Action<SignalNode> action)
    {
        if (_tracked.IsEmpty)
            return;
        
        foreach (var node in _tracked)
        {
            action(node);
        }
    }
    
    public void WithEach<TState>(TState state, Action<TState, SignalNode> action)
    {
        if (_tracked.IsEmpty)
            return;
        
        foreach (var node in _tracked)
        {
            action(state, node);
        }
    }

    public async ValueTask WithEachAsync(Func<SignalNode, ValueTask> action)
    {
        if (_tracked.IsEmpty)
            return;
        
        foreach (var node in _tracked)
        {
            await action(node);
        }
    }
    
    public async ValueTask WithEachAsync<TState>(TState state, Func<TState, SignalNode, ValueTask> action)
    {
        if (_tracked.IsEmpty)
            return;
        
        foreach (var node in _tracked)
        {
            await action(state, node);
        }
    }

    public void Collect<TSignalNode>(ICollection<TSignalNode> target)
        where TSignalNode : SignalNode
    {
        foreach (var node in _tracked)
        {
            if (node is TSignalNode signalNode)
                target.Add(signalNode);
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}