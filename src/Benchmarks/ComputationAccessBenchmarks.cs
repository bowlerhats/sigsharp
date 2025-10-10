using BenchmarkDotNet.Attributes;
using SigSharp;

namespace Benchmarks;

 
[MemoryDiagnoser]
public class ComputationAccessBenchmarks
{
    private const int AccessIterations = 10_000;
    
    private static BenchCalc _calc = new();
    private int _init;

    private AsyncLocal<List<int>> _asyncLocal = new();
    private List<int> _list = []; 
    
    [GlobalSetup]
    public void GlobalSetup()
    {
        _init = _calc.ByComputed + _calc.ByProperty;
    }

    // [Benchmark]
    // public void Access_AsyncLocal()
    // {
    //     // To prove that majority of allocations are done by asynclocal, and not the lib
    //     for (var i = 0; i < AccessIterations; i++)
    //     {
    //         _asyncLocal.Value = _list;
    //         _asyncLocal.Value = null!;
    //     }
    // }
    //
    // [Benchmark]
    // public void AccessByProperty_JustRead()
    // {
    //     for (var i = 0; i < AccessIterations; i++)
    //     {
    //         _init = Math.Min(2, _calc.ByProperty);
    //     }
    // }
    //
    // [Benchmark]
    // public void AccessByComputed_JustRead()
    // {
    //     for (var i = 0; i < AccessIterations; i++)
    //     {
    //         _init = Math.Min(2, _calc.ByComputed);
    //     }
    // }
    //
    // [Benchmark]
    // public void AccessByComputed_WithMutation()
    // {
    //     for (var i = 0; i < AccessIterations; i++)
    //     {
    //         _calc.Signal.Value += _init;
    //         _init = Math.Min(2, _calc.ByComputed);
    //     }
    // }
    
    [Benchmark]
    public void AccessByProperty_WithMutation()
    {
        for (var i = 0; i < AccessIterations; i++)
        {
            _calc.Signal.Value += _init;
            _init = Math.Min(2, _calc.ByProperty);
        }
    }

    private sealed class BenchCalc
    {
        // Use explicit group to avoid anchor lookup
        public SignalGroup Group { get; } = new();
        
        public int ByProperty => this.Signal.Value;
        
        public int ByComputed => this.Group.Computed(this, static c => c.Signal.Value
            //, opts => opts with { AccessStrategy = SignalAccessStrategy.Unrestricted }
            );
        //public int ByComputed => this.Group.Computed(this, static c => c.Signal.Value, ComputedSignalOptions.Defaults with{ AccessStrategy = SignalAccessStrategy.PreemptiveLock });
        
        public Signal<int> Signal { get; } = new(1, SignalOptions.Defaults with { AccessStrategy = SignalAccessStrategy.Unrestricted });
        //public Signal<int> Signal { get; } = new(1, SignalOptions.Defaults with { AccessStrategy = SignalAccessStrategy.PreemptiveLock });
    }
}