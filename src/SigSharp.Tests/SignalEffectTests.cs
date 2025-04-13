using System.Threading;
using Shouldly;

// ReSharper disable AccessToDisposedClosure

namespace SigSharp.Tests;

public class SignalEffectTests
{
    [Test]
    public void Can_RunStaticEffect()
    {
        using SignalGroup group = new();
        int count = 0;
        using var effect = Signals.Effect(
            () => { count++; },
            group,
            opts: SignalEffectOptions.Defaults with { AutoStopWhenNoTrackedSignal = false }
            );
        effect.WaitIdle();
        count.ShouldBe(1, "because is not dormant");
        effect.RunImmediate();
        count.ShouldBe(2);
    }
    
    [Test]
    public void Can_RunStaticEffectDormant()
    {
        using SignalGroup group = new();
        
        int count = 0;
        using var effect = group.Effect(() => { count++; }, SignalEffectOptions.Defaults with { AutoSchedule = false });
        count.ShouldBe(0, "because is dormant");
        effect.RunImmediate();
        count.ShouldBe(1);
    }

    [Test]
    public void Can_Rerun_OnSignalChange()
    {
        using SignalGroup group = new();
        
        int count = 0;
        using var sig = new Signal<int>(1);

        using var effect = group.Effect(() => { count += sig.Value; });
        effect.WaitIdle();
        count.ShouldBe(1);
        
        sig.Set(2);
        effect.WaitIdle();
        
        count.ShouldBe(3);
    }
    
    [Test]
    public void Can_Rerun_OnSignalChangeMultiple()
    {
        using SignalGroup group = new();
        
        int count = 0;
        using var sig1 = new Signal<int>(1);
        using var sig2 = new Signal<int>(5);

        using var effect = group.Effect(() => { count += sig1.Value + sig2.Value; });
        effect.WaitIdle();
        count.ShouldBe(6);
        
        sig1.Set(2);
        sig2.Set(6);
        effect.WaitIdle();
        
        count.ShouldBe(14);
    }

    [Test]
    public void Can_Rerun_DeepChange()
    {
        using SignalGroup group = new();
        int count = 0;
        using var sig1 = new Signal<int>(1);
        using var sig2 = new Signal<int>(5);
        using var c1 = Signals.Computed(() => sig1.Value + 1, group);
        using var c2 = Signals.Computed(() => sig2.Value + 1, group);
        using var c3 = Signals.Computed(() => c1.Value + c2.Value, group);
        using var c4 = Signals.Computed(() => c3.Value + c1.Value + c2.Value, group);

        using var effect = Signals.Effect(() => { count += c4.Value; Thread.Sleep(10); }, group);
        effect.WaitIdle();
        count.ShouldBe(16);
        
        sig1.Set(2);
        effect.WaitIdle();
        
        count.ShouldBe(34);
    }

    [Test]
    public void Should_StopRunning_WhenDisposed()
    {
        using SignalGroup group = new();
        int count = 0;
        using var sig1 = new Signal<int>(1);
        
        var effect = group.Effect(() => { count += sig1.Value; });
        effect.WaitIdle();
        count.ShouldBe(1);
        
        sig1.Set(2);
        effect.WaitIdle();
        count.ShouldBe(3);
        effect.Dispose();
        
        count.ShouldBe(3);
        sig1.Set(3);
        
        Thread.Sleep(500);
        
        count.ShouldBe(3);
    }

    [Test]
    public void Should_RespectUntracked()
    {
        using SignalGroup group = new();
        
        using Signal<int> sig1 = new(1);
        using Signal<int> sig2 = new(2);
        int count = 0;

        using var effect = Signals.Effect(() => { count += sig1.Value + sig2.Untracked; }, group);
        effect.WaitIdle();
        count.ShouldBe(3);
        
        sig2.Set(5);
        effect.WaitIdle();
        
        count.ShouldBe(3);
    }
    
    [Test]
    public void Should_Rerun_WhenSignalChanged()
    {
        using SignalGroup group = new();
        int count = 0;
        using Signal<int> sig1 = new(1);
        using Signal<int> sig2 = new(2);
        using ComputedSignal<int> c1 = Signals.Computed(() => sig1.Value + sig2.Value, group);

        using var effect = Signals.Effect(
            () =>
                {
                    count++;
                    if (c1.Value <= 3)
                    {
                        sig1.Set(2);
                    }
                },
            group
            );
        
        effect.WaitIdle();
        
        count.ShouldBe(2);
        
        c1.IsDirty.ShouldBeFalse();
        effect.IsDirty.ShouldBeFalse();
        c1.Value.ShouldBe(4);
        
        
    }
}