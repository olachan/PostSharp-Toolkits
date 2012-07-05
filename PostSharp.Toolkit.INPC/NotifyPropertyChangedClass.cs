using System.ComponentModel;

namespace PostSharp.Toolkit.INPC
{
    public abstract class NotifyPropertyChangedClass : IRaiseNotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged( string propertyName )
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs( propertyName ));
            }
        }
    }
}