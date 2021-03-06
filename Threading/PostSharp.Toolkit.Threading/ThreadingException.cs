﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Runtime.Serialization;

namespace PostSharp.Toolkit.Threading
{
    [Serializable]
    public class ThreadingException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ThreadingException()
        {
        }

        public ThreadingException( string message ) : base( message )
        {
        }

        public ThreadingException( string message, Exception inner ) : base( message, inner )
        {
        }

        protected ThreadingException(
            SerializationInfo info,
            StreamingContext context ) : base( info, context )
        {
        }
    }
}