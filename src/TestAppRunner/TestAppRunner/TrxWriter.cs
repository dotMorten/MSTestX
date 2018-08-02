using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace TestAppRunner
{
    internal class TrxWriter
    {
        internal static void GenerateReport(string trxOutputPath, IEnumerable<TestResult> tests)
        {
            var trx = new TrxWriter(trxOutputPath);
            trx.InitializeReport();
            foreach(var test in tests)
            {
                trx.RecordResult(test);
            }
            trx.FinalizeReport();
        }

        private const string xmlNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        private const string testListId = "8c84fa94-04c1-424b-9868-57a2d4851a1d";
        private System.IO.Stream outputStream;
        private readonly bool disposeStream;
        private XmlDocument doc;
        private int testCount;
        private int testFailed;
        private int testSucceeded;
        private int testSkipped;
        private XmlElement rootNode;
        private XmlElement resultsNode;
        private XmlElement testDefinitions;
        private XmlElement header;
        private XmlElement testEntries;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxResultChannel"/> class.
        /// </summary>
        /// <param name="filename">The name of the file to write the report to.</param>
        public TrxWriter(string filename) : this(System.IO.File.Open(filename, System.IO.FileMode.Create), true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxResultChannel"/> class.
        /// </summary>
        /// <param name="outputStream">The stream to write the report to.</param>
        /// <param name="disposeStream">Whether this instance should dispose the provided stream on completion</param>
        public TrxWriter(System.IO.Stream outputStream, bool disposeStream = false)
        {
            this.outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            this.disposeStream = disposeStream;
        }

        /// <summary>
        /// Creates a TCP connection and writes the report to the socket at the provided hostname and port
        /// </summary>
        /// <param name="hostName">hostname</param>
        /// <param name="port">port</param>
        public TrxWriter(string hostName, int port) : this(CreateTcpStream(hostName, port), true)
        {
        }

        private static System.IO.Stream CreateTcpStream(string hostName, int port)
        {
            if (port < 0 || port > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("port");
            }
            var client = new System.Net.Sockets.TcpClient(hostName ?? throw new ArgumentNullException(nameof(hostName)), port);
            return client.GetStream();
        }

        /// <summary>
        /// The name of the test run to include in the report header
        /// </summary>
        public string TestRunName { get; set; } = "";

        /// <summary>
        /// The name of the user running the report header
        /// </summary>
        public string TestRunUser { get; set; } = "";

        public void InitializeReport()
        {
            testCount = testFailed = testSucceeded = 0;
            doc = new XmlDocument();
            rootNode = doc.CreateElement("TestRun", xmlNamespace);
            rootNode.SetAttribute("id", Guid.NewGuid().ToString());
            rootNode.SetAttribute("name", TestRunName);
            rootNode.SetAttribute("runUser", TestRunUser);
            doc.AppendChild(rootNode);
            header = doc.CreateElement("Times", xmlNamespace);
            header.SetAttribute("finish", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("start", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("creation", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            rootNode.AppendChild(header);
            resultsNode = doc.CreateElement("Results", xmlNamespace);
            rootNode.AppendChild(resultsNode);
            testDefinitions = doc.CreateElement("TestDefinitions", xmlNamespace);
            rootNode.AppendChild(testDefinitions);
            testEntries = doc.CreateElement("TestEntries", xmlNamespace);
            rootNode.AppendChild(testEntries);
            var testLists = doc.CreateElement("TestLists", xmlNamespace);
            var testList = doc.CreateElement("TestList", xmlNamespace);
            testList.SetAttribute("name", "Results Not in a List");
            testList.SetAttribute("id", testListId);
            testLists.AppendChild(testList);
            testList = doc.CreateElement("TestList", xmlNamespace);
            testList.SetAttribute("name", "All Loaded Results");
            testList.SetAttribute("id", "19431567-8539-422a-85d7-44ee4e166bda");
            testLists.AppendChild(testList);
            rootNode.AppendChild(testLists);
        }

        private static string OutcomeToTrx(TestOutcome outcome)
        {
            switch(outcome)
            {
                case TestOutcome.Failed: return "Failed";
                case TestOutcome.None: return "Pending";
                case TestOutcome.Passed: return "Passed";
                case TestOutcome.Skipped: return "NotExecuted";
                case TestOutcome.NotFound:
                default:
                    return "NotRunnable";
            }
        }

        public void RecordResult(TestResult result)
        {
            var innerResultsCount = GetProperty<int>("InnerResultsCount", result, 0);
            if (innerResultsCount > 0) return; // This is a data test, and we don't store the parent result
            string id1 = result.TestCase.Id.ToString();
            string id2 = GetProperty<Guid>("TestCase.Id", result, new Guid()).ToString();
            string name1 = result.DisplayName;
            string name2 = result.TestCase.DisplayName;
            Guid parentExecId = GetProperty<Guid>("ParentExecId", result, Guid.NewGuid());
            var id = result.TestCase.Id.ToString();
            if (parentExecId != Guid.Empty)
                id = Guid.NewGuid().ToString(); //If this is a child test, create a unique test id

            var executionId = GetProperty<Guid>("ExecutionId", result, Guid.Empty);
            if (executionId == Guid.Empty)
                executionId = Guid.NewGuid();
            string testName = result.DisplayName;
            if (string.IsNullOrEmpty(testName))
                testName = result.TestCase.DisplayName;
            var resultNode = doc.CreateElement("UnitTestResult", xmlNamespace);
            resultNode.SetAttribute("outcome", OutcomeToTrx(result.Outcome));
            resultNode.SetAttribute("testType", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
            resultNode.SetAttribute("testListId", testListId);
            resultNode.SetAttribute("executionId", executionId.ToString());
            resultNode.SetAttribute("testName", testName);
            resultNode.SetAttribute("testId", id);
            resultNode.SetAttribute("duration", result.Duration.ToString("G", CultureInfo.InvariantCulture));
            resultNode.SetAttribute("computerName", result.ComputerName);

            string assemblyName = GetProperty<string>("TestCase.Source", result.TestCase, "");

            if (result.Outcome == TestOutcome.Failed)
            {
                testFailed++;
                var output = doc.CreateElement("Output", xmlNamespace);
                var errorInfo = doc.CreateElement("ErrorInfo", xmlNamespace);
                var message = doc.CreateElement("Message", xmlNamespace);
                message.InnerText = result.ErrorMessage;
                var stackTrace = doc.CreateElement("StackTrace", xmlNamespace);
                stackTrace.InnerText = result.ErrorStackTrace;
                output.AppendChild(errorInfo);
                errorInfo.AppendChild(message);
                errorInfo.AppendChild(stackTrace);
                resultNode.AppendChild(output);
            }
            else if(result.Outcome == TestOutcome.Skipped)
            {
                testSkipped++;
            }
            else if(result.Outcome == TestOutcome.Passed)
            {
                testSucceeded++;
            }
            testCount++;

            resultsNode.AppendChild(resultNode);

            var testNode = doc.CreateElement("UnitTest", xmlNamespace);
            testNode.SetAttribute("name", testName);
            testNode.SetAttribute("id", id);
            testNode.SetAttribute("storage", assemblyName);
            XmlElement properties = null;
            var traits = GetProperty<KeyValuePair<string, string>[]>("TestObject.Traits", result.TestCase, new KeyValuePair<string, string>[] { });
            foreach (var prop in traits)
            {
                if (properties == null)
                {
                    properties = doc.CreateElement("Properties", xmlNamespace);
                    testNode.AppendChild(properties);
                }
                var property = doc.CreateElement("Property", xmlNamespace);
                var key = doc.CreateElement("Key", xmlNamespace);
                key.InnerText = prop.Key;
                property.AppendChild(key);
                var value = doc.CreateElement("Value", xmlNamespace);
                value.InnerText = prop.Value;
                property.AppendChild(value);
                properties.AppendChild(property);
            }
            string[] owners = null;
            if (owners != null && owners.Any())
            {
                var ownersNode = doc.CreateElement("Owners", xmlNamespace);
                foreach (var owner in owners)
                {
                    var item = doc.CreateElement("Owner", xmlNamespace);
                    item.SetAttribute("name", owner);
                    ownersNode.AppendChild(item);
                }
                testNode.AppendChild(ownersNode);
            }

            var categories = GetProperty<string[]>("MSTestDiscoverer.TestCategory", result.TestCase, null);

            if (categories != null && categories.Any())
            {
                var testCategory = doc.CreateElement("TestCategory", xmlNamespace);
                foreach (var category in categories)
                {
                    var item = doc.CreateElement("TestCategoryItem", xmlNamespace);
                    item.SetAttribute("TestCategory", category);
                    testCategory.AppendChild(item);
                }
                testNode.AppendChild(testCategory);
            }
            var execution = doc.CreateElement("Execution", xmlNamespace);
            execution.SetAttribute("id", executionId.ToString());
            testNode.AppendChild(execution);
            var testMethodName = doc.CreateElement("TestMethod", xmlNamespace);
            testMethodName.SetAttribute("name", testName);
            var className = GetProperty<string>("MSTestDiscoverer.TestClassName", result.TestCase, result.TestCase.FullyQualifiedName.Substring(0, result.TestCase.FullyQualifiedName.LastIndexOf(".")));
            testMethodName.SetAttribute("className", className);
            testMethodName.SetAttribute("adapterTypeName", GetProperty<string>("TestCase.ExecutorUri", result.TestCase, ""));
            testMethodName.SetAttribute("codeBase", assemblyName);
            testNode.AppendChild(testMethodName);

            testDefinitions.AppendChild(testNode);

            var testEntry = doc.CreateElement("TestEntry", xmlNamespace);
            testEntry.SetAttribute("testListId", testListId);
            testEntry.SetAttribute("testId", id);
            testEntry.SetAttribute("executionId", executionId.ToString());
            testEntries.AppendChild(testEntry);
        }

        private static T GetProperty<T>(string id, TestObject test, T defaultValue)
        {
            var prop = test.Properties.Where(p => p.Id == id).FirstOrDefault();
            if (prop != null)
                return test.GetPropertyValue<T>(prop, defaultValue);
            return defaultValue;
        }

        public void FinalizeReport()
        {
            header.SetAttribute("finish", DateTime.Now.ToString("O"));
            var resultSummary = doc.CreateElement("ResultSummary", xmlNamespace);
            resultSummary.SetAttribute("outcome", "Completed");
            var counters = doc.CreateElement("Counters", xmlNamespace);
            counters.SetAttribute("passed", testSucceeded.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("failed", testFailed.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("notExecuted", testSkipped.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("total", testCount.ToString(CultureInfo.InvariantCulture));
            resultSummary.AppendChild(counters);
            rootNode.AppendChild(resultSummary);
            doc.Save(outputStream);
            if (disposeStream)
            {
                outputStream.Dispose();
            }
        }
    }
}
