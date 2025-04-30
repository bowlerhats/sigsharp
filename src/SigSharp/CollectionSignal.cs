using System.Collections;
using System.Collections.Generic;
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
            return this.BackingCollection.Count;
        }
    }

    public bool IsReadOnly
    {
        get
        {
            this.MarkTracked();
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
        ) : base(true, name)
    {
        this.Options = opts ?? CollectionSignalOptions.Defaults;
        
        // Justification: The alternative is to make it Lazy, but it would have too much overhead
        // ReSharper disable once VirtualMemberCallInConstructor
        this.BackingCollection = this.CreateBackingCollection(initialValues);
    }

    protected abstract TCollection CreateBackingCollection(IEnumerable<T> initialValues);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposedCapture = DisposedSignalAccess.Capture(
                this.BackingCollection,
                this,
                this.Options.DisposedAccessStrategy,
                TypeUtils.IsNullableByDefault<T>()
                );

            this.BackingCollection = default!;
        }
        
        base.Dispose(disposing);
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
        
        return this.BackingCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public void Set(IEnumerable<T> newValues)
    {
        this.CheckDisposed();
        
        this.BackingCollection.Clear();
        newValues.ForEach(this.BackingCollection.Add);
        this.Changed();
    }
    
    public void Add(T item)
    {
        this.CheckDisposed();
        
        this.BackingCollection.Add(item);
        this.Changed();
    }
    
    public void Clear()
    {
        this.CheckDisposed();
        
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
        
        this.BackingCollection.CopyTo(array, arrayIndex);
    }
    
    public bool Remove(T item)
    {
        this.CheckDisposed();
        
        var res = this.BackingCollection.Remove(item);
        if (res)
        {
            this.Changed();
        }

        return res;
    }
}
