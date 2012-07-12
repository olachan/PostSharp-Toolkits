#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections;
using System.Collections.Generic;

namespace PostSharp.Toolkit.Domain
{
    internal class StackContext : IEnumerable<object>
    {
        //TODO: Consider refactoring. Significantly.
        //We may avoid storing the same object multiple times (whenevering entering a method check if stack trace modification is needed). 
        //      - this would yield necessity to check if current method is outer most of current class (we would have to store some kind of counter)
        //Or maybe that's the simples/fastest way?

        private Stack<object> stackTrace = new Stack<object>();

        public void PushOnStack( object o )
        {
            this.stackTrace.Push( o );
        }

        public bool Pop( object o )
        {
            this.stackTrace.Pop();
            return this.stackTrace.Peek() == o;
        }

        public object Peek()
        {
            return this.stackTrace.Peek();
        }

        public object Pop()
        {
            return this.stackTrace.Pop();
        }

        public int Count
        {
            get
            {
                return this.stackTrace.Count;
            }
        }

        public IEnumerator<object> GetEnumerator()
        {
            return this.stackTrace.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.stackTrace.GetEnumerator();
        }
    }
}