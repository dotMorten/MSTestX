/// A lot of this class is based on implementation from SharpAdbClient (https://github.com/quamotion/madb)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    // Good command-reference overview here: https://android.googlesource.com/platform/packages/modules/adb/+/HEAD/SERVICES.TXT
    // ADB Source and overview: https://android.googlesource.com/platform/packages/modules/adb/
    internal class AdbClient
    {
        private int port;

        public AdbClient(int port=5037)
        {
            this.port = port;
        }

        public async Task<List<Device>> GetDevicesAsync()
        {
            var devicesData = await SendCommandGetStringAsync("host:devices-l").ConfigureAwait(false);
            string[] strDevices = devicesData?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            List<Device> devices = new List<Device>();
            foreach (var item in strDevices)
            {
                var d = Device.FromDataString(item, this);
                if (d != null)
                {
                    await d.Initialize();
                    devices.Add(d);
                }
            }
            return devices;
        }

        internal static Encoding Encoding { get; } = Encoding.GetEncoding("ISO-8859-1");

        public Task SendShellCommandAsync(string command, string deviceId)
        {
            return SendDeviceCommandAsync("shell:" + command, deviceId);
        }

        public async Task<string> SendDeviceCommandGetStringAsync(string command, string deviceId)
        {
            var data = await SendDeviceCommandGetDataAsync(command, deviceId).ConfigureAwait(false);
            string lenHex = Encoding.UTF8.GetString(data, 0, 4);
            int len = int.Parse(lenHex, NumberStyles.HexNumber);
            string result = string.Empty;
            if (len > 0)
            {
                result = Encoding.UTF8.GetString(data, 4, len);
            }
            System.Diagnostics.Debug.WriteLine("Received: " + result);
            return result;
        }

        public async Task<string> SendCommandGetStringAsync(string command)
        {
            var data = await SendCommandGetDataAsync(command).ConfigureAwait(false);
            string lenHex = Encoding.UTF8.GetString(data, 0, 4);
            int len = int.Parse(lenHex, NumberStyles.HexNumber);
            string result = string.Empty;
            if (len > 0)
            {
                result = Encoding.UTF8.GetString(data, 4, len);
            }
            System.Diagnostics.Debug.WriteLine("Received: " + result);
            return result;
        }

        public async Task<byte[]> SendDeviceCommandGetDataAsync(string command, string deviceId)
        {
            using (var s = await OpenDeviceConnectionAsync(command, deviceId))
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    int count = -1;
                    byte[] buffer = new byte[4096];
                    while (count != 0)
                    {
                        count = await s.ReceiveAsync(buffer, 0, 4096, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
                        ms.Write(buffer, 0, count);
                    }
                    return ms.ToArray();
                }
            }
        }

        public async Task<byte[]> SendCommandGetDataAsync(string command)
        {
            using (var s = await OpenConnectionAsync(command))
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    int count = -1;
                    byte[] buffer = new byte[4096];
                    while (count != 0)
                    {
                        count = await s.ReceiveAsync(buffer, 0, 4096, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
                        ms.Write(buffer, 0, count);
                    }
                    return ms.ToArray();
                }
            }
        }

        public async Task<Socket> OpenDeviceConnectionAsync(string command, string deviceId)
        {
            var s = await OpenDeviceConnectionAsync(deviceId);
            await WriteCommandToSocket(s, command);
            return s;
        }

        private Task<Socket> OpenDeviceConnectionAsync(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentNullException(nameof(deviceId));
            return OpenConnectionAsync($"host:transport:" + deviceId);
        }

        public async Task<Socket> OpenConnectionAsync(string command)
        {
            var s = CreateConnection();
            await WriteCommandToSocket(s, command);
            return s;
        }

        public async Task SendDeviceCommandAsync(string command, string deviceId)
        {
            var s = await OpenDeviceConnectionAsync(deviceId);
            await WriteCommandToSocket(s, command);
            //Disposing too fast causes the command to not execute
            var _ = s.ReceiveAsync(new byte[1], 0, 1, SocketFlags.None, default).ContinueWith(t => s.Dispose());
        }

        public async Task SendCommandAsync(string command)
        {
            using (var s = CreateConnection())
            {
                await WriteCommandToSocket(s, command);
            }
        }

        private Socket CreateConnection()
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            return s;
        }

        private static async Task WriteCommandToSocket(Socket s, string command)
        {
            System.Diagnostics.Debug.WriteLine($"Sending command {command}");
            string resultStr = string.Format("{0}{1}", command.Length.ToString("X4"), command);
            var buffer = Encoding.UTF8.GetBytes(resultStr);
            s.Send(buffer, 0, buffer.Length, SocketFlags.None);
            buffer = Encoding.UTF8.GetBytes(resultStr);
            var count = await s.ReceiveAsync(buffer, 0, 4, SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
            var response = Encoding.UTF8.GetString(buffer, 0, count);
            if (response != "OKAY")
            {
                var error = await s.ReadString().ConfigureAwait(false);
                s.Dispose();
                System.Diagnostics.Debug.WriteLine($"Received {response} {error}");
                throw new Exception(error);
            }
            System.Diagnostics.Debug.WriteLine($"OKAY");
        }
    }
}
