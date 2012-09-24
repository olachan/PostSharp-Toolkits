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
    public abstract class UndoRedoButtonBase : Button, INotifyPropertyChanged
    {
        private const string splitElementName = "PART_SplitElement";

        private UIElement splitElement;

        public IEnumerable<Operation> Operations { get; protected set; }

        protected bool IsMouseOverSplitElement { get; private set; }

        protected UndoRedoButtonBase()
        {
            this.DefaultStyleKey = typeof(UndoRedoButtonBase);
        }

        public static readonly DependencyProperty IsListEnabledProperty = DependencyProperty.Register(
            "IsListEnabled", typeof(bool), typeof(UndoRedoButtonBase), new PropertyMetadata(true));

        public bool IsListEnabled
        {
            get
            {
                return (bool)GetValue(IsListEnabledProperty);
            }
            set
            {
                SetValue(IsListEnabledProperty, value);
            }
        }

        public static readonly DependencyProperty MaxOperationsCountProperty = DependencyProperty.Register(
            "MaxOperationsCount", typeof(int), typeof(UndoRedoButtonBase), new PropertyMetadata(10));

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
            "HistoryTracker", typeof(HistoryTracker), typeof(UndoRedoButtonBase), new PropertyMetadata(default(HistoryTracker), HistoryTrackerChanged));

        private bool isOpen;

        private static void HistoryTrackerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            UndoRedoButtonBase sb = (UndoRedoButtonBase)dependencyObject;

            if (dependencyPropertyChangedEventArgs.OldValue != null)
            {
                HistoryTracker ht = (HistoryTracker)dependencyPropertyChangedEventArgs.OldValue;
                ht.UndoOperationsChanged -= sb.HistoryTrackerOnOperationsChanged;
                ht.RedoOperationsChanged -= sb.HistoryTrackerOnOperationsChanged;
            }

            if (dependencyPropertyChangedEventArgs.NewValue != null)
            {
                sb.HistoryTracker.UndoOperationsChanged += sb.HistoryTrackerOnOperationsChanged;
                sb.HistoryTracker.RedoOperationsChanged += sb.HistoryTrackerOnOperationsChanged;
            }

            sb.SetOperations();
            sb.CoerceValue(UIElement.IsEnabledProperty);
        }

        private void HistoryTrackerOnOperationsChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            this.SetOperations();
            this.CoerceValue(UIElement.IsEnabledProperty);
        }

        protected abstract void SetOperations();

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
                return base.IsEnabledCore && this.Operations != null && this.Operations.Any();
            }
        }

        public abstract ICommand RevertOperationCommand { get; }


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
                this.OnClickCore();
            }
        }

        protected abstract void OnClickCore();

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
            if (this.Operations.Any())
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

        protected sealed class UndoRedoCommand : ICommand
        {
            private readonly UndoRedoButtonBase undoRedoButtonBase;

            private readonly bool undo;

            public UndoRedoCommand(UndoRedoButtonBase undoRedoButtonBase, bool undo)
            {
                this.undoRedoButtonBase = undoRedoButtonBase;
                this.undo = undo;
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                if (undo)
                {
                    this.undoRedoButtonBase.HistoryTracker.UndoTo(parameter as Operation);
                }
                else
                {
                    this.undoRedoButtonBase.HistoryTracker.RedoTo(parameter as Operation);
                }
                this.undoRedoButtonBase.IsOpen = false;
            }

            public event EventHandler CanExecuteChanged;
        }
    }
}
