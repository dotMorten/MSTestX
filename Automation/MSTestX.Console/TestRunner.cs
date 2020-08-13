using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    public class TestRunner : IDisposable
    {
        private SocketCommunicationManager socket;
        private System.Net.IPEndPoint _endpoint;

        public TestRunner(System.Net.IPEndPoint endpoint = null)
        {
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
                        System.Console.WriteLine($"Retrying connection.... ({i} of 10)");
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
                System.Console.WriteLine("Connected to test adapter");
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
                    System.Console.WriteLine($"Test Host Launched. Process ID '{pid}'");
                }
                else if (msg.MessageType == MessageType.DiscoveryInitialize)
                {
                    System.Console.Write("Discovering tests...");
                    loggerEvents?.OnDiscoveryStart(new DiscoveryStartEventArgs(new DiscoveryCriteria()));
                }
                else if (msg.MessageType == MessageType.DiscoveryComplete)
                {
                    var dcp = JsonDataSerializer.Instance.DeserializePayload<DiscoveryCompletePayload>(msg);
                    System.Console.WriteLine($"Discovered {dcp.TotalTests} tests");

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
                        System.Console.Write($"    {testName}");
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
                                System.Console.ForegroundColor = ConsoleColor.Green;
                                System.Console.Write("  √ ");
                                break;
                            case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped:
                                System.Console.ForegroundColor = ConsoleColor.Yellow;
                                System.Console.Write("  ! ");
                                break;
                            case Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed:
                                System.Console.ForegroundColor = ConsoleColor.Red;
                                System.Console.Write("  X ");
                                break;
                            default:
                                System.Console.Write("    "); break;
                        }
                    System.Console.ResetColor();
                        System.Console.Write(testName);
                        var d = tr.TestResult.Duration;
                        if (d.TotalMilliseconds < 1)
                            System.Console.WriteLine(" [< 1ms]");
                        else if (d.TotalSeconds < 1)
                                System.Console.WriteLine($" [{d.Milliseconds}ms]");
                        else if (d.TotalMinutes < 1)
                                System.Console.WriteLine($" [{d.Seconds}s {d.Milliseconds.ToString("0")}ms]");
                        else 
                            System.Console.WriteLine($" [{d.Minutes}m {d.Seconds}s {d.Milliseconds.ToString("0")}ms]");
                        if (!string.IsNullOrEmpty(testMessage))
                        {
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            System.Console.WriteLine("  Error Message:");
                            System.Console.WriteLine("   " + testMessage);
                            if (!string.IsNullOrEmpty(tr.TestResult.ErrorStackTrace))
                            {
                                System.Console.WriteLine("  Stack Trace:");
                                System.Console.WriteLine("   " + tr.TestResult.ErrorStackTrace);
                            }
                            System.Console.ResetColor();
                            System.Console.WriteLine();
                            // If test failed, also output messages, if any
                            if (tr.TestResult.Messages?.Any() == true)
                            {
                                System.Console.WriteLine("  Standard Output Messages:");
                                foreach (var message in tr.TestResult.Messages)
                                {
                                    System.Console.WriteLine(message.Text);
                                }
                                System.Console.WriteLine();
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
                    if (trc.RunAttachments != null && trc.RunAttachments.Count > 0)
                    {
                        System.Console.WriteLine($"\t Attachments : {trc.RunAttachments.SelectMany(a=>a.Attachments).Count()} ");
                    }
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
                        System.Console.WriteLine("Failed to receive message : " + ex.Message);
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
    }
}
