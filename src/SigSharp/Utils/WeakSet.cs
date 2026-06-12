// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Runtime;
// using System.Runtime.InteropServices;
// using System.Threading;
// using SigSharp.Nodes;
//
// namespace SigSharp.Utils;
//
// internal sealed class WeakSet<T> : IDisposable
//     where T: class
// {
//     private const int NotOccupiedMarker = -1;
//     
//     public bool IsEmpty => _count == 0;
//     public bool HasAny => _count > 0;
//     
//     private SpinLock _spinLock;
//     
//     private volatile int _count;
//     private int _capacity;
//     
//     private Slot[] _slots;
//     private int[] _hashes;
//
//     private volatile int _markerCollisions;
//     
//     private bool _disposed;
//
//     private readonly int _initialCapacity;
//     
//     public WeakSet(int initialCapacity = 32)
//     {
//         // GCHandle<SignalNode> q;
//         // GCHandle<SignalNode>.ToIntPtr();
//         // GCHandle.Alloc(this, GCHandleType.Weak).
//         _capacity = Math.Max(8, initialCapacity);
//         _initialCapacity = _capacity;
//         
//         _slots = new Slot[_capacity];
//         _hashes = new int[_capacity];
//         Array.Fill(_hashes, NotOccupiedMarker);
//     }
//
//     public WeakSet() : this(32)
//     {
//     }
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
//             _hashes = [];
//             _capacity = 0;
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
//         var lockTaken = false;
//         try
//         {
//             _spinLock.Enter(ref lockTaken);
//
//             var index = this.IndexOf(item);
//
//             if (index >= 0)
//                 return false;
//
//             var freeIndex = this.FindFreeIndex();
//             if (freeIndex < 0)
//             {
//                 this.SweepAndGrow();
//
//                 freeIndex = this.FindFreeIndex();
//             }
//             
//             if (freeIndex < 0 || _slots[freeIndex].IsOccupied)
//                 throw new InvalidOperationException("Failed to grow WeakSet collection ?!");
//             
//             _slots[freeIndex].Capture(item, this);
//             _hashes[freeIndex] = item.GetHashCode();
//             
//             Interlocked.Increment(ref _count);
//
//             if (_hashes[freeIndex] == NotOccupiedMarker)
//             {
//                 Interlocked.Increment(ref _markerCollisions);
//             }
//         }
//         finally
//         {
//             if (lockTaken)
//                 _spinLock.Exit(false);
//         }
//
//         return true;
//     }
//
//     public bool Remove(T item)
//     {
//         this.CheckDisposed();
//
//         if (this.IsEmpty)
//             return false;
//
//         var lockTaken = false;
//         try
//         {
//             _spinLock.Enter(ref lockTaken);
//
//             var index = this.IndexOf(item);
//             if (index >= 0)
//             {
//                 _slots[index].Reset();
//
//                 if (_hashes[index] == NotOccupiedMarker)
//                 {
//                     Interlocked.Decrement(ref _markerCollisions);
//                 }
//                 else
//                 {
//                     _hashes[index] = NotOccupiedMarker;
//                 }
//
//                 Interlocked.Decrement(ref _count);
//
//                 for (var i = index; i < _slots.Length - 1; i++)
//                 {
//                     _slots[i] = _slots[i + 1];
//                     _hashes[i] = _hashes[i + 1];
//                 }
//                 
//                 // this.AttemptShrink();
//
//                 return true;
//             }
//         }
//         finally
//         {
//             if (lockTaken)
//                 _spinLock.Exit(false);
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
//         var lockTaken = false;
//         try
//         {
//             _spinLock.Enter(ref lockTaken);
//             
//             for (var i = 0; i < _slots.Length; i++)
//             {
//                 _slots[i].Reset();
//             }
//
//             if (_capacity > _initialCapacity)
//             {
//                 _capacity = _initialCapacity;
//                 _slots = new Slot[_initialCapacity];
//                 _hashes = new int[_initialCapacity];
//             }
//             
//             Array.Fill(_hashes, NotOccupiedMarker);
//
//             _count = 0;
//             
//             _markerCollisions = 0;
//         }
//         finally
//         {
//             if (lockTaken)
//                 _spinLock.Exit(false);
//         }
//     }
//
//     private int IndexOf(T? item, int startPos = 0)
//     {
//         if (item is null || _count <= 0)
//             return -1;
//
//         var hashCode = item.GetHashCode();
//         var hashLength = _hashes.Length;
//         
//         while (startPos < hashLength)
//         {
//             var index = Array.IndexOf(_hashes, hashCode, startPos);
//
//             if (index < 0)
//                 return -1;
//             
//             var slot = _slots[index];
//             if (slot is { IsOccupied: true, Handle: { IsAllocated: true, Target: T target } })
//             {
//                 if (ReferenceEquals(item, target))
//                     return index;
//             }
//
//             startPos = index + 1;
//         }
//
//         return -1;
//     }
//
//     private int FindFreeIndex()
//     {
//         if (_markerCollisions == 0)
//             return Array.IndexOf(_hashes, NotOccupiedMarker);
//
//         var startPos = 0;
//         var hashesLength = _hashes.Length;
//         while (startPos < hashesLength)
//         {
//             var index = Array.IndexOf(_hashes, NotOccupiedMarker, startPos);
//
//             if (index < 0)
//                 return -1;
//
//             if (!_slots[index].IsOccupied)
//                 return index;
//             
//             startPos = index + 1;
//         }
//
//         return -1;
//     }
//
//     private void Sweep()
//     {
//         var slotLength = _hashes.Length;
//         for (var i = 0; i < slotLength; i++)
//         {
//             if (!_slots[i].IsOccupied)
//             {
//                 _hashes[i] = NotOccupiedMarker;
//                 continue;
//             }
//
//             // if (_slots[i].Handle.IsAllocated)
//             // {
//             //     var target = _slots[i].Handle.Target;
//             //     if (target == null)
//             //     {
//             //         _slots[i].Reset();
//             //         _hashes[i] = NotOccupiedMarker;
//             //         Interlocked.Decrement(ref _count);
//             //     }
//             // }
//             
//             if (!_slots[i].Handle.IsAllocated || _slots[i].Handle.Target == null)
//             {
//                 _slots[i].Reset();
//                 _hashes[i] = NotOccupiedMarker;
//                 Interlocked.Decrement(ref _count);
//             }
//         }
//
//         this.RecountMarkerCollisions();
//     }
//     
//     private void SweepAndGrow()
//     {
//         this.Sweep();
//         if (_count > _capacity * 0.8)
//         {
//             var newSize = _capacity < 1024
//                 ? _capacity * 2
//                 : (int)Math.Ceiling(_capacity + _capacity * 0.3);
//             
//             Array.Resize(ref _slots, newSize);
//             Array.Resize(ref _hashes, newSize);
//             Array.Fill(_hashes, NotOccupiedMarker, _capacity, newSize - _capacity);
//             
//             _capacity = newSize;
//         }
//     }
//
//     private void Shrink()
//     {
//         var newSize = (int)Math.Max(_count * 1.3, _capacity / 2d);
//
//         var slots = _slots;
//         _slots = new Slot[newSize];
//         
//         var hashes = _hashes;
//         _hashes = new int[newSize];
//         Array.Fill(_hashes, NotOccupiedMarker);
//
//         var idx = 0;
//         
//         var oldSlotLength = slots.Length;
//
//         for (var i = 0; i < oldSlotLength; i++)
//         {
//             if (slots[i].IsOccupied)
//             {
//                 _slots[idx] = slots[i];
//                 _hashes[idx] = hashes[i];
//                 idx++;
//             }
//             else
//             {
//                 slots[i].Reset();
//             }
//         }
//
//         _count = idx;
//
//         this.RecountMarkerCollisions();
//     }
//
//     private void AttemptShrink()
//     {
//         if (_count > _initialCapacity && _count < _capacity * 0.25)
//         {
//             this.Shrink();
//         }
//     }
//
//     private void RecountMarkerCollisions()
//     {
//         var collisions = 0;
//         var slotLength = _slots.Length;
//
//         for (var i = 0; i < slotLength; i++)
//         {
//             if (_slots[i].IsOccupied && _hashes[i] == NotOccupiedMarker)
//             {
//                 collisions++;
//             }
//         }
//
//         Interlocked.Exchange(ref _markerCollisions, collisions);
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
//         private readonly WeakSet<T> _weakSet;
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
//             var lockTaken = false;
//             try
//             {
//                 _weakSet._spinLock.Enter(ref lockTaken);
//
//                 if (_weakSet._disposed)
//                     return false;
//                 
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
//             finally
//             {
//                 if (lockTaken)
//                     _weakSet._spinLock.Exit(false);
//             }
//             
//             // lock (_weakSet._lock)
//             // {
//             //     while (++_pos < _weakSet._slots.Length - 1)
//             //     {
//             //         if (_weakSet._slots[_pos].IsOccupied
//             //             && _weakSet._slots[_pos].Handle.IsAllocated
//             //             && _weakSet._slots[_pos].Handle.Target is T item)
//             //         {
//             //             this.Current = item;
//             //
//             //             return true;
//             //         }
//             //     }
//             // }
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