using BenchmarkDotNet.Attributes;
using SigSharp;

namespace Benchmarks;

 
[MemoryDiagnoser]
public class ComputationAccessBenchmarks
{
    private const int AccessIterations = 10_000;
    
    private static BenchCalc _calc = new();
    private int _init;
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _init = _calc.ByComputed + _calc.ByProperty;
    }
    
    [Benchmark]
    public void AccessByProperty_JustRead()
    {
        for (int i = 0; i < AccessIterations; i++)
        {
            _init = Math.Min(2, _calc.ByProperty);
        }
    }
    
    [Benchmark]
    public void AccessByComputed_JustRead()
    {
        for (int i = 0; i < AccessIterations; i++)
        {
            _init = Math.Min(2, _calc.ByComputed);
        }
    }
    
    [Benchmark]
    public void AccessByComputed_WithMutation()
    {
        for (int i = 0; i < AccessIterations; i++)
        {
            _calc.Signal.Value += _init;
            _init = Math.Min(2, _calc.ByComputed);
        }
    }
    
    [Benchmark]
    public void AccessByProperty_WithMutation()
    {
        for (int i = 0; i < AccessIterations; i++)
        {
            _calc.Signal.Value += _init;
            _init = Math.Min(2, _calc.ByProperty);
        }
    }

    private sealed class BenchCalc
    {
        public int ByProperty => this.Signal.Value;
        
        public int ByComputed => this.Computed(static c => c.Signal.Value);
        
        public Signal<int> Signal { get; } = new(1);
    }
}