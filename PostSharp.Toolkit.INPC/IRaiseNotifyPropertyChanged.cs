using System.ComponentModel;

namespace PostSharp.Toolkit.INPC
{
    public interface IRaiseNotifyPropertyChanged : INotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }
}