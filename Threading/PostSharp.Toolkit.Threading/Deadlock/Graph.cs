using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    public enum ResourceType
    {
        Read,
        Write,
        UpgradeableRead,
        Lock,
        Thread
    }

    internal class Graph
    {
        private readonly ConcurrentDictionary<Node, ConcurrentDictionary<Node, Edge>> adjacencyList;

        public Graph()
        {
            this.adjacencyList = new ConcurrentDictionary<Node, ConcurrentDictionary<Node, Edge>>();
        }

        public void RemoveAdjecentEdges(object resource, ResourceType resourceType)
        {
            var node = new Node(resource, resourceType);
            ConcurrentDictionary<Node, Edge> n;
            this.adjacencyList.TryRemove(node, out n);
        }

        public void AddEdge(object from, ResourceType fromType, object to, ResourceType toType)
        {
            var fromNode = new Node(from, fromType);
            var neighbourhood = this.adjacencyList.GetOrAdd(fromNode, n => new ConcurrentDictionary<Node, Edge>());

            var toNode = new Node(to, toType);

            Edge edge = neighbourhood.GetOrAdd(toNode, n => new Edge(fromNode, toNode));

            edge.Counter++;
        }

        public void RemoveEdge(object from, ResourceType fromType, object to, ResourceType toType)
        {
            var fromNode = new Node(from, fromType);
            ConcurrentDictionary<Node, Edge> neighbourhood;
            if (!this.adjacencyList.TryGetValue(fromNode, out neighbourhood))
            {
                return;
            }

            var toNode = new Node(to, toType);

            Edge edge;

            if (!neighbourhood.TryGetValue(toNode, out edge))
            {
                return;
            }

            edge.Counter--;

            if (edge.Counter == 0)
            {
                neighbourhood.TryRemove(toNode, out edge);

                if (neighbourhood.Count == 0)
                {
                    this.adjacencyList.TryRemove(fromNode, out neighbourhood);
                }
            }

            return;
        }

        public bool DetectCycles(out IEnumerable<Edge> cycle)
        {
            var gamma = new Dictionary<Node, int>();

            var predecessors = new Dictionary<Node, Edge>();

            int currentGamma = 1;

            foreach (var r in this.adjacencyList.Keys)
            {
                if (this.DetectCycleInStronglyConnectedComponent(gamma, r, ref currentGamma, predecessors, out cycle))
                {
                    return true;
                }
            }

            cycle = null;
            return false;
        }

        public bool DetectCycles(object startingVertex, ResourceType startingVertexType, out IEnumerable<Edge> cycle)
        {
            var gamma = new Dictionary<Node, int>();

            var predecessors = new Dictionary<Node, Edge>();

            int currentGamma = 1;

            var root = new Node(startingVertex, startingVertexType);

            return this.DetectCycleInStronglyConnectedComponent(gamma, root, ref currentGamma, predecessors, out cycle);
        }

        private bool DetectCycleInStronglyConnectedComponent(Dictionary<Node, int> gamma, Node r, ref int currentGamma, Dictionary<Node, Edge> predecessors, out IEnumerable<Edge> cycle)
        {

            if (gamma.ContainsKey(r))
            {
                cycle = null;
                return false;
            }

            gamma.Add(r, currentGamma++);

            var edgeStack = new Stack<Edge>();

            this.AddAdjecentEdgesToStack(r, edgeStack);

            while (edgeStack.Count > 0)
            {
                var e = edgeStack.Pop();

                if (!gamma.ContainsKey(e.Successor))
                {
                    gamma.Add(e.Successor, currentGamma++);
                    predecessors.Add(e.Successor, e);
                    this.AddAdjecentEdgesToStack(e.Successor, edgeStack);
                }
                else
                {
                    if (gamma[e.Predecessor] > gamma[e.Successor] && // is not a forward edge
                        gamma[r] <= gamma[e.Successor]) // is not a cross edge
                    {
                        cycle = this.GetCycle(e, predecessors);

                        return true;
                    }
                }
            }

            cycle = null;
            return false;
        }

        private IEnumerable<Edge> GetCycle(Edge e, Dictionary<Node, Edge> predecessors)
        {
            Edge cerrentEdge;
            Node currentNode = e.Predecessor;

            yield return e;

            while (predecessors.TryGetValue(currentNode, out cerrentEdge))
            {
                yield return cerrentEdge;
                currentNode = cerrentEdge.Predecessor;
            }
        }

        private void AddAdjecentEdgesToStack(Node v, Stack<Edge> edgeStack)
        {
            if (!this.adjacencyList.ContainsKey(v))
            {
                return;
            }

            foreach (var e in this.adjacencyList[v].Where(x => Environment.TickCount - x.Value.LastChange > 100)) // ignore young edges
            {
                edgeStack.Push(e.Value);
            }
        }
    }

    internal class Node : IEquatable<Node>
    {
        public readonly object SyncObject;

        public readonly ResourceType Role;

        public Node(object syncObject, ResourceType role)
        {
            this.SyncObject = syncObject;
            this.Role = role;
        }

        public bool Equals(Node other)
        {
            return Equals(this.SyncObject, other.SyncObject) && this.Role == other.Role;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((Node)obj);
        }

        public override int GetHashCode()
        {
            return (this.SyncObject.GetHashCode() << 16) | this.Role.GetHashCode();
        }

        public override string ToString()
        {
            Thread thread = this.SyncObject as Thread;
            return thread != null ? 
                string.Format("{{Thread {0}, Name=\"{1}\"}}", thread.ManagedThreadId, thread.Name) : 
                string.Format("{{{0}:{1}}}", this.SyncObject, this.Role);
        }
    }


    internal class Edge : IEquatable<Edge>
    {
        public readonly Node Predecessor;
        public readonly Node Successor;

        public int Counter;
        public int LastChange;

        public Edge(Node predecessor, Node successor)
        {
            this.Predecessor = predecessor;
            this.Successor = successor;
            this.Counter = 0;
            this.LastChange = Environment.TickCount;
        }


        public bool Equals(Edge other)
        {
            return this.Successor.Equals(other.Successor) && this.Predecessor.Equals(other.Predecessor);
        }

        public override int GetHashCode()
        {
            return this.Successor.GetHashCode() | ~this.Predecessor.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals((Edge)obj);
        }

        public override string ToString()
        {
            return string.Format("{{{0}}}->{{{1}}}, Counter={2}",
                                  this.Predecessor,
                                  this.Successor,
                                  this.Counter);
        }
    }
}