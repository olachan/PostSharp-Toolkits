using System;
using PostSharp.Sdk.CodeModel;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public interface ILoggingCategoryBuilder
    {
        bool SupportsIsEnabled { get; }

        void EmitGetIsEnabled(InstructionWriter writer, LogLevel logLevel);

        void EmitWrite(InstructionWriter writer, string messageFormattingString, int argumentsCount,
                       LogLevel logLevel, Action<InstructionWriter> getExceptionAction,
                       Action<int, InstructionWriter> loadArgumentAction, bool useWrapper);
    }
}