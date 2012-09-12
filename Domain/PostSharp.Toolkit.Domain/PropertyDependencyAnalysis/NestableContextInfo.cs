#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Domain.PropertyDependencyAnalysis
{
    internal abstract class NestableContextInfo : IDisposable
    {
        protected INestableContext Owner;

        internal void RegisterOwner( INestableContext owner )
        {
            this.Owner = owner;
        }

        public void Dispose()
        {
            if ( this.Owner != null )
            {
                this.Owner.Pop();
            }
        }
    }
}