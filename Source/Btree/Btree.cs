﻿//
// Library: KaosCollections
// File:    Btree.cs
// Purpose: Define base functionality.
//
// Copyright © 2009-2017 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

[assembly: CLSCompliant (true)]
namespace Kaos.Collections
{
    /// <summary>Abstract base for ranked dictionaries and ranked sets.</summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    public abstract partial class Btree<TKey>
    {
        protected Node root;
        protected readonly KeyLeaf leftmostLeaf;
        protected readonly int maxKeyCount;
        protected readonly IComparer<TKey> compareOp;

        protected const int MinimumOrder = 4;
        protected const int DefaultOrder = 128;
        protected const int MaximumOrder = 256;

        protected Btree (int order, IComparer<TKey> comparer, KeyLeaf leftmostLeaf)
        {
            this.compareOp = comparer ?? Comparer<TKey>.Default;
            this.maxKeyCount = order - 1;
            this.leftmostLeaf = leftmostLeaf;
        }

        #region Nonpublic methods

        /// <summary>Get the rightmost leaf of the tree.</summary>
        /// <returns>Rightmost leaf.</returns>
        protected KeyLeaf GetRightmost()
        {
            for (Node node = root;;)
                if (node is Branch branch)
                    node = branch.GetChild (branch.KeyCount);
                else
                    return (KeyLeaf) node;
        }


        /// <summary>Perform lite search for key.</summary>
        /// <param name="key">Target of search.</param>
        /// <param name="index">When found, holds index of returned Leaf; else ~index of nearest greater key.</param>
        /// <returns>Leaf holding target (found or not).</returns>
        protected KeyLeaf Find (TKey key, out int index)
        {
            //  Unfold on default comparer for 5% speed improvement.
            if (compareOp == Comparer<TKey>.Default)
                for (Node node = root;;)
                {
                    index = node.Search (key);

                    if (node is Branch branch)
                        node = branch.GetChild (index < 0 ? ~index : index + 1);
                    else
                        return (KeyLeaf) node;
                }
            else
                for (Node node = root;;)
                {
                    index = node.Search (key, compareOp);

                    if (node is Branch branch)
                        node = branch.GetChild (index < 0 ? ~index : index + 1);
                    else
                        return (KeyLeaf) node;
                }
        }


        /// <summary>Perform traverse to leaf at index.</summary>
        /// <param name="index">On entry, holds index of set; on exit holds index of leaf.</param>
        /// <returns>Leaf holding item at index position.</returns>
        protected Node Find (ref int index)
        {
            Node node = root;
            while (node is Branch branch)
                for (int ix = 0; ix <= node.KeyCount; ++ix)
                {
                    Node child = branch.GetChild (ix);
                    int cw = child.Weight;
                    if (cw > index)
                    {
                        node = child;
                        break;
                    }
                    index -= cw;
                }

            return node;
        }


        protected void Remove2 (NodeVector nv)
        {
            int leafIndex = nv.TopNodeIndex;
            var leaf = (KeyLeaf) nv.TopNode;

            leaf.Remove (leafIndex);
            nv.UpdateWeight (-1);

            if (leafIndex == 0)
                if (leaf.KeyCount != 0)
                    nv.SetPivot (nv.TopNode.Key0);
                else
                {
                    Debug.Assert (leaf.RightLeaf==null, "only rightmost leaf should ever be empty");

                    // The leaf is empty so prune unless it is leftmost (therefore the only leaf).
                    if (leaf.LeftLeaf != null)
                    {
                        leaf.Prune();
                        nv.Demote();
                    }

                    return;
                }

            // Leaf underflow?
            if (leaf.KeyCount < (maxKeyCount + 1) / 2)
            {
                KeyLeaf rightLeaf = leaf.RightLeaf;
                if (rightLeaf != null)
                    if (leaf.KeyCount + rightLeaf.KeyCount > maxKeyCount)
                    {
                        // Balance leaves by shifting pairs from right leaf.
                        int shifts = (leaf.KeyCount + rightLeaf.KeyCount + 1) / 2 - leaf.KeyCount;
                        leaf.Shift (shifts);
                        nv.TraverseRight();
                        nv.SetPivot (rightLeaf.Key0);
                        nv.TiltLeft (shifts);
                    }
                    else
                    {
                        leaf.Coalesce();
                        nv.TraverseRight();
                        nv.TiltLeft (rightLeaf.KeyCount);
                        nv.Demote();
                    }
            }
        }


        private object syncRoot = null;
        protected object GetSyncRoot()
        {
            if (syncRoot == null)
                Interlocked.CompareExchange (ref syncRoot, new object(), null);
            return syncRoot;
        }

        #endregion

        #region Debug methods
#if DEBUG

        // Telemetry counters:
        /// <summary>Maximum number of keys that the existing branches can hold.</summary>
        public int BranchSlotCount { get; private set; }
        /// <summary>Number of keys contained in the branches.</summary>
        public int BranchSlotsUsed { get; private set; }
        /// <summary>Maximum number of keys that the existing leaves can hold.</summary>
        public int LeafSlotCount { get; private set; }
        /// <summary>Number of keys contained in the leaves.</summary>
        public int LeafSlotsUsed { get; private set; }


        /// <summary>
        /// Perform diagnostics check for data structure sanity errors. Since this is an
        /// in-memory managed structure, any errors would indicate a bug. Also performs space
        /// complexity diagnostics to ensure that all non-rightmost nodes maintain 50% fill.
        /// </summary>
        public void SanityCheck()
        {
            BranchSlotCount = 0;
            BranchSlotsUsed = 0;
            LeafSlotCount = 0;
            LeafSlotsUsed = 0;

            KeyLeaf lastLeaf;
            if (root is Branch)
                lastLeaf = CheckBranch ((Branch) root, 1, GetHeight(), true, default (TKey), null);
            else
                lastLeaf = CheckLeaf ((KeyLeaf) root, true, default (TKey), null);

            if (lastLeaf.RightLeaf != null)
                throw new InvalidOperationException ("Last leaf has invalid RightLeaf");

            if (root.Weight != LeafSlotsUsed)
                throw new InvalidOperationException ("Mismatched Count=" + root.Weight + ", expected=" + LeafSlotsUsed);
        }


        /// <summary>Maximum number of children of a branch.</summary>
        /// <returns>Maximum number of children of a branch.</returns>
        public int GetOrder()
        { return maxKeyCount + 1; }


        /// <summary>Return the number of levels in the tree.</summary>
        /// <returns>Number of levels in the tree.</returns>
        public int GetHeight()
        {
            int level = 1;
            for (Node node = root; node is Branch; node = ((Branch) node).Child0)
                ++level;
            return level;
        }


        private KeyLeaf CheckBranch
        (
            Branch branch,
            int level, int height,
            bool isRightmost,
            TKey anchor,  // ignored when isRightmost true
            KeyLeaf visited
        )
        {
            BranchSlotCount += maxKeyCount;
            BranchSlotsUsed += branch.KeyCount;

            if (! isRightmost && (branch.KeyCount + 1) < maxKeyCount / 2)
                throw new InvalidOperationException ("Branch underfilled");

            if (branch.ChildCount != branch.KeyCount + 1)
                throw new InvalidOperationException ("Branch mismatched ChildCount, KeyCount");

            int actualWeight = 0;
            for (int i = 0; i < branch.ChildCount; ++i)
            {
                TKey anchor0 = i == 0 ? anchor : branch.GetKey (i - 1);
                bool isRightmost0 = isRightmost && i < branch.ChildCount;
                if (i < branch.KeyCount - 1)
                    if (compareOp.Compare (branch.GetKey (i), branch.GetKey (i + 1)) >= 0)
                        throw new InvalidOperationException ("Branch keys not ascending");

                if (level + 1 < height)
                    visited = CheckBranch ((Branch) branch.GetChild (i), level+1, height, isRightmost0, anchor0, visited);
                else
                    visited = CheckLeaf ((KeyLeaf) branch.GetChild (i), isRightmost0, anchor0, visited);

                actualWeight += branch.GetChild (i).Weight;
            }
            if (branch.Weight != actualWeight)
                throw new InvalidOperationException ("Branch mismatched weight");

            return visited;
        }


        private KeyLeaf CheckLeaf (KeyLeaf leaf, bool isRightmost, TKey anchor, KeyLeaf visited)
        {
            LeafSlotCount += maxKeyCount;
            LeafSlotsUsed += leaf.KeyCount;

            if (leaf.RightLeaf != null && leaf.KeyCount < (maxKeyCount + 1) / 2)
                throw new InvalidOperationException ("Leaf underfilled");

            if (! anchor.Equals (default (TKey)) && ! anchor.Equals (leaf.Key0))
                throw new InvalidOperationException ("Leaf has wrong anchor");

            for (int i = 0; i < leaf.KeyCount; ++i)
                if (i < leaf.KeyCount - 1 && compareOp.Compare (leaf.GetKey (i), leaf.GetKey (i + 1)) >= 0)
                    throw new InvalidOperationException ("Leaf keys not ascending");

            if (visited == null)
            {
                if (! anchor.Equals (default (TKey)))
                    throw new InvalidOperationException ("Inconsistent visited, anchor");
            }
            else
                if (visited.RightLeaf != leaf)
                    throw new InvalidOperationException ("Leaf has bad RightLeaf");

            return leaf;
        }


        /// <summary>Return telemetry summary.</summary>
        /// <returns>Telemetry summary.</returns>
        public string GetTreeStatsText()
        {
            SanityCheck();
            string result = "--- height = " + GetHeight();

            if (BranchSlotCount != 0)
                result += ", branch fill = " + (int) (BranchSlotsUsed * 100.0 / BranchSlotCount + 0.5) + "%";

            return result + ", leaf fill = " + (int) (LeafSlotsUsed * 100.0 / LeafSlotCount + 0.5) + "%";
        }


        /// <summary>Generate contents of tree by level (breadth first).</summary>
        /// <returns>Text lines where each line is a level of the tree.</returns>
        public IEnumerable<string> GenerateTreeText (bool showWeight=false)
        {
            int level = 0;
            Node leftmost;
            var sb = new StringBuilder();

            for (;;)
            {
                var branchPath = new NodeVector (this, level);
                leftmost = branchPath.TopNode;
                if (leftmost is KeyLeaf)
                    break;

                var branch = (Branch) leftmost;

                sb.Append ('B');
                sb.Append (level);
                sb.Append (": ");
                for (;;)
                {
                    branch.Append (sb);
                    if (showWeight)
                    {
                        sb.Append (" (");
                        sb.Append (branch.Weight);
                        sb.Append (") ");
                    }

                    branch = (Branch) branchPath.TraverseRight();
                    if (branch == null)
                        break;

                    sb.Append (" | ");
                }
                ++level;
                yield return sb.ToString();
                sb.Length = 0;
            }

            var leafPath = new NodeVector (this, level);
            sb.Append ('L');
            sb.Append (level);
            sb.Append (": ");
            for (var leaf = (KeyLeaf) leftmost;;)
            {
                leaf.Append (sb);
                leaf = (KeyLeaf) leafPath.TraverseRight();
                if (leaf == null)
                    break;

                if (leafPath.Child0)
                    sb.Append (" | ");
                else
                    sb.Append ('|');
            }
            yield return sb.ToString();
        }

#endif
        #endregion
    }
}