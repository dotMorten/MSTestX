using MSTestX.Console;
using MSTestX.Console.Adb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Remote
{
    public class RunSettings
    {
        public string? SettingsXml { get; set; }
        public bool WaitForRemote { get; set; } = false;

        public int WaitForRemoteTimeout { get; set; } = 0;

        public string? RemoteIp { get; set; }

        public string? OutputFilename { get; set; }
        public string? IosAppPath { get; set; }
        public string? ApkPath { get; set; }

        public string? DeviceName { get; set; }

        public string? ApkId { get; set; }
        public string? ActivityName { get; set; }
        public string? Pin { get; set; }
    }

    public interface ILog
    {
        public void LogInfo(string message, bool partial = false);
        public void LogError(string message, bool partial = false);
        public void LogWarning(string message, bool partial = false);
        public void LogOk(string message, bool partial = false);
    }
    public class ConsoleLogger : ILog
    {
        public void LogError(string message, bool partial = false)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            LogInfo(message, partial);
            System.Console.ResetColor();
        }

        public void LogInfo(string message, bool partial = false)
        {
            if(partial)
                System.Console.Write(message);
            else
                System.Console.WriteLine(message);
        }

        public void LogOk(string message, bool partial = false)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            LogInfo(message, partial);
            System.Console.ResetColor();
        }

        public void LogWarning(string message, bool partial = false)
        {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            LogInfo(message, partial);
            System.Console.ResetColor();
        }
    }

    public class Remote
    {
        private Device? device;
        private AdbClient? client;
        private LogCatMonitor? monitor;
        private CancellationTokenSource? processExitCancellationTokenSource;
        private TaskCompletionSource<int> testRunCompleted = new TaskCompletionSource<int>();
        private RunSettings args;
        private ILog logger;
        public static async Task<int> RunTests(RunSettings settings, ILog logger)
        {
            var remote = new Remote() { args = settings, logger = logger };
            await remote.RunTests();
            var exitCode = await remote.testRunCompleted.Task;
            return exitCode;
        }
        private async Task RunTests()
        { 
            processExitCancellationTokenSource = new CancellationTokenSource();


            System.Net.IPEndPoint? testAdapterEndpoint = null;
            if (args.WaitForRemote)
            {
                var pingTask = new TaskCompletionSource<bool>();
                var pingListener = new TcpListener(System.Net.IPAddress.Any, 38302);
                pingListener.Start();
                logger.LogInfo("Waiting for remote ping...");
                CancellationTokenSource cts = new CancellationTokenSource();
                if (args.WaitForRemoteTimeout > 0)
                {
                    cts.CancelAfter(args.WaitForRemoteTimeout * 1000);
                }
                else
                    cts.CancelAfter(10 * 60 * 5); // default 5 minute timeout
                try
                {
                    var pingTaskResult = await pingListener.AcceptTcpClientAsync(cts.Token);
                    testAdapterEndpoint = (System.Net.IPEndPoint)pingTaskResult.Client.RemoteEndPoint!;
                    if (testAdapterEndpoint != null)
                    {
                        testAdapterEndpoint.Port = 38300;
                        pingListener.Stop();
                        logger.LogInfo($"Remote device connected from {testAdapterEndpoint}. Starting test run...");
                    }
                    else return;
                }
                catch (OperationCanceledException)
                {
                    cts.Dispose();
                    logger.LogInfo("Timeout waiting for remote device to connect");
                    Environment.Exit(1);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(args.RemoteIp))
            {
                var val = args.RemoteIp;
                if (val is not null && val.Contains(":") && System.Net.IPAddress.TryParse(val.Split(':')[0], out System.Net.IPAddress? ip) && int.TryParse(val.Split(':')[1], out int port))
                {
                    testAdapterEndpoint = new System.Net.IPEndPoint(ip, port);
                }
                else
                {
                    logger.LogError("Invalid remote ip and/or port");
                    testRunCompleted.TrySetResult(1);
                    return;
                }
            }

            if (testAdapterEndpoint != null)
            {
                if (string.IsNullOrEmpty(args.OutputFilename))
                {
                    string defaultFilename = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                    args.OutputFilename = Path.Combine(System.Environment.CurrentDirectory, defaultFilename + ".trx");
                }
                await OnApplicationLaunched(testAdapterEndpoint);
            }
            if (!string.IsNullOrEmpty(args.IosAppPath)) // iOS app
            {
                if (!OperatingSystem.IsMacOS())
                {
                    logger.LogError("iOS apps much be launched from a Mac");
                    testRunCompleted.TrySetResult(1);
                    return;
                }
                var devices = await devicectl.GetConnectedAppleDevicesAsync();
#if DEBUG
                logger.LogInfo("Connected devices: ");
                foreach (var d in devices.Result.Devices)
                {
                    logger.LogInfo($"{d.DeviceProperties.Name} ({d.Identifier}) : {d.HardwareProperties.DeviceType} {d.HardwareProperties.CpuType.Name} {d.DeviceProperties.OsVersionNumber}");
                }
#endif
                if (args.DeviceName is null && devices.Result.Devices.Length == 1)
                {
                    args.DeviceName = devices.Result.Devices[0].Identifier;
                }
                if (string.IsNullOrEmpty(args.DeviceName))
                {
                    if (devices.Result.Devices.Length == 0)
                        logger.LogError("No devices found");
                    else
                    {
                        logger.LogError("Device parameter '-device <uuid>' missing and multiple devices connected:");
                        foreach (var d in devices.Result.Devices)
                        {
                            logger.LogInfo($"    - {d.Identifier} ({d.DeviceProperties.Name} - {d.HardwareProperties.MarketingName}. OS version: {d.DeviceProperties.OsVersionNumber})");
                        }
                    }

                    testRunCompleted.TrySetResult(1);
                    return;
                }
                var details = await devicectl.GetDeviceDetails(args.DeviceName);
                if (!Directory.Exists(args.IosAppPath) && !File.Exists(args.IosAppPath))
                {
                    logger.LogError("File not found: " + args.IosAppPath);
                    testRunCompleted.TrySetResult(1);
                    return;
                }
                logger.LogInfo("Installing app...");
                var bundleId = await devicectl.InstallApp(args.DeviceName, args.IosAppPath);
                logger.LogInfo($"App {bundleId} installed");

                if (string.IsNullOrEmpty(args.OutputFilename))
                {
                    string defaultFilename = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                    args.OutputFilename = Path.Combine(System.Environment.CurrentDirectory, defaultFilename + ".trx");
                }

                // Set up port forwarding using "mobiledevice"
                MobileDevice tunnel;
                try
                {
                    tunnel = await MobileDevice.CreateTunnelAsync(38300, 38300, details.Result.HardwareProperties.Udid);
                }
                catch (System.Exception ex)
                {
                    logger.LogError("Failed to open tunnel: " + ex.Message);
                    testRunCompleted.TrySetResult(1);
                    return;
                }
                tunnel.Exited += (s, e) => { testRunCompleted.TrySetResult(1); logger.LogError("Tunnel process exited."); };

                CancellationTokenSource closeAppToken = new CancellationTokenSource();
                closeAppToken.Token.Register(t => tunnel.Dispose(), null);
                var appTask = devicectl.LaunchApp(args.DeviceName, bundleId, "--TestAdapterPort 38300 --AutoExit True", args.OutputFilename.Replace(".trx", ".log"), closeAppToken.Token);
                await OnApplicationLaunched(System.Net.IPEndPoint.Parse("127.0.0.1:38300"));
                GC.KeepAlive(tunnel);
                closeAppToken.Cancel();
                testRunCompleted.TrySetResult(0);
            }
            else if(!string.IsNullOrEmpty(args.ApkPath) || !string.IsNullOrEmpty(args.ApkId))
            {
                // Launch Android android app
                client = new AdbClient();
                var devices = await client.GetDevicesAsync();
                if (!devices.Any())
                {
                    logger.LogError("No devices connected to ADB");
                    testRunCompleted.TrySetResult(1);
                    return;
                }
                if (!string.IsNullOrEmpty(args.DeviceName))
                {
                    device = devices.Where(d => d.Serial == args.DeviceName).FirstOrDefault();
                    if (device == null)
                    {
                        logger.LogError($"ERROR Device '{args.DeviceName}' not found");
                        testRunCompleted.TrySetResult(1);
                        return;
                    }
                }
                if (device == null)
                {
                    if (devices.Count() > 1)
                    {
                        logger.LogError($"ERROR. Multiple devices connected. Please specify -deviceId <deviceid>");
                        foreach (var d in devices)
                        {
                            logger.LogInfo($"\t{d.Serial}\t{d.Name}\t{d.Model}\t{d.Product}\t{d.State}");
                        }
                        testRunCompleted.TrySetResult(1);
                        return;
                    }
                    device = devices.First();
                }

                await ShutdownApp();

                if (!string.IsNullOrEmpty(args.ApkPath))
                {
                    var path = args.ApkPath;
                    if (!File.Exists(path))
                    {
                        logger.LogError("ERROR. APK Not Found: " + path);
                        testRunCompleted.TrySetResult(1);
                    }
                    if (args.ApkId == null || args.ActivityName == null)
                    {
                        string id;
                        string name;
                        ApkHelper.GetAPKInfo(path, out id, out name);
                        if (args.ApkId == null)
                            args.ApkId = id;
                        if (args.ActivityName == null)
                            args.ActivityName = name;
                    }
                    logger.LogInfo("Installing app...");
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
                    if (!string.IsNullOrEmpty(args.Pin))
                    {
                        logger.LogInfo("Unlocking phone...");
                        await device.UnlockAsync(args.Pin);
                    }
                    else
                    {
                        logger.LogError("Device appears to be locked. Please unlock your device");
                        //    System.Console.ReadKey();
                    }
                }
                if (File.Exists("RunLog.txt"))
                    File.Delete("RunLog.txt");

                if (string.IsNullOrEmpty(args.OutputFilename))
                {
                    string defaultFilename = device.Serial + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");
                    if (args.ApkId != null)
                        defaultFilename += "_" + args.ApkId;
                    args.OutputFilename = Path.Combine(System.Environment.CurrentDirectory, defaultFilename + ".trx");
                }

                monitor = new LogCatMonitor(device.Serial);
                // Connecting to logcat...                                
                await client.SendDeviceCommandAsync("shell:logcat -b all -c", device.Serial); //Configure logcat
                bool result;
                if (device.ApiLevel > 22)
                    result = await monitor.OpenAsync($"{args.ApkId} -T 1");
                else
                    result = await monitor.OpenAsync($"");
                if (result)
                {
                    logger.LogInfo("Connected logcat to " + device.Serial);
                }
                else
                {
                    logger.LogError("Failed to connect to LogCat");
                    return;
                }
                monitor.LogReceived += Monitor_LogReceived;

                await device.SetPortForward(38300, 38300);  //Set up port forwarding for socket communication

                logger.LogInfo($"Launching app {args.ApkId}/{args.ActivityName} on device " + device.Serial + "...");

                await device.LaunchApp(args.ApkId, args.ActivityName, new Dictionary<string, object>() { { "TestAdapterPort", 38300 } });

                // Keep looking for process starting up
                bool launched = false;
                while (!launched)
                {
                    await Task.Delay(1000);
                    var pid = await device.GetProcessId(args.ApkId);
                    if (appLaunchDetected)
                        break;
                    if (pid > 0)
                    {
                        launched = true;
                        logger.LogInfo($"Test Host Launched. Process ID '{pid}'");
                        _ = OnApplicationLaunched();
                        break;
                    }
                    else
                    {
                        //Keep retrying. Doesn't always launch the first time
                        await device.LaunchApp(args.ApkId, args.ActivityName, new Dictionary<string, object>() { { "TestAdapterPort", 38300 } });
                    }
                }

            }
        }

        private async Task ShutdownApp()
        {
            if (device is null) return;
            var id = await device.GetProcessId(args.ApkId);
            if (id > 0)
            {
                logger.LogInfo($"Application '{args.ApkId}' already running. Shutting down...");
                await device.StopApp(args.ApkId); //Ensure app isn't already running
                while (await device.GetProcessId(args.ApkId) > 0)
                {
                    await Task.Delay(100);
                }
            }
        }

        private bool appLaunchDetected;

        private async Task OnApplicationLaunched(System.Net.IPEndPoint? endpoint = null)
        {
            if (appLaunchDetected)
                return;
            appLaunchDetected = true;
            int result = 0;
            var runner = new TestRunner(logger, endpoint);
            try
            {
                await Task.Delay(5000); //Give app some time to start up
                await runner.RunTests(args.OutputFilename, args.SettingsXml, processExitCancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex.Message);
                result = 1;
            }
            finally
            {
                await KillProcess();
            }
            testRunCompleted.TrySetResult(result);
        }

        private async Task KillProcess()
        {
            if (monitor != null)
            {
                monitor.LogReceived -= Monitor_LogReceived;
            }
            if (device != null)
            {
                await device.StopApp(args.ApkId);
            }
            monitor?.Close();
        }

        private static int processID = -1;

        private void Monitor_LogReceived(object? sender, LogCatMonitor.LogEntry e)
        {
            var msg = e.DataString;
            if (msg == null) return;
            //Log anything from logcat for the process or it the APK ID is in the message
            if (processID > 0 && e.ProcessId == processID || args.ApkId != null && msg.Contains(args.ApkId))
            {
                File.AppendAllText("RunLog.txt", $"{e.TimeStamp}\t{e.Type}\t{e.Tag}\t{e.DataString}{Environment.NewLine}");
            }

            if (processID < 0)
            {
                if (e.Tag == "ActivityManager" &&
                        msg.StartsWith("Start proc ") &&
                        msg.Contains($":{args.ApkId}") &&
                        msg.Contains($" for activity {args.ApkId}/{args.ActivityName}")
                        && !msg.Contains(" for backup "))
                {
                    string processIDStr = msg.Substring(11, msg.IndexOf(":") - 11);
                    if (!int.TryParse(processIDStr, out processID))
                        return;
                    else
                    {
                        File.AppendAllText("RunLog.txt", $"{Environment.NewLine}====================================={Environment.NewLine}Application launched with Process ID {processID}{Environment.NewLine}");
                        logger.LogInfo($"Application launched with Process ID {processID}");
                    }
                }
            }

            //Log anything from logcat for the process or it the APK ID is in the message
            if (processID > 0 && e.ProcessId == processID || args.ApkId != null && msg.Contains(args.ApkId))
            {
                File.AppendAllText("RunLog.txt", $"{e.TimeStamp}\r{e.Type}\r{e.Tag}\t{e.DataString}{Environment.NewLine}");
            }
            if (e.Tag == "ActivityManager")
            {

            }
            if (e.Tag == "ActivityManager" && msg.StartsWith($"Displayed {args.ApkId}/{args.ActivityName}"))
            {
                _ = OnApplicationLaunched(); //Detect app launched and start VSTest connection
                return;
            }
            if (processID > 0 && e.Tag == "ActivityManager" && msg == $"Process {args.ApkId} (pid {processID}) has died.")
            {
                // Application died
                logger.LogError($"{Environment.NewLine}Android application process has died. Exiting...");
                processExitCancellationTokenSource?.Cancel();
                //OnTestRunAborted("Android application process has died");
                //testRunCompleted.TrySetResult(1);
                return;
            }
            if (processID > 0 && e.ProcessId == processID)
            {
                if (e.Tag == "AndroidRuntime" && e.Type == LogCatMonitor.LogEntry.LogType.Error)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    logger.LogError(Environment.NewLine + "ERROR: " + e.DataString);
                    System.Console.ResetColor();
                }
            }
            else if (e.Tag == "libprocessgroup")
            {
                if (msg.StartsWith("Successfully killed process") && msg.Contains($" pid {processID}"))
                {
                    //App likely exited. Check if it's still alive
                    var _ = device!.GetProcessId(args.ApkId).ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            if (t.Result == 0)
                            {
                                logger.LogError($"{Environment.NewLine}Android application process killed. Exiting...");
                                processExitCancellationTokenSource?.Cancel();
                            }
                        }
                    });

                }
            }
        }
    }
}
