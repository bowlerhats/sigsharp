// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Runtime;
// using System.Threading;
//
// namespace SigSharp.Utils;
//
// internal sealed class WeakSet<T> : IDisposable
//     where T: class
// {
//     public bool IsEmpty => _count == 0;
//     public bool HasAny => !this.IsEmpty;
//     
//     private readonly Lock _lock = new();
//     private readonly EqualityComparer<T> _comparer;
//     
//     private Slot[] _slots;
//     private int _count;
//     
//     private bool _disposed;
//     
//     public WeakSet(int initialCapacity, EqualityComparer<T>? comparer = null)
//     {
//         var capacity = Math.Clamp(initialCapacity, 16, 1024);
//         
//         _slots = new Slot[capacity];
//
//         _comparer = comparer ?? EqualityComparer<T>.Default;
//     }
//     
//     public WeakSet(EqualityComparer<T>? comparer = null) : this(32, comparer) { }
//
//     ~WeakSet()
//     {
//         this.Dispose();
//     }
//     
//     public void Dispose()
//     {
//         var wasDisposed = Interlocked.CompareExchange(ref _disposed, true, false); 
//         if (!wasDisposed && _disposed)
//         {
//             this.Clear(false);
//             _slots = [];
//         }
//         
//         GC.SuppressFinalize(this);
//     }
//
//     public Enumerator GetEnumerator()
//     {
//         return new Enumerator(this);
//     }
//     
//     public bool Add(T item)
//     {
//         this.CheckDisposed();
//
//         lock (_lock)
//         {
//             var freeIndex = -1;
//             for (var i = 0; i < _slots.Length; i++)
//             {
//                 if (_slots[i].IsOccupied)
//                 {
//                     if (_slots[i].Handle.IsAllocated)
//                     {
//                         if (_slots[i].Handle.Target is not T hItem)
//                         {
//                             _slots[i].Reset();
//                         } else if (_comparer.Equals(item, hItem))
//                         {
//                             return false;
//                         }
//                     }
//                 } else if (freeIndex < 0)
//                 {
//                     freeIndex = i;
//                 }
//                 
//             }
//
//             if (freeIndex < 0)
//             {
//                 freeIndex = _slots.Length;
//                 
//                 var newSlots = new Slot[_slots.Length * 2];
//                 for (var i = 0; i < _slots.Length; i++)
//                 {
//                     if (_slots[i].Handle.IsAllocated)
//                     {
//                         var target = _slots[i].Handle.Target;
//                         _slots[i].Reset();
//
//                         if (target is not null)
//                         {
//                             newSlots[i] = new Slot(new DependentHandle(target, this), true);
//                         }
//                     }
//                 }
//
//                 _slots = newSlots;
//             }
//             
//             
//             _slots[freeIndex].Capture(item, this);
//             _count++;
//         }
//
//         return true;
//     }
//
//     public bool Remove(T item)
//     {
//         this.CheckDisposed();
//
//         lock (_lock)
//         {
//             if (this.IsEmpty)
//                 return false;
//             
//             for (var i = 0; i < _slots.Length; i++)
//             {
//                 if (_slots[i].Handle.IsAllocated && _slots[i].Handle.Target is T hItem)
//                 {
//                     if (_comparer.Equals(hItem, item))
//                     {
//                         _slots[i].Reset();
//                         _count--;
//                         
//                         return true;
//                     }
//                 }
//             }
//         }
//
//         return false;
//     }
//
//     public void Clear()
//     {
//         this.Clear(true);
//     }
//
//     private void Clear(bool checkDisposed)
//     {
//         if (checkDisposed)
//             this.CheckDisposed();
//
//         lock (_lock)
//         {
//             for (var i = 0; i < _slots.Length; i++)
//             {
//                 _slots[i].Reset();
//             }
//         }
//     }
//     
//     private void CheckDisposed()
//     {
//         ObjectDisposedException.ThrowIf(_disposed, this);
//     }
//
//     private record struct Slot(
//         DependentHandle Handle,
//         bool IsOccupied
//     )
//     {
//         public void Capture(object? handle, object dependent)
//         {
//             if (this.IsOccupied || this.Handle.IsAllocated)
//             {
//                 this.Reset();
//             }
//
//             this.Handle = new DependentHandle(handle, dependent);
//             this.IsOccupied = true;
//         }
//         
//         public void Reset()
//         {
//             this.IsOccupied = false;
//             
//             if (this.Handle.IsAllocated)
//                 this.Handle.Dispose();
//         }
//     }
//     
//     public struct Enumerator : IEnumerator<T>
//     {
//         public T Current { get; private set; } = null!;
//         
//         object? IEnumerator.Current => this.Current;
//
//         private WeakSet<T> _weakSet;
//         
//         private int _pos = -1;
//         
//         public Enumerator(WeakSet<T> weakSet)
//         {
//             _weakSet = weakSet;
//         }
//
//         public void Dispose()
//         {
//         }
//
//         public bool MoveNext()
//         {
//             lock (_weakSet._lock)
//             {
//                 while (++_pos < _weakSet._slots.Length - 1)
//                 {
//                     if (_weakSet._slots[_pos].IsOccupied
//                         && _weakSet._slots[_pos].Handle.IsAllocated
//                         && _weakSet._slots[_pos].Handle.Target is T item)
//                     {
//                         this.Current = item;
//
//                         return true;
//                     }
//                 }
//             }
//
//             return false;
//         }
//
//         public void Reset()
//         {
//             _pos = -1;
//         }
//     }
// }