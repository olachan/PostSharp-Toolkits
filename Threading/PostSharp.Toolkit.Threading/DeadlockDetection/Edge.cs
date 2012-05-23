#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Threading.DeadlockDetection
{
    internal sealed class Edge : IEquatable<Edge>
    {
        public readonly Node Predecessor;
        public readonly Node Successor;

        public int Counter;
        public int LastChange;

        public Edge( Node predecessor, Node successor )
        {
            this.Predecessor = predecessor;
            this.Successor = successor;
            this.Counter = 0;
            this.LastChange = Environment.TickCount;
        }


        public bool Equals( Edge other )
        {
            return this.Successor.Equals( other.Successor ) && this.Predecessor.Equals( other.Predecessor );
        }

        public override int GetHashCode()
        {
            return this.Successor.GetHashCode() | ~this.Predecessor.GetHashCode();
        }

        public override bool Equals( object obj )
        {
            return this.Equals( (Edge) obj );
        }

        public override string ToString()
        {
            return string.Format( "{{{0}}}->{{{1}}}, Counter={2}",
                                  this.Predecessor,
                                  this.Successor,
                                  this.Counter );
        }
    }
}