using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using MSTestX.Remote;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    public class TestRunner : IDisposable
    {
        private SocketCommunicationManager socket;
        private System.Net.IPEndPoint _endpoint;
        private ILog logger;

        public TestRunner(ILog logger, System.Net.IPEndPoint endpoint = null)
        {
            this.logger = logger;
            _endpoint = endpoint;
        }

        public async Task RunTests(string outputFilename, string settingsXml, CancellationToken cancellationToken)
        {
            var loggerEvents = new TestLoggerEventsImpl();
            var logger = new Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger();
            var parameters = new Dictionary<string, string>() { { "TestRunDirectory", "." } };
            if (!string.IsNullOrEmpty(outputFilename))
                parameters.Add("LogFileName", outputFilename);
            logger.Initialize(loggerEvents, parameters);
            try
            {
                await RunTestsInternal(outputFilename, settingsXml, loggerEvents, cancellationToken);
            }
            catch
            {
                if (loggerEvents != null)
                {
                    var result = new TestRunCompleteEventArgs(null, false, true, null, null, TimeSpan.Zero); //TRXLogger doesn't use these values anyway
                    loggerEvents?.OnTestRunComplete(result);
                }
                throw;
            }
            finally
            {
                socket.StopClient();
            }
        }

        private async Task RunTestsInternal(string outputFilename, string settingsXml, TestLoggerEventsImpl loggerEvents, CancellationToken cancellationToken)
        {
            System.Console.WriteLine("Waiting for connection to test adapter...");
            for (int i = 1; i <= 10; i++)
            {
                socket = new SocketCommunicationManager();
                await socket.SetupClientAsync(_endpoint ?? new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 38300)).ConfigureAwait(false);
                if (!socket.WaitForServerConnection(10000))
                {
                    if (i == 10)
                    {
                        throw new Exception("No connection to test host could be established. Make sure the app is running in the foreground.");
                    }
                    else
                    {
                        logger.LogInfo($"Retrying connection.... ({i} of 10)");
                        continue;
                    }
                }
                break;
            }
            socket.SendMessage(MessageType.SessionConnected); //Start session

            //Perform version handshake
            Message msg = await ReceiveMessageAsync(cancellationToken);
            if (msg?.MessageType == MessageType.VersionCheck)
            {
                var version = JsonDataSerializer.Instance.DeserializePayload<int>(msg);
                var success = version == 1;
                logger.LogInfo("Connected to test adapter");
            }
            else
            {
                throw new InvalidOperationException("Handshake failed");
            }

            // Get tests
            socket.SendMessage(MessageType.StartDiscovery,
                new DiscoveryRequestPayload()
                {
                    Sources = new string[] { },
                    RunSettings = settingsXml ?? @"<?xml version=""1.0"" encoding=""utf-8""?><RunSettings><RunConfiguration /></RunSettings>",
                    TestPlatformOptions = null
                });

            int pid = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                msg = await ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (msg == null)
                {
                    continue;
                }

                if (msg.MessageType == MessageType.TestHostLaunched)
                {
                    var thl = JsonDataSerializer.Instance.DeserializePayload<TestHostLaunchedPayload>(msg);
                    pid = thl.ProcessId;
                    logger.LogInfo($"Test Host Launched. Process ID '{pid}'");
                }
                else if (msg.MessageType == MessageType.DiscoveryInitialize)
                {
                    logger.LogInfo("Discovering tests...", true);
                    loggerEvents?.OnDiscoveryStart(new DiscoveryStartEventArgs(new DiscoveryCriteria()));
                }
                else if (msg.MessageType == MessageType.DiscoveryComplete)
                {
                    var dcp = JsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);
                    logger.LogInfo($"Discovered {dcp.TotalTests} tests");

                    loggerEvents?.OnDiscoveryComplete(new DiscoveryCompleteEventArgs(dcp.TotalTests, false));
                    loggerEvents?.OnDiscoveredTests(new DiscoveredTestsEventArgs(dcp.LastDiscoveredTests));
                    //Start testrun
                    socket.SendMessage(MessageType.TestRunSelectedTestCasesDefaultHost,
                        new TestRunRequestPayload() { TestCases = dcp.LastDiscoveredTests.ToList(), RunSettings = settingsXml });
                    loggerEvents?.OnTestRunStart(new TestRunStartEventArgs(new TestRunCriteria(dcp.LastDiscoveredTests, 1)));
                }
                else if (msg.MessageType == MessageType.DataCollectionTestStart)
                {
                    if (!System.Console.IsOutputRedirected)
                    {
                        var tcs = JsonDataSerializer.Instance.DeserializePayload<TestCaseStartEventArgs>(msg);
                        var testName = tcs.TestCaseName;
                        if (string.IsNullOrEmpty(testName))
                            testName = tcs.TestElement.DisplayName;
                        logger.LogInfo($"    {testName}", true);
                    }
                }
                else if (msg.MessageType == MessageType.DataCollectionTestEnd)
                {
                    //Skip
                }
                else if (msg.MessageType == MessageType.DataCollectionTestEndResult)
                {
                    var tr = JsonDataSerializer.Instance.DeserializePayload<Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection.TestResultEventArgs>(msg);
                    var testName = tr.TestResult.DisplayName;
                    if (string.IsNullOrEmpty(testName))
                        testName = tr.TestElement.DisplayName;

                    var outcome = tr.TestResult.Outcome;

                    var parentExecId = tr.TestResult.Properties.Where(t => t.Id == "ParentExecId").Any() ?
                        tr.TestResult.GetPropertyValue<Guid>(tr.TestResult.Properties.Where(t => t.Id == "ParentExecId").First(), Guid.Empty) : Guid.Empty;
                    if (outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed)
                    {
                    }
                    else if (outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Green;
                    }
                    if (!System.Console.IsOutputRedirected)
                    {
                        System.Console.SetCursorPosition(0, System.Console.CursorTop);
                    }
                    string testMessage = tr.TestResult?.ErrorMessage;
                    if (parentExecId == Guid.Empty || !System.Console.IsOutputRedirected)
                    {
                        switch(outcome)
                        {
                            case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed:
                                // System.Console.ForegroundColor = ConsoleColor.Green;
                                // System.Console.Write("  √ ");
                                logger.LogOk("  √ ", true);
                                break;
                            case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped:
                                //System.Console.ForegroundColor = ConsoleColor.Yellow;
                                logger.LogWarning("  ! ", true);
                                break;
                            case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed:
                                //System.Console.ForegroundColor = ConsoleColor.Red;
                                logger.LogError("  X ", true);
                                break;
                            default:
                                logger.LogInfo("    ", true); break;
                        }
                        System.Console.ResetColor();
                        logger.LogInfo(testName, true);
                        var d = tr.TestResult.Duration;
                        if (d.TotalMilliseconds < 1)
                            logger.LogInfo(" [< 1ms]");
                        else if (d.TotalSeconds < 1)
                            logger.LogInfo($" [{d.Milliseconds}ms]");
                        else if (d.TotalMinutes < 1)
                            logger.LogInfo($" [{d.Seconds}s {d.Milliseconds.ToString("0")}ms]");
                        else if (d.TotalHours < 1)
                            logger.LogInfo($" [{d.Minutes}m {d.Seconds}s {d.Milliseconds.ToString("0")}ms]");
                        else if (d.TotalDays < 1)
                            logger.LogInfo($" [{d.Hours}h {d.Minutes}m {d.Seconds}s {d.Milliseconds.ToString("0")}ms]");
                        else
                            logger.LogInfo($" [{Math.Floor(d.TotalDays)}d {d.Hours}h {d.Minutes}m {d.Seconds}s {d.Milliseconds.ToString("0")}ms]"); // I sure hope your tests won't ever need this line of code
                        if (!string.IsNullOrEmpty(testMessage))
                        {
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            logger.LogError("  Error Message:\n", true);
                            logger.LogError("   " + testMessage + "\n", true);
                            if (!string.IsNullOrEmpty(tr.TestResult.ErrorStackTrace))
                            {
                                logger.LogError("  Stack Trace:\n", true);
                                logger.LogError("   " + tr.TestResult.ErrorStackTrace + "\n", true);
                            }
                            logger.LogError("", true);
                            // If test failed, also output messages, if any
                            if (tr.TestResult.Messages?.Any() == true)
                            {
                                logger.LogError("  Standard Output Messages:", true);
                                foreach (var message in tr.TestResult.Messages)
                                {
                                    logger.LogError(message.Text, true);
                                }
                            }
                            logger.LogError("");
                        }
                    }


                    // Make attachment paths absolute
                    foreach (var set in tr.TestResult.Attachments)
                    {
                        for (int i = 0; i < set.Attachments.Count; i++)
                        {
                            var uri = set.Attachments[i].Uri.OriginalString;

                            if (!set.Attachments[i].Uri.IsAbsoluteUri)
                            {
                                DirectoryInfo d = new DirectoryInfo(".");
                                var newPath = Path.Combine(d.FullName, uri);
                                newPath = newPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
                                set.Attachments[i] = new Microsoft.VisualStudio.TestPlatform.ObjectModel.UriDataAttachment(
                                    new Uri(newPath, UriKind.Relative), set.Attachments[i].Description);
                            }
                        }
                    }
                    loggerEvents?.OnTestResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestResultEventArgs(tr.TestResult));
                }
                else if (msg.MessageType == MessageType.ExecutionComplete)
                {
                    var trc = JsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);
                    loggerEvents?.OnTestRunComplete(trc.TestRunCompleteArgs);
                    System.Console.WriteLine();
                    System.Console.WriteLine("Test Run Complete");
                    System.Console.WriteLine($"Total tests: {trc.LastRunTests.TestRunStatistics.ExecutedTests} tests");
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine($"     Passed : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed]} ");
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine($"     Failed : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed]} ");
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine($"    Skipped : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped]} ");
                    System.Console.ResetColor(); 
                    System.Console.WriteLine($" Total time: {trc.TestRunCompleteArgs.ElapsedTimeInRunningTests.TotalSeconds} Seconds");
                    return; //Test run is complete -> Exit message loop
                }
                else if (msg.MessageType == MessageType.AbortTestRun)
                {
                    throw new TaskCanceledException("Test Run Aborted!");
                }
                else if (msg.MessageType == MessageType.CancelTestRun)
                {
                    throw new TaskCanceledException("Test Run Cancelled!");
                }
                else if (msg.MessageType == MessageType.TestMessage)
                {
                    var tm = JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);
                    System.Console.WriteLine($"{tm.MessageLevel}: {tm.Message}");
                }
                else if (msg.MessageType == "AttachmentSet")
                {
                    var set = JsonDataSerializer.Instance.DeserializePayload<FileAttachmentSet>(msg);
                    foreach(var attachment in set.Attachments)
                    {
                        var path = attachment.Uri.OriginalString;
                        try
                        {
                            var dir = Path.GetDirectoryName(path);
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);
                            File.WriteAllBytes(path, attachment.Data);
                        }
                        catch { }
                    }
                }
                else
                {
                    System.Console.WriteLine($"Received: {msg.MessageType} -> {msg.Payload}");
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            return Task.Run<Message>(() =>
            {
                Message msg = null;
                // Set read timeout to avoid blocking receive raw message
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        msg = socket.ReceiveMessage();
                        cancellationToken.ThrowIfCancellationRequested();
                        if (msg != null)
                        {
                            return msg;
                        }
                    }
                    catch (EndOfStreamException endofStreamException)
                    {
                        throw new Exception("Test run is aborted.", endofStreamException);
                    }
                    catch (IOException ioException)
                    {
                        var socketException = ioException.InnerException as SocketException;
                        if (socketException != null && socketException.SocketErrorCode == SocketError.TimedOut)
                        {
                            throw new Exception("Test runner connection timed out", ioException);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.LogError("Failed to receive message : " + ex.Message);
                        continue;
                    }
                }
                return msg;
            });
        }

        public void Dispose()
        {
            socket.StopClient();
        }

        private class TestLoggerEventsImpl : TestLoggerEvents
        {
            public void OnTestRunMessage(TestRunMessageEventArgs e) => TestRunMessage?.Invoke(this, e);
            public override event EventHandler<TestRunMessageEventArgs> TestRunMessage;

            public void OnTestRunStart(TestRunStartEventArgs e) => TestRunStart?.Invoke(this, e);
            public override event EventHandler<TestRunStartEventArgs> TestRunStart;

            public void OnTestResult(Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestResultEventArgs e) => TestResult?.Invoke(this, e);
            public override event EventHandler<Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestResultEventArgs> TestResult;

            public void OnTestRunComplete(TestRunCompleteEventArgs e) => TestRunComplete?.Invoke(this, e);
            public override event EventHandler<TestRunCompleteEventArgs> TestRunComplete;

            public void OnDiscoveryStart(DiscoveryStartEventArgs e) => DiscoveryStart?.Invoke(this, e);
            public override event EventHandler<DiscoveryStartEventArgs> DiscoveryStart;

            public void OnDiscoveryMessage(TestRunMessageEventArgs e) => DiscoveryMessage?.Invoke(this, e);
            public override event EventHandler<TestRunMessageEventArgs> DiscoveryMessage;

            public void OnDiscoveredTests(DiscoveredTestsEventArgs e) => DiscoveredTests?.Invoke(this, e);
            public override event EventHandler<DiscoveredTestsEventArgs> DiscoveredTests;

            public void OnDiscoveryComplete(DiscoveryCompleteEventArgs e) => DiscoveryComplete?.Invoke(this, e);
            public override event EventHandler<DiscoveryCompleteEventArgs> DiscoveryComplete;
        }


        [DataContract]
        private class FileAttachmentSet
        {
            [DataMember]
            public string Uri { get; set; }

            [DataMember]
            public string DisplayName { get; set; }

            [DataMember]
            public IList<FileDataAttachment> Attachments { get; set; }
        }
        [DataContract]
        private class FileDataAttachment
        {
            [DataMember]
            public string Description { get; set; }
            [DataMember]
            public Uri Uri { get; set; }
            private byte[] data;
            [DataMember]
            public byte[] Data { get; set; }
        }
    }
}
