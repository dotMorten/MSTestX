using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TestAppRunner
{
    internal class TestCaseDiscoverySink : ITestCaseDiscoverySink
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestCaseDiscoverySink"/> class.
        /// </summary>
        public TestCaseDiscoverySink()
        {
        }

        /// <summary>
        /// Gets the tests.
        /// </summary>
        public ICollection<TestCase> Tests { get; private set; } = new Collection<TestCase>();
        private object TestsSync = new object();

        /// <summary>
        /// Sends the test case to the discoverer.
        /// </summary>
        /// <param name="discoveredTest">The discovered test.</param>
        void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
        {
            if (discoveredTest != null)
            {
                lock (TestsSync)
                    Tests.Add(discoveredTest);
            }
        }
    }
}
