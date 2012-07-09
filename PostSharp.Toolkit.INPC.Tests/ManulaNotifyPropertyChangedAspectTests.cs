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
    public class NotifyPropertyChangedAspectTests
    {
        private void DoInpcTest<TInpc>( Action<TInpc> propertyChangeAction, int expectedEventFireCount, params string[] propertyNames )
            where TInpc : class, new()
        {
            TInpc bc = new TInpc();

            int eventFireCounter = 0;

            ((INotifyPropertyChanged)bc).PropertyChanged += ( s, e ) =>
                {
                    if ( propertyNames.Contains( e.PropertyName ) )
                    {
                        eventFireCounter++;
                    }
                };

            propertyChangeAction( bc );

            Assert.AreEqual( expectedEventFireCount, eventFireCounter );
        }

        [Test]
        public void SimpleDependsOn()
        {
            this.DoInpcTest<InpcWithManualDependencies>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClass();
                        c.InnerObject.Str1 = "asd";
                        c.InnerObject.Str2 = "asfd";
                    },
                2,
                "ConcatFromInnerObject" );
        }

        [Test]
        public void TwoLevelDependsOn()
        {
            this.DoInpcTest<InpcWithManualDependencies>(
                c =>
                    {
                        c.InnerObject = new InpcInnrClass();
                        c.InnerObject.SuperInnrObject = new InpcSuperInnrClass();
                        c.InnerObject.SuperInnrObject.Str1 = "afasf";
                        c.InnerObject.SuperInnrObject.Str2 = "sgfhdhd";
                        // PropertyChangesTracker.RaisePropertyChanged();
                    },
                3,
                "ConcatFromSuperInnerObject" );
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
    }
}