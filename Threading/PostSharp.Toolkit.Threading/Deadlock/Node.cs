#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;

namespace PostSharp.Toolkit.Threading.Deadlock
{
    internal class Node : IEquatable<Node>
    {
        public readonly object SyncObject;

        public readonly ResourceType Role;

        public Node( object syncObject, ResourceType role )
        {
            this.SyncObject = syncObject;
            this.Role = role;
        }

        public bool Equals( Node other )
        {
            return Equals( this.SyncObject, other.SyncObject ) && this.Role == other.Role;
        }

        public override bool Equals( object obj )
        {
            return this.Equals( (Node) obj );
        }

        public override int GetHashCode()
        {
            return (this.SyncObject.GetHashCode() << 16) | this.Role.GetHashCode();
        }

        public override string ToString()
        {
            Thread thread = this.SyncObject as Thread;
            return thread != null
                       ? string.Format( "{{Thread {0}, Name=\"{1}\"}}", thread.ManagedThreadId, thread.Name )
                       : string.Format( "{{{0}:{1}}}", this.SyncObject, this.Role );
        }
    }
}