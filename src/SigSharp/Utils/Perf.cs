using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.Utils;

public interface ISignalTraceProvider
{
    public IDisposable? StartRootActivity(string? name, string? callerName, int lineNumber);
    public IDisposable? StartActivity(string? name, string? callerName, int lineNumber);
    public void DisposeActivity(IDisposable? activity);

    public void AddInfo(IDisposable? activity, SignalNode node);

    public void Event(IDisposable? activity, string name, string? callerName, int lineNumber);
    
    public void Event(string name, string? callerName, int lineNumber);
}

public class SignalDiagnosticsTraceProvider : ISignalTraceProvider
{
    public static ActivitySource SigSharpActivitySource { get; } = new("SigSharp");
    
    public ActivitySource ActivitySource { get; }

    public SignalDiagnosticsTraceProvider(ActivitySource? activitySource = null)
    {
        this.ActivitySource = activitySource ?? SigSharpActivitySource;
    }

    public IDisposable? StartRootActivity(string? name, string? callerName, int lineNumber)
    {
        return this.StartActivity(this.ActivitySource, name, callerName, lineNumber);
    }
    
    public IDisposable? StartActivity(string? name, string? callerName, int lineNumber)
    {
        var source = Activity.Current?.Source ?? this.ActivitySource;

        return this.StartActivity(source, name, callerName, lineNumber);
    }

    public virtual IDisposable? StartActivity(ActivitySource source, string? name, string? callerName, int lineNumber)
    {
        name ??= $".{callerName ?? "???"}";
        
        var activity = source.StartActivity(name);

        if (activity is not null)
        {
            activity.AddTag("code.function", callerName);
            activity.AddTag("code.line", lineNumber);
        }

        return activity;
    }

    public void DisposeActivity(IDisposable? disposable)
    {
        disposable?.Dispose();
    }

    public void AddInfo(IDisposable? handle, SignalNode node)
    {
        if (handle is Activity activity)
        {
            activity.AddTag("signal.node.id", node.NodeId);
            activity.AddTag("signal.node.name", node.Name);
            activity.AddTag("signal.node.dirty", node.IsDirty);
            activity.AddTag("signal.node.disposing", node.IsDisposing);
            activity.AddTag("signal.node.disposed", node.IsDisposed);
        }
    }

    public void Event(IDisposable? handle, string name, string? callerName, int lineNumber)
    {
        if (handle is Activity activity)
        {
            activity.AddEvent(new ActivityEvent(name));
        }
    }
    
    public void Event(string name, string? callerName, int lineNumber)
    {
        var activity = Activity.Current;
        activity?.AddEvent(new ActivityEvent(name));
    }
}

internal static class Perf
{
    public static SignalDiagnosticsTraceProvider DefaultTraceProvider { get; } = new();

    public static TraceActivity StartActivity(
        string? name = null,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0
        )
    {
        if (!Signals.Options.Logging.TraceEnabled)
            return new TraceActivity();

        var provider = Signals.Options.Logging.TraceProvider;
        if (provider is null)
            return new TraceActivity();

        var activity = provider.StartActivity(name, callerName, lineNumber);

        return new TraceActivity(provider, activity);
    }
    
    public static TraceActivity StartActivity(
        this SignalNode node,
        string? name = null,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0
        )
    {
        name ??= $".{callerName} ({node.Name})";
        // ReSharper disable once ExplicitCallerInfoArgument
        var activity = StartActivity(name, callerName, lineNumber);
        
        activity.AddInfo(node);

        return activity;
    }

    public static void Event(
        string name,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0
        )
    {
        var provider = Signals.Options.Logging.TraceProvider;
        provider?.Event(name, callerName, lineNumber);
    }

    public static void Event(
        this SignalNode node,
        string name,
        [CallerMemberName] string? callerName = null,
        [CallerLineNumber] int lineNumber = 0
        )
    {
        var provider = Signals.Options.Logging.TraceProvider;
        if (provider is not null)
        {
            name += $" ({node.Name})";
            provider.Event(name, callerName, lineNumber);
        }
    }
}

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