using System;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp.Utils;

public sealed class SignalSuspender : IDisposable, IAsyncDisposable
{
    private readonly bool _alsoDisposeNode;
    private SignalNode _node;
    private bool _disposed;
    
    public SignalSuspender(SignalNode node, bool alsoDisposeNode)
    {
        _node = node;
        _alsoDisposeNode = alsoDisposeNode;
    }

    ~SignalSuspender()
    {
        this.Dispose();
    }

    public void Resume()
    {
        if (_disposed)
            return;
        
        _node.Resume();
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
        
        this.Resume();

        _disposed = true;

        if (_alsoDisposeNode)
        {
            _node?.Dispose();
        }

        _node = null;
        
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        this.Dispose();
        
        GC.SuppressFinalize(this);
        
        return ValueTask.CompletedTask;
    }
}