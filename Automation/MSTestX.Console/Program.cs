#nullable enable
using MSTestX.Console.Adb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    class Program 
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "-?" || args[0] == "?")
            {
                PrintUsage();
                return;
            }
            var exitCode = await RunTest(ParseArguments(args));
            Environment.Exit(exitCode);
        }

        private static void PrintUsage()
        {
            System.Console.WriteLine(@"Test Runner commandline arguments:
    -remoteIp <ip:port>                 The IP address of a device already running the unit test app
    -waitForRemote                      If IP of device isn't known, use waitForRemote to wait for app to ping back on port 38302.
    -logFileName <path>                 TRX Output Filename
    -settings <path>                    MSTest RunSettings xml file

Android specific (ignored if using remoteIp):
    -deviceid <Device Serial Number>    If more than one device is connected, specifies which device to use
    -apkpath <file path>                Path to an APK to install.
    -apkid <id>                         Package ID of the test app (if not provided, auto-discovered from manifest)
    -activity <activity id>             Activity to launch (if not provided, auto-discovered from manifest)
    -pin <pin code>                     Pin to use to unlock your phone (or empty to just unlock phone with no pin)

iOs specific (MacOS only):
    -apppath <file path>                Path to app to install and launch
    -device <uuid|ecid|serial_number|udid|name|dns_name> The identifier, ECID, serial number, UDID, user-provided name, or DNS name of the device.
");
        }

        static async Task<int> RunTest(Dictionary<string, string?> arguments)
        {
            var args = new Remote.RunSettings();
            if (arguments.ContainsKey("settings") && File.Exists(arguments["settings"]))
            {
                args.SettingsXml = File.ReadAllText(arguments["settings"]!);
            }

            if (arguments.ContainsKey("waitForRemote"))
            {
                args.WaitForRemote = true;
                if (arguments.ContainsKey("waitForRemoteTimeout"))
                {
                    if (int.TryParse(arguments["waitForRemoteTimeout"], out int timeoutSeconds))
                    {
                        args.WaitForRemoteTimeout = timeoutSeconds;
                    }
                    else
                    {
                        System.Console.WriteLine($"Invalid timeout value '{arguments["waitForRemoteTimeout"]}'.");
                        return 1;
                    }
                }
            }

            if (arguments.ContainsKey("remoteIp"))
                args.RemoteIp = arguments["remoteIp"];
            if (arguments.ContainsKey("logFileName"))
                args.OutputFilename = arguments["logFileName"];

            if (arguments.ContainsKey("apppath")) // iOS app
                args.IosAppPath = arguments["apppath"];
            args.DeviceName = arguments.ContainsKey("device") ? arguments["device"] : null;
            if (arguments.ContainsKey("deviceid"))
                args.DeviceName = arguments["deviceid"];
            if (arguments.ContainsKey("apkid"))
                args.ApkId = arguments["apkid"];
            if (arguments.ContainsKey("activity"))
                args.ActivityName = arguments["activity"];

            if (arguments.ContainsKey("apkpath"))
                args.ApkPath = arguments["apkpath"];
            if (arguments.ContainsKey("pin"))
                args.Pin = arguments["pin"];
            return await Remote.Remote.RunTests(args, new Remote.ConsoleLogger());
        }

        private static Dictionary<string, string?> ParseArguments(string[] args)
        {
            var result = new Dictionary<string, string?>();
            for (int i = 0; i < args.Length; i++)
            {
                string? key = null;
                string? value = null;
                if (args[i].StartsWith("-"))
                {
                    key = args[i].Substring(1);
                    if (i < args.Length - 1 && !args[i + 1].StartsWith("-"))
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
