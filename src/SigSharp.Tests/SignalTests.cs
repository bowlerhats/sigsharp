using System;
using System.Collections.Generic;
using Shouldly;

namespace SigSharp.Tests;

public class SignalTests
{
    [Test]
    public void Can_CreateSignal()
    {
        using Signal<int> signal = new(5);
        signal.Value.ShouldBe(5);
    }
    
    [Test]
    public void Can_SetSignal()
    {
        using Signal<int> signal = new(5);
        signal.Value.ShouldBe(5);
        signal.Set(10);
        signal.Value.ShouldBe(10);
    }

    [Test]
    public void Can_ReadLastScalar_AfterDispose()
    {
        using Signal<int> signal = new(5);
        signal.Options.DisposedAccessStrategy.ShouldBe(DisposedSignalAccess.Strategy.LastScalarOrDefault);
        signal.Value.ShouldBe(5);
        
        // ReSharper disable once DisposeOnUsingVariable
        signal.Dispose();
        
        signal.Value.ShouldBe(5);
    }
    
    [Test]
    public void CanNot_ReadLastScalar_AfterDispose_NullableStruct()
    {
        using Signal<int?> signal = new(5, SignalOptions.Defaults with { DisposedAccessStrategy = DisposedSignalAccess.Strategy.LastScalar });
        signal.Options.DisposedAccessStrategy.ShouldBe(DisposedSignalAccess.Strategy.LastScalar);
        signal.Value.ShouldBe(5);
        
        // ReSharper disable once DisposeOnUsingVariable
        signal.Dispose();
        
        Assert.Throws<SignalDisposedException>(() => signal.Value.ShouldBe(5));
    }
    
    [Test]
    public void Can_ReadLastScalar_AfterDispose_WhenItIsString()
    {
        using Signal<string> signal = new("test");
        signal.Value.ShouldBe("test");
        
        // ReSharper disable once DisposeOnUsingVariable
        signal.Dispose();
        
        signal.Value.ShouldBe("test");
    }
    
    [Test]
    public void CanNot_ReadLastScalar_AfterDispose_WhenNullable()
    {
        object testObject = new();
        
        using Signal<object> signal = new(testObject, SignalOptions.Defaults with{ DisposedAccessStrategy = DisposedSignalAccess.Strategy.LastScalar });
        signal.Value.ShouldBe(testObject);
        
        // ReSharper disable once DisposeOnUsingVariable
        signal.Dispose();
        
        Assert.Throws<SignalDisposedException>(() => signal.Value.ShouldBe(testObject));
    }

    [Test]
    public void CanNot_Set_AfterDispose()
    {
        Signal<int> signal = new(5);
        signal.Dispose();
        Assert.Throws<SignalDisposedException>(() => signal.Set(1));
    }

    [Test]
    public void Can_Observe()
    {
        List<int> observed = [];
        
        using Signal<int> signal = new(5);
        signal.AsObservable().Subscribe(v => observed.Add(v));
        signal.Set(10);
        
        observed.ShouldBe([5, 10]);
    }

    [Test]
    public void Should_CompleteObserve_OnDispose()
    {
        var completed = false;
        Signal<int> signal = new(5);
        signal.AsObservable().Subscribe(_ => { }, _ => { }, () => completed = true);
        signal.Dispose();
        completed.ShouldBeTrue();
    }
}