using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SigSharp.Nodes;

namespace SigSharp.Utils.Perf;

internal static class Perf
{
    public static SignalDiagnosticsTraceProvider DefaultTraceProvider { get; } = new();
    public static SignalDiagnosticsMetricsProvider DefaultMetricsProvider { get; } = new();

    public static FrozenDictionary<string, PerfCounterType> WellKnownCounters { get; }
        = BuildWellKnownCounters();

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
    
    public static BoundCounter Count(SignalNode node, string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return new BoundCounter();
    
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        
        return new BoundCounter(provider, node, counterName);
    }

    public static BoundTiming MeasureTime(SignalNode node, string measurementName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return new BoundTiming();
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;

        return new BoundTiming(provider, node, measurementName);
    }

    public static void MonoIncrement(SignalNode node, string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.MonoIncrement(node, counterName);
    }

    public static void Increment(SignalNode node, string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.Increment(node, counterName);
    }

    public static void Decrement(SignalNode node, string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.Decrement(node, counterName);
    }
    
    public static BoundCounter Count(string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return new BoundCounter();
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        
        return new BoundCounter(provider, null, counterName);
    }
    
    public static BoundTiming MeasureTime(string measurementName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return new BoundTiming();
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;

        return new BoundTiming(provider, null, measurementName);
    }
    
    public static void MonoIncrement(string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.MonoIncrement(counterName);
    }

    public static void Increment(string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.Increment(counterName);
    }

    public static void Decrement(string counterName)
    {
        if (!Signals.Options.Logging.MetricsEnabled)
            return;
        
        var provider = Signals.Options.Logging.MetricsProvider ?? DefaultMetricsProvider;
        provider.Decrement(counterName);
    }

    internal static FrozenDictionary<string, PerfCounterType> BuildWellKnownCounters()
    {
        return new Dictionary<string, PerfCounterType>
            {
                { "signal.node.count", PerfCounterType.UpDownCounter },
                { "signal.computed.count", PerfCounterType.UpDownCounter },
                { "signal.effect.count", PerfCounterType.UpDownCounter },
                { "signal.tracker.active_count", PerfCounterType.UpDownCounter },
                
                { "signal.wait.global_idle", PerfCounterType.TimeMeasurer },
                
                { "signal.tracker.access.requests.all", PerfCounterType.MonoCounter },
                { "signal.tracker.access.requests.exclusive_locks", PerfCounterType.MonoCounter },
                { "signal.tracker.access.requests.preemptive_locks", PerfCounterType.MonoCounter },
                { "signal.tracker.access.requests.optimistic", PerfCounterType.MonoCounter },
                
                { "signal.tracker.update.requests.all", PerfCounterType.MonoCounter },
                { "signal.tracker.update.requests.exclusive_locks", PerfCounterType.MonoCounter },
                { "signal.tracker.update.requests.preemptive_locks", PerfCounterType.MonoCounter },
                { "signal.tracker.update.requests.optimistic", PerfCounterType.MonoCounter },
                
                { "signal.computed.reads.untracked", PerfCounterType.MonoCounter },
                { "signal.computed.reads.all", PerfCounterType.MonoCounter },
                { "signal.computed.reads.cached", PerfCounterType.MonoCounter },
                { "signal.computed.errors.deadlocks", PerfCounterType.MonoCounter },
                { "signal.computed.errors.preemptions", PerfCounterType.MonoCounter },
                
                { "signal.effect.run.errors.versionchanged", PerfCounterType.MonoCounter },
                { "signal.effect.run.errors.deadlocks", PerfCounterType.MonoCounter },
                { "signal.effect.run.errors.disposed", PerfCounterType.MonoCounter },
                { "signal.effect.run.errors.preemptions", PerfCounterType.MonoCounter },
                { "signal.effect.run.errors.unexpected", PerfCounterType.MonoCounter },
                
                
                { "signal.wait.access.acquire_latch", PerfCounterType.TimeMeasurer },
                { "signal.wait.update.wait_latch", PerfCounterType.TimeMeasurer },
                
                { "signal.wait.computed.update_lock", PerfCounterType.TimeMeasurer },
                { "signal.computed.run", PerfCounterType.TimeMeasurer },
                
                { "signal.effect.run", PerfCounterType.TimeMeasurer },

            }.ToFrozenDictionary();
    }
}

internal readonly struct BoundCounter : IDisposable
{
    private readonly ISignalMetricsProvider? _provider;
    private readonly SignalNode? _node;
    private readonly string? _name;
    
    public BoundCounter(ISignalMetricsProvider? provider, SignalNode? node, string name)
    {
        _provider = provider;
        _node = node;
        _name = name;

        if (_provider is not null)
        {
            if (node is null)
            {
                _provider.Increment(name);
            }
            else
            {
                _provider.Increment(node, name);
            }
        }
    }
    
    public void Dispose()
    {
        if (_provider is null || _name is null)
            return;
        
        if (_node is null)
        {
            _provider.Decrement(_name);
        }
        else
        {
            _provider.Decrement(_node, _name);
        }
    }
}

internal readonly struct BoundTiming : IDisposable
{
    private static readonly double TickRatio = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    
    private readonly ISignalMetricsProvider? _provider;
    private readonly SignalNode? _node;
    private readonly string? _name;
    
    private readonly long _startTime;

    public BoundTiming(ISignalMetricsProvider? provider, SignalNode? node, string name)
    {
        _provider = provider;
        _node = node;
        _name = name;

        if (provider is not null)
        {
            _startTime = Stopwatch.GetTimestamp();
        }
    }
    
    public void Dispose()
    {
        if (_provider is null || _name is null)
            return;
        
        var endTime = Stopwatch.GetTimestamp();
        var durationTicks = endTime - _startTime;
        
        if (durationTicks >= 1)
        {
            var ticks = (long)(TickRatio * durationTicks);
            var duration = new TimeSpan(ticks);

            if (_node is null)
            {
                _provider.MeasureTime(_name, duration);
            }
            else
            {
                _provider.MeasureTime(_node, _name, duration);
            }
        }
    }
}