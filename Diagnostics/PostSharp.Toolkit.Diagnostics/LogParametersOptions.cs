using System;

namespace PostSharp.Toolkit.Diagnostics
{
    /// <summary>
    /// Specifies the options for logging parameter names, types and values.
    /// </summary>
    [Flags]
    public enum LogParametersOptions
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
        IncludeParameterValue = 4
    }
}