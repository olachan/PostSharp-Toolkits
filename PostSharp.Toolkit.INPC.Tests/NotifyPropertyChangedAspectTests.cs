using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.INPC.Tests
{
    [TestFixture]
    public class NotifyPropertyChangedAspectTests
    {
        [Test]
        public void SetFieldsViaSeparateMethodTest()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
                                                                                        {
                                                                                            if (e.PropertyName == "Sum")
                                                                                            {
                                                                                                eventFireCounter++;

                                                                                            }
                                                                                        };

            bc.SetField1(2);
            bc.SetField2(4);

            Assert.AreEqual(2, eventFireCounter);
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Sum")
                {
                    eventFireCounter++;

                }
            };

            bc.SetField1(2);
            bc.SetField2(4);

            Assert.AreEqual(2, eventFireCounter);
        }

        [Test]
        public void SetFieldsDirectlyTest()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Sum")
                {
                    eventFireCounter++;

                }
            };

            bc.SetFields(2, 3);

            Assert.AreEqual(1, eventFireCounter);
        }

        [Test]
        public void AutoPropertyTest()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) => eventFireCounter++;
            bc.AutoProperty = 4;

            Assert.AreEqual(1, eventFireCounter);
        }

        [Test]
        public void SetFieldsViaSeparateMethodTest_MethodBasedProperty()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SumViaMethod")
                {
                    eventFireCounter++;

                }
            };

            bc.SetField1(2);
            bc.SetField2(4);

            Assert.AreEqual(2, eventFireCounter);
        }

        [Test]
        public void SetFieldsViaCompositeMethodTest_MethodBasedProperty()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SumViaMethod")
                {
                    eventFireCounter++;

                }
            };

            bc.SetField1(2);
            bc.SetField2(4);

            Assert.AreEqual(2, eventFireCounter);
        }

        [Test]
        public void SetFieldsDirectlyTest_MethodBasedProperty()
        {
            BaseClass bc = new BaseClass();

            int eventFireCounter = 0;

            Post.Cast<BaseClass, INotifyPropertyChanged>(bc).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "SumViaMethod")
                {
                    eventFireCounter++;

                }
            };

            bc.SetFields(2, 3);

            Assert.AreEqual(1, eventFireCounter);
        }

    }

    [NotifyPropertyChangedAspect]
    public class BaseClass
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
