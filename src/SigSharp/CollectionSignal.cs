using System.Collections;
using System.Collections.Generic;
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
            this.MarkTracked();
            this.RequestAccess();
            
            return this.BackingCollection.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            this.MarkTracked();
            this.RequestAccess();
            
            return this.BackingCollection.IsReadOnly;
        }
    }

    public CollectionSignalOptions Options { get; }
    
    protected TCollection BackingCollection { get; private set; }

    private DisposedSignalAccess.DisposedCapture<TCollection> _disposedCapture;
    
    protected CollectionSignal(
        IEnumerable<T> initialValues,
        CollectionSignalOptions? opts = null,
        string? name = null
        ) : base(true, false, name ?? $"CollectionSignal<{typeof(T).Name}, {typeof(TCollection).Name}>")
    {
        this.Options = opts ?? CollectionSignalOptions.Defaults;

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

        _disposedCapture = DisposedSignalAccess.Capture(
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
                .Access(_disposedCapture, this)
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
        this.CheckDisposed();
        
        this.RequestUpdate();
        
        this.BackingCollection.Clear();
        newValues.ForEach(this.BackingCollection.Add);
        this.Changed();
    }
    
    public void Add(T item)
    {
        this.CheckDisposed();
        
        this.RequestUpdate();
        
        this.BackingCollection.Add(item);
        this.Changed();
    }
    
    public void Clear()
    {
        this.CheckDisposed();
        
        this.RequestUpdate();
        
        this.BackingCollection.Clear();
        this.Changed();
    }
    
    public bool Contains(T item)
    {
        if (this.IsDisposed)
        {
            return DisposedSignalAccess
                .Access(_disposedCapture, this)
                .Contains(item);
        }

        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.Contains(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (this.IsDisposed)
        {
            DisposedSignalAccess
                .Access(_disposedCapture, this)
                .CopyTo(array, arrayIndex);
            
            return;
        }
        
        this.MarkTracked();
        this.RequestAccess();
        
        this.BackingCollection.CopyTo(array, arrayIndex);
    }
    
    public bool Remove(T item)
    {
        this.CheckDisposed();
        
        this.RequestUpdate();
        
        var res = this.BackingCollection.Remove(item);
        if (res)
        {
            this.Changed();
        }

        return res;
    }
}
