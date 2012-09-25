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