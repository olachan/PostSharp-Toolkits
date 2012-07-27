using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests
{
    [TestFixture]
    public class PropertyChainInvocationTests
    {
        [Test]
        public void SimpleTest()
        {
            TestHelpers.DoInpcTest<ChainInvocationClass>(
                c =>
                {
                    c.innerClassField = new InnerClass();
                    c.innerClassField.SuperInnerClassProperty = new SupeInnerClass();
                    c.innerClassField.SuperInnerClassProperty.returnedString = "jkhas";
                },
                3,
                "Chain");
        }
    }

    [NotifyPropertyChanged]
    public class ChainInvocationClass
    {
        public InnerClass innerClassField;

        public string Chain
        {
            get
            {
                return this.innerClassField.SuperInnerClassProperty.SupeInnerClassStringProperty;
            }
        }

    }

    [NotifyPropertyChanged]
    public class InnerClass
    {
        public SupeInnerClass SuperInnerClassProperty { get; set; }
    }

    [NotifyPropertyChanged]
    public class SupeInnerClass
    {
        public string returnedString;

        public string SupeInnerClassStringProperty
        {
            get
            {
                return returnedString;
            }
        }
    }
}
