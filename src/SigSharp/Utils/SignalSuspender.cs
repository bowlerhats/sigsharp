using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SigSharp.Nodes;

namespace SigSharp.Utils;

public sealed class SignalSuspender : IDisposable, IAsyncDisposable
{
    private readonly bool _alsoDisposeNode;
    private SignalNode? _node;
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
        
        _node?.Resume();
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            this.Resume();
        }
        catch (Exception ex)
        {
            _node?.Logger.LogDebug(ex, "Suspender failed to resume node");
        }

        if (Interlocked.Exchange(ref _disposed, true))
            return;

        var node = Interlocked.Exchange(ref _node, null);
        if (_alsoDisposeNode)
        {
            node?.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        this.Dispose();
        
        GC.SuppressFinalize(this);
        
        return ValueTask.CompletedTask;
    }
}