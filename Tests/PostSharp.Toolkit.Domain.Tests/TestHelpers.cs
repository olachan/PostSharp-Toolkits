using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests
{
    internal static class TestHelpers
    {
        public static void DoInpcTest<TInpc>(Action<TInpc> propertyChangeAction, int expectedEventFireCount, params string[] propertyNames)
           where TInpc : class, new()
        {
            TInpc bc = new TInpc();

            DoInpcTest( bc, propertyChangeAction, expectedEventFireCount, propertyNames );
        }

        public static void DoInpcTest<TInpc>(TInpc source, Action<TInpc> propertyChangeAction, int expectedEventFireCount, params string[] propertyNames)
        {
            int eventFireCounter = 0;

            ((INotifyPropertyChanged)source).PropertyChanged += (s, e) =>
            {
                if (propertyNames.Contains(e.PropertyName))
                {
                    eventFireCounter++;
                }
                Assert.AreSame( source, s );
            };

            propertyChangeAction(source);

            Assert.AreEqual(expectedEventFireCount, eventFireCounter);
        }
    }
}
