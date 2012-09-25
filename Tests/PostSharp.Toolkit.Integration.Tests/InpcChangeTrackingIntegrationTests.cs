using System.ComponentModel;

using NUnit.Framework;

using PostSharp.Toolkit.Domain;

namespace PostSharp.Toolkit.Integration.Tests
{
    // ReSharper disable InconsistentNaming

    [TestFixture]
    public class InpcChangeTrackingIntegrationTests : BaseTestsFixture
    {
        [Test]
        public void ChangeTrackingAnInpc_WhenPropertiesChangedAndUndoRequested_UndoExecutesAndEventsAreRaised()
        {
            InpcAndChangeTrackingEnabled sut = new InpcAndChangeTrackingEnabled();

            sut.PropertyChanged += UniversalPropertyChangedHandler;

            ObjectTracker.Track( sut );
            var restorePoint = ObjectTracker.SetRestorePoint( sut );

            sut.StringProperty = "string";
            sut.IntProperty = 1;

            ObjectTracker.UndoTo( sut, restorePoint );

            Assert.AreEqual(null, sut.StringProperty);
            Assert.AreEqual(0, sut.IntProperty );

            Assert.AreEqual(2, this.GetEventCount(sut, s => s.StringProperty ));
            Assert.AreEqual(2, this.GetEventCount(sut, s => s.IntProperty));
        }

        [Test]
        public void ChangeTrackingAnInpc_WhenNestedPropertyChangedAndUndoRequested_UndoExecutesAndEventsAreRaised()
        {
            InpcAndChangeTrackingEnabled sut = new InpcAndChangeTrackingEnabled();

            sut.PropertyChanged += UniversalPropertyChangedHandler;

            ObjectTracker.Track(sut);

            var nested = new InpcAndChangeTrackingNested();

            nested.PropertyChanged += this.UniversalPropertyChangedHandler;

            sut.Nested = nested;

            var restorePoint = ObjectTracker.SetRestorePoint(sut);

            sut.StringProperty = "string";
            sut.IntProperty = 1;

            nested.FirstName = "first";
            nested.LastName = "last";

            ObjectTracker.UndoTo(sut, restorePoint);

            Assert.AreEqual(null, sut.StringProperty);
            Assert.AreEqual(0, sut.IntProperty);
            Assert.AreEqual(" ", sut.Nested.Name);

            Assert.AreEqual(2, this.GetEventCount(sut, s => s.StringProperty ));
            Assert.AreEqual(2, this.GetEventCount(sut, s => s.IntProperty ));
            Assert.AreEqual(5, this.GetEventCount(sut, s => s.NestedName ));
            Assert.AreEqual(5, this.GetEventCount(sut, s => s.NestedIndirectName ));
            Assert.AreEqual(2, this.GetEventCount(nested, s => s.FirstName ));
            Assert.AreEqual(2, this.GetEventCount(nested, s => s.LastName ));
        }
    }

    // ReSharper restore InconsistentNaming 

    public class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    [TrackedObject(true)]
    [NotifyPropertyChanged]
    public class InpcAndChangeTrackingNested : NotifyPropertyChangedBase
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Name
        {
            get
            {
                return this.FirstName + " " + this.LastName;
            }
        }
    }

    [TrackedObject(true)]
    [NotifyPropertyChanged]
    public class InpcAndChangeTrackingEnabled : NotifyPropertyChangedBase
    {
        [NestedTrackedObject]
        public InpcAndChangeTrackingNested Nested { get; set; }

        public string StringProperty { get; set; }

        public int IntProperty { get; set; }

        public string NestedLastName
        {
            get
            {
                return this.Nested.LastName;
            }
        }

        public string NestedName
        {
            get
            {
                return this.Nested.Name;
            }
        }

        public string NestedIndirectName
        {
            get
            {
                if (Depends.Guard) 
                    Depends.On(this.Nested.FirstName, this.Nested.LastName);

                return this.GetName();
            }
        }

        private string GetName()
        {
            var firstName = this.Nested.FirstName;
            var lastName = this.Nested.LastName;

            return lastName + " " + firstName;
        }
    }
}