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
        private const int UnvisitedVertex = 0;

        private readonly Dictionary<Node, Dictionary<Node, Edge>> adjacencyList;

        public Graph()
        {
            this.adjacencyList = new Dictionary<Node, Dictionary<Node, Edge>>();
        }

        public bool AddEdge(object from, object fromObjectInfo, ResourceType fromType, object to, object toObjectInfo, ResourceType toType)
        {
            var fromNode = new Node(from, fromType);
            Dictionary<Node, Edge> neighbourhood;
            if (!this.adjacencyList.TryGetValue(fromNode, out neighbourhood))
            {
                neighbourhood = new Dictionary<Node, Edge>();
                this.adjacencyList.Add(fromNode, neighbourhood);
            }

            var toNode = new Node(to, toType);
            Edge edge;

            if (!neighbourhood.TryGetValue(toNode, out edge))
            {
                edge = new Edge(fromNode, toNode, fromObjectInfo, toObjectInfo);
                neighbourhood.Add(toNode, edge);
                return true;
            }

            edge.Counter++;

            return false;
        }

        public bool RemoveEdge(object from, ResourceType fromType, object to, ResourceType toType)
        {
            var fromNode = new Node(from, fromType);
            Dictionary<Node, Edge> neighbourhood;
            if (!this.adjacencyList.TryGetValue(fromNode, out neighbourhood))
            {
                return false;
            }

            var toNode = new Node(to, toType);

            Edge edge;

            if (!neighbourhood.TryGetValue(toNode, out edge))
            {
                return false;
            }

            edge.Counter--;

            if (edge.Counter == 0)
            {
                neighbourhood.Remove(toNode);

                if (neighbourhood.Count == 0)
                {
                    this.adjacencyList.Remove(fromNode);
                }
            }

            return true;
        }

        public bool DetectCycles(out IList<Edge> cycle)
        {
            var gamma = this.adjacencyList.Keys.ToDictionary(v => v, v => 0);

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

        public bool DetectCycles(object startingVertex, ResourceType startingVertexType, out IList<Edge> cycle)
        {
            var gamma = this.adjacencyList.Keys.ToDictionary(v => v, v => 0);

            var predecessors = new Dictionary<Node, Edge>();

            int currentGamma = 1;

            var root = new Node(startingVertex, startingVertexType);

            return this.DetectCycleInStronglyConnectedComponent(gamma, root, ref currentGamma, predecessors, out cycle);
        }

        private bool DetectCycleInStronglyConnectedComponent(Dictionary<Node, int> gamma, Node r, ref int currentGamma, Dictionary<Node, Edge> predecessors, out IList<Edge> cycle)
        {
            if (gamma[r] != UnvisitedVertex)
            {
                cycle = null;
                return false;
            }

            gamma[r] = currentGamma++;

            var edgeStack = new Stack<Edge>();

            this.AddAdjecentEdgesToStack(r, edgeStack, gamma);

            while (edgeStack.Count > 0)
            {
                var e = edgeStack.Pop();

                if (gamma[e.Successor] == UnvisitedVertex)
                {
                    gamma[e.Successor] = currentGamma++;
                    predecessors.Add(e.Successor, e);
                    this.AddAdjecentEdgesToStack(e.Successor, edgeStack, gamma);
                }
                else
                {
                    if (gamma[e.Predecessor] > gamma[e.Successor] && // is not a forward back edge
                        gamma[r] <= gamma[e.Successor]) // is not a cross edge
                    {
                        cycle = this.GetCycle(e, predecessors).Reverse().ToList();

                        return true;
                    }
                }
            }

            cycle = null;
            return false;
        }

        private IList<Edge> GetCycle(Edge e, Dictionary<Node, Edge> predecessors)
        {
            IList<Edge> cycle = new List<Edge> { e };

            Edge cerrentEdge;
            Node currentNode = e.Predecessor;

            while (predecessors.TryGetValue(currentNode, out cerrentEdge))
            {
                cycle.Add(cerrentEdge);
                currentNode = cerrentEdge.Predecessor;
            }

            return cycle;
        }

        private void AddAdjecentEdgesToStack(Node v, Stack<Edge> edgeStack, Dictionary<Node, int> color)
        {
            if (!this.adjacencyList.ContainsKey(v))
            {
                return;
            }

            foreach (var e in this.adjacencyList[v].Where(x => Environment.TickCount - x.Value.LastChange > 50)) // ignore young edges
            {
                int c;
                if (!color.TryGetValue(e.Value.Successor, out c))
                {
                    c = UnvisitedVertex;
                    color.Add(e.Value.Successor, c);
                }

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
            return Format(null);
        }

        public string Format(object objInfo)
        {
            Thread thread = this.SyncObject as Thread;
            if (thread != null)
            {
                return string.Format("{{Thread {0}, Name=\"{1}\"}}", thread.ManagedThreadId, thread.Name);
            }
            else
            {
                return string.Format("{{{0}:{1}}}", objInfo ?? this.SyncObject, this.Role);
            }
        }
    }


    internal class Edge : IEquatable<Edge>
    {
        public readonly Node Predecessor;
        public readonly Node Successor;

        public readonly object PredecessorInfo;
        public readonly object SuccessorInfo;

        public int Counter;
        public int LastChange;

        public Edge(Node predecessor, Node successor, object predecessorInfo, object successorInfo)
        {
            this.Predecessor = predecessor;
            this.Successor = successor;
            this.PredecessorInfo = predecessorInfo;
            this.SuccessorInfo = successorInfo;
            this.Counter = 1;
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
                                  this.Predecessor.Format(this.PredecessorInfo),
                                  this.Successor.Format(this.SuccessorInfo),
                                  this.Counter);
        }
    }
}