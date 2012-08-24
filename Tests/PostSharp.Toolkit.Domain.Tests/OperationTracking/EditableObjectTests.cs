using System;
using System.ComponentModel;
using System.Diagnostics;

using NUnit.Framework;

using PostSharp.Toolkit.Domain.OperationTracking;

namespace PostSharp.Toolkit.Domain.Tests.OperationTracking
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
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;
            editable.StringP = "a";

            ((ITrackedObject)editable).Undo();

            editable.Edit(1, "b");

            editable.CancelEdit();

            Assert.AreEqual(0, editable.IntP);
            Assert.IsNull(editable.StringP);
        }

        [Test]
        public void EditableObjectWithUndoWhileEditingTest_EndEditCommits()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;
            editable.StringP = "a";

            ((ITrackedObject)editable).Undo();

            editable.Edit(1, "b");

            editable.EndEdit();

            Assert.AreEqual(1, editable.IntP);
            Assert.AreEqual("b", editable.StringP);

            ((ITrackedObject)editable).Undo();

            Assert.AreEqual(5, editable.IntP);
            Assert.IsNull(editable.StringP);
        }

        [Test]
        public void ChangeTracking_WhenEdited_SetsIsChanged()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;

            Assert.IsTrue(editable.IsChanged);

            editable.StringP = "a";

            Assert.IsTrue(editable.IsChanged);


            editable.Edit(1, "b");

            editable.EndEdit();

            Assert.IsTrue( editable.IsChanged );
        }

        [Test]
        public void ChangeTracking_WhenRollbacked_UnsetsIsChanged()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;

            Assert.IsTrue(editable.IsChanged);

            editable.StringP = "a";

            Assert.IsTrue(editable.IsChanged);


            editable.Edit(1, "b");

            editable.CancelEdit();

            Assert.IsFalse(editable.IsChanged);
        }

        [Test]
        public void ChangeTracking_WhenEditAfterRollback_SetsIsChanged()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;

            Assert.IsTrue(editable.IsChanged);

            editable.StringP = "a";

            Assert.IsTrue(editable.IsChanged);


            editable.Edit(1, "b");

            editable.CancelEdit();

            editable.Edit(1, "b");

            Assert.IsTrue(editable.IsChanged);
        }

        [Test]
        public void ChangeTracking_ChangesAccepted_UnsetsIsChangedAndClearsHistory()
        {
            EditableObjectClass editable = new EditableObjectClass();

            editable.BeginEdit();

            editable.IntP = 5;

            Assert.IsTrue(editable.IsChanged);

            editable.StringP = "a";

            Assert.IsTrue(editable.IsChanged);


            editable.Edit(1, "b");

            editable.EndEdit();

            editable.AcceptChanges();

            Assert.IsFalse(editable.IsChanged);

            ((ITrackedObject)editable).Undo();

            Assert.AreEqual(1, editable.IntP);
            Assert.AreEqual("b", editable.StringP);
        }
    }
    // ReSharper restore InconsistentNaming 

    [EditableObject]
    public class EditableObjectClass : IEditableObject, IChangeTracking
    {
        public int IntP { get; set; }

        public string StringP { get; set; }

        public void Edit(int i, string s)
        {
            IntP = i;
            StringP = s;
        }

        public void BeginEdit()
        {
            throw new System.NotImplementedException();
        }

        public void EndEdit()
        {
            throw new System.NotImplementedException();
        }

        public void CancelEdit()
        {
            throw new System.NotImplementedException();
        }

        public void AcceptChanges()
        {
            throw new System.NotImplementedException();
        }

        public bool IsChanged { get{throw new NotImplementedException();} }
    }
}