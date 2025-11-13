using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    internal static class SocketExtensions
    {

        public static async Task<ushort?> ReadUInt16Async(this Socket s,CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] data = await ReadAsync(s, 2, cancellationToken).ConfigureAwait(false);
            if (data == null)
                return null;
            return BitConverter.ToUInt16(data, 0);
        }
        public static async Task<int?> ReadInt32Async(this Socket s,CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] data = await ReadAsync(s, 4, cancellationToken).ConfigureAwait(false);
            if (data == null)
                return null;
            return BitConverter.ToInt32(data, 0);
        }

        public static async Task<string> ReadString(this Socket s)
        {
            byte[] buffer = new byte[4];
            int count = await s.ReceiveAsync(buffer, 0, 4, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);

            if (count == 0)
            {
                // There is no data to read
                return null;
            }

            // Convert the bytes to a hex string
            string lenHex = AdbClient.Encoding.GetString(buffer);
            int len = int.Parse(lenHex, NumberStyles.HexNumber);
            if (len == 0) return string.Empty;
            // And get the string
            buffer = new byte[len];
            count = await s.ReceiveAsync(buffer, 0, len, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
            return AdbClient.Encoding.GetString(buffer);
        }


        public static async Task<byte[]> ReadAsync(this Socket s, int count, CancellationToken cancellationToken = default(CancellationToken))
        {
            int totalRead = 0;
            int read = 0;

            byte[] data = new byte[count];

            while ((read = await ReceiveAsync(s, data, totalRead, count - totalRead, System.Net.Sockets.SocketFlags.None, cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalRead += read;
            }

            if (totalRead < count)
            {
                return null;
            }

            return data;
        }
        public static Task<int> ReceiveAsync(this Socket socket, byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var cancellationTokenRegistration = cancellationToken.Register(() => socket.Dispose());
            var tcs = new TaskCompletionSource<int>(socket);

            socket.BeginReceive(
                buffer,
                offset,
                size,
                socketFlags,
                iar =>
                {
                    var t = (TaskCompletionSource<int>)iar.AsyncState;
                    var s = (Socket)t.Task.AsyncState;
                    try
                    {
                        t.TrySetResult(s.EndReceive(iar));
                    }
                    catch (Exception ex)
                    {
                        // Did the cancellationToken's request for cancellation cause the socket to be closed
                        // and an ObjectDisposedException to be thrown? If so, indicate the caller that we were
                        // cancelled. If not, bubble up the original exception.
                        if (ex is ObjectDisposedException && cancellationToken.IsCancellationRequested)
                        {
                            t.TrySetCanceled();
                        }
                        else
                        {
                            t.TrySetException(ex);
                        }
                    }
                    finally
                    {
                        cancellationTokenRegistration.Dispose();
                    }
                },
                tcs);

            return tcs.Task;
        }
    }
}
