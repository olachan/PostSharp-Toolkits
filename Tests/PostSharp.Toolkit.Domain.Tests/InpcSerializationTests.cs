#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace PostSharp.Toolkit.Domain.Tests
{
    [TestFixture]
    public class InpcSerializationTests
    {
        [Test]
        public void Serialization_DoesNotBreakInpc()
        {
            var obj = new ParentClass();
            obj.Child = new ChildClass();
            obj.Child.AutoProp = "C";
            obj.AutoProp = "D";

            var serializer = new DataContractSerializer( typeof(ParentClass) );
            var stream = new MemoryStream();
            serializer.WriteObject( stream, obj );
            stream.Position = 0;
            obj = (ParentClass) serializer.ReadObject( stream );

            TestHelpers.DoInpcTest( obj, o =>
                {
                    o.ChangeFields();
                    o.Child.AutoProp = "Z";
                }, 2,
                "CompoundProp", "NestedProp");
        }
 
        [DataContract]
        [NotifyPropertyChanged]
        public class ParentClass
        {
            [DataMember]
            private string s1 = "A";
            [DataMember]
            private string s2 = "B";

            [DataMember]
            public ChildClass Child { get; set; }

            [DataMember]
            public string AutoProp { get; set; }

            public string CompoundProp
            {
                get { return string.Concat( s1, s2 ); }
            }

            public string NestedProp
            {
                get { return this.Child.AutoProp; }
            }

            public void ChangeFields()
            {
                s1 += "A";
                s2 += "B";
            }
        }

        [DataContract]
        [NotifyPropertyChanged]
        public class ChildClass
        {
            [DataMember]
            public string AutoProp { get; set; }
        }
    }
}