// @ExpectedMessage(DOM016)
// @ExpectedMessage(PS0060)

using System;
using System.ComponentModel;

namespace PostSharp.Toolkit.Domain.BuilTests.ChangeTracking
{
    namespace EditableObjectWithPrivateImplementation
    {
        class Program
        {
            public static int Main()
            {
                return 1;
            }
        }

        [EditableObject]
        public class EditableObjectWithPrivateImplementation : IEditableObject
        {
            public void BeginEdit()
            {
                throw new NotImplementedException();
            }

            public void EndEdit()
            {
                throw new NotImplementedException();
            }

            public void CancelEdit()
            {
                throw new NotImplementedException();
            }
        }
    }
}
