#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;

namespace PostSharp.Toolkit.Diagnostics
{
    /// <summary>
    /// Specifies the options for logging parameter names, types and values.
    /// </summary>
    [Flags]
    [Serializable]
    public enum LogOptions
    {
        /// <summary>
        /// No parameter information included.
        /// </summary>
        None = 0,

        /// <summary>
        /// Includes parameter type information.
        /// </summary>
        IncludeParameterType = 1,

        /// <summary>
        /// Includes parameter name.
        /// </summary>
        IncludeParameterName = 2,

        /// <summary>
        /// Includes parameter value, by calling <see cref="object.ToString"/> on the object instance.
        /// </summary>
        IncludeParameterValue = 4,

        /// <summary>
        /// Includes the return value.
        /// </summary>
        IncludeReturnValue = 8,

        /// <summary>
        /// Includes the value of <c>this</c> argument in an instance method.
        /// </summary>
        IncludeThisArgument = 16,
    }
}