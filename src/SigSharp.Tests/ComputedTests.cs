using System;
using Shouldly;

// ReSharper disable AccessToDisposedClosure

namespace SigSharp.Tests;

[TestFixture]
public class ComputedTests
{
    [Test]
    public void Can_ComputeStatic()
    {
        using var group = new SignalGroup();
        using var computed = Signals.Computed(() => 1, group);
        computed.HasTracking.ShouldBeFalse();
        computed.Value.ShouldBe(1);
        computed.Value.ShouldBe(1);
    }

    [Test]
    public void Should_Recompute_WithoutSignals()
    {
        using var group = new SignalGroup();
        var count = 0;
        using var computed = Signals.Computed(() => ++count, group);
        computed.HasTracking.ShouldBeFalse();
        computed.Value.ShouldBe(1);
        computed.HasTracking.ShouldBeFalse();
        computed.Value.ShouldBe(2);
        count.ShouldBe(2);
    }

    [Test]
    public void Can_Compute_SimpleSignals()
    {
        using SignalGroup group = new();
        using Signal<int> sig1 = new(1);
        using var computed = Signals.Computed(() => sig1, group);
        computed.HasTracking.ShouldBeFalse();
        computed.Value.ShouldBe(1);
        computed.HasTracking.ShouldBeTrue();
    }
    
    [Test]
    public void Can_Compute_SimpleChangingSignals()
    {
        using SignalGroup group = new();
        Signal<int> sig1 = new(1);
        using var computed = Signals.Computed(() => sig1, group);
        computed.HasTracking.ShouldBeFalse();
        
        computed.Value.ShouldBe(1);
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeFalse();
        
        sig1.Set(2);
        
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeTrue();
        
        computed.Value.ShouldBe(2);
        computed.IsDirty.ShouldBeFalse();
    }
    
    [Test]
    public void Can_Compute_SimpleChangingSignals2()
    {
        using SignalGroup group = new();
        var sig1 = new Signal<int>(1);
        var sig2 = new Signal<int>(10);
        using var computed = Signals.Computed(() => sig1.Value + sig2.Value, group);
        computed.HasTracking.ShouldBeFalse();
        
        computed.Value.ShouldBe(11);
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeFalse();
        
        sig1.Set(2);
        
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeTrue();
        
        computed.Value.ShouldBe(12);
        computed.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void Can_Compute_Combined()
    {
        using SignalGroup group = new();
        var sig1 = new Signal<int>(1);
        var sig2 = new Signal<int>(10);
        var c1 = Signals.Computed(() => sig1.Value + 1, group);
        var c2 = Signals.Computed(() => sig2.Value + 10, group);
        var computed = Signals.Computed(() => c1.Value + c2.Value, group);
        computed.Value.ShouldBe(22);
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeFalse();
        
        computed.Value.ShouldBe(22);
        computed.HasTracking.ShouldBeTrue();
        computed.IsDirty.ShouldBeFalse();
        
        sig1.Set(2);
        
        computed.IsDirty.ShouldBeTrue();
        computed.Value.ShouldBe(23);
        computed.IsDirty.ShouldBeFalse();
    }
    
    [Test]
    public void Can_Observe_Combined()
    {
        using SignalGroup group = new();
        var sig1 = new Signal<int>(1);
        var sig2 = new Signal<int>(10);
        var c1 = Signals.Computed(() => sig1.Value + 1, group);
        var c2 = Signals.Computed(() => sig2.Value + 10, group);
        using var computed = Signals.Computed(() => c1.Value + c2.Value, group);

        int observed = 0;
        computed.AsObservable().Subscribe(d => observed = d);
        
        computed.Value.ShouldBe(22);
        observed.ShouldBe(22);
        
        sig1.Set(2);
        
        computed.Value.ShouldBe(23);
        observed.ShouldBe(23);
    }

    [Test]
    public void CanNot_SetSignals_WhenComputing()
    {
        using SignalGroup group = new();
        var sig1 = new Signal<int>(1);
        var computed = Signals.Computed(() => { sig1.Set(3); return 3; }, group);
        Assert.Throws<SignalException>(() => computed.Update());
    }

    [Test]
    public void CanNot_Compute_WhenDisposed()
    {
        using SignalGroup group = new();
        var sig1 = new Signal<int>(1,
            SignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.Throw }
            );
        var sig2 = new Signal<int>(10);
        var c1 = Signals.Computed(() => sig1.Value + 1, group,
            ComputedSignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.Throw }
        );
        var c2 = Signals.Computed(() => sig2.Value + 10, group);
        var computed = Signals.Computed(() => c1.Value + c2.Value, group);
        computed.Value.ShouldBe(22);
        
        sig1.Dispose();
        
        c1.IsDirty.ShouldBe(true);
        c2.IsDirty.ShouldBe(false);
        computed.IsDirty.ShouldBe(true);
        
        Assert.Throws<ObjectDisposedException>(() => computed.Value.ShouldBe(22));
        
        computed.IsDirty.ShouldBe(true);
        
        c1.Dispose();
        
        computed.IsDirty.ShouldBe(true);
        
        Assert.Throws<ObjectDisposedException>(() => computed.Update());

    }
    
    [Test]
    public void Can_Compute_EvenDisposed()
    {
        SignalGroup group = new();
        Signal<int> sig1 = new(1);
        sig1.Options.DisposedAccessStrategy.ShouldBe(DisposedSignalAccess.Strategy.LastScalar);
        
        Signal<int> sig2 = new(10);
        var c1 = Signals.Computed(() => sig1.Value + 1, group);
        c1.Options.DisposedAccessStrategy.ShouldBe(DisposedSignalAccess.Strategy.LastScalar);
        
        var c2 = group.Computed(() => sig2.Value + 10);
        var computed = Signals.Computed(() => c1.Value + c2, group);
        computed.Value.ShouldBe(22);
        
        sig1.Dispose();
        
        computed.Value.ShouldBe(22, "because sig1's last is 1");
        
        c1.Dispose();
        
        computed.Value.ShouldBe(22, "because c1 with sig1=0 is constant 1");
    }
    
    [Test]
    public void Can_Compute_EvenDisposed_UsingDefault()
    {
        SignalGroup group = new();
        Signal<int> sig1 = new(1,
            SignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.DefaultScalar }
            );
        Signal<int> sig2 = new(10);
        var c1 = Signals.Computed(() => sig1.Value + 1, group,
            ComputedSignalOptions.Defaults with
            {
                DisposedAccessStrategy = DisposedSignalAccess.Strategy.DefaultScalar
            });
        
        var c2 = group.Computed(() => sig2.Value + 10);
        var computed = Signals.Computed(() => c1.Value + c2, group);
        computed.Value.ShouldBe(22);
        
        sig1.Dispose();
        
        computed.Value.ShouldBe(21, "because sig1's default is 0");
        
        c1.Dispose();
        
        computed.Value.ShouldBe(20, "because c1 with sig1=0 is constant 1");
    }
    
    // [Test]
    // public void Can_Compute_EvenDisposed_UsingLast()
    // {
    //     SignalGroup group = new(SignalGroupOptions.Defaults with{ AllowsDisposedTracking = true });
    //     Signal<int> sig1 = new(1,
    //         SignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.LastScalar }
    //         );
    //     Signal<int> sig2 = new(10);
    //     var c1 = Signals.Computed(() => sig1.Value + 1, group,
    //         ComputedSignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.LastScalar }
    //         );
    //     var c2 = group.Computed(() => sig2.Value + 10);
    //     var computed = Signals.Computed(() => c1.Value + c2, group);
    //     computed.Value.ShouldBe(22);
    //     
    //     sig1.Dispose();
    //     
    //     computed.Value.ShouldBe(22, "because sig1's last is 1");
    //     
    //     c1.Dispose();
    //     
    //     computed.Value.ShouldBe(22, "because c1 with sig1=1 is constant 2");
    // }

    [Test]
    public void Can_Track_Conditionally()
    {
        using SignalGroup group = new();
        int count = 0;
        var sig1 = new Signal<int>(1);
        // ReSharper disable once AccessToModifiedClosure
        var computed = Signals.Computed(() => ++count <= 1 ? (sig1.Value + 1) : 20, group);
        computed.Value.ShouldBe(2, "because sig1 branch");
        computed.HasTracking.ShouldBeTrue();
        computed.Value.ShouldBe(2, "because memoized");
        computed.HasTracking.ShouldBeTrue();
        computed.Update().ShouldBe(20, "because recalced without sig1");
        
        count = 0;
        computed.Update().ShouldBe(2, "because recalced with sig1");
        
        sig1.Set(5);
        count = 0;
        computed.Value.ShouldBe(6, "because sig1 changed");
        computed.Update().ShouldBe(20, "becase recalced without sig1");
        
        computed.Value.ShouldBe(20, "because recalced without sig1");
        count = 0;
        computed.Update().ShouldBe(6, "because recalced sig1");
        sig1.Set(10);
        computed.Value.ShouldBe(20, "because dirty recalced sig1, but not anymore");
    }

    [Test]
    public void Can_Anchor()
    {
        var test = new TestClass();
        test.S1.Value.ShouldBe(2);
        
        test.C2.Value.ShouldBe(7);
        test.C1.ShouldBe(2);
        test.C1.ShouldBe(2);
        
        test.C3.ShouldBe(17);

        // on class "test" with name of "Can_Anchor" !!!
        var computed1 = test.Computed(() => test.S1.Value + 100);
        
        var computed2 = test.Computed(() => test.S1.Value + 120, name: "a2");
        
        computed1.ShouldNotBe(computed2);
        
        computed1.ShouldBe(102);
        computed2.ShouldBe(122);
        test.C1.ShouldBe(2);
        test.C1.ShouldBe(2);
        test.C2.Value.ShouldBe(7);
        test.C2.Update().ShouldBe(7);

        using SignalGroup group = new(); 

        var computed3 = Signals.Computed(() => test.S1.Value + 1000, group);
        var computed4 = Signals.Computed(() => test.S1.Value + 1000, group);
        ReferenceEquals(computed3, computed4).ShouldBeFalse();
    }

    private sealed class TestClass
    {
        public Signal<int> S1 { get; } = new(2);
        
        public int C1 => this.Computed(() => this.S1);

        public ComputedSignal<int> C2
            => Signals.Computed(
                () => this.S1.Value + this.C1 + 3,
                SignalGroup.Of(this, SignalGroupOptions.Defaults)
            );
        
        public int C3 => this.Computed(() => this.C2.Value + 10);
    }
}