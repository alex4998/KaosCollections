﻿// File: RankedBag.cs
//
// Copyright © 2009-2017 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if NET35 || NET40 || SERIALIZE
using System.Runtime.Serialization;
#endif

namespace Kaos.Collections
{
    /// <summary>Represents a collection of sorted items.</summary>
    /// <typeparam name="T">The type of the items in the bag.</typeparam>
    /// <remarks>
    /// <para>
    /// This class is similar to a RankedSet but with duplicate items allowed.
    /// Duplicate items are each stored individually rather than
    /// once with a count of occurrences.
    /// This allows RankedBag to be used as a multimap as well as a multiset data structure.
    /// Multimap usage requires supplying a user-defined comparer to the constructor.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy (typeof (ICollectionDebugView<>))]
    [DebuggerDisplay ("Count = {Count}")]
#if NET35 || NET40 || SERIALIZE
    [Serializable]
#endif
    public class RankedBag<T> :
        Btree<T>
        , ICollection<T>
        , ICollection
#if ! NET35 && ! NET40
        , IReadOnlyCollection<T>
#endif
#if NET35 || NET40 || SERIALIZE
        , ISerializable
        , IDeserializationCallback
#endif
    {
        #region Constructors

        /// <summary>Initializes a new bag of sorted items that uses the default comparer.</summary>
        /// <exception cref="InvalidOperationException">When <em>comparer</em> is <b>null</b> and no other comparer available.</exception>
        public RankedBag() : base (Comparer<T>.Default, new Leaf())
        { }

        /// <summary>Initializes a new bag of sorted items that uses the supplied comparer.</summary>
        /// <param name="comparer">The comparer to use for sorting items.</param>
        /// <example>
        /// This program shows usage of a custom comparer combined with serialization.
        /// Note: Serialization is not supported in .NET Standard 1.0.
        /// <code source="..\Bench\RbExample05\RbExample05.cs" lang="cs" />
        /// </example>
        /// <exception cref="InvalidOperationException">When <em>comparer</em> is <b>null</b> and no other comparer available.</exception>
        public RankedBag (IComparer<T> comparer) : base (comparer, new Leaf())
        { }

        /// <summary>Initializes a new bag that contains items copied from the supplied collection.</summary>
        /// <param name="collection">The enumerable collection to be copied.</param>
        /// <remarks>
        /// This constructor is a O(<em>n</em> log <em>n</em>) operation, where <em>n</em> is the size of <em>collection</em>.
        /// </remarks>
        /// <example>
        /// This program shows using his class for some basic statistical calcuations.
        /// <code source="..\Bench\RbExample03\RbExample03.cs" lang="cs" />
        /// </example>
        /// <exception cref="InvalidOperationException">When <em>comparer</em> is <b>null</b> and no other comparer available.</exception>
        /// <exception cref="ArgumentNullException">When <em>collection</em> is <b>null</b>.</exception>
        public RankedBag (IEnumerable<T> collection) : this (collection, Comparer<T>.Default)
        { }

        /// <summary>Initializes a new bag that contains items copied from the supplied collection.</summary>
        /// <param name="collection">The enumerable collection to be copied. </param>
        /// <param name="comparer">The comparer to use for item sorting.</param>
        /// <remarks>
        /// This constructor is a O(<em>n</em> log <em>n</em>) operation, where <em>n</em> is the size of <em>collection</em>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">When <em>collection</em> is <b>null</b>.</exception>
        /// <exception cref="InvalidOperationException">When <em>comparer</em> is <b>null</b> and no other comparer available.</exception>
        public RankedBag (IEnumerable<T> collection, IComparer<T> comparer) : this (comparer)
        {
            if (collection == null)
                throw new ArgumentNullException (nameof (collection));

            foreach (T key in collection)
                Add (key);
        }

        #endregion

        #region Properties

        /// <summary>Indicates that the collection is not read-only.</summary>
        bool ICollection<T>.IsReadOnly => false;

        /// <summary>Indicates that the collection is not thread safe.</summary>
        bool ICollection.IsSynchronized => false;

        /// <summary>Gets an object that can be used to synchronize access to the collection.</summary>
        object ICollection.SyncRoot => GetSyncRoot();

        #endregion

        #region Methods

        /// <summary>Adds an item to the bag.</summary>
        /// <param name="item">The item to add.</param>
        void ICollection<T>.Add (T item)
        { AddKey (item, new NodeVector (this, item, seekNext:true)); }

        /// <summary>Adds an item to the bag.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns><b>true</b> if <em>item</em> was not already in the uniques; otherwise <b>false</b>.</returns>
        /// <remarks>
        /// <para>This operation allows duplicate items.
        /// If any items that match the supplied item already exist in the bag
        /// then the newer item is added sequentially following the older items.
        /// </para>
        /// <para>This is a O(log <em>n</em>) operation.</para>
        /// </remarks>
        /// <exception cref="ArgumentException">When no comparer is available.</exception>
        public bool Add (T item)
        {
            var path = new NodeVector (this, item, seekNext:true);
            AddKey (item, path);
            return ! path.IsFound;
        }


        /// <summary>Adds the supplied number of copies of the supplied item to the bag.</summary>
        /// <param name="item">The item to add.</param>
        /// <param name="count">The number of copies to add.</param>
        /// <returns><b>true</b> if <em>item</em> was not already in the bag; otherwise <b>false</b>.</returns>
        /// <remarks>
        /// <para>
        /// This operation allows duplicate items.
        /// If any items that match the supplied item already exist in the bag
        /// then the newer items are added sequentially following the older items.
        /// </para>
        /// <para>
        /// This is a O(<em>m</em> log <em>n</em>) operation
        /// where <em>m</em> is <em>count</em> and <em>n</em> is the total item count of the bag.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">When <em>count</em> is less than zero.</exception>
        public bool Add (T item, int count)
        {
            if (count < 0)
                throw new ArgumentException ("Must be non-negative.", nameof (count));

            var path = new NodeVector (this, item);
            bool result = path.IsFound;

            if (count > 0)
                for (;;)
                {
                    int leafAdds = maxKeyCount - path.TopNode.KeyCount;
                    if (leafAdds == 0)
                    {
                        AddKey (item, path);
                        --count;
                    }
                    else
                    {
                        if (leafAdds > count)
                            leafAdds = count;
                        path.TopNode.InsertKey (path.TopIndex, item, leafAdds);
                        path.ChangePathWeight (leafAdds);
                        count -= leafAdds;
                    }

                    if (count == 0)
                        break;

                    path = new NodeVector (this, item);
                }

            return result;
        }


        /// <summary>Determines whether a supplied item exists in the bag.</summary>
        /// <param name="item">The item to check for existence in the bag.</param>
        /// <returns><b>true</b> if the bag contains <em>item</em>; otherwise <b>false</b>.</returns>
        /// <remarks>This is a O(<em>n</em>) operation.</remarks>
        public bool Contains (T item)
        {
            Leaf leaf = Find (item, out int ix);
            return ix >= 0;
        }


        /// <summary>Determines whether the bag is a subset of the supplied collection.</summary>
        /// <param name="other">The collection to compare to this bag.</param>
        /// <returns><b>true</b> if the bag is a subset of <em>other</em>; otherwise <b>false</b>.</returns>
        /// <exception cref="ArgumentNullException">When <em>other</em> is <b>null</b>.</exception>
        public bool ContainsAll (IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException (nameof (other));

            var oBag = other as RankedBag<T> ?? new RankedBag<T> (other, Comparer);

            if (Count < oBag.Count)
                return false;
            if (oBag.Count == 0)
                return true;

            Leaf leaf1 = oBag.leftmostLeaf;
            int leafIx1 = 0;
            int treeIx1 = 0;
            for (;;)
            {
                T key = leaf1.GetKey (leafIx1);
                int treeIx2 = oBag.FindEdgeForIndex (key, out Leaf leaf2, out int leafIx2, leftEdge:false);
                if (treeIx2 - treeIx1 > GetCount (key))
                    return false;
                if (leafIx2 < leaf2.KeyCount)
                { leaf1 = leaf2; leafIx1 = leafIx2; }
                else
                {
                    leaf1 = leaf2.rightLeaf;
                    if (leaf1 == null)
                        return true;
                    leafIx1 = 0;
                }
                treeIx1 = treeIx2;
            }
        }


        /// <summary>Copies the items to a compatible array.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
        /// <remarks>This is a O(<em>n</em>) operation.</remarks>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentException">When not enough space is available for the copy.</exception>
        public void CopyTo (T[] array)
        { CopyKeysTo1 (array, 0, Count); }

        /// <summary>Copies the items to a compatible array, starting at the supplied position.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
        /// <param name="index">The zero-based starting position in <em>array</em>.</param>
        /// <remarks>This is a O(<em>n</em>) operation.</remarks>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
        /// <exception cref="ArgumentException">When not enough space is available for the copy.</exception>
        public void CopyTo (T[] array, int index)
        { CopyKeysTo1 (array, index, Count); }

        /// <summary>Copies a supplied number of items to a compatible array, starting at the supplied position.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
        /// <param name="index">The zero-based starting position in <em>array</em>.</param>
        /// <param name="count">The number of items to copy.</param>
        /// <remarks>This is a O(<em>n</em>) operation.</remarks>
        /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
        /// <exception cref="ArgumentException">When not enough space is available for the copy.</exception>
        public void CopyTo (T[] array, int index, int count)
        { CopyKeysTo1 (array, index, count); }

        /// <summary>Copies the bag to a compatible array, starting at the supplied array index.</summary>
        /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
        /// <param name="index">The zero-based starting position in <em>array</em>.</param>
        void ICollection.CopyTo (Array array, int index)
        { CopyKeysTo2 (array, index, Count); }


        /// <summary>Returns the number of occurrences of the supplied item in the bag.</summary>
        /// <param name="item">The item to return the number of occurrences for.</param>
        /// <returns>The number of occurrences of the supplied item.</returns>
        /// <remarks>
        /// <para>
        /// This is a O(log <em>n</em>) operation
        /// where <em>n</em> is the total item count.
        /// </para>
        /// </remarks>
        public int GetCount (T item)
        {
            int result = 0;
            var path1 = new NodeVector (this, item, seekNext:false);

            if (path1.IsFound)
            {
                var path2 = new NodeVector (this, item, seekNext:true);
                result = path2.GetIndex() - path1.GetIndex();
            }

            return result;
        }


        /// <summary>Returns the number of distinct items in the bag.</summary>
        /// <returns>The number of distinct items in the bag.</returns>
        /// <remarks>
        /// This is a O(<em>m</em> log <em>n</em>) operation
        /// where <em>m</em> is the distinct item count
        /// and <em>n</em> is the total item count.
        /// </remarks>
        public int GetDistinctCount()
        {
            int result = 0;

            if (Count > 0)
            {
                Leaf leaf = leftmostLeaf;
                int leafIndex = 0;

                for (T currentKey = leaf.Key0;;)
                {
                    ++result;
                    if (leafIndex < leaf.KeyCount - 1)
                    {
                        ++leafIndex;
                        T nextKey = leaf.GetKey (leafIndex);
                        if (Comparer.Compare (currentKey, nextKey) != 0)
                        { currentKey = nextKey; continue; }
                    }

                    FindEdgeRight (currentKey, out leaf, out leafIndex);
                    if (leafIndex >= leaf.KeyCount)
                    {
                        leaf = leaf.rightLeaf;
                        if (leaf == null)
                            break;
                        leafIndex = 0;
                    }
                    currentKey = leaf.GetKey (leafIndex);
                }
            }

            return result;
        }


        /// <summary>Gets the index of the first occurrence of supplied item.</summary>
        /// <param name="item">The item of the index to get.</param>
        /// <returns>The index of <em>item</em> if found; otherwise the bitwise complement of the insert point.</returns>
        /// <remarks>
        /// <para>
        /// For duplicate items, the lowest index is returned.
        /// </para>
        /// <para>
        /// This is a O(log <em>n</em>) operation.
        /// </para>
        /// </remarks>
        public int IndexOf (T item)
        {
            return FindEdgeForIndex (item, out Leaf leaf, out int leafIndex, leftEdge:true);
        }


        /// <summary>Removes an item from the collection.</summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><b>true</b> if <em>item</em> was found and removed; otherwise <b>false</b>.</returns>
        /// <remarks>
        /// <para>
        /// For duplicate items, the lowest indexed item is deleted.
        /// </para>
        /// <para>
        /// This is a O(log <em>n</em>) operation.
        /// </para>
        /// </remarks>
        public bool Remove (T item)
        {
            var path = new NodeVector (this, item, seekNext:false);
            if (! path.IsFound)
                return false;

            Remove2 (path);
            return true;
        }


        /// <summary>Removes a supplied number of items from the bag.</summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <returns><b>true</b> if at least one <em>item</em> was found and removed; otherwise <b>false</b>.</returns>
        /// <remarks>
        /// <para>
        /// For duplicate items, lowest indexed items are removed first.
        /// </para>
        /// <para>
        /// This is a O(<em>m</em> log <em>n</em>) operation
        /// where <em>m</em> is <em>count</em>
        /// and <em>n</em> is the total item count.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">When <em>count</em> is less than zero.</exception>
        public bool Remove (T item, int count)
        {
            if (count < 0)
                throw new ArgumentException ("Must be non-negative.", nameof (count));
            if (count == 0)
                return false;

            int treeDels = GetCount (item);
            if (treeDels == 0)
                return false;
            if (treeDels > count)
                treeDels = count;

            StageBump();
            while (treeDels > 0)
            {
                var path = new NodeVector (this, item, seekNext:false);
                if (! path.IsFound)
                    break;

                if (path.TopIndex >= path.TopNode.KeyCount)
                    path.TraverseRight();

                int leafIx = path.TopIndex;
                var leaf = (Leaf) path.TopNode;

                int leafDels = Math.Min (treeDels, leaf.KeyCount - leafIx);
                leaf.RemoveKeys (leafIx, leafDels);
                path.ChangePathWeight (-leafDels);
                Balance (path);
                treeDels -= leafDels;
            }
            return true;
        }


        /// <summary>
        ///  Remove all items of the bag that are in the supplied collection.
        /// </summary>
        /// <param name="other">The items to remove.</param>
        /// <returns>The number of items removed from the bag.</returns>
        /// <exception cref="ArgumentNullException">When <em>other</em> is <b>null</b>.</exception>
        public int RemoveAll (IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException (nameof (other));

            int removed = 0;
            if (Count > 0)
            {
                StageBump();
                var oBag = other as RankedBag<T> ?? new RankedBag<T> (other, Comparer);
                if (oBag.Count > 0)
                    foreach (var oKey in oBag.Distinct())
                    {
                        var oCount = oBag.GetCount (oKey);
                        Remove (oKey, oCount);
                        removed += oCount;
                    }
            }
            return removed;
        }


        /// <summary>Removes the item at the supplied index.</summary>
        /// <param name="index">The zero-based position of the item to remove.</param>
        /// <remarks>
        /// This is a O(log <em>n</em>) operation
        /// where <em>n</em> is the total item count.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero or greater than or equal to the total item count.</exception>
        public void RemoveAt (int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException (nameof (index), "Argument is out of the range of valid values.");

            var path = NodeVector.CreateForIndex (this, index);
            Remove2 (path);
        }


        /// <summary>Removes all items that match the condition defined by the supplied predicate.</summary>
        /// <param name="match">The condition of the items to remove.</param>
        /// <returns>The number of items removed from the bag.</returns>
        /// <remarks>
        /// This is a O(<em>n</em> log <em>m</em>) operation
        /// where <em>m</em> is the count of items removed and <em>n</em> is the size of the bag.
        /// </remarks>
        /// <exception cref="ArgumentNullException">When <em>match</em> is <b>null</b>.</exception>
        public int RemoveWhere (Predicate<T> match)
        {
            return RemoveWhere2 (match);
        }


        /// <summary>
        ///  Remove any items of the bag that are not in the supplied collection.
        /// </summary>
        /// <param name="other">The items to retain.</param>
        /// <returns>The number of items removed from the bag.</returns>
        /// <exception cref="ArgumentNullException">When <em>other</em> is <b>null</b>.</exception>
        public int RetainAll (IEnumerable<T> other)
        {
            if (other == null)
                throw new ArgumentNullException (nameof (other));

            int removed = 0;
            if (Count > 0)
            {
                StageBump();
                var oBag = other as RankedBag<T> ?? new RankedBag<T> (other, Comparer);

                foreach (var key in Distinct())
                {
                    int oCount = oBag.GetCount (key);
                    int tCount = GetCount (key);
                    int diff = tCount - oCount;
                    if (diff > 0)
                    {
                        Remove (key, diff);
                        removed += diff;
                    }
                }
            }
            return removed;
        }

        #endregion

        #region ISerializable implementation and support
#if NET35 || NET40 || SERIALIZE

        private SerializationInfo serializationInfo;
        protected RankedBag (SerializationInfo info, StreamingContext context) : base (new Btree<T>.Leaf())
        {
            this.serializationInfo = info;
        }


        /// <summary>Populates a SerializationInfo with target data.</summary>
        /// <param name="info">The SerializationInfo to populate.</param>
        /// <param name="context">The destination.</param>
        /// <exception cref="ArgumentNullException">When <em>info</em> is <b>null</b>.</exception>
        protected virtual void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException (nameof (info));

            info.AddValue ("Count", Count);
            info.AddValue ("Comparer", Comparer, typeof (IComparer<T>));
            info.AddValue ("Stage", stage);

            var items = new T[Count];
            CopyTo (items, 0);
            info.AddValue ("Items", items, typeof (T[]));
        }

        protected virtual void OnDeserialization (object sender)
        {
            if (keyComparer != null)
                return;  // Owner did the fixups.

            if (serializationInfo == null)
                throw new SerializationException ("Missing information.");

            keyComparer = (IComparer<T>) serializationInfo.GetValue ("Comparer", typeof (IComparer<T>));
            int storedCount = serializationInfo.GetInt32 ("Count");
            stage = serializationInfo.GetInt32 ("Stage");

            if (storedCount != 0)
            {
                var items = (T[]) serializationInfo.GetValue ("Items", typeof (T[]));
                if (items == null)
                    throw new SerializationException ("Missing Items.");

                for (int ix = 0; ix < items.Length; ++ix)
                    Add (items[ix]);

                if (storedCount != Count)
                    throw new SerializationException ("Mismatched count.");
            }

            serializationInfo = null;
        }

        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
        { GetObjectData (info, context); }

        void IDeserializationCallback.OnDeserialization (Object sender)
        { OnDeserialization (sender); }

#endif
        #endregion

        #region LINQ instance implementation

        /// <summary>Gets the item at the supplied index.</summary>
        /// <param name="index">The zero-based index of the item to get.</param>
        /// <returns>The item at the supplied index.</returns>
        /// <remarks>This is a O(log <em>n</em>) operation.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero or not less than the number of items.</exception>
        public T ElementAt (int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException (nameof (index), "Argument was out of the range of valid values.");

            var leaf = (Leaf) Find (ref index);
            return leaf.GetKey (index);
        }


        /// <summary>Gets the item at the supplied index or the default if index is out of range.</summary>
        /// <param name="index">The zero-based index of the item to get.</param>
        /// <returns>The item at the supplied index.</returns>
        /// <remarks>This is a O(log <em>n</em>) operation.</remarks>
        /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
        public T ElementAtOrDefault (int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException (nameof (index), "Argument was out of the range of valid values.");

            if (index >= Count)
                return default (T);

            var leaf = (Leaf) Find (ref index);
            return leaf.GetKey (index);
        }


        /// <summary>Gets the last item.</summary>
        /// <returns>The item sorted to the end of the bag.</returns>
        /// <remarks>This is a O(1) operation.</remarks>
        /// <exception cref="InvalidOperationException">When the collection is empty.</exception>
        public T Last()
        {
            if (Count == 0)
                throw new InvalidOperationException ("Sequence contains no elements.");

            return rightmostLeaf.GetKey (rightmostLeaf.KeyCount - 1);
        }

        #endregion

        #region Enumeration

        /// <summary>Returns an IEnumerable that iterates thru distinct items in the bag.</summary>
        /// <returns>An enumerator that iterates thru distinct items.</returns>
        /// <remarks>
        /// This is a O(<em>m</em> log(<em>n</em>) operation
        /// where <em>m</em> is the distinct item count
        /// and <em>n</em> is the total item count.
        /// </remarks>
        public IEnumerable<T> Distinct()
        {
            if (Count == 0)
                yield break;

            int ix = 0;
            Leaf leaf = leftmostLeaf;
            for (T key = leaf.Key0;;)
            {
                yield return key;

                if (ix < leaf.KeyCount - 1)
                {
                    ++ix;
                    T nextKey = leaf.GetKey (ix);
                    if (Comparer.Compare (key, nextKey) != 0)
                    { key = nextKey; continue; }
                }

                FindEdgeRight (key, out leaf, out ix);
                if (ix >= leaf.KeyCount)
                {
                    leaf = leaf.rightLeaf;
                    if (leaf == null)
                        yield break;
                    ix = 0;
                }
                key = leaf.GetKey (ix);
            }
        }


        /// <summary>Returns a subset range.</summary>
        /// <param name="lower">Minimum item value of range.</param>
        /// <param name="upper">Maximum item value of range.</param>
        /// <returns>An enumerator for all items between <em>lower</em> and <em>upper</em> inclusive.</returns>
        /// <remarks>
        /// <para>Neither <em>lower</em> or <em>upper</em> need to be present in the collection.</para>
        /// <para>
        /// Retrieving the initial item is a O(log <em>n</em>) operation.
        /// Retrieving each subsequent item is a O(1) operation.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When the bag was modified after the enumerator was created.</exception>
        public IEnumerable<T> ElementsBetween (T lower, T upper)
        {
            int stageFreeze = stage;

            FindEdgeLeft (lower, out Leaf leaf, out int ix);
            for (;;)
            {
                if (ix < leaf.KeyCount)
                {
                    T key = leaf.GetKey (ix);
                    if (Comparer.Compare (key, upper) > 0)
                        yield break;

                    yield return key;
                    StageCheck (stageFreeze);
                    ++ix;
                    continue;
                }

                leaf = leaf.rightLeaf;
                if (leaf == null)
                    yield break;

                ix = 0;
            }
        }


        /// <summary>Provides range query support with ordered results.</summary>
        /// <param name="lower">Minimum value of range.</param>
        /// <returns>An enumerator for the bag for items greater than or equal to <em>item</em>.</returns>
        /// <remarks>
        /// <para>
        /// Retrieving the initial item is a O(log <em>n</em>) operation.
        /// Retrieving each subsequent item is a O(1) operation.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When the bag was modified after the enumerator was created.</exception>
        public IEnumerable<T> ElementsFrom (T lower)
        {
            int stageFreeze = stage;

            FindEdgeLeft (lower, out Leaf leaf, out int ix);
            for (;;)
            {
                if (ix < leaf.KeyCount)
                {
                    yield return leaf.GetKey (ix);
                    StageCheck (stageFreeze);
                    ++ix;
                    continue;
                }

                leaf = (Leaf) leaf.rightLeaf;
                if (leaf == null)
                    yield break;

                ix = 0;
            }
        }


        /// <summary>Returns an IEnumerable that iterates thru the bag in reverse order.</summary>
        /// <returns>An enumerator that reverse iterates thru the bag.</returns>
        public IEnumerable<T> Reverse()
        {
            Enumerator enor = new Enumerator (this, isReverse:true);
            while (enor.MoveNext())
                yield return enor.Current;
        }


        /// <summary>Returns an enumerator that iterates thru the bag.</summary>
        /// <returns>An enumerator that iterates thru the bag in sorted order.</returns>
        public Enumerator GetEnumerator() => new Enumerator (this);

        /// <summary>Returns an enumerator that iterates thru the bag.</summary>
        /// <returns>An enumerator that iterates thru the bag in sorted order.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator (this);

        /// <summary>Returns an enumerator that iterates thru the collection.</summary>
        /// <returns>An enumerator that iterates thru the collection in sorted order.</returns>
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator (this);


        /// <summary>Enumerates the sorted items of a <see cref="RankedBag{T}"/>.</summary>
        public sealed class Enumerator : IEnumerator<T>
        {
            private readonly RankedBag<T> tree;
            private readonly bool isReverse;
            private Leaf leaf;
            private int index;
            private int stageFreeze;
            private int state;  // -1=rewound; 0=active; 1=consumed

            internal Enumerator (RankedBag<T> bag, bool isReverse=false)
            {
                this.tree = bag;
                this.isReverse = isReverse;
                ((IEnumerator) this).Reset();
            }

            /// <summary>Gets the element at the current position.</summary>
            object IEnumerator.Current
            {
                get
                {
                    tree.StageCheck (stageFreeze);
                    if (state != 0)
                        throw new InvalidOperationException();
                    return (object) leaf.GetKey (index);
                }
            }

            /// <summary>Gets the item at the current position of the enumerator.</summary>
            /// <exception cref="InvalidOperationException">When the bag was modified after the enumerator was created.</exception>
            public T Current
            {
                get
                {
                    tree.StageCheck (stageFreeze);
                    return state != 0 ? default (T) : leaf.GetKey (index);
                }
            }

            /// <summary>Advances the enumerator to the next item in the bag.</summary>
            /// <returns><b>true</b> if the enumerator was successfully advanced to the next item; <b>false</b> if the enumerator has passed the end of the bag.</returns>
            /// <exception cref="InvalidOperationException">When the bag was modified after the enumerator was created.</exception>
            public bool MoveNext()
            {
                tree.StageCheck (stageFreeze);

                if (state != 0)
                    if (state > 0)
                        return false;
                    else
                    {
                        leaf = isReverse ? tree.rightmostLeaf : tree.leftmostLeaf;
                        index = isReverse ? leaf.KeyCount : -1;
                        state = 0;
                    }

                if (isReverse)
                {
                    if (--index >= 0)
                        return true;

                    leaf = leaf.leftLeaf;
                    if (leaf != null)
                    { index = leaf.KeyCount - 1; return true; }
                }
                else
                {
                    if (++index < leaf.KeyCount)
                        return true;

                    leaf = leaf.rightLeaf;
                    if (leaf != null)
                    { index = 0; return true; }
                }

                state = 1;
                return false;
            }

            /// <summary>Rewinds the enumerator to its initial state.</summary>
            void IEnumerator.Reset()
            {
                stageFreeze = tree.stage;
                state = -1;
            }

            /// <summary>Releases all resources used by the enumerator.</summary>
            public void Dispose() { }
        }

        #endregion
    }
}