# SigSharp

Reactive signals for .NET — automatic dependency tracking, lazy evaluation, no manual subscriptions.

A computational dependency library for .NET inspired by Angular signals.

**Alpha** &nbsp;·&nbsp; .NET 10 &nbsp;·&nbsp; Apache-2.0 &nbsp;·&nbsp; [NuGet](https://www.nuget.org/packages/SigSharp)

You write property expressions the same way you always would — the only change is wrapping them in `Computed()`. The library discovers dependencies automatically by observing which signals each expression reads; no need to wire up events or observers by hand. Signal changes propagate automatically through a dependency graph, recomputing only what is needed.

**When to reach for SigSharp:**
- **Expensive derived computations** that are read frequently but change infrequently — compiler/analyzer output, parsed models, heavy aggregations
- **Lazy resolvers** and **async lookups** — external service calls, database queries, or file reads that re-execute only when their reactive inputs change, not on every access
- **Calculation chains** where intermediate results should be cached and only recomputed when their specific inputs change
- **Rule or constraint evaluation** over reactive data — rules that re-run only when the data they inspect actually changes
- **Reactive pipelines** — filter → transform → generate workflows where each stage only reruns when its upstream changes
- **Complex financial calculations** or **pricing engines**
- Report and model data preparation
- Model-based generators
- Any state where manually wiring change propagation becomes painful

---

## Installation

```shell
dotnet add package SigSharp
```

---

## Quick Start

The three primitives are `Signal<T>`, `Computed`, and `Effect`. Inside a class, the `this.Computed()` and `this.Effect()` extension methods wire everything up automatically — no group management needed.

```csharp
using SigSharp;

var calculator = new InvoiceCalculator();

calculator.InvoiceLinePrices.Add(4);

Console.WriteLine($"Total: {calculator.InvoiceTotalDebug}");
Console.WriteLine($"Total: {calculator.InvoiceTotalDebug}"); // memoized — body does not run again
Console.Out.Flush();

/* Output:
Total updating...
Total updated: 10
Total: 10
Total: 10
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
        this.Effect(() => {
            Console.WriteLine($"Total updated: {InvoiceTotal}");
            Console.Out.Flush();
        });
    }
}
```

The second call to `InvoiceTotalDebug` returns the cached result — `Computed` only re-evaluates when a dependency has changed since the last read.

More examples are in the [examples](./examples/) folder.

---

## Features / Goals

- Healthy balance between ease of use and performance
- Extensibility: covers the common cases out of the box, with dedicated extension points so you can implement that last 5% specific to your project yourself
- Self-contained and AOT compatible.
- **Custom signal and effect types** — the architecture supports implementing your own signal and effect primitives; not yet stable but a first-class goal for release. Planned examples include:
  - a **lens signal**: a focused, bidirectional node over a structured source that isolates change propagation — if the field it watches hasn't changed, it stays pristine and shields its dependents from the update entirely
  - a **sequenced effect**: unlike the built-in debounced effect where only the latest state is processed, a sequenced effect observes every state transition in order — useful for audit logs, undo history, or event sourcing


## Key Concepts

A **Signal** is a piece of data that can change, and those changes are tracked by reactive nodes.

A **ComputedSignal** is a lazily-evaluated, memoized expression and a reactive signal. Every evaluation of its expression records which signals were read. If any of those signals change later, it is marked dirty and on the next call it recalculates its expression.

An **Effect** is a reaction to signal changes. It may have side-effects and by default re-runs automatically when any of its signal dependencies change.

If you imagine all the dependencies between signals, computations, and effects, they form a **compute graph**. `Signal`s are the leaf nodes; `ComputedSignal`s and `Effect`s are the reactive nodes that track their own dependencies at runtime.

Reactive nodes must belong to exactly one **SignalGroup**. The `this.Computed()` / `this.Effect()` extension methods create and manage groups implicitly. The calling instance serves as the **anchor** key via a weak map. When you need a group outside a class, create one explicitly:

```csharp
using var group = new SignalGroup();
var effect = group.Effect(() => Console.WriteLine($"Torque: {engine.CurrentTorque}"));
engine.AddGas();
effect.WaitIdle(); // <- good practice to wait for the effect to finish before the group is disposed
```

---

## Advanced Features

### Untracked access

Read a signal's value without registering a dependency. The enclosing computed will not re-run when that signal changes.

```csharp
class Some
{
    private Signal<int> _source = new(0);

    // Calculated once; never dirty because the read is untracked.
    public int CalcOnce  => this.Computed(() => _source.Untracked);
    public int CalcOnce2 => this.Computed(() => Signals.Untracked(() => _source));
}
```

### Effect suspension (batching)

Suspend effect execution while making many signal changes. Suspensions are async-local, not global, so they are safe in concurrent code.

```csharp
public async Task Load()
{
    await using var suspender = Signals.Suspend();
    // Set many signals; effects are held back until the suspender disposes.
}
```

### Disposed value handling

Signals can be configured to retain their last value after disposal, so downstream code continues to read a stable value after the owning object is gone.

### Weak effects and computations

Effects and computations can hold a `WeakRef` to their captured state, tying their lifetime to that object rather than keeping it alive. Useful in fire-and-forget scenarios where you do not want signals to prevent garbage collection.

---

## Contributing

SigSharp is in **alpha**. Available as a source-available project.

Pull requests are closed, but issues are welcome — especially those regarding overall behaviour or proven performance-related problems.

