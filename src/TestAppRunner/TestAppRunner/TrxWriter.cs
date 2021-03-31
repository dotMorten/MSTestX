using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        /// Initializes a new instance of the <see cref="TrxWriter"/> class.
        /// </summary>
        /// <param name="filename">The name of the file to write the report to.</param>
        public TrxWriter(string filename) : this(System.IO.File.Open(filename, System.IO.FileMode.Create), true) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrxWriter"/> class.
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
            rootNode = (XmlElement)doc.AppendChild(doc.CreateElement("TestRun", xmlNamespace));
            rootNode.SetAttribute("id", Guid.NewGuid().ToString());
            rootNode.SetAttribute("name", TestRunName);
            rootNode.SetAttribute("runUser", TestRunUser);
            header = (XmlElement)rootNode.AppendChild(doc.CreateElement("Times", xmlNamespace));
            header.SetAttribute("finish", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("start", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            header.SetAttribute("creation", DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            resultsNode = (XmlElement)rootNode.AppendChild(doc.CreateElement("Results", xmlNamespace));
            testDefinitions = (XmlElement)rootNode.AppendChild(doc.CreateElement("TestDefinitions", xmlNamespace));
            testEntries = (XmlElement)rootNode.AppendChild(doc.CreateElement("TestEntries", xmlNamespace));
            var testLists = rootNode.AppendChild(doc.CreateElement("TestLists", xmlNamespace));
            var testList = (XmlElement)testLists.AppendChild(doc.CreateElement("TestList", xmlNamespace));
            testList.SetAttribute("name", "Results Not in a List");
            testList.SetAttribute("id", testListId);
            testList = (XmlElement)testLists.AppendChild(doc.CreateElement("TestList", xmlNamespace));
            testList.SetAttribute("name", "All Loaded Results");
            testList.SetAttribute("id", "19431567-8539-422a-85d7-44ee4e166bda");
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
            if (result.Outcome == TestOutcome.Failed) testFailed++;
            else if (result.Outcome == TestOutcome.Skipped) testSkipped++;
            else if (result.Outcome == TestOutcome.Passed) testSucceeded++;
            testCount++;

            var innerResultsCount = GetProperty<int>("InnerResultsCount", result, 0);
            if (innerResultsCount > 0) return; // This is a data test, and we don't store the parent result
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
            var resultNode = (XmlElement)resultsNode.AppendChild(doc.CreateElement("UnitTestResult", xmlNamespace));
            resultNode.SetAttribute("outcome", OutcomeToTrx(result.Outcome));
            resultNode.SetAttribute("testType", "13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");
            resultNode.SetAttribute("testListId", testListId);
            resultNode.SetAttribute("executionId", executionId.ToString());
            resultNode.SetAttribute("testName", testName);
            resultNode.SetAttribute("testId", id);
            resultNode.SetAttribute("duration", result.Duration.ToString("G", CultureInfo.InvariantCulture));
            resultNode.SetAttribute("computerName", result.ComputerName);

            string assemblyName = GetProperty<string>("TestCase.Source", result.TestCase, "");
            
            StringBuilder debugTrace = new StringBuilder();
            StringBuilder stdErr = new StringBuilder();
            StringBuilder stdOut = new StringBuilder();
            List<string> textMessages = new List<string>();
            XmlElement outputNode = doc.CreateElement("Output", xmlNamespace);
            if (result.Messages?.Any() == true)
            {
                foreach (TestResultMessage message in result.Messages)
                {
                    if (TestResultMessage.AdditionalInfoCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                        textMessages.Add(message.Text);
                    else if (TestResultMessage.DebugTraceCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                        debugTrace.AppendLine(message.Text);
                    else if (TestResultMessage.StandardErrorCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                        stdErr.AppendLine(message.Text);
                    else if (TestResultMessage.StandardOutCategory.Equals(message.Category, StringComparison.OrdinalIgnoreCase))
                        stdOut.AppendLine(message.Text);
                    else
                        continue; // The message category does not match any predefined category.
                }
            }
            if(stdOut.Length > 0)
                outputNode.AppendChild(doc.CreateElement("StdOut", xmlNamespace)).InnerText = stdOut.ToString();
            if (stdErr.Length > 0)
                outputNode.AppendChild(doc.CreateElement("StdErr", xmlNamespace)).InnerText = stdErr.ToString();
            if (debugTrace.Length > 0)
                outputNode.AppendChild(doc.CreateElement("DebugTrace", xmlNamespace)).InnerText = debugTrace.ToString();
            if (!string.IsNullOrEmpty(result.ErrorMessage) || !string.IsNullOrEmpty(result.ErrorStackTrace))
            {
                var errorInfo = (XmlElement)outputNode.AppendChild(doc.CreateElement("ErrorInfo", xmlNamespace));
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    errorInfo.AppendChild(doc.CreateElement("Message", xmlNamespace)).InnerText = result.ErrorMessage;
                if(!string.IsNullOrEmpty(result.ErrorStackTrace))
                    errorInfo.AppendChild(doc.CreateElement("StackTrace", xmlNamespace)).InnerText = result.ErrorStackTrace;
            }
            if (outputNode.ChildNodes.Count > 0)
            {
                resultNode.AppendChild(outputNode);
            }
            if (textMessages.Any())
            {
                var txtMsgsNode = resultsNode.AppendChild(doc.CreateElement("TextMessages", xmlNamespace));
                foreach(var msg in textMessages)
                    txtMsgsNode.AppendChild(doc.CreateElement("Message", xmlNamespace)).InnerText = msg;
            }

            var testNode = (XmlElement)testDefinitions.AppendChild(doc.CreateElement("UnitTest", xmlNamespace));
            testNode.SetAttribute("name", testName);
            testNode.SetAttribute("id", id);
            testNode.SetAttribute("storage", assemblyName);

            XmlNode properties = null;
            var traits = GetProperty<KeyValuePair<string, string>[]>("TestObject.Traits", result.TestCase, new KeyValuePair<string, string>[] { });
            foreach (var prop in traits)
            {
                if (properties == null)
                    properties = testNode.AppendChild(doc.CreateElement("Properties", xmlNamespace));

                var property = properties.AppendChild(doc.CreateElement("Property", xmlNamespace));
                property.AppendChild(doc.CreateElement("Key", xmlNamespace)).InnerText = prop.Key;
                var value = property.AppendChild(doc.CreateElement("Value", xmlNamespace)).InnerText = prop.Value;
            }

            string[] owners = null;
            if (owners != null && owners.Any())
            {
                var ownersNode = testNode.AppendChild(doc.CreateElement("Owners", xmlNamespace));
                foreach (var owner in owners)
                {
                    var item = (XmlElement)ownersNode.AppendChild(doc.CreateElement("Owner", xmlNamespace));
                    item.SetAttribute("name", owner);
                }
            }

            var categories = GetProperty<string[]>("MSTestDiscoverer.TestCategory", result.TestCase, null);
            if (categories != null && categories.Any())
            {
                var testCategory = testNode.AppendChild(doc.CreateElement("TestCategory", xmlNamespace));
                foreach (var category in categories)
                {
                    var item = (XmlElement)testCategory.AppendChild(doc.CreateElement("TestCategoryItem", xmlNamespace));
                    item.SetAttribute("TestCategory", category);
                }
            }

            var execution = (XmlElement)testNode.AppendChild(doc.CreateElement("Execution", xmlNamespace));
            execution.SetAttribute("id", executionId.ToString());
            var testMethodName = (XmlElement)testNode.AppendChild(doc.CreateElement("TestMethod", xmlNamespace));
            testMethodName.SetAttribute("name", testName);
            var className = GetProperty<string>("MSTestDiscoverer.TestClassName", result.TestCase, result.TestCase.FullyQualifiedName.Substring(0, result.TestCase.FullyQualifiedName.LastIndexOf(".")));
            testMethodName.SetAttribute("className", className);
            testMethodName.SetAttribute("adapterTypeName", GetProperty<string>("TestCase.ExecutorUri", result.TestCase, ""));
            testMethodName.SetAttribute("codeBase", assemblyName);

            var testEntry = (XmlElement)testEntries.AppendChild(doc.CreateElement("TestEntry", xmlNamespace));
            testEntry.SetAttribute("testListId", testListId);
            testEntry.SetAttribute("testId", id);
            testEntry.SetAttribute("executionId", executionId.ToString());
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
            var resultSummary = (XmlElement)rootNode.AppendChild(doc.CreateElement("ResultSummary", xmlNamespace));
            resultSummary.SetAttribute("outcome", "Completed");
            var counters = (XmlElement)resultSummary.AppendChild(doc.CreateElement("Counters", xmlNamespace));
            counters.SetAttribute("passed", testSucceeded.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("failed", testFailed.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("notExecuted", testSkipped.ToString(CultureInfo.InvariantCulture));
            counters.SetAttribute("total", testCount.ToString(CultureInfo.InvariantCulture));
            doc.Save(outputStream);
            if (disposeStream)
            {
                outputStream.Dispose();
            }
        }
    }
}
