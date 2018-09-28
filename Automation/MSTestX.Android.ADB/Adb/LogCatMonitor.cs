using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSTestX.Console.Adb
{
    internal class LogCatMonitor : IDisposable
    {
        private Socket s;
        private string deviceId;
        private int port;
        private CancellationTokenSource cancellationSource;
        private LogCatStream lcs;

        public LogCatMonitor(string deviceId, int port = 5037)
        {
            this.deviceId = deviceId;
            this.port = port;
        }

        public async Task<bool> OpenAsync(string parameters)
        {
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port));
            Send($"host:transport:" + deviceId);
            byte[] buffer = new byte[4];
            int count = await s.ReceiveAsync(buffer, 0, 4, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
            if (!IsOkayResponse(buffer, 0, count))
            {
                Close();
                return false;
            }

            Send($"shell:logcat " + parameters + " -T 1 -B");
            count = await s.ReceiveAsync(buffer, 0, 4, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
            cancellationSource = new CancellationTokenSource();
            if (!IsOkayResponse(buffer, 0, count))
            {
                Close();
                return false;
            }
            lcs = new LogCatStream();
            var _ = Task.Run(() => ProcessInputs(cancellationSource.Token));
            _ = Task.Run(() => ProcessBuffer(cancellationSource.Token));
            return true;
        }

        private bool IsOkayResponse(byte[] buffer, int offset, int count)
        {
            //Checks if the response is the ASCII version of "OKAY"
            return count == 4 && buffer[offset] == 79 && buffer[offset + 1] == 75 && buffer[offset + 2] == 65 && buffer[offset + 3] == 89;
        }
        private async void ProcessBuffer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var entry = lcs.ReadLogEntry();
                LogReceived?.Invoke(this, entry);
            }
        }

      
        private async void ProcessInputs(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            int count = 0;
            while (!token.IsCancellationRequested)
            {
                count = await s.ReceiveAsync(buffer, 0, 4096, System.Net.Sockets.SocketFlags.None, CancellationToken.None).ConfigureAwait(true);
                if (count > 0)
                {
                    lcs.EnqueueData(buffer, count);
                }
            }
        }

        private class LogCatStream : System.IO.Stream
        {
            private object l = new object();
            AutoResetEvent autoEvent = new AutoResetEvent(false);

            public LogCatStream()
            {
                IsOpen = true;
            }
            private Queue<byte> data = new Queue<byte>(4096);
            internal void EnqueueData(byte[] bytes, int count)
            {
                lock (l)
                {
                    for (int i = 0; i < count; i++)
                        data.Enqueue(bytes[i]);
                }
                autoEvent.Set();
            }
            public LogEntry ReadLogEntry()

            {
                LogEntry value = new LogEntry();
                var payloadLength = this.ReadUInt16();
                var headerSize = this.ReadUInt16();
                var pid = this.ReadInt32();
                var tid = this.ReadInt32();
                var sec = this.ReadInt32();
                var nsec = this.ReadInt32();
                uint id = 0;
                uint uid = 0;
                if (headerSize != 0)
                {
                    if (headerSize >= 0x18)
                    {
                        id = ReadUInt32Async();
                    }                                       
                    if (headerSize >= 0x1c)
                    {
                        uid = ReadUInt32Async();
                    }

                    if (headerSize >= 0x20)
                    {
                        ReadUInt32Async(); //Skip 4 bytes
                    }

                    if (headerSize > 0x20)
                    {
                        //throw new AdbException($"An error occurred while reading data from the ADB stream. Although the header size was expected to be 0x18, a header size of 0x{headerSize:X} was sent by the device");
                    }
                }

                byte[] data = this.GetBytes(payloadLength);

                if (data == null)
                {
                    return null;
                }
                DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(sec * 1000L + nsec / 1000000d)).ToLocalTime();
                string tag = null;
                byte type = 0;
                if (id != 2) //Not an event message
                {
                    int tagEnd = 1;
                    type = data[0];
                    while (tagEnd < data.Length && data[tagEnd] != '\0')
                    {
                        tagEnd++;
                    }
                   
                    tag = Encoding.ASCII.GetString(data, 1, tagEnd - 1);
                    data = data.AsSpan(tagEnd + 1, data.Length - tagEnd - 2).ToArray();
                }

                return new LogEntry()
                {
                    Data = data,
                    ProcessId = pid,
                    ThreadId = tid,
                    TimeStamp = timestamp,
                    Id = id,
                    Tag = tag,
                    Type = (LogEntry.LogType)type
                };
            }

            public bool IsOpen { get; private set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int i = 0;
                lock (l)
                {
                    for (; i < count && data.Count > 0; i++)
                        buffer[i + offset] = data.Dequeue();
                }
                return i;
            }

            public ushort ReadUInt16() => BitConverter.ToUInt16(GetBytes(2));

            public uint ReadUInt32Async() => BitConverter.ToUInt32(GetBytes(4));

            public int ReadInt32() => BitConverter.ToInt32(GetBytes(4));

            public byte[] GetBytes(int count)
            {
                int length = 0;
                lock (l) length = data.Count;
                while (length < count)
                {
                    lock (l) length = data.Count;
                    autoEvent.WaitOne();
                    if (!IsOpen)
                        throw new TaskCanceledException();
                }
                byte[] buffer = new byte[count];
                lock (l)
                    for (int i = 0; i < count; i++)
                    {
                        buffer[i] = data.Dequeue();
                    }
                return buffer;
            }
            public override bool CanSeek => false;
            public override bool CanRead => true;
            public override long Position { get => 0; set => throw new NotSupportedException(); }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override bool CanWrite => false;
            public override void Flush() { }
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Length { get { lock (l) { return data.Count; } } }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        }

        public class LogEntry
        {
            public int ProcessId { get; internal set; }
            public int ThreadId { get; internal set; }
            public DateTimeOffset TimeStamp { get; internal set; }
            public uint Id { get; internal set; }
            public byte[] Data { get; internal set; }
            public string DataString => Data != null ? Encoding.ASCII.GetString(Data) : null;
            public LogType Type { get; internal set; }
            public string Tag { get; internal set; }
            public enum LogType
            {
                Unknown, Verbose = 2, Debug = 3, Info = 4, Warn = 5, Error = 6, Assert = 7
            }
        }
        public event EventHandler<LogEntry> LogReceived;

        public void Close()
        {
            if (s == null) return;
                cancellationSource?.Cancel();
            cancellationSource = null;
            s.Dispose();
            s = null;
        }

        public bool IsConnected => s?.Connected ?? false;

        private void Send(string data)
        {
            string resultStr = string.Format("{0}{1}\n", data.Length.ToString("X4"), data);

            var buffer = Encoding.UTF8.GetBytes(resultStr);
            s.Send(buffer, 0, buffer.Length, System.Net.Sockets.SocketFlags.None);
        }
        
        public void Dispose()
        {
            s?.Dispose();
            s = null;
        }
    }
}
