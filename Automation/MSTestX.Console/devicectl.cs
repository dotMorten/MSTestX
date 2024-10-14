using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    internal static class devicectl
    {
        public static async Task<bool> IsDeviceCtlInstalled()
        {
            try
            {
                var version = await DeviceCtl("--version", CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Task<DeviceCtl.DeviceDetails> GetDeviceDetails(string deviceId)
        {
            return DeviceCtl<DeviceCtl.DeviceDetails>($"device info details --device \"{deviceId}\"", CancellationToken.None);
        }

        public static Task<DeviceCtl.Devices> GetConnectedAppleDevicesAsync()
        {
            return DeviceCtl<DeviceCtl.Devices>("list devices --timeout 5", CancellationToken.None);
        }

        public static async Task<string> InstallApp(string deviceId, string appPath)
        {
            var output = await DeviceCtl($"device install app --device {deviceId} {appPath}", CancellationToken.None);
            var idLine = output.Split(Environment.NewLine).Where(s => s.Contains("bundleID: ")).First();
            var bundleId = idLine.Substring(idLine.IndexOf("bundleID: " + 10)).Trim();
            return bundleId;
        }

        public static Task LaunchApp(string deviceId, string appId, string? arguments = null, string? stdOutputFile = null)
        {
            return DeviceCtl($"list device process launch --device {deviceId} --terminate-existing --console {appId} {arguments}", CancellationToken.None);
        }

        private static async Task<T> DeviceCtl<T>(string arguments, CancellationToken cancellationToken)
        {
            var tmpPath = System.IO.Path.GetTempFileName();
            await DeviceCtl($"{arguments} --json-output \"{tmpPath}\"", cancellationToken);
            var json = System.IO.File.ReadAllText(tmpPath);
            File.Delete(tmpPath);
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }

        private static Task<string> DeviceCtl(string arguments, CancellationToken cancellationToken, string? stdOutputFile = null)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
            Process xcrun = new Process();
            if (cancellationToken.CanBeCanceled)
                cancellationToken.Register(() => { tcs.TrySetCanceled(); xcrun.Close(); });
            xcrun.StartInfo = new ProcessStartInfo("xcrun", "devicectl " + arguments);
            StringBuilder sb = new StringBuilder();
            xcrun.Exited += (s, e) =>
            {
                tcs.TrySetResult(sb.ToString());
            };
            xcrun.ErrorDataReceived += (s, e) =>
            {
                if (stdOutputFile != null)
                    File.AppendAllText(stdOutputFile, e.Data + Environment.NewLine);
                if (e.Data.Contains("Locked"))
                    tcs.TrySetException(new InvalidOperationException("Device is locked, unlock and try again."));
                else
                    tcs.TrySetException(new Exception(e.Data));
            };
            xcrun.OutputDataReceived += (s,e) =>
            {
                Debug.WriteLine(e.Data);
                sb.AppendLine(e.Data);
                if (stdOutputFile != null)
                    File.AppendAllText(stdOutputFile, e.Data + Environment.NewLine);
            };
            return tcs.Task;
        }

    }
}
