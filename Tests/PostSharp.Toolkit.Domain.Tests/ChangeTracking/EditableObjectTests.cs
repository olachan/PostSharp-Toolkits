using System;
using System.ComponentModel;
using NUnit.Framework;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Tests.ChangeTracking
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class EditableObjectTests
    {
        [Test]
        public void EditableObjectTest_CancelEditRollbackes()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;
            editable.StringP = "a";

            editable.Edit(1, "b");

            editable.CancelEdit();

            Assert.AreEqual(0, editable.IntP);
            Assert.IsNull(editable.StringP);
        }

        [Test]
        public void EditableObjectWithUndoWhileEditingTest_CancelEditRollbackes()
        {
            EditableTrackedObjectClass editable = new EditableTrackedObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;
            editable.StringP = "a";

            //((ITrackedObject)editable).Tracker.Undo();

            editable.Edit(1, "b");

            editable.CancelEdit();

            Assert.AreEqual(0, editable.IntP);
            Assert.IsNull(editable.StringP);
        }

        [Test]
        public void EditableObjectWithUndoWhileEditingTest_EndEditCommits()
        {
            EditableTrackedObjectClass editable = new EditableTrackedObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;
            editable.StringP = "a";

            ((ITrackedObject)editable).Tracker.Undo();

            editable.Edit(1, "b");

            editable.EndEdit();

            Assert.AreEqual(1, editable.IntP);
            Assert.AreEqual("b", editable.StringP);

            ((ITrackedObject)editable).Tracker.Undo();

            Assert.AreEqual(5, editable.IntP);
            Assert.IsNull(editable.StringP);
        }
    }
    // ReSharper restore InconsistentNaming 

    [EditableObject]
    [TrackedObject]
    public class EditableTrackedObjectClass : IEditableObject
    {
        public int IntP { get; set; }

        public string StringP { get; set; }

        public void Edit(int i, string s)
        {
            this.IntP = i;
            this.StringP = s;
        }

        public void BeginEdit()
        {
            throw new ToBeIntroducedException();
        }

        public void EndEdit()
        {
            throw new ToBeIntroducedException();
        }

        public void CancelEdit()
        {
            throw new ToBeIntroducedException();
        }
    }

    [EditableObject]
    public class EditableObjectClass : IEditableObject
    {
        public int IntP { get; set; }

        public string StringP { get; set; }

        public void Edit(int i, string s)
        {
            this.IntP = i;
            this.StringP = s;
        }

        public void BeginEdit()
        {
            throw new ToBeIntroducedException();
        }

        public void EndEdit()
        {
            throw new ToBeIntroducedException();
        }

        public void CancelEdit()
        {
            throw new ToBeIntroducedException();
        }
    }
}