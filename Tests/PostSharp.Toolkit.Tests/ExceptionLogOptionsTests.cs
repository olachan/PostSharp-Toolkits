#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Diagnostics;

using NUnit.Framework;

namespace PostSharp.Toolkit.Tests
{
    [TestFixture]
    public class ExceptionLogOptionsTests : BaseTestsFixture
    {
        [Test]
        public void ExceptionLoging_WithArgument_LogsArguments()
        {
            ExceptionThrowingClass ei = new ExceptionThrowingClass();
            try
            {
                ei.ThrowException(1);
            }
            catch ( Exception )
            {
            }

            string output = OutputString.ToString();
            Debug.WriteLine( output );
            StringAssert.Contains("An exception occurred in PostSharp.Toolkit.Tests.ExceptionThrowingClass.ThrowException(int i = 1):\nSystem.Exception", output);
        }
    }
}