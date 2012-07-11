using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace PostSharp.Toolkit.INPC.Tests
{
    internal static class TestHelpers
    {
        public static void DoInpcTest<TInpc>(Action<TInpc> propertyChangeAction, int expectedEventFireCount, params string[] propertyNames)
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
    }
}
