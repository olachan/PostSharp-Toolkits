using System.ComponentModel;
using System.Windows;
using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.TestApp
{
    [NotifyPropertyChanged]
    public class ViewModelBase
    {
        
    }

    [NotifyPropertyChanged]
    [TrackedObject]
    public class ModelBase
    {
    }

    [EditableObject]
    public class EditableModelBase : ModelBase, IEditableObject
    {
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
    }
}