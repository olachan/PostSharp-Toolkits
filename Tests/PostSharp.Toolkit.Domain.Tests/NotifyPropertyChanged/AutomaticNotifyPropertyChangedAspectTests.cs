﻿using System.ComponentModel;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests.NotifyPropertyChanged
{

    [TestFixture]
    public class AutomaticNotifyPropertyChangedAspectTests
    {
        [Test]
        public void InnerClassPropertyChange_GeneratesSingleEventOnDependentProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
                c =>
                {
                    c.Inner.Property = 5;
                },
                1,
                "InnerClassProperty");
            TestHelpers.DoInpcTest<InpcBasicClass>(
                c =>
                {
                    var x = c.Inner;
                    x.Property = 7;
                },
                1,
                "InnerClassProperty");
        }

        [Test]
        public void MultipleInstancesTest()
        {
            InpcDerrivedClass[] objects1 = new[] { new InpcDerrivedClass(), new InpcDerrivedClass(), new InpcDerrivedClass(), new InpcDerrivedClass(), new InpcDerrivedClass() };
            InpcBasicClass[] objects2 = new[] { new InpcBasicClass(), new InpcBasicClass(), new InpcBasicClass(), new InpcBasicClass(), new InpcBasicClass() };
            int[] firedEvents1 = new int[5];
            int[] firedEvents2 = new int[5];

            for (int i = 0; i < 5; i++)
            {
                int j = i;
                ((INotifyPropertyChanged)objects1[i]).PropertyChanged += (s, e) => firedEvents1[j]++;
                ((INotifyPropertyChanged)objects2[i]).PropertyChanged += (s, e) => firedEvents2[j]++;
            }

            for (int i = 0; i < 5; i++)
            {
                objects1[i].AutoProperty = 2;
                objects2[i].AutoProperty = 3;
            }

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(1, firedEvents1[i]);
                Assert.AreEqual(1, firedEvents2[i]);
            }
        }

        [Test]
        public void SetFieldsViaSeparateMethodTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
                c =>
                {
                    c.SetField1(2);
                    c.SetField2(4);
                },
                2,
                "Sum");
        }

        [Test]
        public void SetFieldsViaSeparateMethodTest_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
                c =>
                {
                    c.SetField1(2);
                    c.SetField2(4);
                },
                2,
                "Sum");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
               c => c.SetFields(2, 3),
               1,
               "Sum");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
               c => c.SetFields(2, 3),
               1,
               "Sum");
        }

        [Test]
        public void SetFieldsDirectlyTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
              c =>
              {
                  c.Field1 = 2;
                  c.Field2 = 3;
              },
              2,
              "Sum");
        }

        [Test]
        public void MultipleSetFieldsDirectlyTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
              c =>
              {
                  c.Field1 = 2;
                  c.Field1 = 2;
                  c.Field1 = 2;
                  c.Field1 = 2;

                  c.Field2 = 3;
                  c.Field2 = 3;
                  c.Field2 = 3;
                  c.Field2 = 3;
              },
              2,
              "Sum");
        }

        [Test]
        public void SumViaExternalMethod_SetFieldsDirectlyTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
              c =>
              {
                  c.Field1 = 2;
                  c.Field2 = 3;
              },
              2,
              "SumViaExternalMethod");
        }

        [Test]
        public void SetFieldsDirectlyTest_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
              c =>
              {
                  c.Field1 = 2;
                  c.Field2 = 3;
              },
              2,
              "Sum");
        }

        [Test]
        public void AutoPropertyTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
             c =>
             {
                 c.AutoProperty = 4;
             },
             1,
             "AutoProperty");
        }

        [Test]
        public void AutoPropertyTest_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
             c =>
             {
                 c.AutoProperty = 4;
             },
             1,
             "AutoProperty");
        }

        [Test]
        public void RecurrentlyCalculatedPropertyTest()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
            c =>
            {
                c.FieldForRecurrentCalculation = 10;
            },
            1,
            "RecurrentlyCalculatedValue");
        }

        [Test]
        public void SetFieldsViaSeparateMethodTest_MethodBasedProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
            c =>
            {
                c.SetField1(2);
                c.SetField2(4);
            },
            2,
            "SumViaMethod");
        }

        [Test]
        public void SetFieldsViaSeparateMethodTest_MethodBasedProperty_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
            c =>
            {
                c.SetField1(2);
                c.SetField2(4);
            },
            2,
            "SumViaMethod");
        }

        [Test]
        public void SetFieldForLongMethodChain()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
           c => c.FieldForLongMethodChain = 5,
           1,
           "LongMethodChainProperty");
        }

        [Test]
        public void StaticFrameworkMethodBasedProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
           c =>
           {
               c.Str1 = "sdaf";
               c.Str2 = "sdakflj";
           },
           2,
           "StaticFrameworkMethodBasedProperty");
        }

        [Test]
        public void StateIndependentMethodBasedProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
           c =>
           {
               c.Str1 = "sdaf";
               c.Str2 = "sdakflj";
           },
           2,
           "StateIndependentMethodBasedProperty");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_MethodBasedProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
           c => c.SetFields(2, 3),
           1,
           "SumViaMethod");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_MethodBasedProperty_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
           c => c.SetFields(2, 3),
           1,
           "SumViaMethod");
        }

        [Test]
        public void SetFieldsDirectlyTest_MethodBasedProperty()
        {
            TestHelpers.DoInpcTest<InpcBasicClass>(
           c =>
           {
               c.Field1 = 2;
               c.Field2 = 3;
           },
           2,
           "SumViaMethod");
        }

        [Test]
        public void SetFieldsDirectlyTest_MethodBasedProperty_ForDerivedClass()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
           c =>
           {
               c.Field1 = 2;
               c.Field2 = 3;
           },
           2,
           "SumViaMethod");
        }

        [Test]
        public void BaseField_RaisesDerivedEvent()
        {
            TestHelpers.DoInpcTest<InpcDerrivedClass>(
            c =>
            {
                c.BaseField1 = 1;
            },
            1,
            "BaseClasseBasedProperty");
        }

        [Test]
        public void Idexers_AreIgnored()
        {
            int eventCount = 0;
            InpcWithIndexer c = new InpcWithIndexer();

            ((INotifyPropertyChanged)c).PropertyChanged += ( sender, args ) => eventCount++;

            c.IndexerOffset = 5;
            c[1] = 2;

            Assert.IsTrue( eventCount == 0 );
        }

    }

    [NotifyPropertyChanged]
    public class InpcBaseClass//: INotifyChildPropertyChanged, INotifyPropertyChanged
    {
        public int BaseField1;

        public int BaseMethod1()
        {
            return this.BaseField1;
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    this.RaisePropertyChanged( propertyName );
        //}

        //public virtual void RaisePropertyChanged(string propertyName)
        //{
        //    PropertyChangedEventHandler handler = this.PropertyChanged;
        //    if (handler != null)
        //    {
        //        handler(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}

        //public void RaiseChildPropertyChanged(ChildPropertyChangedEventArgs args)
        //{
        //    EventHandler<ChildPropertyChangedEventArgs> handler = this.ChildPropertyChanged;
        //    if (handler != null)
        //    {
        //        handler(this, args);
        //    }
        //}

        //public event EventHandler<ChildPropertyChangedEventArgs> ChildPropertyChanged;
    }

    [NotifyPropertyChanged]
    public class InpcWithIndexer
    {
        public int IndexerOffset;

        public int this[int index]
        {
            get
            {
                return index + this.IndexerOffset;
            }
            set
            {
                this.IndexerOffset = value;
            }
        }
    }

    public class InpcDerrivedClass : InpcBaseClass
    {
        public int Field1;

        public int Field2;

        public int Sum
        {
            get
            {
                return this.Field1 + this.Field2;
            }
        }

        public int SumViaMethod
        {
            get
            {
                return this.GetSum();
            }
        }

        public int AutoProperty { get; set; }

        public int BaseClasseBasedProperty
        {
            get
            {
                return this.BaseMethod1();
            }
        }

        public void SetField1(int value)
        {
            this.Field1 = value;
        }

        public void SetField2(int value)
        {
            this.Field2 = value;
        }

        public void SetFields(int field1, int field2)
        {
            this.Field1 = field1;
            this.Field2 = field2;
        }

        public int GetSum()
        {
            return this.Field1 + this.Field2;
        }

        [IdempotentMethod]
        public static string StateIndependentMethod(string format, params object[] parameters)
        {
            return string.Format(format, parameters);
        }
    }

    [NotifyPropertyChanged]
    public class InpcAutoInnerClass
    {
        public int Sum(int left, int right)
        {
            return left + right;
        }

        public int Property { get; set; }
    }

   

    [NotifyPropertyChanged]
    public class InpcBasicClass
    {
        public int Field1;

        public int Field2;

        public int FieldForRecurrentCalculation;

        public int FieldForLongMethodChain;

        public string Str1;

        public string Str2;

        public InpcAutoInnerClass Inner = new InpcAutoInnerClass();

        public int InnerClassProperty
        {
            get { return this.Inner.Property; }
        }

        public string StaticFrameworkMethodBasedProperty
        {
            get
            {
                return string.Format("{0} {1}", this.Str1, this.Str2);
            }
        }

        public string StateIndependentMethodBasedProperty
        {
            get
            {
                return InpcDerrivedClass.StateIndependentMethod("{0} {1}", this.Str1, this.Str2);
            }
        }

        public string StateIndependentMethodBasedProperty2
        {
            get
            {
                return InpcDerrivedClass.StateIndependentMethod("{0}", this.ToString());
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Str1, this.Str2);
        }

        [NotifyPropertyChangedSafe]
        public int SumViaExternalMethod
        {
            get
            {
                return this.Inner.Sum(this.Field1, this.Field2);
            }
        }

        public int Sum
        {
            get
            {
                return this.Field1 + this.Field2;
            }
        }

        public int SumViaMethod
        {
            get
            {
                return this.GetSum();
            }
        }

        public int RecurrentlyCalculatedValue
        {
            get
            {
                return this.CalculateRecurrently(this.FieldForRecurrentCalculation);
            }
        }

        private int CalculateRecurrently(int x)
        {
            if (x <= 0) return 0;
            return x + this.CalculateRecurrently(x - 1);
        }

        public int LongMethodChainProperty
        {
            get
            {
                return this.LongMethodChain();
            }
        }

        private int LongMethodChain()
        {
            return this.LongMethodCahinInner();
        }

        private int LongMethodCahinInner()
        {
            return this.LongMethodChainInnerInner();
        }

        private int LongMethodChainInnerInner()
        {
            return this.FieldForLongMethodChain;
        }

        public int AutoProperty { get; set; }

        public void SetField1(int value)
        {
            this.Field1 = value;
        }

        public void SetField2(int value)
        {
            this.Field2 = value;
        }

        public void SetFields(int field1, int field2)
        {
            this.Field1 = field1;
            this.Field2 = field2;
        }

        public int GetSum()
        {
            return this.Field1 + this.Field2;
        }
    }

    //public class Base: INotifyChildPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;

    //    public virtual void RaisePropertyChanged(string propertyName)
    //    {
    //        PropertyChangedEventHandler handler = this.PropertyChanged;
    //        if (handler != null)
    //        {
    //            handler(this, new PropertyChangedEventArgs(propertyName));
    //        }
    //    }

    //    public void RaiseChildPropertyChanged(ChildPropertyChangedEventArgs args)
    //    {
    //        EventHandler<ChildPropertyChangedEventArgs> handler = this.ChildPropertyChanged;
    //        if (handler != null)
    //        {
    //            handler(this, args);
    //        }
    //    }

    //    public event EventHandler<ChildPropertyChangedEventArgs> ChildPropertyChanged;
    //}

    //[NotifyPropertyChanged]
    //public class Derrived : Base
    //{

    //}
}
