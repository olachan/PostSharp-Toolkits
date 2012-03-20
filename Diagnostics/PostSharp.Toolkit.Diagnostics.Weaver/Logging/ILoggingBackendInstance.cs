using System;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public interface ILoggingBackendInstance
    {
        ILoggingCategoryBuilder GetCategoryBuilder(string categoryName);
    }
}