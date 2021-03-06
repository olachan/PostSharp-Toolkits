﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

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
    public class ManulaNotifyPropertyChangedAspectTests
    {
        // TODO test checking if all events are raised on appropriate objects
        //[Test]
        //public void DependsOnHierarchyCheck()
        //{
        //    int masterEventCount = 0;
        //    int innerObjectEventCount = 0;
        //    int innerObject2EventCount = 0;
        //    int innerObjectSuperInnrObjectEventCount = 0;
        //    int innerObject2SuperInnrObjectEventCount = 0;

        //    InpcWithManualDependencies master = new InpcWithManualDependencies();
        //    ((INotifyPropertyChanged)master).PropertyChanged += ( s, a ) => masterEventCount++;
        //    master.InnerObject = new InpcInnrClass();
        //    ((INotifyPropertyChanged)master.InnerObject).PropertyChanged += (s, a) => innerObjectEventCount++;
        //    master.InnerObject2 = new InpcInnrClass();
        //    ((INotifyPropertyChanged)master.InnerObject2).PropertyChanged += (s, a) => innerObject2EventCount++;
        //    master.InnerObject.SuperInnrObject = new InpcSuperInnrClass();
        //    ((INotifyPropertyChanged)master.InnerObject.SuperInnrObject).PropertyChanged += (s, a) => innerObjectSuperInnrObjectEventCount++;

        //    master.InnerObject2.SuperInnrObject = new InpcSuperInnrClass();
        //    ((INotifyPropertyChanged)master.InnerObject2.SuperInnrObject).PropertyChanged += (s, a) => innerObject2SuperInnrObjectEventCount++;
        //    master.InnerObject.SuperInnrObject.Str1 = "sadf";

        //    master.DoSyblingChange();

        //    // Assert.AreEqual(  );
        //}

        [Test]
        public void SimpleDependsOn()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClass();
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
            TestHelpers.DoInpcTest<InpcWithNotInstrumentedDependencies>(
                c =>
                {
                    c.InnerObject = new NotInstrumentedInpc();
                    c.InnerObject.Property = "asd";
                },
                2,
                "StringFromNotInstrumented");
        }

        [Test]
        public void TwoLevelDependsOn()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClass();
                        c.InnerObject.SuperInnrObject = new InpcSuperInnrClass();
                        c.InnerObject.SuperInnrObject.Str1 = "afaskhjkhf";
                        c.InnerObject.SuperInnrObject.Str2 = "sgfhdhd";
                    },
                4,
                "ConcatFromSuperInnerObject" );
        }

        [Test]
        public void TwoLevelDependsOn_WithNonAutoProperty()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                {
                    c.InnerObject = new InpcInnrClass();
                    c.InnerObject.SuperInnrObjectNonAuto = new InpcSuperInnrClass();
                    c.InnerObject.SuperInnrObjectNonAuto.Str1 = "afasf";
                    c.InnerObject.SuperInnrObjectNonAuto.Str2 = "sgfhdhd";
                },
                4,
                "ConcatFromSuperInnerObjectNonAuto");
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                {
                    c.InnerObject2 = new InpcInnrClass();
                    c.InnerObjectProperty.SuperInnrObject = new InpcSuperInnrClass();
                    c.InnerObjectProperty.SuperInnrObject.Str1 = "afasf";
                    c.InnerObjectProperty.SuperInnrObject.Str2 = "sgfhdhd";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty_SideObjectChange()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                {
                    var innerObject = new InpcInnrClass();
                    c.InnerObject2 = innerObject;
                    var superInnerObject = new InpcSuperInnrClass();
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
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                {
                    c.InnerObject2 = new InpcInnrClass();
                    var a = c.ConcatFromSuperInnerObjectViaProperty;
                    var superInnerObject = new InpcSuperInnrClass();
                    c.InnerObject2.SuperInnrObject = superInnerObject;
                    superInnerObject.Str1 = "sadf";
                    superInnerObject.Str2 = "sag";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
        }

        [Test]
        public void CyclicDependsOn()
        {
            TestHelpers.DoInpcTest<InpcCyclicDependency>(
                c =>
                    { 
                        c.Str1 = "sdaf";
                        c.Str2 = "sdgfdsf";
                    },
                2,
                "A");
        }

        [Test]
        [Ignore]
        public void CyclicViaMultipleObjectsDependsOn()
        {
            TestHelpers.DoInpcTest<InpcCyclicDependencyObjectA>(
                c =>
                {
                    c.ObjectB = new InpcCyclicDependencyObjectB();
                    c.ObjectB.ObjectA = c;
                    c.StringA = "stringA";
                    c.ObjectB.StringB = "stringB";
                },
                4,
                "B");
        }

        [Test]
        [Ignore]
        public void SelfCyclicObjectsDependsOn()
        {
            TestHelpers.DoInpcTest<InpcSelfCyclicDependencyObject>(
                c =>
                {
                    c.CyclicObject = new InpcSelfCyclicDependencyObject();
                    c.CyclicObject.StringB = "inner";
                    c.StringB = "outer";
                },
                3,
                "A");
        }

        [Test]
        public void ManualRaiseTest()
        {
            TestHelpers.DoInpcTest<InpcWithIgnoreClass>(
            c =>
            {
                c.IgnoredProperty = 1;
            },
            0,
            "DependentProperty");

            TestHelpers.DoInpcTest<InpcWithIgnoreClass>(
            c =>
            {
                c.IgnoredProperty = 1;
                NotifyPropertyChangedController.RaisePropertyChanged(c, x => x.IgnoredProperty);
            },
            1,
            "DependentProperty");
        }
    }

    [NotifyPropertyChanged]
    public class InpcWithIgnoreClass
    {
        [NotifyPropertyChangedIgnore]
        public int IgnoredProperty { get; set; }

        public int DependentProperty
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(IgnoredProperty);
                }

                return this.IgnoredProperty;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcCyclicDependency
    {
        public string Str1;

        public string Str2;

        public string A
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(B, Str1);
                }

                return this.Str1;
            }
        }
        
        public string B
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(A, Str2);
                }

                return this.Str2;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcCyclicDependencyObjectA
    {
        public InpcCyclicDependencyObjectB ObjectB;

        public string StringA;

        public string B
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(this.ObjectB.A, StringA);
                }

                return this.ObjectB.StringB;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcCyclicDependencyObjectB
    {
        public InpcCyclicDependencyObjectA ObjectA;

        public string StringB;

        public string A
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(this.ObjectA.B, StringB);
                }

                return this.ObjectA.StringA;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcSelfCyclicDependencyObject
    {
        public InpcSelfCyclicDependencyObject CyclicObject;

        public string StringB;

        public string A
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(this.CyclicObject.A, StringB);
                }

                return this.CyclicObject.StringB;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcSuperInnrClass
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
    public class InpcInnrClass
    {
        public string Str1;

        public string Str2;
        private InpcSuperInnrClass superInnrObjectNonAuto;

        public string StrConcat
        {
            get
            {
                return this.Str1 + this.Str2;
            }
        }

        public void SetProperty(InpcSuperInnrClass c)
        {
            this.Str1 = "sdfjkh";
            this.Str2 = "sdkafkl";
            c.Str1 = "sdjkafh";
            c.Str2 = "sdjkafh";
        }

        public InpcSuperInnrClass SuperInnrObject { get; set; }

        public InpcSuperInnrClass SuperInnrObjectNonAuto
        {
            get { return this.superInnrObjectNonAuto; }
            set { this.superInnrObjectNonAuto = value; }
        }
    }

    public class NotInstrumentedInpc : INotifyPropertyChanged
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
    public class InpcWithNotInstrumentedDependencies
    {
        public NotInstrumentedInpc InnerObject;

        public string StringFromNotInstrumented
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(InnerObject.Property);
                }

                var io = this.InnerObject;
                return io.Property;
            }
        }
    }

    [NotifyPropertyChanged]
    public class InpcWithManualDependencies
    {
        public InpcInnrClass InnerObject;

        public InpcInnrClass InnerObject2;

        public InpcInnrClass InnerObjectProperty
        {
            get
            {
                var io2 = this.InnerObject2;
                return io2;
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
                {
                    Depends.On(InnerObject.StrConcat);
                }

                var io = this.InnerObject;
                return io.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObject
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(InnerObject.SuperInnrObject.StrConcat);
                }

                var io = this.InnerObject;
                return io.SuperInnrObject.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObjectNonAuto
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(InnerObject.SuperInnrObjectNonAuto.StrConcat);
                }

                var io = this.InnerObject;
                return io.SuperInnrObjectNonAuto.StrConcat;
            }
        }

        public string ConcatFromSuperInnerObjectViaProperty
        {
            get
            {
                if (Depends.Guard)
                {
                    Depends.On(InnerObjectProperty.SuperInnrObject.StrConcat);
                }

                var iop = this.InnerObject;
                if (iop != null && iop.SuperInnrObject != null)
                {
                    return iop.SuperInnrObject.StrConcat;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}