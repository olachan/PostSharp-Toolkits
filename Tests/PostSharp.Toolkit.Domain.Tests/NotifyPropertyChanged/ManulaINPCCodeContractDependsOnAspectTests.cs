#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.ComponentModel;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests.NotifyPropertyChanged
{
    [TestFixture]
    public class ManulaINPCCodeContractDependsOnAspectTests
    {
        [Test]
        public void SimpleDependsOn()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClassCC();
                        c.InnerObject.Str1 = "asd";
                        c.InnerObject.Str2 = "asfd";
                    },
                3,
                "ConcatFromInnerObject" );
        }

        [Test]
        [Ignore] // TODO: implement this with compile time error based on depends on string
        [ExpectedException(typeof(NotInstrumentedClassInDependsOnException))]
        public void DependsOnWithNotInstrumentedDependency()
        {
            TestHelpers.DoInpcTest<InpcWithNotInstrumentedDependenciesCC>(
                c =>
                {
                    c.InnerObject = new NotInstrumentedInpcCC();
                    c.InnerObject.Property = "asd";
                },
                2,
                "StringFromNotInstrumented");
        }

        [Test]
        public void TwoLevelDependsOn()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClassCC();
                        c.InnerObject.SuperInnrObject = new InpcSuperInnrClassCC();
                        c.InnerObject.SuperInnrObject.Str1 = "afaskhjkhf";
                        c.InnerObject.SuperInnrObject.Str2 = "sgfhdhd";
                    },
                4,
                "ConcatFromSuperInnerObject" );
        }

        [Test]
        public void TwoLevelDependsOn_WithNonAutoProperty()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                {
                    c.InnerObject = new InpcInnrClassCC();
                    c.InnerObject.SuperInnrObjectNonAuto = new InpcSuperInnrClassCC();
                    c.InnerObject.SuperInnrObjectNonAuto.Str1 = "afasf";
                    c.InnerObject.SuperInnrObjectNonAuto.Str2 = "sgfhdhd";
                },
                4,
                "ConcatFromSuperInnerObjectNonAuto");
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                {
                    c.InnerObject2 = new InpcInnrClassCC();
                    c.InnerObjectProperty.SuperInnrObject = new InpcSuperInnrClassCC();
                    c.InnerObjectProperty.SuperInnrObject.Str1 = "afasf";
                    c.InnerObjectProperty.SuperInnrObject.Str2 = "sgfhdhd";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty_SideObjectChange()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                {
                    var innerObject = new InpcInnrClassCC();
                    c.InnerObject2 = innerObject;
                    var superInnerObject = new InpcSuperInnrClassCC();
                    innerObject.SuperInnrObject = superInnerObject;
                    superInnerObject.Str1 = "sadf";
                    superInnerObject.Str2 = "sag";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty_PartlySideObjectChange()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependenciesCC>(
                c =>
                {
                    c.InnerObject2 = new InpcInnrClassCC();
                    var a = c.ConcatFromSuperInnerObjectViaProperty;
                    var superInnerObject = new InpcSuperInnrClassCC();
                    c.InnerObject2.SuperInnrObject = superInnerObject;
                    superInnerObject.Str1 = "sadf";
                    superInnerObject.Str2 = "sag";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
        }
    }

    [NotifyPropertyChanged]
    public class InpcSuperInnrClassCC
    {
        public string Str1;

        public string Str2;

        public string StrConcat
        {
            get
            {
                return this.Str1 + this.Str2;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcInnrClassCC
    {
        public string Str1;

        public string Str2;
        private InpcSuperInnrClassCC superInnrObjectNonAuto;

        public string StrConcat
        {
            get
            {
                return this.Str1 + this.Str2;
            }
        }

        public void SetProperty(InpcSuperInnrClassCC c)
        {
            this.Str1 = "sdfjkh";
            this.Str2 = "sdkafkl";
            c.Str1 = "sdjkafh";
            c.Str2 = "sdjkafh";
        }

        public InpcSuperInnrClassCC SuperInnrObject { get; set; }

        public InpcSuperInnrClassCC SuperInnrObjectNonAuto
        {
            get { return this.superInnrObjectNonAuto; }
            set { this.superInnrObjectNonAuto = value; }
        }
    }

    public class NotInstrumentedInpcCC : INotifyPropertyChanged
    {
        private string property;

        public string Property
        {
            get
            {
                return this.property;
            }
            set
            {
                this.property = value;
                this.OnPropertyChanged("Property");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged( string propertyName )
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if ( handler != null )
            {
                handler( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcWithNotInstrumentedDependenciesCC
    {
        public NotInstrumentedInpcCC InnerObject;

        public string StringFromNotInstrumented
        {
            get
            {
                if (Depends.Guard)
                    Depends.On(this.InnerObject.Property);
                return this.InnerObject.Property;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcWithManualDependenciesCC
    {
        public InpcInnrClassCC InnerObject;

        public InpcInnrClassCC InnerObject2;

        public InpcInnrClassCC InnerObjectProperty
        {
            get
            {
                return this.InnerObject2;
            }
        }

        public void DoSyblingChange()
        {
            this.InnerObject.SetProperty( this.InnerObject2.SuperInnrObject );
        }

        public string ConcatFromInnerObject
        {
            get
            {
                if (Depends.Guard)
                    Depends.On(this.InnerObject.StrConcat);
                return this.InnerObject.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObject
        {
            get
            {
                if (Depends.Guard)
                    Depends.On(this.InnerObject.SuperInnrObject.StrConcat);
                return this.InnerObject.SuperInnrObject.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObjectNonAuto
        {
            get
            {
                if (Depends.Guard)
                    Depends.On(this.InnerObject.SuperInnrObjectNonAuto.StrConcat);

                var io = this.InnerObject;

                return io.SuperInnrObjectNonAuto.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObjectViaProperty
        {
            get
            {
                if (Depends.Guard)
                    Depends.On(this.InnerObjectProperty.SuperInnrObject.StrConcat);

                var iop = this.InnerObjectProperty;

                if (iop != null && iop.SuperInnrObject != null)
                {
                    return this.InnerObjectProperty.SuperInnrObject.StrConcat;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}