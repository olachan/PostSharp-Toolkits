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
    /// Specifies the severity of a logged message.
    /// </summary>
    [Serializable]
    public enum LogLevel
    {
        /// <summary>
        /// No message should be logged.
        /// </summary>
        None,

        /// <summary>
        /// The message should be logged at Debug/Trace level (when applicable).
        /// </summary>
        Debug,

        /// <summary>
        /// The message should be logged at Info level (when applicable).
        /// </summary>
        Info,

        /// <summary>
        /// The message should be logged at Warning level (when applicable).
        /// </summary>
        Warning,

        /// <summary>
        /// The message should be logged at Error level (when applicable).
        /// </summary>
        Error,

        /// <summary>
        /// The message should be logged at Fatal level (when applicable).
        /// </summary>
        Fatal,
    }
}