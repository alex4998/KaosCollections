﻿//
// Library: KaosCollections
// File:    Btree.NodeVector.cs
//
// Copyright © 2009-2017 Kasey Osborn (github.com/kaosborn)
// MIT License - Use and redistribute freely
//

using System.Collections.Generic;
using System.Diagnostics;

namespace Kaos.Collections
{
    public abstract partial class Btree<TKey>
    {
        /// <summary>Stack trace from root to leaf.</summary>
        /// <remarks>
        /// Provides traversal path to existing key or insertion point for non-existing key
        /// along with various helper methods.
        /// </remarks>
        protected class NodeVector
        {
            private readonly Btree<TKey> owner;
            private readonly List<int> indexStack;
            private readonly List<Node> nodeStack;

            #region Constructors

            /// <summary>Perform search and store each level of path on the stack.</summary>
            /// <param name="tree">Tree to search.</param>
            /// <param name="key">Value to find.</param>
            public NodeVector (Btree<TKey> tree, TKey key)
            {
                this.owner = tree;
                this.indexStack = new List<int>();
                this.nodeStack = new List<Node>();

                for (Node node = tree.root;;)
                {
                    Debug.Assert (node != null);

                    this.nodeStack.Add (node);
                    int ix = node.Search (key, tree.compareOp);

                    if (! (node is Branch))
                    {
                        IsFound = ix >= 0;
                        if (! IsFound)
                            ix = ~ix;
                        this.indexStack.Add (ix);
                        return;
                    }

                    ix = (ix < 0) ? ~ix : ix+1;
                    this.indexStack.Add (ix);
                    node = ((Branch) node).GetChild (ix);
                }
            }

            #endregion

            #region Properties

            public bool IsFound
            { get; private set; }

            public Node TopNode
            { get { return nodeStack[indexStack.Count - 1]; } }

            public int TopNodeIndex
            { get { return indexStack[indexStack.Count - 1]; } }

            public int Height
            { get { return indexStack.Count; } }

            #endregion

            #region Methods

            /// <summary>Calculate the rank of the current pair.</summary>
            /// <returns>The rank of the current pair.</returns>
            public int GetIndex()
            {
                int result = TopNodeIndex;

                for (int level = indexStack.Count-2; level >= 0; --level)
                    if (indexStack[level] <= nodeStack[level].KeyCount / 2)
                        for (int ix = 0; ix < indexStack[level]; ++ix)
                            result += ((Branch) nodeStack[level]).GetChild (ix).Weight;
                    else
                    {
                        result += nodeStack[level].Weight;
                        for (int ix = indexStack[level]; ix <= nodeStack[level].KeyCount; ++ix)
                            result -= ((Branch) nodeStack[level]).GetChild (ix).Weight;
                    }

                return result;
            }

            /// <summary>
            /// Get the node to the immediate left of the node specified by NodeVector.
            /// </summary>
            public Node GetLeftNode()
            {
                Debug.Assert (indexStack.Count == nodeStack.Count);

                for (int level = indexStack.Count - 2; level >= 0; --level)
                    if (indexStack[level] > 0)
                        for (Node node = ((Branch) nodeStack[level]).GetChild (indexStack[level]-1);;)
                            if (++level < indexStack.Count-1)
                                node = ((Branch) node).GetChild (node.KeyCount);
                            else
                                return node;

                return null;
            }


            public void TiltLeft (int delta)
            {
                for (int level = indexStack.Count-2; level >= 0; --level)
                    if (indexStack[level] == 0)
                        ((Branch) nodeStack[level]).AdjustWeight (- delta);
                    else if (level >= indexStack.Count-2)
                        return;
                    else
                        for (var bh = (Branch) ((Branch) nodeStack[level]).GetChild (indexStack[level]-1);;)
                        {
                            bh.AdjustWeight (+ delta);
                            if (++level >= indexStack.Count-2)
                                return;
                            bh = (Branch) bh.GetChild (bh.KeyCount);
                        }

                Debug.Assert (false, "tilt one sided");
            }


            /// <summary>Get nearest key where left child path taken.</summary>
            /// <remarks>On entry, top of path refers to a branch.</remarks>
            public TKey GetPivot()
            {
                Debug.Assert (TopNode is Branch);
                for (int level = indexStack.Count - 2; level >= 0; --level)
                {
                    if (indexStack[level] > 0)
                        return nodeStack[level].GetKey (indexStack[level] - 1);
                }

                Debug.Assert (false, "no left pivot");
                return default (TKey);
            }


            /// <summary>Set nearest key where left child path taken.</summary>
            /// <remarks>On entry, top of vector refers to a branch.</remarks>
            public void SetPivot (TKey key)
            {
                for (int level = indexStack.Count - 2; level >= 0; --level)
                    if (indexStack[level] > 0)
                    {
                        nodeStack[level].SetKey (indexStack[level] - 1, key);
                        return;
                    }
            }


            public void Clear()
            {
                indexStack.Clear();
                nodeStack.Clear();
            }


            public void Pop()
            {
                nodeStack.RemoveAt (nodeStack.Count - 1);
                indexStack.RemoveAt (indexStack.Count - 1);
            }


            public void Push (Node newNode, int newNodeIndex)
            {
                nodeStack.Add (newNode);
                indexStack.Add (newNodeIndex);
            }


            /// <summary>Adjust tree path to node to the right.</summary>
            /// <returns>Node to immediate right of current path;
            /// <b>null</b> if current path at rightmost node.</returns>
            public Node TraverseRight()
            {
                Node node = null;
                int height = indexStack.Count;
                for (;;)
                {
                    if (indexStack.Count < 2)
                    {
                        Clear();
                        node = null;
                        break;
                    }

                    Pop();
                    node = TopNode;
                    int newIndex = TopNodeIndex + 1;

                    if (newIndex < ((Branch) node).ChildCount)
                    {
                        indexStack[indexStack.Count - 1] = newIndex;
                        node = ((Branch) node).GetChild (newIndex);
                        for (;;)
                        {
                            Push (node, 0);
                            if (indexStack.Count >= height)
                                break;
                            node = ((Branch) node).Child0;
                        }
                        break;
                    }
                }

                return node;
            }


            public void UpdateWeight (int delta)
            {
                for (int level = Height-2; level >= 0; --level)
                    ((Branch) nodeStack[level]).AdjustWeight (+ delta);
            }


            // Leaf or branch has been split so insert the new anchor into a branch.
            public void Promote (TKey key, Node newNode, bool isAppend)
            {
                for (;;)
                {
                    if (Height == 1)
                    {
                        // Graft new root.
                        Debug.Assert (owner.root == TopNode);
                        owner.root = new Branch (owner.maxKeyCount, TopNode, TopNode.Weight + newNode.Weight);
                        ((Branch) owner.root).Add (key, newNode);
                        break;
                    }

                    Pop();
                    var branch = (Branch) TopNode;
                    int branchIndex = TopNodeIndex;

                    if (branch.KeyCount < owner.maxKeyCount)
                    {
                        // Typical case where branch has room.
                        branch.InsertKey (branchIndex, key);
                        branch.Insert (branchIndex + 1, newNode);
                        break;
                    }

                    // Branch is full so right split a new branch.
                    var newBranch = new Branch (owner.maxKeyCount);
                    int splitIndex = isAppend ? branch.KeyCount - 1 : (branch.KeyCount + 1) / 2;

                    if (branchIndex < splitIndex)
                    {
                        // Split branch with left-side insert.
                        for (int ix = splitIndex; ; ++ix)
                        {
                            newBranch.AdjustWeight (+ branch.GetChild (ix).Weight);
                            if (ix >= branch.KeyCount)
                            {
                                newBranch.Add (branch.GetChild (ix));
                                break;
                            }
                            newBranch.Add (branch.GetKey (ix), branch.GetChild (ix));
                        }

                        TKey newPromotion = branch.GetKey (splitIndex - 1);
                        branch.Truncate (splitIndex - 1);
                        branch.InsertKey (branchIndex, key);
                        branch.Insert (branchIndex + 1, newNode);
                        key = newPromotion;
                        branch.AdjustWeight (- newBranch.Weight);
                    }
                    else
                    {
                        // Split branch with right-side insert (or cascade promote).
                        int leftIndex = splitIndex;
                        newBranch.AdjustWeight (newNode.Weight);

                        if (branchIndex > splitIndex)
                        {
                            for (;;)
                            {
                                ++leftIndex;
                                newBranch.Add (branch.GetChild (leftIndex));
                                newBranch.AdjustWeight (+ branch.GetChild (leftIndex).Weight);
                                if (leftIndex >= branchIndex)
                                    break;
                                newBranch.AddKey (branch.GetKey (leftIndex));
                            }
                            newBranch.AddKey (key);
                            key = branch.GetKey (splitIndex);
                        }

                        newBranch.Add (newNode);

                        while (leftIndex < branch.KeyCount)
                        {
                            newBranch.AddKey (branch.GetKey (leftIndex));
                            ++leftIndex;
                            newBranch.Add (branch.GetChild (leftIndex));
                            newBranch.AdjustWeight (+ branch.GetChild (leftIndex).Weight);
                        }

                        branch.Truncate (splitIndex);
                        branch.AdjustWeight (- newBranch.Weight);
                    }

                    newNode = newBranch;
                }
            }


            // Leaf has been emptied so non-lazy delete its pivot.
            public void Demote()
            {
                for (;;)
                {
                    Debug.Assert (Height > 0);
                    Pop();

                    var branch = (Branch) TopNode;
                    if (TopNodeIndex == 0)
                    {
                        if (branch.KeyCount == 0)
                            // Cascade when rightmost branch is empty.
                            continue;

                        // Rotate pivot for first pair.
                        TKey pivot = branch.Key0;
                        branch.RemoveKey (0);
                        branch.RemoveChild (0);
                        SetPivot (pivot);
                    }
                    else
                    {
                        // Typical branch pivot delete.
                        branch.RemoveKey (TopNodeIndex - 1);
                        branch.RemoveChild (TopNodeIndex);
                    }

                    var right = (Branch) TraverseRight();
                    if (right == null)
                    {
                        if (branch == owner.root && branch.KeyCount == 0)
                            // Prune an empty root.
                            owner.root = branch.Child0;
                        return;
                    }

                    if (branch.KeyCount + right.KeyCount < owner.maxKeyCount)
                    {
                        // Coalesce left: move pivot and right sibling nodes.
                        branch.AddKey (GetPivot());

                        for (int ix = 0; ; ++ix)
                        {
                            branch.Add (right.GetChild (ix));
                            if (ix >= right.KeyCount)
                                break;
                            branch.AddKey (right.GetKey (ix));
                        }
                        branch.AdjustWeight (+ right.Weight);
                        TiltLeft (+ right.Weight);

                        // Cascade demotion.
                        continue;
                    }

                    // Branch underflow?
                    if (branch.KeyCount < owner.maxKeyCount / 2)
                    {
                        // Balance branches to keep ratio.  Rotate thru the pivot.
                        int shifts = (branch.KeyCount + right.KeyCount - 1) / 2 - branch.KeyCount;
                        branch.AddKey (GetPivot());

                        int delta = 0;
                        for (int rightIndex = 0; ; ++rightIndex)
                        {
                            branch.Add (right.GetChild (rightIndex));
                            delta += right.GetChild (rightIndex).Weight;

                            if (rightIndex >= shifts)
                                break;

                            branch.AddKey (right.GetKey (rightIndex));
                        }

                        SetPivot (right.GetKey (shifts));
                        right.Remove (0, shifts + 1);
                        branch.AdjustWeight (+ delta);
                        right.AdjustWeight (- delta);
                        TiltLeft (delta);
                    }

                    return;
                }
            }

#endregion

#if DEBUG
            /// <summary>Returns <b>true</b> if no left sibling.</summary>
            public bool Child0
            { get { return this.indexStack[Height - 2] == 0; } }


            /// <summary>Make an empty path.</summary>
            /// <param name="tree">Target of path.</param>
            /// <remarks>Used only for diagnostics.</remarks>
            public NodeVector (Btree<TKey> tree)
            {
                this.indexStack = new List<int>();
                this.nodeStack = new List<Node>();
                this.IsFound = false;
                Push (tree.root, 0);
            }


            /// <summary>Make a path to leftmost branch or leaf at the supplied level.</summary>
            /// <param name="tree">Target of path.</param>
            /// <param name="level">Level of node to seek (root is level 0).</param>
            /// <remarks>Used only for diagnostics.</remarks>
            public NodeVector (Btree<TKey> tree, int level) : this (tree)
            {
                for (Node node = TopNode; level > 0; --level)
                {
                    node = ((Branch) node).GetChild (0);
                    Push (node, 0);
                }
            }
#endif
        }
    }
}