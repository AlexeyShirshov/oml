using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace CoreFramework.Debugging
{
    public static class Stack
    {
        /// <summary>
        /// Stolen from <a href="http://weblogs.asp.net/fmarguerie/archive/2008/01/02/rethrowing-exceptions-and-preserving-the-full-call-stack-trace.aspx">here</a>
        /// </summary>
        /// <param name="exception"></param>
        public static void PreserveStackTrace(Exception exception)
        {
            MethodInfo preserveStackTrace = typeof(Exception).GetMethod("InternalPreserveStackTrace",
              BindingFlags.Instance | BindingFlags.NonPublic);
            preserveStackTrace.Invoke(exception, null);
        }

    }
}
