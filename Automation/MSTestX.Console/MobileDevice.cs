﻿#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console
{
    /// <summary>
    /// Helper for calling into https://github.com/imkira/mobiledevice
    /// </summary>
    internal sealed class MobileDevice : IDisposable
    {
        private Process mobileDeviceProcess;
        private TaskCompletionSource? tunnelCompletion;
        private MobileDevice()
        {
            if (!OperatingSystem.IsMacOS())
                throw new PlatformNotSupportedException();
            var path = Path.Combine(new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory!.FullName, "mobiledevice");
            Process.Start(new ProcessStartInfo("chmod", "+x " + path));
            mobileDeviceProcess = new Process() { StartInfo = new ProcessStartInfo(path) { RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true, UseShellExecute = false } };
            mobileDeviceProcess.EnableRaisingEvents = true;
            mobileDeviceProcess.OutputDataReceived += MobileDeviceProcess_OutputDataReceived;
            mobileDeviceProcess.Exited += MobileDeviceProcess_Exited;
        }
        string? lastMessage = null;
        private void MobileDeviceProcess_OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;
            lastMessage = e.Data;
            System.Console.WriteLine("mobiledevice: " + e.Data);
            if (e.Data.Contains("Tunneling from local port") && tunnelCompletion is not null)
            {
                tunnelCompletion.TrySetResult();
            }
            if (e.Data.Contains("Failed to set up port forwarding") && tunnelCompletion is not null)
            {
                tunnelCompletion.TrySetException(new Exception(e.Data));
            }
        }

        private void MobileDeviceProcess_Exited(object? sender, EventArgs e)
        {
            mobileDeviceProcess = null!;
            Task.Delay(1).ContinueWith(t =>
            {
                tunnelCompletion?.TrySetException(new Exception("MobileDevice exited unexpectedly with code " + mobileDeviceProcess.ExitCode + (string.IsNullOrEmpty(lastMessage) ? "" : ". " + lastMessage)));
            });
            Exited?.Invoke(this, mobileDeviceProcess.ExitCode);
        }

        private async Task StartTunnelAsync(int fromPort, int toPort, string uuid)
        {
            Process.Start("pkill", "mobiledevice"); // Ensure mobiledevice isn't already running
            await Task.Delay(100);
            tunnelCompletion = new TaskCompletionSource();
            mobileDeviceProcess.StartInfo.Arguments = $"tunnel -u {uuid} {fromPort} {toPort}";
            CancellationTokenSource tcs = new CancellationTokenSource();
            tcs.CancelAfter(5000);
            tcs.Token.Register(() => tunnelCompletion.TrySetException(new TimeoutException()));
            mobileDeviceProcess.Start();
            mobileDeviceProcess.BeginOutputReadLine();
            await tunnelCompletion.Task;
        }

        public static async Task<MobileDevice> CreateTunnelAsync(int fromPort, int toPort, string uuid)
        {
            var mobileDevice = new MobileDevice();
            await mobileDevice.StartTunnelAsync(fromPort, toPort, uuid);
            return mobileDevice;
        }

        public void Dispose()
        {
            if (mobileDeviceProcess != null && !mobileDeviceProcess.HasExited)
            {
                mobileDeviceProcess.Kill(true);
            }
            mobileDeviceProcess?.Dispose();
            mobileDeviceProcess = null!;
        }

        ~MobileDevice()
        {
            Dispose();
        }

        public event EventHandler<int>? Exited;
    }
}