using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace PostSharp.Toolkit.Threading.Tests
{
    [TestFixture]
    public class ThreadAffinedTests : ThreadingBaseTestFixture
    {
#if !(DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void BuildConfigurationTest()
        {
            Assert.Inconclusive("ThreadAffinedTests can run only in DEBUG configuration");
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ModifyingField_FromThreadSafeMethod_NeverThrows()
        {
            var o = new ThreadAffinedObject();
            TestHelpers.InvokeSimultaneouslyAndWait(() => o.ThreadSafeModifyField(200), () => o.ThreadSafeModifyField(200));
        }
#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
#endif
        public void UnsafeMethod_CallFromBackgroundThread_Throws()
        {
            var o = new ThreadAffinedObject();
            try
            {
                Task.Factory.StartNew(() => o.ModifyField(0)).Wait();
            }
            catch (AggregateException aggrExc)
            {
                if (aggrExc.InnerExceptions.Count == 1) throw aggrExc.InnerExceptions[0];
                else throw;
            }
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
        [ExpectedException(typeof(ThreadUnsafeException))]
#endif
        public void ModifyingIsntanceField_FromStaticUnsafeMethod_Throws()
        {
            var o = new ThreadAffinedObject();
            try
            {
                Task.Factory.StartNew(() => ThreadAffinedObject.StaticMethodModifyingInstanceField(o)).Wait();
            }
            catch (AggregateException aggrExc)
            {
                if (aggrExc.InnerExceptions.Count == 1) throw aggrExc.InnerExceptions[0];
                else throw;
            }
        }

#if (DEBUG || DEBUG_THREADING)
        [Test]
#endif
        public void ThreadSafeField_Modification_NeverThrows()
        {
            var o = new ThreadAffinedObject();
            TestHelpers.InvokeSimultaneouslyAndWait(() => o.ModifyThreadSafeField(200), () => o.ModifyThreadSafeField(200));
            TestHelpers.InvokeSimultaneouslyAndWait(() => ThreadAffinedObject.StaticMethodModifyingThreadSafeField(o), () => ThreadAffinedObject.StaticMethodModifyingThreadSafeField(o));
        }


        [ThreadUnsafeObject(ThreadUnsafePolicy.ThreadAffined, CheckFieldAccess = true)]
        public class ThreadAffinedObject
        {
            protected int protectedField = 7;
            protected static int staticField = 4;

            [ThreadSafe]
            protected int threadSafeField = 11;

            public static void StaticMethodModifyingInstanceField(ThreadAffinedObject instance)
            {
                instance.protectedField++;
            }

            public static void StaticMethodModifyingThreadSafeField(ThreadAffinedObject instance)
            {
                instance.threadSafeField++;
            }

            public static void StaticMethodModifyingStaticField()
            {
                staticField++;
            }

            public void ModifyField(int sleep)
            {
                protectedField++;
                Thread.Sleep(200);
            }

            [ThreadSafe]
            public void ThreadSafeModifyField(int sleep)
            {
                protectedField++;
                Thread.Sleep(200);
            }

            [ThreadSafe]
            public void ModifyThreadSafeField(int sleep)
            {
                threadSafeField++;
                Thread.Sleep(sleep);
            }
        }
    }
}