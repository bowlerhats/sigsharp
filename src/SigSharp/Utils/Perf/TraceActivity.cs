using System;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.Utils.Perf;

internal readonly struct TraceActivity : IDisposable
{
    private readonly ISignalTraceProvider? _provider;
    private readonly IDisposable? _activity;
        
    public TraceActivity()
    {
    }

    public TraceActivity(ISignalTraceProvider provider, IDisposable? activity)
    {
        _provider = provider;
        _activity = activity;
    }

    public void AddInfo(SignalNode node)
    {
        _provider?.AddInfo(_activity, node);
    }
        
    public void Dispose()
    {
        _provider?.DisposeActivity(_activity);
    }

    public void Event(
        string name,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        _provider?.Event(_activity, name, callerName, lineNumber);
    }
}