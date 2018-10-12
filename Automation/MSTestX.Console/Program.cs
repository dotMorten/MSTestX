//using Microsoft.VisualStudio.TestPlatform.ObjectModel;
//using Newtonsoft.Json;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using MSTestX.Console.Adb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    class Program 
    {
        private static Device device;
        private static string apk_id = null;
        private static string activityName = null;
        private static AdbClient client;
        private static string outputFilename;
        private static string settingsXml = null;
        private static LogCatMonitor monitor;
        private static SocketCommunicationManager socket;
        private static CancellationTokenSource processExitCancellationTokenSource;
        private static TaskCompletionSource<int> testRunCompleted = new TaskCompletionSource<int>();

        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "-?" || args[0] == "?")
            {
                PrintUsage();
                return;
            }
            await RunTest(ParseArguments(args));
            var exitCode = await testRunCompleted.Task;
            Environment.Exit(exitCode);
        }

        private static void PrintUsage()
        {
            System.Console.WriteLine(@"Test Runner commandline arguments:
    /remoteIp <ip:port>                 The IP address of a device already running the unit test app
    /logFileName <path>                 TRX Output Filename
    /settings <path>                    MSTest RunSettings xml file

Android specific (ignored if using remoteIp):
    /deviceid <Device Serial Number>    If more than one device is connected, specifies which device to use
    /apkpath <file path>                Path to an APK to install.
    /apkid <id>                         Package ID of the test app
    /activity <activity id>             Activity to launch
    /pin <pin code>                     Pin to use to unlock your phone (or empty to just unlock phone with no pin)
");
        }

        static async Task RunTest(Dictionary<string, string> arguments)
        {
            processExitCancellationTokenSource = new CancellationTokenSource();

            if (arguments.ContainsKey("settings") && File.Exists(arguments["settings"]))
            {
                settingsXml = File.ReadAllText(arguments["settings"]);
            }

            System.Net.IPEndPoint testAdapterEndpoint = null;
            if (arguments.ContainsKey("remoteIp"))
            {
                var val = arguments["remoteIp"];
                if (val.Contains(":") && System.Net.IPAddress.TryParse(val.Split(':')[0], out System.Net.IPAddress ip) && int.TryParse(val.Split(':')[1], out int port))
                {
                    testAdapterEndpoint = new System.Net.IPEndPoint(ip, port);
                }
                else
                {
                    System.Console.WriteLine("Invalid remote ip and/or port");
                    testRunCompleted.TrySetResult(1);
                    return;
                }
            }
            if (testAdapterEndpoint != null)
            {
                await OnApplicationLaunched(testAdapterEndpoint);
            }
            else
            {
                // Launch Android android app
                client = new AdbClient();
                var devices = await client.GetDevicesAsync();
                if (!devices.Any())
                {
                    System.Console.WriteLine("No devices connected to ADB");
                    testRunCompleted.TrySetResult(1);
                    return;
                }
                if (arguments.ContainsKey("deviceid"))
                {
                    device = devices.Where(d => d.Serial == arguments["deviceid"]).FirstOrDefault();
                    if (device == null)
                    {
                        System.Console.WriteLine($"ERROR Device '{arguments["deviceid"]}' not found");
                        testRunCompleted.TrySetResult(1);
                        return;
                    }
                }
                if (device == null)
                {
                    if (devices.Count() > 1)
                    {
                        System.Console.WriteLine($"ERROR. Multiple devices connected. Please specify -deviceId <deviceid>");
                        foreach (var d in devices)
                        {
                            System.Console.WriteLine($"\t{d.Serial}\t{d.Name}\t{d.Model}\t{d.Product}\t{d.State}");
                        }
                        testRunCompleted.TrySetResult(1);
                        return;
                    }
                    device = devices.First();
                }

                if (arguments.ContainsKey("apkid"))
                    apk_id = arguments["apkid"];
                if (arguments.ContainsKey("activity"))
                    activityName = arguments["activity"];

                await client.SendShellCommandAsync($"am force-stop {apk_id}", device.Serial); //Ensure app isn't already running

                if (arguments.ContainsKey("apkpath"))
                {
                    var path = arguments["apkpath"];
                    if (!File.Exists(path))
                    {
                        System.Console.WriteLine("ERROR. APK Not Found: " + path);
                        testRunCompleted.TrySetResult(1);
                    }
                    System.Console.WriteLine("Installing app...");
                    await client.InstallApk(device.Serial, path);
                }

                // Get unlock state
                var dmpData = await client.SendCommandAndReceiveDataAsync("shell: dumpsys power", device.Serial);
                string[] data = Encoding.ASCII.GetString(dmpData).Split('\n');
                var mHoldingWakeLockSuspendBlocker = data.Where(d => d.TrimStart().StartsWith("mHoldingWakeLockSuspendBlocker")).FirstOrDefault()?.EndsWith("=true") == true;
                var mHoldingDisplaySuspendBlocker = data.Where(d => d.TrimStart().StartsWith("mHoldingDisplaySuspendBlocker")).FirstOrDefault()?.EndsWith("=true") == true;
                bool isDisplayOn = false;
                bool isLocked = false;
                if (!mHoldingWakeLockSuspendBlocker && mHoldingDisplaySuspendBlocker)
                {
                    // Display on but locked
                    isDisplayOn = true;
                    isLocked = true;
                }
                else if (mHoldingWakeLockSuspendBlocker && mHoldingDisplaySuspendBlocker)
                {
                    isDisplayOn = true;
                    isLocked = false;
                }
                if (!mHoldingWakeLockSuspendBlocker && !mHoldingDisplaySuspendBlocker)
                {
                    isDisplayOn = false;
                    isLocked = true;
                }
                if (!isDisplayOn)
                    await client.TurnOnDisplayAsync(device.Serial);
                if (arguments.ContainsKey("pin") && isLocked)
                {
                    string pin = null;
                    if (int.TryParse(arguments["pin"], out int numericPin)) //Ensures it's numeric
                    {
                        pin = arguments["pin"];
                    }
                    System.Console.WriteLine("Unlocking phone...");
                    await client.UnlockAsync(device.Serial, pin);
                }

                if (File.Exists("RunLog.txt"))
                    File.Delete("RunLog.txt");

                if (arguments.ContainsKey("logFileName"))
                    outputFilename = arguments["logFileName"];
                else
                    outputFilename = Path.Combine(System.Environment.CurrentDirectory, "TestRunReport.trx");

                monitor = new LogCatMonitor(device.Serial);
                // Connecting to logcat...
                bool result = await monitor.OpenAsync($"{ apk_id}");
                if (result)
                {
                    System.Console.WriteLine("Connected logcat to " + device.Serial);
                }
                else
                {
                    System.Console.WriteLine("Failed to connect to LogCat");
                    return;
                }
                monitor.LogReceived += Monitor_LogReceived;
                await client.SendCommandAsync($"host-serial:{device.Serial}:forward:tcp:38300;tcp:38300", null);  //Set up port forwarding for socket communication
                
                System.Console.WriteLine($"Launching app {apk_id}/{activityName} on device " + device.Serial + "...");
                string launchCommand = $"am start -n {apk_id}/{activityName} --ez TestAdapterPort 38300";
                await client.SendShellCommandAsync(launchCommand, device.Serial);
            }
        }

        private class TestLoggerEventsImpl : Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.TestLoggerEvents
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
        private static Task<Message> ReceiveMessageAsync()
        {
            return Task.Run<Message>(() =>
            {
                Message msg = null;
                // Set read timeout to avoid blocking receive raw message
                while (!processExitCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        msg = socket.ReceiveMessage();
                        if (msg != null)
                            return msg;
                    }
                    catch (IOException ioException)
                    {
                        var socketException = ioException.InnerException as SocketException;
                        if (socketException != null && socketException.SocketErrorCode == SocketError.TimedOut)
                        {
                            System.Console.WriteLine("Test runner connection timed out");
                            testRunCompleted.TrySetResult(1);
                            break;
                        }
                        else
                        {
                            //System.Console.WriteLine("Failed to receive message : " + ioException.Message);
                            //testRunCompleted.TrySetResult(1);
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

        private static async Task OnApplicationLaunched(System.Net.IPEndPoint endpoint = null)
        {
            TestLoggerEventsImpl loggerEvents = new TestLoggerEventsImpl();
            var logger = new Microsoft.VisualStudio.TestPlatform.Extensions.TrxLogger.TrxLogger();
            var parameters = new Dictionary<string, string>() { { "TestRunDirectory", "." } };
            if (!string.IsNullOrEmpty(outputFilename))
                parameters.Add("LogFileName", outputFilename);
            logger.Initialize(loggerEvents, parameters);

            System.Console.WriteLine("Waiting for connection to test adapter...");
            socket = new SocketCommunicationManager();
            await socket.SetupClientAsync(endpoint ?? new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 38300)).ConfigureAwait(false);
            if (!socket.WaitForServerConnection(5 * 10000))
            {
                System.Console.WriteLine("No connection to test host could be established. Make sure the app is running in the foreground.");
                KillProcess();
                testRunCompleted.TrySetResult(1);
                return;
            }
            socket.SendMessage(MessageType.SessionConnected); //Start session
            
            //Perform version handshake
            Message msg = await ReceiveMessageAsync();
            if(msg.MessageType == MessageType.VersionCheck)
            {
                var version = JsonDataSerializer.Instance.DeserializePayload<int>(msg);
                var success = version == 1;
                System.Console.WriteLine("Connected to test adapter");
            }
            else
            {
                testRunCompleted.TrySetException(new InvalidOperationException("Handshake failed"));
                return;
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
            
            while (!processExitCancellationTokenSource.Token.IsCancellationRequested)
            {
                msg = await ReceiveMessageAsync().ConfigureAwait(false);
                if (msg == null)
                {
                    return;
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
                    // foreach (var t in dcp.LastDiscoveredTests)
                    //     System.Console.WriteLine($"\t{t.FullyQualifiedName}");

                    loggerEvents?.OnDiscoveryComplete(new DiscoveryCompleteEventArgs(dcp.TotalTests, false));
                    loggerEvents?.OnDiscoveredTests(new DiscoveredTestsEventArgs(dcp.LastDiscoveredTests));
                    //Start testrun
                    socket.SendMessage(MessageType.TestRunSelectedTestCasesDefaultHost,
                        new Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.TestRunRequestPayload() { TestCases = dcp.LastDiscoveredTests.ToList(), RunSettings = settingsXml });
                    loggerEvents?.OnTestRunStart(new TestRunStartEventArgs(new TestRunCriteria(dcp.LastDiscoveredTests, 1)));
                }
                else if (msg.MessageType == MessageType.DataCollectionTestStart)
                {
                    var tcs = JsonDataSerializer.Instance.DeserializePayload<TestCaseStartEventArgs>(msg);
                    var testName = tcs.TestCaseName;
                    testName = tcs.TestElement.DisplayName;
                    if(!System.Console.IsOutputRedirected)
                        System.Console.Write($"Running {testName}");
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
                        System.Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (outcome == Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    if (!System.Console.IsOutputRedirected)
                    {
                        if (parentExecId == Guid.Empty) //Not a data test child item
                            System.Console.SetCursorPosition(0, System.Console.CursorTop);
                        else
                            System.Console.Write("\t");
                    }
                    string testMessage = tr.TestResult?.ErrorMessage;
                    if(parentExecId == Guid.Empty || !System.Console.IsOutputRedirected)
                        System.Console.WriteLine($"{outcome}\t{testName}\t{testMessage}");
                    System.Console.ResetColor();
                    loggerEvents?.OnTestResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestResultEventArgs(tr.TestResult));
                }
                else if (msg.MessageType == MessageType.ExecutionComplete)
                {
                    System.Console.WriteLine("Test Run Complete");
                    var trc = JsonDataSerializer.Instance.DeserializePayload<TestRunCompletePayload>(msg);
                    System.Console.WriteLine($"Result: Ran {trc.LastRunTests.TestRunStatistics.ExecutedTests} tests");
                    System.Console.WriteLine($"\t Passed : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed]} ");
                    System.Console.WriteLine($"\t Failed : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed]} ");
                    System.Console.WriteLine($"\t Skipped : {trc.LastRunTests.TestRunStatistics.Stats[Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped]} ");
                    loggerEvents?.OnTestRunComplete(trc.TestRunCompleteArgs);
                    KillProcess();
                    return;
                }
                else if (msg.MessageType == MessageType.AbortTestRun)
                {
                    System.Console.WriteLine("Test Run Aborted!");
                    KillProcess();
                    return;
                }
                else if(msg.MessageType == MessageType.CancelTestRun)
                {
                    System.Console.WriteLine("Test Run Cancelled!");
                    KillProcess();
                    return;
                }
                else if(msg.MessageType == MessageType.TestMessage)
                {
                    var tm = JsonDataSerializer.Instance.DeserializePayload<TestMessagePayload>(msg);
                    System.Console.WriteLine($"{tm.MessageLevel}: {tm.Message}");
                }
                else
                {
                    System.Console.WriteLine($"Received: {msg.MessageType} -> {msg.Payload}");
                }
            }
        }

        private static async void KillProcess()
        {
            socket.StopClient();
            if (monitor != null)
            {
                monitor.LogReceived -= Monitor_LogReceived;
            }
            if (client != null)
            {
                await client.SendShellCommandAsync($"am force-stop {apk_id}", device.Serial);
            }
            monitor?.Close();
            testRunCompleted.TrySetResult(0);
        }

        private static int processID = -1;
        
        private static void Monitor_LogReceived(object sender, LogCatMonitor.LogEntry e)
        {
            var msg = e.DataString;
            if (msg == null) return;
            //Log anything from logcat for the process or it the APK ID is in the message
            if (processID > 0 && e.ProcessId == processID || msg.Contains(apk_id))
            {
                File.AppendAllText("RunLog.txt", $"{e.TimeStamp}\t{e.Type}\t{e.Tag}\t{e.DataString}{Environment.NewLine}");
            }

            if (processID < 0)
            {
                if (msg.Contains("Start proc") && msg.Contains($":{apk_id}") && !msg.Contains(" for backup ") && msg.Contains(":"))
                {
                    string processIDStr = msg.Substring(11, msg.IndexOf(":") - 11);
                    if (!int.TryParse(processIDStr, out processID))
                        return;
                    else
                    {
                        File.AppendAllText("RunLog.txt", $"{Environment.NewLine}====================================={Environment.NewLine}Application launched with Process ID {processID}{Environment.NewLine}");
                        System.Console.WriteLine($"Application launched with Process ID {processID}");
                    }
                }
            }

            //Log anything from logcat for the process or it the APK ID is in the message
            if (processID > 0 && e.ProcessId == processID || msg.Contains(apk_id))
            {
                File.AppendAllText("RunLog.txt", $"{e.TimeStamp}\r{e.Type}\r{e.Tag}\t{e.DataString}{Environment.NewLine}");
            }

            if (e.Tag == "ActivityManager" && msg.StartsWith($"Displayed {apk_id}/{activityName}"))
            {
                OnApplicationLaunched(); //Detect app launched and start VSTest connection
                return;
            }
            if (processID > 0 && e.ProcessId == processID)
            {
                if (e.Tag == "AndroidRuntime" && e.Type == LogCatMonitor.LogEntry.LogType.Error)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine(Environment.NewLine + "ERROR: " + e.DataString);
                    System.Console.ResetColor();
                }
            }
            else if (e.Tag == "libprocessgroup")
            {
                if (msg.StartsWith("Successfully killed process") && msg.Contains($" pid {processID}")) //App likely exited
                {
                    var _ = client.SendCommandAndReceiveDataAsync($"shell: pidof {apk_id}", device.Serial).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            if (t.Result.Length == 0)
                            {
                                processExitCancellationTokenSource.Cancel();
                                System.Console.WriteLine($"{Environment.NewLine}Android application process killed. Exiting...");
                                if (Debugger.IsAttached)
                                    System.Console.ReadKey();
                                testRunCompleted.TrySetResult(1);
                            }
                        }
                    });

                }
            }
        }
        
        private static Dictionary<string, string> ParseArguments(string[] args)
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                string key = null;
                string value = null;
                if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    key = args[i].Substring(1);
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("-") && !args[i + 1].StartsWith("/"))
                    {
                        i++;
                        value = args[i];
                    }
                }
                if (key != null)
                    result[key] = value;
            }
            return result;
        }
    }
}
