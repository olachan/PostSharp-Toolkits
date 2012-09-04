using System;
using System.Reflection;
using NUnit.Framework;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Domain.Tests.NotifyPropertyChanged
{
    [TestFixture]
    public class CustomAspectIntegrationTests
    {
        [Test]
        public void ObjectWithCustomAspect_INPCDoesNotThrow()
        {
            var o = new TestObject( null );
            o.DbName = "x";
            string a = o.DbName;
            o.DbName = null;
            a = o.DbName;
        }


        [NotifyPropertyChanged]
        public class TestObject
        {
            public TestObject(string dbName, string obfuscatedId)
            {
                DbName = dbName;
                ObfuscatedId = obfuscatedId;
            }

            public TestObject(string dbName)
                : this(dbName, string.Empty)
            {

            }

            [NullToEmptyString]
            public string DbName { get; set; }

            [NullToEmptyString]
            public string ObfuscatedId { get; set; }
        }
    }

    [Serializable]
    public class NullToEmptyStringAttribute : MethodInterceptionAspect, IInstanceScopedAspect
    {
        public override void OnInvoke(MethodInterceptionArgs args)
        {
            base.OnInvoke(args);
            if (((MethodInfo)args.Method).ReturnType == typeof(string))
            {
                if (args.ReturnValue == null)
                {
                    args.ReturnValue = string.Empty;
                }
            }
        }

        public object CreateInstance( AdviceArgs adviceArgs )
        {
            return new NullToEmptyStringAttribute();
        }

        public void RuntimeInitializeInstance()
        {
            
        }
    }
}