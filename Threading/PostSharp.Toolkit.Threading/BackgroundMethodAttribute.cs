#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a method, makes it asynchronous, i.e.
    /// queued to the <see cref="ThreadPool"/>.
    /// </summary>
    [Serializable]
    [MethodInterceptionAspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    public sealed class BackgroundMethodAttribute : MethodInterceptionAspect
    {
        // TODO [NOW]: Check that the method returns 'void', has no out/ref argument.

        /// <inheritdoc />
        public override void OnInvoke( MethodInterceptionArgs args )
        {
            TaskCreationOptions options = TaskCreationOptions.None;
            if ( this.IsLongRunning ) options |= TaskCreationOptions.LongRunning;

            Task task = new Task( args.Proceed, options );
            task.Start();
        }

        public bool IsLongRunning { get; set; }
    }
}