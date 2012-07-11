#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.ComponentModel;
using System.Linq;

using NUnit.Framework;

namespace PostSharp.Toolkit.INPC.Tests
{
    [TestFixture]
    public class ManulaNotifyPropertyChangedAspectTests
    {
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
        public void TwoLevelDependsOn()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClass();
                        c.InnerObject.SuperInnrObject = new InpcSuperInnrClass();
                        c.InnerObject.SuperInnrObject.Str1 = "afasf";
                        c.InnerObject.SuperInnrObject.Str2 = "sgfhdhd";
                    },
                4,
                "ConcatFromSuperInnerObject" );
        }

        [Test]
        public void TwoLevelDependsOn_ViaProperty()
        {
            TestHelpers.DoInpcTest<InpcWithManualDependencies>(
                c =>
                {
                    c.InnerObject2 = new InpcInnrClass();
                    var a = c.ConcatFromSuperInnerObjectViaProperty;
                    c.InnerObject2.SuperInnrObject = new InpcSuperInnrClass();
                    c.InnerObject2.SuperInnrObject.Str1 = "afasf";
                    c.InnerObject2.SuperInnrObject.Str2 = "sgfhdhd";
                },
                4,
                "ConcatFromSuperInnerObjectViaProperty");
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

        public string StrConcat
        {
            get
            {
                return this.Str1 + this.Str2;
            }
        }

        public InpcSuperInnrClass SuperInnrObject { get; set; }
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
                return this.InnerObject2;
            }
        }

        [DependsOn( "InnerObject.StrConcat" )]
        public string ConcatFromInnerObject
        {
            get
            {
                return this.InnerObject.StrConcat;
            }
        }

        [DependsOn( "InnerObject.SuperInnrObject.StrConcat" )]
        public string ConcatFromSuperInnerObject
        {
            get
            {
                return this.InnerObject.SuperInnrObject.StrConcat;
            }
        }

        [DependsOn("InnerObjectProperty.SuperInnrObject.StrConcat")]
        public string ConcatFromSuperInnerObjectViaProperty
        {
            get
            {
                if (this.InnerObjectProperty != null && this.InnerObjectProperty.SuperInnrObject != null)
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