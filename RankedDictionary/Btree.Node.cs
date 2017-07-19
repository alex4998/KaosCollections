﻿//
// Library: KaosCollections
// File:    Btree.Node.cs
//
// Copyright © 2009-2017 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Kaos.Collections
{
    public abstract partial class Btree<TKey>
    {
        /// <summary>Base page of the B+ tree. May be internal (Branch) or terminal (Leaf).</summary>
        protected abstract class Node
        {
            protected readonly List<TKey> keys;

            public Node (int keyCapacity)
            { this.keys = new List<TKey> (keyCapacity); }

            public int KeyCount { get { return keys.Count; } }
            public TKey Key0 { get { return keys[0]; } }
            public abstract int Weight { get; }

            public void AddKey (TKey key) { keys.Add (key); }
            public TKey GetKey (int index) { return keys[index]; }
            public int Search (TKey key) { return keys.BinarySearch (key); }
            public int Search (TKey key, IComparer<TKey> comparer) { return keys.BinarySearch (key, comparer); }
            public void SetKey (int index, TKey key) { keys[index] = key; }
            public void RemoveKey (int index) { keys.RemoveAt (index); }
            public void RemoveKeys (int index, int count) { keys.RemoveRange (index, count); }
            public void TruncateKeys (int index) { keys.RemoveRange (index, keys.Count - index); }
            public void InsertKey (int index, TKey key) { keys.Insert (index, key); }

#if DEBUG
            public StringBuilder Append (StringBuilder sb)
            {
                for (int ix = 0; ix < KeyCount; ix++)
                {
                    if (ix > 0)
                        sb.Append (',');
                    sb.Append (GetKey (ix));
                }
                return sb;
            }
#endif
        }


        /// <summary>An non-leaf B+ tree page.</summary>
        /// <remarks>
        /// Contains copies of the first key ('anchor') of every leaf except the leftmost.
        /// </remarks>
        protected sealed class Branch : Node
        {
            private readonly List<Node> childNodes;
            private int weight;

            public Branch (int keyCapacity) : base (keyCapacity)
            {
                this.childNodes = new List<Node> (keyCapacity + 1);
            }

            public Branch (int keyCapacity, Node child, int weight = 0) : base (keyCapacity)
            {
                this.childNodes = new List<Node> (keyCapacity + 1) { child };
                this.weight = weight;
            }

            public int ChildCount
            { get { return childNodes.Count; } }

            public Node GetChild (int childIndex)
            { return childNodes[childIndex]; }

            public Node Child0
            { get { return childNodes[0]; } set { childNodes[0] = value; } }

            /// <summary>Number of key/value pairs in the subtree.</summary>
            public override int Weight
            { get { return weight; } }

            /// <summary>Change count of key/value pairs in subtree.</summary>
            /// <param name="adjustment">Change in value.</param>
            public void AdjustWeight (int adjustment)
            { weight += adjustment; }

            public void RemoveChild (int index)
            { childNodes.RemoveAt (index); }

            public void Truncate (int index)
            {
                TruncateKeys (index);
                childNodes.RemoveRange (index + 1, childNodes.Count - (index + 1));
            }

            public void Add (Node node)
            { childNodes.Add (node); }

            public void Add (TKey key, Node node)
            {
                AddKey (key);
                childNodes.Add (node);
            }

            public void Insert (int index, Node node)
            {
                childNodes.Insert (index, node);
            }

            public void Remove (int index, int count)
            {
                RemoveKeys (index, count);
                childNodes.RemoveRange (index, count);
            }
        }

        protected class KeyLeaf : Node
        {
            private KeyLeaf leftKeyLeaf;
            public KeyLeaf LeftLeaf { get { return leftKeyLeaf; } }

            private KeyLeaf rightKeyLeaf;
            public KeyLeaf RightLeaf { get { return rightKeyLeaf; } }


            /// <summary>Create a siblingless leaf.</summary>
            /// <param name="capacity">The initial number of elements the page can store.</param>
            public KeyLeaf (int capacity=0) : base (capacity)
            {
                this.leftKeyLeaf = this.rightKeyLeaf = null;
            }


            /// <summary>Splice this leaf to right of <paramref name="leftLeaf"/>.</summary>
            /// <param name="leftLeaf">Provides linked list insert point.</param>
            /// <param name="capacity">The initial number of elements the page can store.</param>
            public KeyLeaf (KeyLeaf leftLeaf, int capacity) : base (capacity)
            {
                // Doubly linked list insertion.
                this.rightKeyLeaf = leftLeaf.rightKeyLeaf;
                leftLeaf.rightKeyLeaf = this;
                this.leftKeyLeaf = leftLeaf;
                if (this.rightKeyLeaf != null)
                    this.rightKeyLeaf.leftKeyLeaf = this;
            }


            /// <summary>Number of key/value pairs in the subtree.</summary>
            public override int Weight
            { get { return keys.Count; } }

            public void Add (KeyLeaf source, int sourceStart, int sourceStop)
            {
                for (int ix = sourceStart; ix < sourceStop; ++ix)
                    keys.Add (source.GetKey (ix));
            }

            public void Chop()
            {
                Truncate (0);
                rightKeyLeaf = null;
            }

            public virtual void Coalesce()
            {
                for (int ix = 0; ix < rightKeyLeaf.KeyCount; ++ix)
                    keys.Add (rightKeyLeaf.keys[ix]);

                rightKeyLeaf = rightKeyLeaf.rightKeyLeaf;
                if (rightKeyLeaf != null)
                    rightKeyLeaf.leftKeyLeaf = this;
            }

            public void Insert (int index, TKey key)
            {
                Debug.Assert (index >= 0 && index <= keys.Count);
                InsertKey (index, key);
            }

            public void Prune()
            {
                leftKeyLeaf.rightKeyLeaf = rightKeyLeaf;
                if (rightKeyLeaf != null)
                    rightKeyLeaf.leftKeyLeaf = leftKeyLeaf;
            }

            public virtual void Remove (int index)
            {
                keys.RemoveAt (index);
            }

            public virtual void Shift (int shiftCount)
            {
                for (int ix = 0; ix < shiftCount; ++ix)
                    keys.Add (rightKeyLeaf.keys[ix]);
                rightKeyLeaf.keys.RemoveRange (0, shiftCount);
            }

            public virtual void Truncate (int index)
            {
                keys.RemoveRange (index, keys.Count-index);
            }
        }
    }
}
