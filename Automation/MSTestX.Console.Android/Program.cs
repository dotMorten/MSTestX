//using Microsoft.VisualStudio.TestPlatform.ObjectModel;
//using Newtonsoft.Json;
using MSTestX.Console.Adb;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "-?" || args[0] == "?")
            {
                PrintUsage();
                return;
            }
            RunTest(ParseArguments(args));
            while(true) System.Console.ReadKey(true);
        }

        private static void PrintUsage()
        {
            System.Console.WriteLine(@"Test Runner commandline arguments:
    /deviceid <Device Serial Number>    If more than one device is connected, specifies which device to use
    /apkpath <file path>                Path to an APK to install
    /apkid <id>                         Package ID of the test app
    /activity <activity id>             Activity to launch
    /pin <pin code>                     Pin to use to unlock your phone (or empty to just unlock phone with no pin)
    /logFileName                        TRX Output Filename
");
        }

        static async void RunTest(Dictionary<string, string> arguments)
        {
            client = new AdbClient();
            var devices = await client.GetDevicesAsync();
            if(!devices.Any())
            {
                System.Console.WriteLine("No devices connected to ADB");
                Environment.Exit(0);
                return;
            }
            if (arguments.ContainsKey("deviceid"))
            {
                device = devices.Where(d => d.Serial == arguments["deviceid"]).FirstOrDefault();
                if(device == null)
                {
                    System.Console.WriteLine($"ERROR Device '{arguments["deviceid"]}' not found");
                    Environment.Exit(0);
                    return;
                }
            }
            if (device == null)
            {
                if(devices.Count() > 1)
                {
                    System.Console.WriteLine($"ERROR. Multiple devices connected. Please specify -deviceId <deviceid>");
                    foreach(var d in devices)
                    {
                        System.Console.WriteLine($"\t{d.Serial}\t{d.Name}\t{d.Model}\t{d.Product}\t{d.State}");
                    }
                    Environment.Exit(0);
                    return;
                }
                device = devices.First();
            }
            await client.SendShellCommandAsync($"am force-stop {apk_id}", device.Serial); //Ensure app isn't already running

            if (arguments.ContainsKey("apkpath"))
            {
                var path = arguments["apkpath"];
                if (!File.Exists(path))
                {
                    System.Console.WriteLine("ERROR. APK Not Found: " + path);
                    Environment.Exit(0);
                }
                System.Console.WriteLine("Installing app...");
                await client.InstallApk(device.Serial, path);
            }
            string settingsXml = null;
            if (arguments.ContainsKey("settings") && File.Exists(arguments["settings"]))
            {
                settingsXml = File.ReadAllText(arguments["settings"]);
            }

            // Get unlock state
            var dmpData = await client.SendCommandAndReceiveDataAsync("shell: dumpsys power", device.Serial);
            string[] data = Encoding.ASCII.GetString(dmpData).Split('\n');
            var mHoldingWakeLockSuspendBlocker = data.Where(d => d.TrimStart().StartsWith("mHoldingWakeLockSuspendBlocker")).FirstOrDefault()?.EndsWith("=true") == true;
            var mHoldingDisplaySuspendBlocker = data.Where(d => d.TrimStart().StartsWith("mHoldingDisplaySuspendBlocker")).FirstOrDefault()?.EndsWith("=true") == true;
            bool isDisplayOn = false;
            bool isLocked = false;
            if(!mHoldingWakeLockSuspendBlocker && mHoldingDisplaySuspendBlocker)
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
            if (arguments.ContainsKey("apkid"))
                apk_id = arguments["apkid"];
            if (arguments.ContainsKey("activity"))
                activityName = arguments["activity"];

            if (arguments.ContainsKey("logFileName"))
                outputFilename = arguments["logFileName"];
            else
                outputFilename = Path.Combine(System.Environment.CurrentDirectory, "TestRunReport.trx");
            
            LogCatMonitor monitor = new LogCatMonitor(device.Serial);
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
            System.Console.WriteLine($"Launching app {apk_id}/{activityName} on device " + device.Serial + "...");
            settingsXml = settingsXml?.Replace("\n", "").Replace("\r", "").Replace("\"","\\\"");
            await client.SendCommandAsync("forward tcp:38300 tcp:38300", device.Serial);
            string launchCommand = $"am start -n {apk_id}/{activityName} --ez AutoRun true --es ReportFile TestRunReport --es SettingsXml \"{settingsXml}\"";
            await client.SendShellCommandAsync(launchCommand, device.Serial);
        }


        private static int processID = -1;
        private static string trxReportPath;
        
        private static void Monitor_LogReceived(object sender, LogCatMonitor.LogEntry e)
        {
            var msg = e.DataString;
            if (processID < 0)
            {
                if (msg.Contains("Start proc") && msg.Contains($":{apk_id}"))
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
                else
                    return;
            }

            //Log anything from logcat for the process or it the APK ID is in the message
            if (e.ProcessId == processID || msg.Contains(apk_id))
            {
                File.AppendAllText("RunLog.txt", $"{e.TimeStamp}\r{e.Type}\r{e.Tag}\t{e.DataString}{Environment.NewLine}");
            }

            if (e.ProcessId == processID)
            {
                if (e.Tag == "MSTestX")
                {
                    if(msg.StartsWith("TRXREPORT LOCATION: "))
                    {
                        trxReportPath = msg.Substring(20);
                    }
                    else if (msg.StartsWith("STARTING TESTRUN"))
                    {
                        System.Console.WriteLine(msg);
                        System.Console.WriteLine("=============================");
                    }
                    else if (msg.StartsWith("COMPLETED TESTRUN"))
                    {
                        System.Console.WriteLine("=============================");
                        System.Console.WriteLine(msg);
                        System.Console.WriteLine("=============================");
                        System.Console.WriteLine("Saving report to " + outputFilename);
                        FileInfo fi = new FileInfo(outputFilename);
                        if (!fi.Directory.Exists)
                            fi.Directory.Create();
                        client.SendCommandAndReceiveDataAsync(
                              $"exec:run-as {apk_id} cat /data/data/{apk_id}/files/TestRunReport.trx",
                              device.Serial).ContinueWith(t =>
                              {
                                  File.WriteAllBytes(outputFilename, t.Result);
                                  System.Console.WriteLine("Report saved");
                                  if (!Debugger.IsAttached)
                                      Environment.Exit(0);
                              });
                    }
                    else if (msg.StartsWith("TEST STARTING:"))
                    {
                        var testName = msg.Substring(14).Trim();
                        testName = testName.Substring(testName.LastIndexOf(".") + 1);
                        System.Console.Write($"Running {testName}");
                    }
                    else if (msg.StartsWith("TEST COMPLETED:"))
                    {
                        var testName = msg.Substring(15).Trim();
                        var outcomeIdx = testName.LastIndexOf(" - ");
                        string outcome = outcomeIdx < 0 ? "" : testName.Substring(outcomeIdx + 3).Trim();
                        testName = outcomeIdx < 0 ? testName : testName.Substring(0, outcomeIdx);
                        bool isDataTest = testName.Contains(" (");
                        var outcomeMessageIdx = outcome.IndexOf(' ');
                        string testMessage = "";
                        if (outcomeMessageIdx > 0)
                        {
                            testMessage = outcome.Substring(outcomeMessageIdx).Trim();
                            outcome = outcome.Substring(0, outcomeMessageIdx);
                        }
                        if (outcome == "Failed" || outcome == "Error")
                        {
                            System.Console.ForegroundColor = ConsoleColor.Red;
                        }
                        else if (outcome == "Skipped" || outcome == "Inconclusive")
                        {
                            System.Console.ForegroundColor = ConsoleColor.Yellow;
                        }
                        if (!isDataTest)
                            System.Console.SetCursorPosition(0, System.Console.CursorTop);
                        else
                            System.Console.Write("\t");
                        testName = testName.Substring(testName.LastIndexOf(".") + 1);
                        System.Console.WriteLine($"{outcome}\t{testName}\t{testMessage}");
                        System.Console.ResetColor();
                    }
                }
                else if (e.Tag == "AndroidRuntime" && e.Type == LogCatMonitor.LogEntry.LogType.Error)
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
                                System.Console.WriteLine($"{Environment.NewLine}Android application process killed. Exiting...");
                                if (Debugger.IsAttached)
                                    System.Console.ReadKey();
                                Environment.Exit(0);
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
