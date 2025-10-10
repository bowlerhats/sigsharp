using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SigSharp;

public class DictionarySignal<TKey, TValue>
    : CollectionSignal<KeyValuePair<TKey, TValue>, ConcurrentDictionary<TKey, TValue>>,
        IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    public TValue this[TKey key]
    {
        get { this.MarkTracked(); this.RequestAccess(); return this.BackingCollection[key]; }
        set {
            this.RequestUpdate();
            
            if (this.BackingCollection.TryUpdate(key, value, value)
                || this.BackingCollection.TryAdd(key, value))
            {
                this.Changed();
            }
        }
    }

    public ICollection<TKey> Keys
    {
        get { this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Keys; }
    }

    public ICollection<TValue> Values
    {
        get { this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Values; }
    }

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
    {
        get { this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Keys; }
    }

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
    {
        get { this.MarkTracked(); this.RequestAccess(); return this.BackingCollection.Values; }
    }

    public DictionarySignal(IEnumerable<KeyValuePair<TKey, TValue>> initialValues, CollectionSignalOptions? opts = null, string? name = null)
        : base(initialValues, opts, name ?? $"DictionarySignal<{typeof(TKey).Name}, {typeof(TValue).Name}>")
    {
    }

    public DictionarySignal(CollectionSignalOptions? opts = null, string? name = null)
        : this([], opts, name)
    {
    }

    protected override ConcurrentDictionary<TKey, TValue> CreateBackingCollection(
        IEnumerable<KeyValuePair<TKey, TValue>> initialValues)
    {
        return new ConcurrentDictionary<TKey, TValue>(initialValues);
    }
    
    public void Add(TKey key, TValue value)
    {
        this.RequestUpdate();
        
        if (!this.BackingCollection.TryAdd(key, value))
        {
            this.BackingCollection[key] = value;
            
            this.Changed();
        }
    }
    
    public bool ContainsKey(TKey key)
    {
        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.ContainsKey(key);
    }
    
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        this.MarkTracked();
        this.RequestAccess();
        
        return this.BackingCollection.TryGetValue(key, out value);
    }

    public bool Remove(TKey key)
    {
        this.RequestUpdate();
        
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
        this.RequestAccess();
        
        return this.ContainsKey(key);
    }

    bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        this.MarkTracked();
        this.RequestAccess();
        
        return this.TryGetValue(key, out value);
    }
}