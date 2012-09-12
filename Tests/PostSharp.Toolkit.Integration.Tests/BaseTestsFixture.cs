#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace PostSharp.Toolkit.Integration.Tests
{
    public class BaseTestsFixture
    {
        public ThreadSafeStringWriter TextWriter { get; private set; }

        public StringBuilder OutputString { get; private set; }

        protected Dictionary<Tuple<object, string>, int> FiredEvents;
        
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            // swallow all unobserved exceptions
            TaskScheduler.UnobservedTaskException += (sender, args) => args.SetObserved();
        }

        [SetUp]
        public virtual void SetUp()
        {
            FiredEvents = new Dictionary<Tuple<object, string>, int>();
            this.OutputString = new StringBuilder();
            this.TextWriter = new ThreadSafeStringWriter(this.OutputString);
            Console.SetOut(this.TextWriter);
            Trace.Listeners.Add(new TextWriterTraceListener(this.TextWriter));
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (this.TextWriter != null)
            {
                this.TextWriter.Dispose();
            }

            // wait for any pending exceptions from background tasks
            try
            {
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
            }
            catch { }

            FiredEvents.Clear();
        }

        protected void UniversalPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            var key = new Tuple<object, string>( sender, e.PropertyName );
            if (!this.FiredEvents.ContainsKey( key ))
            {
                this.FiredEvents.Add( key, 1 );
            }
            else
            {
                this.FiredEvents[key] += 1;
            }
        }

        protected int GetEventCount<T, TProperty>(T instance, Expression<Func<T, TProperty>> propertySelector)
        {
            var memberExpression = propertySelector.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = propertySelector.Body as UnaryExpression;
                if (unaryExpression != null)
                {
                    memberExpression = unaryExpression.Operand as MemberExpression;
                    if (memberExpression == null)
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();
            }

            var key = new Tuple<object, string>( instance, memberExpression.Member.Name );

            return this.FiredEvents.ContainsKey( key ) ? this.FiredEvents[key] : 0;
        }
    }

    /// <summary>
    /// thread safe StringWriter
    /// </summary>
    [Serializable]
    public class ThreadSafeStringWriter : TextWriter
    {
        private static UnicodeEncoding encoding;
        private readonly StringBuilder sb;
        private bool isOpen;

        public override Encoding Encoding
        {
            get
            {
                if (encoding == null)
                    encoding = new UnicodeEncoding(false, false);
                return encoding;
            }
        }

        public ThreadSafeStringWriter()
            : this(new StringBuilder(), CultureInfo.CurrentCulture)
        {
        }

        public ThreadSafeStringWriter(IFormatProvider formatProvider)
            : this(new StringBuilder(), formatProvider)
        {
        }

        public ThreadSafeStringWriter(StringBuilder sb)
            : this(sb, CultureInfo.CurrentCulture)
        {
        }

        public ThreadSafeStringWriter(StringBuilder sb, IFormatProvider formatProvider)
            : base(formatProvider)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");
            this.sb = sb;
            this.isOpen = true;
        }

        public override void Close()
        {
            this.Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            this.isOpen = false;
            base.Dispose(disposing);
        }

        public virtual StringBuilder GetStringBuilder()
        {
            return this.sb;
        }

        public override void Write(char value)
        {
            if (!this.isOpen)
                throw new ObjectDisposedException("ThreadSafeStringWriter");

            lock (this.sb)
            {
                this.sb.Append(value);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (buffer.Length - index < count)
                throw new ArgumentException();
            if (!this.isOpen)
                throw new ObjectDisposedException("ThreadSafeStringWriter");

            lock (this.sb)
            {
                this.sb.Append(buffer, index, count);
            }
        }

        public override void Write(string value)
        {
            if (!this.isOpen)
                throw new ObjectDisposedException("ThreadSafeStringWriter");
            if (value == null)
                return;

            lock (this.sb)
            {
                this.sb.Append(value);
            }
        }

        public override string ToString()
        {
            lock (this.sb)
            {
                return this.sb.ToString();
            }
        }
    }
}