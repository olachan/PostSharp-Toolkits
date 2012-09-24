using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using PostSharp.Toolkit.Domain.ChangeTracking;

namespace PostSharp.Toolkit.Domain.Controls
{
    [TemplatePart(Name = splitElementName, Type = typeof(UIElement))]
    public class UndoRedoButton : Button, INotifyPropertyChanged
    {
        private const string splitElementName = "PART_SplitElement";
        
        private UIElement splitElement;

        public IEnumerable<Operation> UndoOperations { get; private set; }

        protected bool IsMouseOverSplitElement { get; private set; }

        public UndoRedoButton()
        {
            this.DefaultStyleKey = typeof(UndoRedoButton);
        }

        public static readonly DependencyProperty MaxOperationsCountProperty = DependencyProperty.Register(
            "MaxOperationsCount", typeof(int), typeof(UndoRedoButton), new PropertyMetadata(10));

        public int MaxOperationsCount
        {
            get
            {
                return (int)GetValue(MaxOperationsCountProperty);
            }
            set
            {
                SetValue(MaxOperationsCountProperty, value);
            }
        }

        public static readonly DependencyProperty HistoryTrackerProperty = DependencyProperty.Register(
            "HistoryTracker", typeof(HistoryTracker), typeof(UndoRedoButton), new PropertyMetadata(default(HistoryTracker), HistoryTrackerChanged));

        private bool isOpen;

        private static void HistoryTrackerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UndoRedoButton sb = (UndoRedoButton)dependencyObject;

            if (dependencyPropertyChangedEventArgs.OldValue != null)
            {
                HistoryTracker ht = (HistoryTracker)dependencyPropertyChangedEventArgs.OldValue;
                ht.UndoOperationsChanged -= sb.HistoryTrackerOnUndoOperationsChanged;
                ht.RedoOperationsChanged -= sb.HistoryTrackerOnRedoOperationsChanged;
            }

            if (dependencyPropertyChangedEventArgs.NewValue != null)
            {
                sb.HistoryTracker.UndoOperationsChanged += sb.HistoryTrackerOnUndoOperationsChanged;
                sb.HistoryTracker.RedoOperationsChanged += sb.HistoryTrackerOnRedoOperationsChanged;
            }

            sb.SetUndoOperations();

        }

        private void HistoryTrackerOnRedoOperationsChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {

        }

        private void HistoryTrackerOnUndoOperationsChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            this.SetUndoOperations();
        }

        private void SetUndoOperations()
        {
            this.UndoOperations = this.HistoryTracker.UndoOperations.Reverse().Take(MaxOperationsCount);
            this.OnPropertyChanged("UndoOperations");
        }

        public HistoryTracker HistoryTracker
        {
            get
            {
                return (HistoryTracker)GetValue(HistoryTrackerProperty);
            }
            set
            {
                SetValue(HistoryTrackerProperty, value);
            }
        }

        protected override bool IsEnabledCore
        {
            get
            {
                return this.UndoOperations != null && this.UndoOperations.Any();
            }
        }

        public ICommand UndoToCommand
        {
            get
            {
                return this.HistoryTracker != null ? new UndoCommand(this) : null;
            }
        }

        public override void OnApplyTemplate()
        {
            if (null != this.splitElement)
            {
                this.splitElement.MouseEnter -= this.SplitElementMouseEnter;
                this.splitElement.MouseLeave -= this.SplitElementMouseLeave;
                this.splitElement = null;
            }

            base.OnApplyTemplate();

            this.splitElement = this.GetTemplateChild(splitElementName) as UIElement;

            if (null != this.splitElement)
            {
                this.splitElement.MouseEnter += this.SplitElementMouseEnter;
                this.splitElement.MouseLeave += this.SplitElementMouseLeave;
            }

        }

        protected override void OnClick()
        {
            if (this.IsMouseOverSplitElement)
            {
                this.OpenButtonMenu();
            }
            else
            {
                base.OnClick();

                if (this.HistoryTracker != null)
                {
                    this.HistoryTracker.Undo();
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            if (null == eventArgs)
            {
                throw new ArgumentNullException("eventArgs");
            }

            if ((Key.Down == eventArgs.Key) || (Key.Up == eventArgs.Key))
            {
                this.Dispatcher.BeginInvoke((Action)(this.OpenButtonMenu));
            }
            else
            {
                base.OnKeyDown(eventArgs);
            }
        }

        protected void OpenButtonMenu()
        {
            if (this.UndoOperations.Any())
            {
                this.IsOpen = true;
            }
        }

        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }
            set
            {
                this.isOpen = value;
                this.OnPropertyChanged("IsOpen");
            }
        }

        private void SplitElementMouseEnter(object sender, MouseEventArgs e)
        {
            this.IsMouseOverSplitElement = true;
        }

        private void SplitElementMouseLeave(object sender, MouseEventArgs e)
        {
            this.IsMouseOverSplitElement = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private sealed class UndoCommand : ICommand
        {
            private readonly UndoRedoButton undoRedoButton;

            public UndoCommand(UndoRedoButton undoRedoButton)
            {
                this.undoRedoButton = undoRedoButton;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                this.undoRedoButton.HistoryTracker.UndoTo(parameter as Operation);
                this.undoRedoButton.IsOpen = false;
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}
