#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;

using PostSharp.Toolkit.Domain.Tools;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    [Serializable]
    internal class ObjectAccessors
    {
        public Dictionary<string, FieldInfoWithCompiledAccessors> FieldAccessors { get; private set; }

        public ObjectAccessors(Type type)
        {
            FieldAccessors = new Dictionary<string, FieldInfoWithCompiledAccessors>();

            FieldInfo[] fields = type.GetFields( BindingFlagsSet.AllInstance );

            foreach ( FieldInfo fieldInfo in fields )
            {
                FieldAccessors.Add( fieldInfo.FullName(), new FieldInfoWithCompiledAccessors( fieldInfo, type ) );
            }
        }
    }
}