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
    /apkid <id>                         Package ID of the test app (if not provided, auto-discovered from manifest)
    /activity <activity id>             Activity to launch (if not provided, auto-discovered from manifest)
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

                await ShutdownApp();

                if (arguments.ContainsKey("apkpath"))
                {
                    var path = arguments["apkpath"];
                    if (!File.Exists(path))
                    {
                        System.Console.WriteLine("ERROR. APK Not Found: " + path);
                        testRunCompleted.TrySetResult(1);
                    }
                    if(apk_id == null || activityName == null)
                    {
                        string id;
                        string name;
                        ApkHelper.GetAPKInfo(path, out id, out name);
                        if (apk_id == null)
                            apk_id = id;
                        if (activityName == null)
                            activityName = name;
                    }
                    System.Console.WriteLine("Installing app...");
                    await device.InstallApk(path);
                }

                // Get unlock state
                var state = await device.GetPowerState();
                if (!state.IsDisplayOn)
                {
                    await device.TurnOnDisplayAsync();

                    while (!state.IsDisplayOn)
                    {
                        await Task.Delay(500);
                        state = await device.GetPowerState();
                    }
                }

                if (state.DisplayState == Device.DisplayState.OnAndLocked)
                {
                    if (arguments.ContainsKey("pin"))
                    {
                        string pin = null;
                        if (int.TryParse(arguments["pin"], out int numericPin)) //Ensures it's numeric
                        {
                            pin = arguments["pin"];
                        }
                        System.Console.WriteLine("Unlocking phone...");
                        await device.UnlockAsync(pin);
                    }
                    else
                    {
                        System.Console.WriteLine("Device appears to be locked. Please unlock your device");
                    //    System.Console.ReadKey();
                    }
                }
                if (File.Exists("RunLog.txt"))
                    File.Delete("RunLog.txt");

                if (arguments.ContainsKey("logFileName"))
                    outputFilename = arguments["logFileName"];
                else
                {
                    string defaultFilename = device.Serial + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                    if (apk_id != null)
                        defaultFilename += "_" + apk_id;
                    outputFilename = Path.Combine(System.Environment.CurrentDirectory, defaultFilename + ".trx");
                }

                monitor = new LogCatMonitor(device.Serial);
                // Connecting to logcat...                                
                await client.SendDeviceCommandAsync("shell:logcat -b all -c", device.Serial); //Configure logcat
                bool result;
                if (device.ApiLevel > 22)
                    result = await monitor.OpenAsync($"{apk_id} -T 1");
                else
                    result = await monitor.OpenAsync($"");
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

                await device.SetPortForward(38300, 38300);  //Set up port forwarding for socket communication
                
                System.Console.WriteLine($"Launching app {apk_id}/{activityName} on device " + device.Serial + "...");

                await device.LaunchApp(apk_id, activityName, new Dictionary<string, object>() { { "TestAdapterPort", 38300 } });
               
                // Keep looking for process starting up
                bool launched = false;
                while (!launched)
                {
                    await Task.Delay(1000);
                    var pid = await device.GetProcessId(apk_id);
                    if (appLaunchDetected)
                        break;
                    if (pid > 0)
                    {
                        launched = true;
                        System.Console.WriteLine($"Test Host Launched. Process ID '{pid}'");
                        OnApplicationLaunched();
                        break;
                    }
                    else
                    {
                        //Keep retrying. Doesn't always launch the first time
                        await device.LaunchApp(apk_id, activityName, new Dictionary<string, object>() { { "TestAdapterPort", 38300 } });
                    }
                }
                
            }
        }

        private static async Task ShutdownApp()
        {
            var id = await device.GetProcessId(apk_id);
            if (id > 0)
            {
                System.Console.WriteLine($"Application '{apk_id}' already running. Shutting down...");
                await device.StopApp(apk_id); //Ensure app isn't already running
                while (await device.GetProcessId(apk_id) > 0)
                {
                    await Task.Delay(100);
                }
            }
        }

        private static bool appLaunchDetected;

        private static async Task OnApplicationLaunched(System.Net.IPEndPoint endpoint = null)
        {
            if (appLaunchDetected)
                return;
            appLaunchDetected = true;
            int result = 0;
            var runner = new TestRunner(endpoint);
            try
            {
                await Task.Delay(1000); //Give app some time to start up
                await runner.RunTests(outputFilename, settingsXml, processExitCancellationTokenSource.Token);
            }
            catch(System.Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                result = 1;
            }
            finally
            {
                await KillProcess();
            }
            testRunCompleted.TrySetResult(result);
        }

        private static async Task KillProcess()
        {
            if (monitor != null)
            {
                monitor.LogReceived -= Monitor_LogReceived;
            }
            if (device != null)
            {
                await device.StopApp(apk_id);
            }
            monitor?.Close();
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
                if(e.Tag == "ActivityManager" && 
                        msg.StartsWith("Start proc ") &&
                        msg.Contains($":{apk_id}") &&
                        msg.Contains($" for activity {apk_id}/{activityName}")
                        && !msg.Contains(" for backup "))
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
            if(e.Tag == "ActivityManager")
            {

            }
            if (e.Tag == "ActivityManager" && msg.StartsWith($"Displayed {apk_id}/{activityName}"))
            {
                OnApplicationLaunched(); //Detect app launched and start VSTest connection
                return;
            }
            if (processID > 0 && e.Tag == "ActivityManager" && msg == $"Process {apk_id} (pid {processID}) has died.")
            {
                // Application died
                System.Console.WriteLine($"{Environment.NewLine}Android application process has died. Exiting...");
                processExitCancellationTokenSource.Cancel();
                //OnTestRunAborted("Android application process has died");
                //testRunCompleted.TrySetResult(1);
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
                if (msg.StartsWith("Successfully killed process") && msg.Contains($" pid {processID}")) 
                {
                    //App likely exited. Check if it's still alive
                    var _ = device.GetProcessId(apk_id).ContinueWith(t => 
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            if (t.Result == 0)
                            {
                                System.Console.WriteLine($"{Environment.NewLine}Android application process killed. Exiting...");
                                processExitCancellationTokenSource.Cancel();
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
