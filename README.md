SigSharp
==========

A computational dependency library for .Net inspired by Angular signals.

Supports `Signal`, `Computed`, `Effect` constructs and collection signals (`HashSetSignal<>`, `DictionarySignal<>`)
with full reference and lifecycle tracking, memoization.

SigSharp is intended to be used when you need complex calculation chains which are not deterministic or fast enough to be implemented with just using simple properties. 

Some example usage scenarios:
- Complex financial calculations
- Report data or model preparation
- Model based generators
- Complex state representations


Minimal usage example
---------------------

```bash
> dotnet add package SigSharp
```

```C#
using SigSharp;

var calculator = new InvoiceCalculator();

calculator.InvoiceLinePrices.Add(4);

Console.WriteLine($"Debug total: {calculator.InvoiceTotalDebug}");
Console.Out.Flush();

// second call to get property value will be memoized, not recomputed
Console.WriteLine($"Debug total: {calculator.InvoiceTotalDebug}");
Console.Out.Flush();

/* Output:
Total updating...
Debug total: 10
Total updated: 10
Debug total: 10
*/

class InvoiceCalculator
{
    public readonly HashSetSignal<double> InvoiceLinePrices = new([1, 2, 3]);

    public double InvoiceTotal => this.Computed(() => InvoiceLinePrices.Sum());
    
    public double InvoiceTotalDebug => this.Computed(() =>
    {
        Console.WriteLine("Total updating...");
        
        return InvoiceTotal;
    });

    public InvoiceCalculator()
    {
        this.Effect(() =>
        {
            Console.WriteLine($"Total updated: {InvoiceTotal}");
            Console.Out.Flush();
        });
    }
}


```

Other examples can be found in [Examples](./examples/SimpleDemo/Program.cs) folder

Features / Goals
--------

- Healthy balance between ease of use and performance
- Extensibility: letting you implement that last 5% you very much need for your project/usage
- Self-contained and AOT compatible.

Key concepts
------------

A `Signal` is a piece of data that can change, and those changes are tracked.
For example, tracked by a `ComputedSignal` which is a glorified memoized expression.
When the system calculates the value of that expression and touches any signal (including other computed signals),
it remembers to watch for changes of those signals. When any of the changes it marks itself dirty which indicates
that it needs to be recalculated next time someone asks for its value.

An `Effect` is a reaction to signal changes.

If you imagine all the dependencies between various signals, computations and effects,
then it forms a compute graph. Each of these elements form the nodes.
`Signal`, `HashSetSignal<>`, etc. are primitive `signal nodes`, while `ComputedSignal` and `Effect` are `reactive nodes`.

Reactive nodes must be part of a `SignalGroup`. If you are using the extension methods, the group management is handled automatically for you.
The target (this param) of the extension methods act as `anchors` or in other words `keys` for implicit signal groups.
They are linked via a weakmap.

Other notable features
--------------

1. `Untracked` support

In the example below the two public properties will be calculated once, because the reference to
the signal is explicitly untracked, so their value will never be dirty.
```C#
class Some 
{
    private Signal<int> dontReactToMe = new(0);
    public int CalcOnce => this.Computed(() => dontReactToMe.Untracked);
    public int CalcOnce2 => this.Computed(() => Signals.Untracked(() => dontReactToMe));
}

```

2. Effect suspensions

Suspensions are "async local", not global. They are useful when you want to batch together lot of changes.

```C#
public async Task Load() 
{
    await using var suspender = Signals.Suspend();
    // do tons of loading, setting signals, etc.
    
    // at the end of loading the disposal
    // of the suspender will resume the affected effects
}
```

3. Disposed value access handling

Signals can be configured how to behave after they are disposed. By default they try to "remember" their last scalar value.

4. Weak effect and weak computation support

When effects or computations have a reference to a state, that state can be a WeakRef, so
that the lifetime of these signals are tied to the WeakRef.
It should be rarely used, but useful in fire and forget scenarios.



Contributions
-------------

While the library is in an alpha state limited contributions will be accepted.


