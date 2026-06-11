using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public abstract class CollectionSignal<T, TCollection> : SignalNode, ICollection<T>, IReadOnlyCollection<T>
    where TCollection: ICollection<T>
{
    public int Count
    {
        get
        {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess
                    .Access(this.DisposedCapture, this)
                    .Count;
            }
            
            this.MarkTracked();
            this.RequestAccess();
            
            return this.BackingCollection.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            if (this.IsDisposed)
            {
                return DisposedSignalAccess
                    .Access(this.DisposedCapture, this)
                    .IsReadOnly;
            }
            
            this.MarkTracked();
            this.RequestAccess();
            
            return this.BackingCollection.IsReadOnly;
        }
    }

    public CollectionSignalOptions Options { get; }
    
    protected TCollection BackingCollection { get; private set; }

    protected DisposedSignalAccess.DisposedCapture<TCollection> DisposedCapture { get; set; }
    
    protected CollectionSignal(
        IEnumerable<T> initialValues,
        CollectionSignalOptions? opts = null,
        string? name = null
        ) : base(true, false, name ?? ConstructName())
    {
        this.Options = opts ?? CollectionSignalOptions.Defaults;

        if (this.Options.AccessStrategy == SignalAccessStrategy.Optimistic)
            throw new NotSupportedException("Optimistic strategy should not be used with collection signals");

        this.SetAccessStrategy(this.Options.AccessStrategy);
        
        // Justification: The alternative is to make it Lazy, but it would have too much overhead
        // ReSharper disable once VirtualMemberCallInConstructor
        this.BackingCollection = this.CreateBackingCollection(initialValues);
    }

    protected abstract TCollection CreateBackingCollection(IEnumerable<T> initialValues);

    protected override ValueTask DisposeAsyncCore()
    {
        var emptyCollection = this.Options.DisposedAccessStrategy switch
            {
                DisposedSignalAccess.Strategy.DefaultValue or DisposedSignalAccess.Strategy.DefaultScalar
                    => this.CreateDefaultEmptyBackingCollection(),
                _   => default!
            };

        this.DisposedCapture = DisposedSignalAccess.Capture(
            this.BackingCollection,
            emptyCollection,
            this,
            this.Options.DisposedAccessStrategy,
            TypeUtils.IsNullableByDefault<T>()
            );

        this.BackingCollection = default!;
        
        return base.DisposeAsyncCore();
    }
    
    public sealed override void MarkDirty() { }
    public sealed override void MarkPristine() { }

    protected virtual TCollection CreateDefaultEmptyBackingCollection()
    {
        return this.CreateBackingCollection([]);
    }

    

    public IEnumerator<T> GetEnumerator()
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .GetEnumerator();
        }

        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public void Set(IEnumerable<T> newValues)
    {
        ArgumentNullException.ThrowIfNull(newValues);
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && newValues.TryGetNonEnumeratedCount(out var count))
        {
            if (count == 0 && this.BackingCollection.Count == 0)
                return;
        }
        
        this.RequestUpdate();
        
        this.BackingCollection.Clear();
        newValues.ForEach(this.BackingCollection.Add);
        this.Changed();
    }
    
    public void Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && this.BackingCollection.Contains(item))
            return;
        
        this.RequestUpdate();

        if (!this.BackingCollection.Contains(item))
        {
            this.BackingCollection.Add(item);
            this.Changed();
        }
    }

    public void Add(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        this.CheckDisposed();

        if (this.Options.PrecheckUpdateRequests && items is ICollection<T> coll)
        {
            if (coll.Count <= 0)
                return;
            
            if (coll.All(d => this.BackingCollection.Contains(d)))
                return;
            
        }
        else if (items.TryGetNonEnumeratedCount(out var count) && count <= 0)
        {
            return;
        }
        
        this.RequestUpdate();
        
        var changed = false;
        
        foreach (var item in items)
        {
            if (!this.BackingCollection.Contains(item))
            {
                this.BackingCollection.Add(item);
                changed = true;
            }
        }

        if (changed)
        {
            this.Changed();
        }
    }
    
    public void Clear()
    {
        this.CheckDisposed();

        if (this.Options.PrecheckUpdateRequests && this.BackingCollection.Count <= 0)
            return;
        
        this.RequestUpdate();
        
        if (this.BackingCollection.Count <= 0)
            return;
        
        this.BackingCollection.Clear();
        this.Changed();
    }
    
    public bool Contains(T item)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .Contains(item);
        }

        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.Contains(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        
        if (this.IsDisposed)
        {
            DisposedSignalAccess
                .Access(this.DisposedCapture, this)
                .CopyTo(array, arrayIndex);
            
            return;
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        this.BackingCollection.CopyTo(array, arrayIndex);
    }
    
    public bool Remove(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        this.CheckDisposed();
        
        if (this.Options.PrecheckUpdateRequests && !this.BackingCollection.Contains(item))
            return false;
        
        this.RequestUpdate();
        
        var res = this.BackingCollection.Remove(item);
        if (res)
        {
            this.Changed();
        }

        return res;
    }

    private static string ConstructName()
    {
        var ck = ValueTuple.Create(typeof(T), typeof(TCollection));
        
        if (InternalCaches.CollectionNameCache.TryGetValue(ck, out var name))
            return name;

        name = $"CollectionSignal<{typeof(T).Name}, {typeof(TCollection).Name}>";
        
        InternalCaches.CollectionNameCache[ck] = name;

        return name;
    }
}
