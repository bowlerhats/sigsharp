using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SigSharp;

public class DictionarySignal<TKey, TValue>
    : CollectionSignal<KeyValuePair<TKey, TValue>, ConcurrentDictionary<TKey, TValue>>,
        IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public TValue this[TKey key]
    {
        get => this.BackingCollection[key];
        set => this.BackingCollection[key] = value;
    }

    public ICollection<TKey> Keys => this.BackingCollection.Keys;

    public ICollection<TValue> Values => this.BackingCollection.Values;
    
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.BackingCollection.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.BackingCollection.Values;
    
    public DictionarySignal(IEnumerable<KeyValuePair<TKey, TValue>> initialValues, CollectionSignalOptions opts = null)
        :base(initialValues, opts)
    {
    }
    
    protected override ConcurrentDictionary<TKey, TValue> CreateBackingCollection(
        IEnumerable<KeyValuePair<TKey, TValue>> initialValues)
    {
        return new ConcurrentDictionary<TKey, TValue>(initialValues);
    }
    
    public void Add(TKey key, TValue value)
    {
        if (!this.BackingCollection.TryAdd(key, value))
        {
            this.BackingCollection[key] = value;
        }
        
        this.Changed();
    }
    
    public bool ContainsKey(TKey key)
    {
        this.MarkTracked();
        
        return this.BackingCollection.ContainsKey(key);
    }
    
    public bool TryGetValue(TKey key, out TValue value)
    {
        this.MarkTracked();
        
        return this.BackingCollection.TryGetValue(key, out value);
    }

    public bool Remove(TKey key)
    {
        var res = this.BackingCollection.Remove(key, out _);
        if (res)
        {
            this.Changed();
        }

        return res;
    }
    
    bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
    {
        this.MarkTracked();
        
        return this.ContainsKey(key);
    }

    bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
    {
        this.MarkTracked();
        
        return this.TryGetValue(key, out value);
    }

    
}