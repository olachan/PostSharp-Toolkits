#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    internal static class ThreadingMessageSource
    {
        public static MessageSource Instance = new MessageSource( "PostSharp.Toolkit.Threading", new ThreadingMessageDispenser() );

        private class ThreadingMessageDispenser : MessageDispenser
        {
            public ThreadingMessageDispenser()
                : base( "THR" )
            {
            }

            protected override string GetMessage( int number )
            {
                switch ( number )
                {
                    case 1:
                        return
                            "Asynchronous DispatchedMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods with no ref or out parameters.";
                    case 2:
                        return "Method {0}.{1} cannot be marked [ThreadUnsafe] because it is static.";
                    case 3:
                        return "Method {0}.{1} should not be marked [ThreadUnsafe] when ThreadUnsafePolicy is Static";
                    case 4:
                        return "ActorAttribute cannot be applied to type {0}. It can only be applied to types derived from Actor class.";
                    case 5:
                        return "Fiels {0}.{1} cannot be public because its declaring class {0} derives from Actor. Apply the [ThreadSafe] custom attribute to this field to opt out from this rule.";
                    case 6:
                        return "BackgroundMethodAttribute cannot be applied to {0}.{1}. It can only be applied to void methods without out/ref parameters.";
                    case 7:
                        return
                            "Field {0}.{1} cannot be public because its declaring class {0} implements the [ReaderWriterSynchronized] threading model. Apply the [ThreadSafe] custom attribute to this field to opt out from this rule.";
                    case 8:
                        return "Field {0}.{1} cannot be public because its declaring class {0} implements the [ThreadUnsafeObject] threading model. Apply the [ThreadSafe] custom attribute to this field to opt out from this rule.";
                    case 9:
                        return
                            "Method {0} cannot return a value or have out/ref parameters because its declaring class derives from Actor and the method can be invoked from outside the actor.";
                    case 10:
                        return "Aspect DeadlockDetectionPolicy must be added to the current assembly only.";
                    case 11:
                        return "Cannot find the field representing the 'this' instance in type {0}.";
                    case 12:
                        return "Not ThreadSafe field {0} accessed from static method {1}";
                    case 13:
                        return "Not public or internal or ThreadUnsafeMethod method {0} accessed from static method {1}";
                    default:
                        return null;
                }
            }
        }
    }
}