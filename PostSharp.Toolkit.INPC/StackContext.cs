using System.Collections;
using System.Collections.Generic;

namespace PostSharp.Toolkit.INPC
{
    internal class StackContext : IEnumerable<object>
    {
        //TODO: Consider refactoring. Significantly.
        //We may avoid storing the same object multiple times (whenevering entering a method check if stack trace modification is needed).
        //Or maybe that's the simples/fastest way?

        private Stack<object> stackTrace = new Stack<object>();
        
        public void PushOnStack(object o)
        {
            this.stackTrace.Push(o);
        }

        public bool Pop(object o)
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
            get { return this.stackTrace.Count; }
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