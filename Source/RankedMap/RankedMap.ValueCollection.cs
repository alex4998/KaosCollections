﻿//
// Library: KaosCollections
// File:    RankedMap.ValueCollection.cs
// Purpose: Define ValueCollection nested class.
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
    public partial class RankedMap<TKey,TValue>
    {
        /// <summary>
        /// Represents a collection of values of the <see cref="RankedMap{TKey,TValue}"/>.
        /// </summary>
        [DebuggerTypeProxy (typeof (ICollectionValuesDebugView<,>))]
        [DebuggerDisplay ("Count = {Count}")]
        public sealed class ValueCollection :
            ICollection<TValue>
            , ICollection
#if ! NET35 && ! NET40
            , IReadOnlyCollection<TValue>
#endif
        {
            private readonly RankedMap<TKey,TValue> tree;

            #region Constructors

            /// <summary>Initializes a new collection that reflects the values of a <see cref="RankedMap{TKey,TValue}"/>.</summary>
            /// <param name="map">Map containing these keys.</param>
            /// <remarks>This is a O(1) operation.</remarks>
            /// <exception cref="ArgumentNullException">When <em>map</em> is <b>null</b>.</exception>
            public ValueCollection (RankedMap<TKey,TValue> map)
            {
                if (map == null)
#pragma warning disable IDE0016
                    throw new ArgumentNullException (nameof (map));
#pragma warning restore IDE0016

                this.tree = map;
            }

            #endregion

            #region Properties

            /// <summary>Gets the value at the supplied index.</summary>
            /// <param name="index">The zero-based index of the value to get.</param>
            /// <returns>The value at <em>index</em>.</returns>
            /// <remarks>This is a O(log <em>n</em>) operation.</remarks>
            /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero or not less than the number of items.</exception>
            public TValue this[int index]
            { get { return ElementAt (index); } }

            /// <summary>Gets the number of values in the collection.</summary>
            public int Count => tree.Count;

            /// <summary>Indicates that the collection is read-only.</summary>
            bool ICollection<TValue>.IsReadOnly => true;

            /// <summary>Indicates that the collection is not thread safe.</summary>
            bool ICollection.IsSynchronized => false;

            /// <summary>Gets an object that can be used to synchronize access to the collection.</summary>
            object ICollection.SyncRoot => tree.GetSyncRoot();

            #endregion

            #region Methods

            /// <summary>This implementation always throws a <see cref="NotSupportedException" />.</summary>
            /// <param name="value">The object to add.</param>
            void ICollection<TValue>.Add (TValue value)
            { throw new NotSupportedException(); }


            /// <summary>This implementation always throws a <see cref="NotSupportedException" />.</summary>
            void ICollection<TValue>.Clear()
            { throw new NotSupportedException(); }


            /// <summary>Determines whether the map contains the supplied value.</summary>
            /// <param name="value">The value to find.</param>
            /// <returns><b>true</b> if <em>value</em> is found in the map; otherwise <b>false</b>.</returns>
            /// <remarks>This is a O(<em>n</em>) operation.</remarks>
            bool ICollection<TValue>.Contains (TValue value)
            { return tree.ContainsValue2<TValue> (value) >= 0; }


            /// <summary>Copies values to a supplied array, starting as the supplied position.</summary>
            /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
            /// <param name="index">The zero-based starting position in <em>array</em>.</param>
            /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
            /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
            /// <exception cref="ArgumentException">When not enough space is given for the copy.</exception>
            public void CopyTo (TValue[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException (nameof (array));

                if (index < 0)
                    throw new ArgumentOutOfRangeException (nameof (index), index, "Argument was out of the range of valid values.");

                if (Count > array.Length - index)
                    throw new ArgumentException ("Destination array is not long enough to copy all the items in the collection. Check array index and length.", nameof (array));

                for (var leaf = (PairLeaf<TValue>) tree.leftmostLeaf; leaf != null; leaf = (PairLeaf<TValue>) leaf.rightLeaf)
                {
                    leaf.CopyValuesTo (array, index, leaf.ValueCount);
                    index += leaf.ValueCount;
                }
            }

            /// <summary>Copies values to a supplied array, starting at the supplied position.</summary>
            /// <param name="array">A one-dimensional array that is the destination of the copy.</param>
            /// <param name="index">The zero-based starting position in <em>array</em>.</param>
            /// <exception cref="ArgumentNullException">When <em>array</em> is <b>null</b>.</exception>
            /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero.</exception>
            /// <exception cref="ArgumentException">When not enough space is given for the copy.</exception>
            void ICollection.CopyTo (Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException (nameof (array));

                if (array.Rank > 1)
                    throw new ArgumentException ("Multidimension array is not supported on this operation.", nameof (array));

                if (index < 0)
                    throw new ArgumentOutOfRangeException (nameof (index), index, "Index is less than zero.");

                if (Count > array.Length - index)
                    throw new ArgumentException ("Destination array is not long enough to copy all the items in the collection. Check array index and length.", nameof (array));

                for (var leaf = (PairLeaf<TValue>) tree.leftmostLeaf; leaf != null; leaf = (PairLeaf<TValue>) leaf.rightLeaf)
                    for (int ix = 0; ix < leaf.KeyCount; ++ix)
                    {
                        array.SetValue (leaf.GetValue (ix), index);
                        ++index;
                    }
            }


            /// <summary>This implementation always throws a <see cref="NotSupportedException"/>.</summary>
            /// <param name="value">The value to remove.</param>
            /// <returns><b>true</b> if the object was removed; otherwise <b>false</b>.</returns>
            bool ICollection<TValue>.Remove (TValue value)
            { throw new NotSupportedException(); }

            #endregion

            #region Bonus methods

            /// <summary>Gets the value at the supplied index.</summary>
            /// <param name="index">The zero-based index of the value to get.</param>
            /// <returns>The value at <em>index</em>.</returns>
            /// <remarks>This is a O(log <em>n</em>) operation.</remarks>
            /// <exception cref="ArgumentOutOfRangeException">When <em>index</em> is less than zero or greater than or equal to the number of keys.</exception>
            public TValue ElementAt (int index)
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException (nameof (index), "Argument is out of the range of valid values.");

                var leaf = (PairLeaf<TValue>) tree.Find (index, out int leafIndex);
                return leaf.GetValue (leafIndex);
            }


            /// <summary>Gets the value at the supplied index or the default if the index is out of range.</summary>
            /// <param name="index">The zero-based index of the value to get.</param>
            /// <returns>The value at <em>index</em>.</returns>
            /// <remarks>This is a O(log <em>n</em>) operation.</remarks>
            public TValue ElementAtOrDefault (int index)
            {
                if (index < 0 || index >= Count)
                    return default (TValue);

                var leaf = (PairLeaf<TValue>) tree.Find (index, out int leafIndex);
                return leaf.GetValue (leafIndex);
            }


            /// <summary>Gets the index of the first occurrence the supplied value.</summary>
            /// <param name="value">The value to find.</param>
            /// <returns>The index of the first occurrence of <em>value</em> if found; otherwise -1.</returns>
            /// <remarks>This is a O(<em>n</em>) operation.</remarks>
            public int IndexOf (TValue value) => tree.ContainsValue2<TValue> (value);

            #endregion

            #region Enumeration

            /// <summary>Gets an enumerator that iterates thru the collection.</summary>
            /// <returns>An enumerator for the collection.</returns>
            public Enumerator GetEnumerator() => new Enumerator (tree);

            /// <summary>Gets an enumerator that iterates thru the collection.</summary>
            /// <returns>An enumerator for the collection.</returns>
            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator (tree);

            /// <summary>Gets an enumerator that iterates thru the collection.</summary>
            /// <returns>An enumerator for the collection.</returns>
            IEnumerator IEnumerable.GetEnumerator() => new Enumerator (tree);


            /// <summary>Enumerates the items of a <see cref="RankedMap{TKey,TValue}.ValueCollection"/>.</summary>
            public sealed class Enumerator : IEnumerator<TValue>
            {
                private readonly RankedMap<TKey,TValue> tree;
                private PairLeaf<TValue> leaf;
                private int index;
                private int stageFreeze;

                internal Enumerator (RankedMap<TKey,TValue> map)
                {
                    this.tree = map;
                    ((IEnumerator) this).Reset();
                }

                /// <summary>Gets the value at the current position.</summary>
                object IEnumerator.Current
                {
                    get
                    {
                        tree.StageCheck (stageFreeze);
                        if (index < 0)
                            throw new InvalidOperationException();
                        return (object) Current;
                    }
                }

                /// <summary>Gets the value at the current position of the enumerator.</summary>
                /// <exception cref="InvalidOperationException">When the map was modified after the enumerator was created.</exception>
                public TValue Current
                {
                    get
                    {
                        tree.StageCheck (stageFreeze);
                        return index < 0 ? default (TValue) : leaf.GetValue (index);
                    }
                }

                /// <summary>Advances the enumerator to the next value in the collection.</summary>
                /// <returns><b>true</b> if the enumerator was successfully advanced to the next value; <b>false</b> if the enumerator has passed the end of the collection.</returns>
                /// <exception cref="InvalidOperationException">When the map was modified after the enumerator was created.</exception>
                    public bool MoveNext()
                {
                    tree.StageCheck (stageFreeze);

                    if (leaf != null)
                    {
                        if (++index < leaf.KeyCount)
                            return true;

                        leaf = (PairLeaf<TValue>) leaf.rightLeaf;
                        if (leaf != null)
                        { index = 0; return true; }

                        index = -1;
                    }

                    return false;
                }

                /// <summary>Rewinds the enumerator to its initial state.</summary>
                void IEnumerator.Reset()
                {
                    stageFreeze = tree.stage;
                    index = -1;
                    leaf = (PairLeaf<TValue>) tree.leftmostLeaf;
                }

                /// <summary>Releases all resources used by the enumerator.</summary>
                public void Dispose() { }
            }

            #endregion
        }
    }
}