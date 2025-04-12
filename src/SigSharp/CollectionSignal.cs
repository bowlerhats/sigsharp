using System.Collections;
using System.Collections.Generic;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public abstract class CollectionSignal<T, TCollection> : SignalNode, ICollection<T>, IReadOnlyCollection<T>
    where TCollection: ICollection<T>
{
    public int Count => this.BackingCollection.Count;
    public bool IsReadOnly => this.BackingCollection.IsReadOnly;

    public CollectionSignalOptions Options { get; }
    public TCollection BackingCollection { get; }
    
    protected CollectionSignal(IEnumerable<T> initialValues, CollectionSignalOptions opts = null, string name = null)
        : base(true, name)
    {
        this.Options = opts ?? CollectionSignalOptions.Defaults;
        
        //initialValues.ForEach(this.BackingCollection.Add);
        
        // ReSharper disable once VirtualMemberCallInConstructor
        this.BackingCollection = this.CreateBackingCollection(initialValues);
    }

    protected abstract TCollection CreateBackingCollection(IEnumerable<T> initialValues);
    
    public IEnumerator<T> GetEnumerator()
    {
        this.MarkTracked();
        
        return this.BackingCollection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public void Set(IEnumerable<T> newValues)
    {
        this.BackingCollection.Clear();
        newValues.ForEach(this.BackingCollection.Add);
        this.Changed();
    }
    
    public void Add(T item)
    {
        this.BackingCollection.Add(item);
        this.Changed();
    }
    
    public void Clear()
    {
        this.BackingCollection.Clear();
        this.Changed();
    }
    
    public bool Contains(T item)
    {
        this.MarkTracked();
        
        return this.BackingCollection.Contains(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        this.MarkTracked();
        
        this.BackingCollection.CopyTo(array, arrayIndex);
    }
    
    public bool Remove(T item)
    {
        var res = this.BackingCollection.Remove(item);
        if (res)
        {
            this.Changed();
        }

        return res;
    }
}

// public abstract class CollectionSignal2<T, TCollection> : SignalNode, ICollection<T>, IReadOnlyCollection<T>
//     where TCollection: ICollection<T>, new()
// {
//     public int Count => this.BackingCollection.Count;
//     public bool IsReadOnly => this.BackingCollection.IsReadOnly;
//
//     public CollectionSignalOptions Options { get; }
//     public TCollection BackingCollection { get; } = [];
//     
//     protected CollectionSignal(IEnumerable<T> initialValues, CollectionSignalOptions opts = null)
//         : base(true)
//     {
//         this.Options = opts ?? CollectionSignalOptions.Defaults;
//         
//         initialValues.ForEach(this.BackingCollection.Add);
//     }
//     
//     public IEnumerator<T> GetEnumerator()
//     {
//         this.MarkTracked();
//         
//         return this.BackingCollection.GetEnumerator();
//     }
//
//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return this.GetEnumerator();
//     }
//
//     public void Set(IEnumerable<T> newValues)
//     {
//         this.BackingCollection.Clear();
//         newValues.ForEach(this.BackingCollection.Add);
//         this.Changed();
//     }
//     
//     public void Add(T item)
//     {
//         this.BackingCollection.Add(item);
//         this.Changed();
//     }
//     
//     public void Clear()
//     {
//         this.BackingCollection.Clear();
//         this.Changed();
//     }
//     
//     public bool Contains(T item)
//     {
//         this.MarkTracked();
//         
//         return this.BackingCollection.Contains(item);
//     }
//     
//     public void CopyTo(T[] array, int arrayIndex)
//     {
//         this.MarkTracked();
//         
//         this.BackingCollection.CopyTo(array, arrayIndex);
//     }
//     
//     public bool Remove(T item)
//     {
//         var res = this.BackingCollection.Remove(item);
//         if (res)
//         {
//             this.Changed();
//         }
//
//         return res;
//     }
// }