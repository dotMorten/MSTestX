using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter
{
    static class ReflectionExtensions
    {
        public static void Verify(this ExpectedExceptionBaseAttribute attr, Exception realException)
        {
            var method = typeof(ExpectedExceptionBaseAttribute).GetMethod("Verify", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(attr, new object[] { realException });
        }
    }
}
