using System;
using System.Runtime.CompilerServices;
using EngineX.Baseline.FixedPoint;

namespace EngineX.Physics.Internal.Broadphase
{
    public delegate bool TreeQueryCallback<T>(ref T context, int userData);

    internal sealed class DynamicAabbTree
    {
        private const int NullNode = -1;

        private struct Node
        {
            public Aabb Aabb;
            public int UserData;
            public int Child1;
            public int Child2;
            public int Parent;
            public int Height;
        }

        private Node[] _nodes;
        private int _root;
        private int _freeList;
        private int _insertionCount;
        private int[] _queryStack;
        private int[] _countStack;

        public int Capacity => _nodes.Length;

        public DynamicAabbTree(int initialCapacity)
        {
            if (initialCapacity < 16) initialCapacity = 16;
            _nodes = new Node[initialCapacity];
            for (int i = 0; i < _nodes.Length - 1; i++)
            {
                _nodes[i].Child1 = i + 1;
            }
            _nodes[_nodes.Length - 1].Child1 = NullNode;
            _freeList = 0;
            _root = NullNode;
            _insertionCount = 0;
            _queryStack = new int[256];
            _countStack = new int[256];
        }

        public int Insert(int userData, in Aabb aabb)
        {
            int leaf = AllocateNode();
            if (leaf == NullNode) return NullNode;
            _nodes[leaf].Aabb = aabb;
            _nodes[leaf].UserData = userData;
            _nodes[leaf].Height = 0;
            InsertLeaf(leaf);
            return leaf;
        }

        public bool Remove(int proxyId)
        {
            if (proxyId == NullNode || proxyId < 0 || proxyId >= _nodes.Length) return false;
            if (_nodes[proxyId].UserData == NullNode) return false;
            RemoveLeaf(proxyId);
            FreeNode(proxyId);
            return true;
        }

        public void Update(int proxyId, in Aabb aabb, bool forceUpdate)
        {
            if (proxyId == NullNode) return;
            Node n = _nodes[proxyId];
            if (!forceUpdate && n.Aabb.Contains(aabb))
            {
                return;
            }
            RemoveLeaf(proxyId);
            n.Aabb = aabb;
            _nodes[proxyId] = n;
            InsertLeaf(proxyId);
        }

        public Aabb GetFatAabb(int proxyId)
        {
            return _nodes[proxyId].Aabb;
        }

        public void Query<T>(in Aabb aabb, ref T context, TreeQueryCallback<T> callback)
        {
            if (_root == NullNode) return;
            QueryStack(_root, aabb, ref context, callback);
        }

        private void QueryStack<T>(int nodeId, in Aabb aabb, ref T context, TreeQueryCallback<T> callback)
        {
            int[] stack = _queryStack;
            int top = 0;
            stack[top++] = nodeId;

            while (top > 0)
            {
                int id = stack[--top];
                if (id == NullNode) continue;
                Node n = _nodes[id];
                if (!n.Aabb.Overlaps(aabb)) continue;
                if (n.Child1 == NullNode)
                {
                    bool proceed = callback(ref context, n.UserData);
                    if (!proceed) return;
                }
                else
                {
                    if (top + 2 > stack.Length)
                    {
                        Array.Resize(ref stack, stack.Length * 2);
                        _queryStack = stack;
                    }
                    stack[top++] = n.Child1;
                    stack[top++] = n.Child2;
                }
            }
        }

        public int CountLeaves()
        {
            if (_root == NullNode) return 0;
            return CountLeavesStack(_root);
        }

        private int CountLeavesStack(int nodeId)
        {
            int[] stack = _countStack;
            int top = 0;
            stack[top++] = nodeId;
            int count = 0;
            while (top > 0)
            {
                int id = stack[--top];
                Node n = _nodes[id];
                if (n.Child1 == NullNode)
                {
                    count++;
                }
                else
                {
                    if (top + 2 > stack.Length)
                    {
                        Array.Resize(ref stack, stack.Length * 2);
                        _countStack = stack;
                    }
                    stack[top++] = n.Child1;
                    stack[top++] = n.Child2;
                }
            }
            return count;
        }

        private void InsertLeaf(int leaf)
        {
            _insertionCount++;
            if (_root == NullNode)
            {
                _root = leaf;
                _nodes[leaf].Parent = NullNode;
                return;
            }

            Aabb leafAabb = _nodes[leaf].Aabb;
            int index = _root;
            while (_nodes[index].Child1 != NullNode)
            {
                int child1 = _nodes[index].Child1;
                int child2 = _nodes[index].Child2;
                FP area = _nodes[index].Aabb.SurfaceArea;
                Aabb combined = _nodes[index].Aabb.Merge(leafAabb);
                FP combinedArea = combined.SurfaceArea;

                FP cost = (FP)2 * combinedArea;
                FP inheritanceCost = (FP)2 * (combinedArea - area);

                FP cost1 = ChildCost(child1, leafAabb, inheritanceCost);
                FP cost2 = ChildCost(child2, leafAabb, inheritanceCost);

                if (cost < cost1 && cost < cost2) break;
                index = cost1 < cost2 ? child1 : child2;
            }

            int sibling = index;
            int oldParent = _nodes[sibling].Parent;
            int newParent = AllocateNode();
            if (newParent == NullNode)
            {
                _root = leaf;
                _nodes[leaf].Parent = NullNode;
                return;
            }

            _nodes[newParent].Parent = oldParent;
            _nodes[newParent].UserData = NullNode;
            _nodes[newParent].Aabb = _nodes[sibling].Aabb.Merge(leafAabb);
            _nodes[newParent].Height = _nodes[sibling].Height + 1;

            if (oldParent != NullNode)
            {
                if (_nodes[oldParent].Child1 == sibling)
                {
                    _nodes[oldParent].Child1 = newParent;
                }
                else
                {
                    _nodes[oldParent].Child2 = newParent;
                }
                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].Parent = newParent;
                _nodes[leaf].Parent = newParent;
            }
            else
            {
                _nodes[newParent].Child1 = sibling;
                _nodes[newParent].Child2 = leaf;
                _nodes[sibling].Parent = newParent;
                _nodes[leaf].Parent = newParent;
                _root = newParent;
            }

            index = _nodes[leaf].Parent;
            while (index != NullNode)
            {
                index = Balance(index);
                int c1 = _nodes[index].Child1;
                int c2 = _nodes[index].Child2;
                _nodes[index].Height = 1 + Math.Max(_nodes[c1].Height, _nodes[c2].Height);
                _nodes[index].Aabb = _nodes[c1].Aabb.Merge(_nodes[c2].Aabb);
                index = _nodes[index].Parent;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FP ChildCost(int child, in Aabb leafAabb, FP inheritanceCost)
        {
            if (_nodes[child].Child1 == NullNode)
            {
                Aabb aabb = leafAabb.Merge(_nodes[child].Aabb);
                return aabb.SurfaceArea + inheritanceCost;
            }
            Aabb aabb2 = leafAabb.Merge(_nodes[child].Aabb);
            FP oldArea = _nodes[child].Aabb.SurfaceArea;
            FP newArea = aabb2.SurfaceArea;
            return (newArea - oldArea) + inheritanceCost;
        }

        private int Balance(int iA)
        {
            if (_nodes[iA].Child1 == NullNode || _nodes[iA].Height < 2) return iA;

            int iB = _nodes[iA].Child1;
            int iC = _nodes[iA].Child2;
            int balance = _nodes[iC].Height - _nodes[iB].Height;

            if (balance > 1)
            {
                int iF = _nodes[iC].Child1;
                int iG = _nodes[iC].Child2;

                _nodes[iC].Child1 = iA;
                int newParent = _nodes[iA].Parent;
                _nodes[iC].Parent = newParent;
                _nodes[iA].Parent = iC;

                if (newParent != NullNode)
                {
                    if (_nodes[newParent].Child1 == iA)
                    {
                        _nodes[newParent].Child1 = iC;
                    }
                    else
                    {
                        _nodes[newParent].Child2 = iC;
                    }
                }
                else
                {
                    _root = iC;
                }

                if (_nodes[iF].Height > _nodes[iG].Height)
                {
                    _nodes[iC].Child2 = iF;
                    _nodes[iA].Child2 = iG;
                    _nodes[iG].Parent = iA;
                    _nodes[iA].Aabb = _nodes[iB].Aabb.Merge(_nodes[iG].Aabb);
                    _nodes[iC].Aabb = _nodes[iA].Aabb.Merge(_nodes[iF].Aabb);
                    _nodes[iA].Height = 1 + Math.Max(_nodes[iB].Height, _nodes[iG].Height);
                    _nodes[iC].Height = 1 + Math.Max(_nodes[iA].Height, _nodes[iF].Height);
                }
                else
                {
                    _nodes[iC].Child2 = iG;
                    _nodes[iA].Child2 = iF;
                    _nodes[iF].Parent = iA;
                    _nodes[iA].Aabb = _nodes[iB].Aabb.Merge(_nodes[iF].Aabb);
                    _nodes[iC].Aabb = _nodes[iA].Aabb.Merge(_nodes[iG].Aabb);
                    _nodes[iA].Height = 1 + Math.Max(_nodes[iB].Height, _nodes[iF].Height);
                    _nodes[iC].Height = 1 + Math.Max(_nodes[iA].Height, _nodes[iG].Height);
                }

                return iC;
            }

            if (balance < -1)
            {
                int iD = _nodes[iB].Child1;
                int iE = _nodes[iB].Child2;

                _nodes[iB].Child1 = iA;
                int newParent = _nodes[iA].Parent;
                _nodes[iB].Parent = newParent;
                _nodes[iA].Parent = iB;

                if (newParent != NullNode)
                {
                    if (_nodes[newParent].Child1 == iA)
                    {
                        _nodes[newParent].Child1 = iB;
                    }
                    else
                    {
                        _nodes[newParent].Child2 = iB;
                    }
                }
                else
                {
                    _root = iB;
                }

                if (_nodes[iD].Height > _nodes[iE].Height)
                {
                    _nodes[iB].Child2 = iD;
                    _nodes[iA].Child1 = iE;
                    _nodes[iE].Parent = iA;
                    _nodes[iA].Aabb = _nodes[iC].Aabb.Merge(_nodes[iE].Aabb);
                    _nodes[iB].Aabb = _nodes[iA].Aabb.Merge(_nodes[iD].Aabb);
                    _nodes[iA].Height = 1 + Math.Max(_nodes[iC].Height, _nodes[iE].Height);
                    _nodes[iB].Height = 1 + Math.Max(_nodes[iA].Height, _nodes[iD].Height);
                }
                else
                {
                    _nodes[iB].Child2 = iE;
                    _nodes[iA].Child1 = iD;
                    _nodes[iD].Parent = iA;
                    _nodes[iA].Aabb = _nodes[iC].Aabb.Merge(_nodes[iD].Aabb);
                    _nodes[iB].Aabb = _nodes[iA].Aabb.Merge(_nodes[iE].Aabb);
                    _nodes[iA].Height = 1 + Math.Max(_nodes[iC].Height, _nodes[iD].Height);
                    _nodes[iB].Height = 1 + Math.Max(_nodes[iA].Height, _nodes[iE].Height);
                }

                return iB;
            }

            return iA;
        }

        private void RemoveLeaf(int leaf)
        {
            if (leaf == _root)
            {
                _root = NullNode;
                return;
            }

            int parent = _nodes[leaf].Parent;
            int grandParent = _nodes[parent].Parent;
            int sibling = _nodes[parent].Child1 == leaf
                ? _nodes[parent].Child2
                : _nodes[parent].Child1;

            if (grandParent != NullNode)
            {
                if (_nodes[grandParent].Child1 == parent)
                {
                    _nodes[grandParent].Child1 = sibling;
                }
                else
                {
                    _nodes[grandParent].Child2 = sibling;
                }
                _nodes[sibling].Parent = grandParent;
                FreeNode(parent);

                int index = grandParent;
                while (index != NullNode)
                {
                    index = Balance(index);
                    int c1 = _nodes[index].Child1;
                    int c2 = _nodes[index].Child2;
                    _nodes[index].Aabb = _nodes[c1].Aabb.Merge(_nodes[c2].Aabb);
                    _nodes[index].Height = 1 + Math.Max(_nodes[c1].Height, _nodes[c2].Height);
                    index = _nodes[index].Parent;
                }
            }
            else
            {
                _root = sibling;
                _nodes[sibling].Parent = NullNode;
                FreeNode(parent);
            }
        }

        private int AllocateNode()
        {
            if (_freeList == NullNode)
            {
                int oldCap = _nodes.Length;
                int newCap = oldCap * 2;
                Array.Resize(ref _nodes, newCap);
                for (int i = oldCap; i < newCap - 1; i++)
                {
                    _nodes[i].Child1 = i + 1;
                }
                _nodes[newCap - 1].Child1 = NullNode;
                _freeList = oldCap;
            }
            int id = _freeList;
            _freeList = _nodes[id].Child1;
            _nodes[id].Child1 = NullNode;
            _nodes[id].Child2 = NullNode;
            _nodes[id].Parent = NullNode;
            return id;
        }

        private void FreeNode(int id)
        {
            _nodes[id].Child1 = _freeList;
            _nodes[id].Child2 = NullNode;
            _nodes[id].Parent = NullNode;
            _nodes[id].UserData = NullNode;
            _nodes[id].Height = -1;
            _freeList = id;
        }
    }
}