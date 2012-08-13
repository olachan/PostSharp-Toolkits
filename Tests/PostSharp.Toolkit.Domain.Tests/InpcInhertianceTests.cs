using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests
{
    [TestFixture]
    public class InpcInhertianceTests
    {
        [Test]
        public void InheritedPropertyTest()
        {
            TestHelpers.DoInpcTest<DerivedInpc>(
            c =>
            {
                c.DerivedProp = 2;
            },
            1,
            "DerivedProp");
        }

        [Test]
        [Ignore]
        public void BasePropertyTest()
        {
            TestHelpers.DoInpcTest<DerivedInpc>(
            c =>
            {
                c.f = 2;
            },
            1,
            "BaseProp");
        }

        [Test]
        [Ignore]
        public void DerivedBaseBasedPropTest()
        {
            TestHelpers.DoInpcTest<DerivedInpc>(
            c =>
            {
                c.f = 2;
            },
            1,
            "DerivedBaseBasedProp");
        }
    }

    public class BaseNoInpc
    {
        public int f = 0;

        public int BaseProp
        {
            get
            {
                return f;
            }
        }
    }

    [NotifyPropertyChanged]
    public class DerivedInpc : BaseNoInpc
    {
        public int DerivedProp { get; set; }

        public int DerivedBaseBasedProp
        {
            get
            {
                return f;
            }
        }
    }
}
