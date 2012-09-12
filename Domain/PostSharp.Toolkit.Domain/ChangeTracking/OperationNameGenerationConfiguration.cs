namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    internal sealed class OperationNameGenerationConfiguration
    {
        private static readonly OperationNameGenerationConfiguration defaultConfiguration = 
            new OperationNameGenerationConfiguration()
            {
                FieldSetOperationStringFormat = "{0} set",
                MethodOperationStringFormat = "{0}",
                UndoOperationStringFormat = "Undo - {0}",
                RedoOperationStringFormat = "Redo - {0}",
                UndoToStringFormat = "Undo to {0}",
                RedoToStringFormat = "Redo to {0}"
            };

        public static OperationNameGenerationConfiguration Default { get { return defaultConfiguration; } }

        public string FieldSetOperationStringFormat { get; set; }

        public string MethodOperationStringFormat { get; set; }

        public string UndoOperationStringFormat { get; set; }

        public string RedoOperationStringFormat { get; set; }

        public string UndoToStringFormat { get; set; }

        public string RedoToStringFormat { get; set; }
    }
}