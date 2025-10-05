using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices.Interface;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter
{
    internal class TestDeployment : ITestDeployment
    {
        public void Cleanup()
        {
        }

        public bool Deploy(IEnumerable<TestCase> testCases, IRunContext? runContext, IFrameworkHandle frameworkHandle)
        {
            return false;
        }

        public string GetDeploymentDirectory()
        {
            return string.Empty;
        }

        public KeyValuePair<string, string>[] GetDeploymentItems(MethodInfo method, Type type, ICollection<string> warnings)
        {
            return new KeyValuePair<string, string>[] { };
        }
    }
}
