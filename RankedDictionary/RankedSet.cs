﻿//
// Library: KaosCollections
// File:    RankedSet.cs
// Purpose: Defines BtreeDictionary generic API.
//
// Copyright © 2009-2017 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kaos.Collections
{
    /// <summary>Represents a collection of sorted, unique items.
    /// </summary>
    /// <typeparam name="TKey">The type of the items in the set.</typeparam>
    /// <remarks>
    /// This class is a functional equivalent of the
    /// <see cref="System.Collections.Generic.SortedSet&lt;TKey&gt;"/> class
    /// with additional capabilities.
    /// </remarks>
    [DebuggerDisplay ("Count = {Count}")]
    public partial class RankedSet<TKey> : Btree<TKey>,
#if ! NET35
        ISet<TKey>,
#endif
        ICollection<TKey>,
        ICollection
#if ! NET35 && ! NET40
        , IReadOnlyCollection<TKey>
#endif
        where TKey : IComparable
    {
        private KeyLeaf LeftmostLeaf { get { return leftmostLeaf; } }

        #region Constructors

        /// <summary>Initializes a new set of sorted items.</summary>
        public RankedSet() : this (DefaultOrder, Comparer<TKey>.Default)
        { }

        /// <summary>Initializes a new set of sorted items.</summary>
        /// <param name="order">Maximum number of children of a branch.</param>
        public RankedSet (int order) : this (order, Comparer<TKey>.Default)
        { }

        /// <summary>Initializes a new set of sorted items.</summary>
        /// <param name="order">Maximum number of children of a branch.</param>
        /// <param name="comparer">The comparer to use for sorting items.</param>
        public RankedSet (int order, IComparer<TKey> comparer) : base (order, comparer, new KeyLeaf())
        { this.root = this.leftmostLeaf; }

        /// <summary>Initializes a new set of sorted items.</summary>
        /// <param name="comparer">The comparer to use for sorting items.</param>
        public RankedSet (IComparer<TKey> comparer) : this (DefaultOrder, comparer)
        { }

        /// <summary>Initializes a new set that contains items copied from the specified collection.</summary>
        /// <param name="collection">The enumerable collection to be copied. </param>
        public RankedSet (IEnumerable<TKey> collection) : this (collection, Comparer<TKey>.Default)
        { }

        /// <summary>Initializes a new set that contains items copied from the specified collection.</summary>
        /// <param name="collection">The enumerable collection to be copied. </param>
        /// <param name="comparer">The comparer to use for sorting items.</param>
        /// <exception cref="ArgumentNullException">When <em>collection</em> is <b>null</b>.</exception>
        public RankedSet (IEnumerable<TKey> collection, IComparer<TKey> comparer) : this (comparer)
        {
            if (collection == null)
                throw new ArgumentNullException (nameof (collection));

            foreach (TKey item in collection)
                Add (item);
        }

        #endregion

        #region Public properties

        /// <summary>Gets the number of items in the set.</summary>
        public int Count
        { get { return root.Weight; } }

        bool ICollection<TKey>.IsReadOnly
        { get { return false; } }

        bool ICollection.IsSynchronized
        { get { return false; } }

        /// <summary>Gets the maximum value in the set per the comparer.</summary>
        public TKey Max
        {
            get
            {
                if (Count == 0)
                    return default (TKey);
                KeyLeaf rightmost = GetRightmost();
                return rightmost.GetKey (rightmost.KeyCount-1);
            }
        }

        /// <summary>Gets the minimum value in the set per the comparer.</summary>
        public TKey Min
        {
            get
            {
                if (Count == 0)
                    return default (TKey);
                return LeftmostLeaf.Key0;
            }
        }

        object ICollection.SyncRoot => throw new NotImplementedException ();

        #endregion

        /// <summary>Removes all elements from the set.</summary>
        public void Clear()
        {
            leftmostLeaf.Chop();
            root = leftmostLeaf;
        }


        /// <summary>Adds an item to the set and returns a success indicator.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns><b>true</b> if the item was added to the set; otherwise <b>false</b>.</returns>
        public bool Add (TKey item)
        {
            var path = new NodeVector (this, item);
            if (path.IsFound)
                return false;

            Add2 (path, item);
            return true;
        }

        void ICollection<TKey>.Add (TKey item)
        { Add (item); }

        private void Add2 (NodeVector nv, TKey key)
        {
            var leaf = (KeyLeaf) nv.TopNode;
            int pathIndex = nv.TopNodeIndex;

            nv.UpdateWeight (1);
            if (leaf.KeyCount < maxKeyCount)
            {
                leaf.Insert (pathIndex, key);
                return;
            }

            // Leaf is full so right split a new leaf.
            var newLeaf = new KeyLeaf (leaf, maxKeyCount);

            if (newLeaf.RightLeaf == null && pathIndex == leaf.KeyCount)
                newLeaf.AddKey (key);
            else
            {
                int splitIndex = leaf.KeyCount / 2 + 1;

                if (pathIndex < splitIndex)
                {
                    // Left-side insert: Copy right side to the split leaf.
                    newLeaf.Add (leaf, splitIndex - 1, leaf.KeyCount);
                    leaf.Truncate (splitIndex - 1);
                    leaf.Insert (pathIndex, key);
                }
                else
                {
                    // Right-side insert: Copy split leaf parts and new key.
                    newLeaf.Add (leaf, splitIndex, pathIndex);
                    newLeaf.AddKey (key);
                    newLeaf.Add (leaf, pathIndex, leaf.KeyCount);
                    leaf.Truncate (splitIndex);
                }
            }

            // Promote anchor of split leaf.
            nv.Promote (newLeaf.Key0, (Node) newLeaf, newLeaf.RightLeaf == null);
        }


        /// <summary>Determines whether the set contains a specific item.</summary>
        /// <param name="item">The item to check for existence in the set.</param>
        /// <returns><b>true</b> if the set contains the item; otherwise <b>false</b>.</returns>
        public bool Contains (TKey item)
        {
            KeyLeaf leaf = Find (item, out int index);
            return index >= 0;
        }


        /// <summary>Copies the set to a compatible array, starting at the beginning of the target array.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the items to copy from the set.</param>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentException">When not enough space is given for the copy.</exception>
        public void CopyTo (TKey[] array)
        { CopyTo (array, 0, Count); }

        /// <summary>Copies the set to a compatible array, starting at the beginning of the target array.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the items to copy from the set.</param>
        /// <param name="index">The zero-based starting position.</param>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
        /// <exception cref="ArgumentException">When not enough space is given for the copy.</exception>
        public void CopyTo (TKey[] array, int index)
        { CopyTo (array, index, Count); }

        /// <summary>Copies the set to a compatible array, starting at the specified position.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the items to copy from the set.</param>
        /// <param name="index">The zero-based starting position.</param>
        /// <param name="count">The number of items to copy.</param>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> or <em>count</em> is less than zero.</exception>
        /// <exception cref="ArgumentException">When not enough space is given for the copy.</exception>
        public void CopyTo (TKey[] array, int index, int count)
        {
            if (array == null)
                throw new ArgumentNullException (nameof (array));

            if (index < 0)
                throw new ArgumentOutOfRangeException (nameof (index), index, "Specified argument was out of the range of valid values.");

            if (count < 0)
                throw new ArgumentOutOfRangeException (nameof (count), count, "Specified argument was out of the range of valid values.");

            if (Count > array.Length - index)
                throw new ArgumentException ("Destination array is not long enough to copy all the items in the collection. Check array index and length.", nameof (array));

            for (KeyLeaf leaf = LeftmostLeaf; leaf != null; leaf = leaf.RightLeaf)
                for (int klix = 0; klix < leaf.KeyCount; ++klix)
                    array[index++] = leaf.GetKey (klix);
        }

        void ICollection.CopyTo (Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException (nameof (array));

            if (array.Rank != 1)
                throw new ArgumentException ("Multidimension array is not supported on this operation.", nameof (array));

            if (array.GetLowerBound (0) != 0)
                throw new ArgumentException ("Target array has non-zero lower bound.", nameof (array));

            if (index < 0)
                throw new ArgumentOutOfRangeException (nameof (index), "Non-negative number required.");

            if (Count > array.Length - index)
                throw new ArgumentException ("Destination array is not long enough to copy all the items in the collection. Check array index and length.", nameof (array));

            if (array is TKey[] genArray)
            {
                CopyTo (genArray, index);
                return;
            }

            if (array is object[] obArray)
            {
                try
                {
                    int ix = 0;
                    foreach (var item in this)
                        obArray[ix++] = item;
                }
                catch (ArrayTypeMismatchException)
                { throw new ArgumentException ("Mismatched array type.", nameof (array)); }
            }
            else
                throw new ArgumentException ("Invalid array type.", nameof (array));
        }


        /// <summary>Removes a specified item from the set.</summary>
        /// <param name="item"></param>
        /// <returns><b>true</b> if the item was found and removed; otherwise <b>false</b>.</returns>
        public bool Remove (TKey item)
        {
            var path = new NodeVector (this, item);
            if (! path.IsFound)
                return false;

            Remove2 (path);
            return true;
        }


        /// <summary>Returns an IEnumerable that iterates over the set in reverse order.</summary>
        /// <returns>An enumerator that reverse iterates over the set.</returns>
        public IEnumerable<TKey> Reverse()
        {
            Enumerator enor = new Enumerator (this, reverse:true);
            while (enor.MoveNext())
                yield return enor.Current;
        }


        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return new Enumerator (this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator (this);
        }

        #region ISET implementation
        #if ! NET35

        /// <summary>Removes all items that are in a specified collection.</summary>
        /// <param name="other">The collection of items to remove.</param>
        /// <exception cref="ArgumentNullException">When <em>other</em> is <b>null</b>.</exception>
        public void ExceptWith (IEnumerable<TKey> other)
        {
            if (other == null)
                throw new ArgumentNullException (nameof (other));

            if (Count == 0)
                return;

            if (other == this)
            {
                Clear();
                return;
            }

            foreach (TKey item in other)
                if (Contains (item))
                    Remove (item);
        }


        public void IntersectWith (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool IsProperSubsetOf (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool IsProperSupersetOf (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool IsSubsetOf (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool IsSupersetOf (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool Overlaps (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public bool SetEquals (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public void SymmetricExceptWith (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        public void UnionWith (IEnumerable<TKey> other)
        {
            throw new NotImplementedException ();
        }

        #endif
        #endregion

        /// <summary>Enumerates the sorted elements of a KeyCollection.</summary>
        public sealed class Enumerator : IEnumerator<TKey>
        {
            private readonly RankedSet<TKey> tree;
            private readonly bool isReverse;
            private KeyLeaf currentLeaf;
            private int leafIndex;

            internal Enumerator (RankedSet<TKey> set, bool reverse=false)
            {
                this.tree = set;
                this.isReverse = reverse;
                ((IEnumerator) this).Reset();
            }

            object IEnumerator.Current
            {
                get
                {
                    if (leafIndex < 0)
                        throw new InvalidOperationException();
                    return (object) Current;
                }
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TKey Current
            { get { return leafIndex < 0? default (TKey) : currentLeaf.GetKey (leafIndex); } }

            /// <summary>Advances the enumerator to the next element in the collection.</summary>
            /// <returns><b>true</b> if the enumerator was successfully advanced to the next element; <b>false</b> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                if (currentLeaf != null)
                    if (isReverse)
                    {
                        if (--leafIndex >= 0)
                            return true;

                        currentLeaf = currentLeaf.LeftLeaf;
                        if (currentLeaf != null)
                        { leafIndex = currentLeaf.KeyCount - 1; return true; }
                    }
                    else
                    {
                        if (++leafIndex < currentLeaf.KeyCount)
                            return true;

                        currentLeaf = currentLeaf.RightLeaf;
                        if (currentLeaf != null)
                        { leafIndex = 0; return true; }
                    }

                leafIndex = -1;
                return false;
            }

            void IEnumerator.Reset()
            {
                currentLeaf = isReverse? tree.GetRightmost() : tree.LeftmostLeaf;
                leafIndex = isReverse? currentLeaf.KeyCount : -1;
            }

            /// <summary>Releases all resources used by the Enumerator.</summary>
            public void Dispose() { }
        }

        #region Bonus methods

        /// <summary>Gets the key at the specified index.</summary>
        /// <param name="index">The zero-based index of the key to get.</param>
        /// <returns>The key at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero or greater than or equal to the number of keys.</exception>
        public TKey GetByIndex (int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException (nameof (index), "Specified argument was out of the range of valid values.");

            var leaf = (KeyLeaf) Find (ref index);
            return leaf.GetKey (index);
        }


        /// <summary>Gets the index of the specified item.</summary>
        /// <param name="item">The item of the index to get.</param>
        /// <returns>The index of the specified item if found; -1 if not found.</returns>
        public int IndexOf (TKey item)
        {
            var path = new NodeVector (this, item);
            if (! path.IsFound)
                return -1;

            return path.GetIndex();
        }

        #endregion

    }
}