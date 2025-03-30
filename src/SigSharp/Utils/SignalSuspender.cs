using System;
using System.Threading.Tasks;
using SigSharp.Nodes;

namespace SigSharp.Utils;

public sealed class SignalSuspender : IDisposable, IAsyncDisposable
{
    private readonly SignalNode _node;
    private readonly bool _disposing;
    
    public SignalSuspender(SignalNode node, bool disposing)
    {
        _node = node;
        _disposing = disposing;
        // TODO: Handle disposing
    }

    public void Resume()
    {
        _node.Resume();
    }
    
    public void Dispose()
    {
        this.Resume();
    }

    public ValueTask DisposeAsync()
    {
        this.Resume();
        
        return ValueTask.CompletedTask;
    }
}