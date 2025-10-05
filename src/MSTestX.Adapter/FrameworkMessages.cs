using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter2
{
    internal static class FrameworkMessages
    {
        public static string UTF_FailedToGetExceptionMessage = "(Failed to get the message for an exception of type {0} due to an exception.)";

        public static string UTF_TestMethodNoExceptionDefault = "Test method did not throw an exception. An exception was expected by attribute {0} defined on the test method.";

        public static string DynamicDataIEnumerableEmpty = "Property or method {0} on {1} returns empty IEnumerable<object[]>.";

        public static string DataDrivenResultDisplayName = "{0} ({1})";
    }
}
