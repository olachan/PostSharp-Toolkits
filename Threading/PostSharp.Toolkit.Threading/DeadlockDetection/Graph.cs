#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal sealed class Graph
    {
        private readonly ConcurrentDictionary<Node, ConcurrentDictionary<Node, Edge>> adjacencyList;

        public Graph()
        {
            this.adjacencyList = new ConcurrentDictionary<Node, ConcurrentDictionary<Node, Edge>>();
        }

        public void RemoveAdjecentEdges( object resource, ResourceType resourceType )
        {
            Node node = new Node( resource, resourceType );
            ConcurrentDictionary<Node, Edge> n;
            this.adjacencyList.TryRemove( node, out n );
        }

        // this method is thread-safe because every edge is edited only by thread on the end of this edge. 
        public void AddEdge( object from, ResourceType fromType, object to, ResourceType toType )
        {
            Node fromNode = new Node( from, fromType );
            ConcurrentDictionary<Node, Edge> neighbourhood = this.adjacencyList.GetOrAdd( fromNode, n => new ConcurrentDictionary<Node, Edge>() );

            Node toNode = new Node( to, toType );

            Edge edge = neighbourhood.GetOrAdd( toNode, n => new Edge( fromNode, toNode ) );

            edge.Counter++;
        }

        // this method is thread-safe because every edge is edited only by thread on the end of this edge. 
        public void RemoveEdge( object from, ResourceType fromType, object to, ResourceType toType )
        {
            Node fromNode = new Node( from, fromType );
            ConcurrentDictionary<Node, Edge> neighbourhood;
            if ( !this.adjacencyList.TryGetValue( fromNode, out neighbourhood ) )
            {
                return;
            }

            Node toNode = new Node( to, toType );

            Edge edge;

            if ( !neighbourhood.TryGetValue( toNode, out edge ) )
            {
                return;
            }

            edge.Counter--;

            if ( edge.Counter == 0 )
            {
                neighbourhood.TryRemove( toNode, out edge );

                if ( neighbourhood.Count == 0 )
                {
                    this.adjacencyList.TryRemove( fromNode, out neighbourhood );
                }
            }

            return;
        }

        public bool DetectCycles( out IEnumerable<Edge> cycle )
        {
            Dictionary<Node, int> gamma = new Dictionary<Node, int>();

            Dictionary<Node, Edge> predecessors = new Dictionary<Node, Edge>();

            int currentGamma = 1;

            foreach ( Node r in this.adjacencyList.Keys )
            {
                if ( this.DetectCycleInStronglyConnectedComponent( gamma, r, ref currentGamma, predecessors, out cycle ) )
                {
                    return true;
                }
            }

            cycle = null;
            return false;
        }

        public bool DetectCycles( object startingVertex, ResourceType startingVertexType, out IEnumerable<Edge> cycle )
        {
            Dictionary<Node, int> gamma = new Dictionary<Node, int>();

            Dictionary<Node, Edge> predecessors = new Dictionary<Node, Edge>();

            int currentGamma = 1;

            Node root = new Node( startingVertex, startingVertexType );

            return this.DetectCycleInStronglyConnectedComponent( gamma, root, ref currentGamma, predecessors, out cycle );
        }

        private bool DetectCycleInStronglyConnectedComponent( Dictionary<Node, int> gamma, Node r, ref int currentGamma, Dictionary<Node, Edge> predecessors,
                                                              out IEnumerable<Edge> cycle )
        {
            if ( gamma.ContainsKey( r ) )
            {
                cycle = null;
                return false;
            }

            gamma.Add( r, currentGamma++ );

            Stack<Edge> edgeStack = new Stack<Edge>();

            this.AddAdjecentEdgesToStack( r, edgeStack );

            while ( edgeStack.Count > 0 )
            {
                Edge e = edgeStack.Pop();

                if ( !gamma.ContainsKey( e.Successor ) )
                {
                    gamma.Add( e.Successor, currentGamma++ );
                    predecessors.Add( e.Successor, e );
                    this.AddAdjecentEdgesToStack( e.Successor, edgeStack );
                }
                else
                {
                    if ( gamma[e.Predecessor] > gamma[e.Successor] && // is not a forward edge
                         gamma[r] <= gamma[e.Successor] ) // is not a cross edge
                    {
                        IEnumerable<Edge> tempCycle = this.GetCycle( e, predecessors );
                        if ( tempCycle.Skip( 2 ).Any() )
                            // Ignoring cycles containig only 2 edges these are not deadlocks (e.g. thread having upgradeableReadLock waiting for writerLock)
                        {
                            cycle = tempCycle;
                            return true;
                        }
                    }
                }
            }

            cycle = null;
            return false;
        }

        private IEnumerable<Edge> GetCycle( Edge e, Dictionary<Node, Edge> predecessors )
        {
            Edge cerrentEdge;
            Node currentNode = e.Predecessor;

            yield return e;

            while ( predecessors.TryGetValue( currentNode, out cerrentEdge ) )
            {
                yield return cerrentEdge;
                currentNode = cerrentEdge.Predecessor;
            }
        }

        private void AddAdjecentEdgesToStack( Node v, Stack<Edge> edgeStack )
        {
            if ( !this.adjacencyList.ContainsKey( v ) )
            {
                return;
            }

            foreach ( KeyValuePair<Node, Edge> e in this.adjacencyList[v].Where( x => Environment.TickCount - x.Value.LastChange > 100 ) ) // ignore young edges
            {
                edgeStack.Push( e.Value );
            }
        }
    }
}