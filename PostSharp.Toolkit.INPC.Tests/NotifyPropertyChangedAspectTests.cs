using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;

using PostSharp.Toolkit.INPC;

namespace PostSharp.Toolkit.INPC.Tests
{

    [TestFixture]
    public class NotifyPropertyChangedAspectTests
    {
        private void DoInpcTest<TInpc>(Action<TInpc> propertyChangeAction, int expectedEventFireCount, params string[] propertyNames)
            where TInpc : class, new()
        {

            TInpc bc = new TInpc();

            int eventFireCounter = 0;

            ((INotifyPropertyChanged)bc).PropertyChanged += (s, e) =>
            {
                if (propertyNames.Contains(e.PropertyName))
                {
                    eventFireCounter++;
                }
            };

            propertyChangeAction(bc);

            Assert.AreEqual(expectedEventFireCount, eventFireCounter);

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
                objects1[i].PropertyChanged += (s, e) => firedEvents1[j]++;
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
            DoInpcTest<InpcBasicClass>(
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
            DoInpcTest<InpcDerrivedClass>(
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
            DoInpcTest<InpcBasicClass>(
               c => c.SetFields(2, 3),
               1,
               "Sum");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_ForDerivedClass()
        {
            DoInpcTest<InpcDerrivedClass>(
               c => c.SetFields(2, 3),
               1,
               "Sum");
        }

        [Test]
        public void SetFieldsDirectlyTest()
        {
            DoInpcTest<InpcBasicClass>(
              c =>
              {
                  c.Field1 = 2;
                  c.Field2 = 3;
              },
              2,
              "Sum");
        }

        [Test]
        public void SetFieldsDirectlyTest_ForDerivedClass()
        {
            DoInpcTest<InpcDerrivedClass>(
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
            DoInpcTest<InpcBasicClass>(
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
            DoInpcTest<InpcDerrivedClass>(
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
            DoInpcTest<InpcBasicClass>(
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
            DoInpcTest<InpcBasicClass>(
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
            DoInpcTest<InpcDerrivedClass>(
            c =>
            {
                c.SetField1(2);
                c.SetField2(4);
            },
            2,
            "SumViaMethod");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_MethodBasedProperty()
        {
            DoInpcTest<InpcBasicClass>(
           c => c.SetFields(2, 3),
           1,
           "SumViaMethod");
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_MethodBasedProperty_ForDerivedClass()
        {
            DoInpcTest<InpcDerrivedClass>(
           c => c.SetFields(2, 3),
           1,
           "SumViaMethod");
        }

        [Test]
        public void SetFieldsDirectlyTest_MethodBasedProperty()
        {
            DoInpcTest<InpcBasicClass>(
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
            DoInpcTest<InpcDerrivedClass>(
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
           DoInpcTest<InpcDerrivedClass>(
           c =>
               {
                   c.BaseField1 = 1;
               },
           1,
           "BaseClasseBasedProperty");
        }

    }

    [NotifyPropertyChanged]
    public class InpcBaseClass : INotifyPropertyChanged, IRaiseNotifyPropertyChanged
    {
        public int BaseField1;

        public int BaseMethod1()
        {
            return BaseField1;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NoAutomaticPropertyChangedNotifications]
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
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
            return Field1 + Field2;
        }
    }

    [NotifyPropertyChanged]
    public class InpcBasicClass
    {
        public int Field1;

        public int Field2;

        public int FieldForRecurrentCalculation;

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
            return x + this.CalculateRecurrently( x - 1 );
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
            return Field1 + Field2;
        }
    }
}
