using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using SigSharp.Nodes;

namespace SigSharp.Utils.Perf;

public enum PerfCounterType
{
    MonoCounter,
    UpDownCounter,
    TimeMeasurer
}

public interface ISignalMetricsProvider
{
    void Increment(SignalNode node, string counterName);
    void Increment(string counterName);
    void Decrement(SignalNode node, string counterName);
    void Decrement(string counterName);

    void MeasureTime(SignalNode node, string measurementName, TimeSpan elapsed);
    void MeasureTime(string measurementName, TimeSpan elapsed);

    void MonoIncrement(SignalNode node, string counterName);
    void MonoIncrement(string counterName);

}

public sealed class SignalDiagnosticsMetricsProvider : ISignalMetricsProvider
{
    private readonly FrozenDictionary<string, Counter<long>> _monoCounters;
    private readonly FrozenDictionary<string, UpDownCounter<long>> _counters;
    private readonly FrozenDictionary<string, Histogram<long>> _histograms;

    public Meter Meter { get; } = new("SigSharp.Core");
    
    public SignalDiagnosticsMetricsProvider()
    {
        Dictionary<string, Counter<long>> monoCounters = [];
        Dictionary<string, UpDownCounter<long>> counters = [];
        Dictionary<string, Histogram<long>> histograms = [];
        
        foreach (var (counterName, cType) in Perf.BuildWellKnownCounters())
        {
            switch (cType)
            {
                case PerfCounterType.MonoCounter:
                    monoCounters.Add(counterName, this.Meter.CreateCounter<long>(counterName));
                    break;
                case PerfCounterType.UpDownCounter:
                    counters.Add(counterName, this.Meter.CreateUpDownCounter<long>(counterName));
                    break;
                case PerfCounterType.TimeMeasurer:
                    histograms.Add(counterName, this.Meter.CreateHistogram<long>(counterName));
                    break;
                default:                            throw new ArgumentOutOfRangeException();
            }
        }

        _monoCounters = monoCounters.ToFrozenDictionary();
        _counters = counters.ToFrozenDictionary();
        _histograms = histograms.ToFrozenDictionary();
    }

    public void Increment(SignalNode node, string counterName)
    {
        var counter = this.FindCounter(counterName);
        counter?.Add(1, new KeyValuePair<string, object?>("signal.node.name", node.Name));
    }
    
    public void Increment(string counterName)
    {
        var counter = this.FindCounter(counterName);
        counter?.Add(1);
    }
    
    public void Decrement(SignalNode node, string counterName)
    {
        var counter = this.FindCounter(counterName);
        counter?.Add(-1, new KeyValuePair<string, object?>("signal.node.name", node.Name));
    }
    
    public void Decrement(string counterName)
    {
        var counter = this.FindCounter(counterName);
        counter?.Add(-1);
    }
    
    public void MeasureTime(SignalNode node, string measurementName, TimeSpan elapsed)
    {
        var histogram = this.FindHistogram(measurementName);
        histogram?.Record(elapsed.Microseconds, new KeyValuePair<string, object?>("signal.node.name", node.Name));
        
    }
    public void MeasureTime(string measurementName, TimeSpan elapsed)
    {
        var histogram = this.FindHistogram(measurementName);
        histogram?.Record(elapsed.Microseconds);
    }
    
    public void MonoIncrement(SignalNode node, string counterName)
    {
        var counter = this.FindMonoCounter(counterName);
        counter?.Add(1, new KeyValuePair<string, object?>("signal.node.name", node.Name));
    }
    
    public void MonoIncrement(string counterName)
    {
        var counter = this.FindMonoCounter(counterName);
        counter?.Add(1);
    }

    private UpDownCounter<long>? FindCounter(string name)
    {
        return _counters.GetValueOrDefault(name);
    }

    private Counter<long>? FindMonoCounter(string name)
    {
        return _monoCounters.GetValueOrDefault(name);
    }

    private Histogram<long>? FindHistogram(string name)
    {
        return _histograms.GetValueOrDefault(name);
    }
}