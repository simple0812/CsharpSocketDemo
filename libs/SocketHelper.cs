using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Shunxi.Infrastructure.Common.Configuration;
using Shunxi.Infrastructure.Common.Log;
using Shunxi.Infrastructure.Common.Utility;

namespace Shunxi.Business.Protocols.Helper
{
    public class SocketHelper :IDisposable
    {
        public string address;
        public int port = 8080;
        public Socket client = null;
        private ConcurrentQueue<byte[]> resultCQ = new ConcurrentQueue<byte[]>();
        private ConcurrentQueue<IBuffer> waitForSend = new ConcurrentQueue<IBuffer>();
        CancellationTokenSource readCancellationTokenSource;
        private static object _locker = new object();
        public event Action<byte[]> ReceiveHandler;

        private bool isConnect => null != client && client.Connected;

        public SocketHelper()
        {
            try
            {
                address = Environment.GetEnvironmentVariable("IOT_DIRECTIVE_IP") ?? Config.IOT_DIRECTIVE_IP;

            }
            catch (Exception)
            {
                //
            }

            var timer = new Timer(new TimerCallback((p) =>
            {
                Init();
            }),null, 0 , 10 * 1000 );
        }


        public void Dispose()
        {
            try
            {
                client?.Shutdown(SocketShutdown.Both);
                client?.Dispose();
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void ClearBuffer()
        {
            if (resultCQ != null) resultCQ = new ConcurrentQueue<byte[]>();
        }

        public void Cancel()
        {
            if (readCancellationTokenSource != null)
            {
                if (!readCancellationTokenSource.IsCancellationRequested)
                {
                    readCancellationTokenSource.Cancel();
                }
            }
        }

        public void Close()
        {
            try
            {
                client?.Shutdown(SocketShutdown.Both);
                client?.Dispose();
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void Init()
        {
            lock (_locker)
            {
                if (isConnect) return;

                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                var ip = IPAddress.Parse(address);
                var endPoint = new IPEndPoint(ip, port);

                var args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;


                args.Completed += (obj, e) =>
                {
                    var receciveArg = new SocketAsyncEventArgs();
                    var sendbuffers = new byte[1024];
                    receciveArg.SetBuffer(sendbuffers, 0, 1024);
                    receciveArg.Completed += Rececive_Completed;
                    client.ReceiveAsync(receciveArg);

                    Task.Run(async () =>
                    {
                        while (true)
                        {
                            IBuffer data;
                            if (waitForSend.TryDequeue(out data))
                            {
                                ProcessSend(data);
                            }

                            await Task.Delay(10);
                        }
                    });

                };

                readCancellationTokenSource = new CancellationTokenSource();

                client.ConnectAsync(args);
            }
            
        }

        public async Task Open()
        {
            if(!isConnect)
                Init();

            await Task.Delay(0);
        }

        public async Task<IBuffer> Receive()
        {
            byte[] data = null;
            do
            {
                await Task.Delay(5);
            }
            while (resultCQ.Count == 0);
            resultCQ.TryDequeue(out data);

            LogFactory.Create().Info($"receive ->{Common.BytesToString(data)}<- receive end");
            return data.AsBuffer();
        }

        private void Rececive_Completed(object sender, SocketAsyncEventArgs e)
        {
            var _client = sender as Socket;
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    byte[] data = new byte[e.BytesTransferred];

                    for (var i = 0; i < e.BytesTransferred; i++)
                        data[i] = e.Buffer[i];
                    //resultCQ.Enqueue(data);
                    LogFactory.Create().Info($"receive ->{Common.BytesToString(data)}<- receive end");
                    OnReceiveHandler(data);
                    Array.Clear(e.Buffer, 0, e.Buffer.Length);
                }
            }

            _client?.ReceiveAsync(e);
        }

        public async Task<int> Send(IBuffer buffer)
        {
            await Task.Delay(0);
            waitForSend.Enqueue(buffer);
            return (int)buffer.Length;
        }

        private void ProcessSend(IBuffer buffer)
        {
            var data = buffer.ToArray();
            if (client == null || client.Connected == false)
            {
                LogFactory.Create().Info("unconnect directive server");
                return;
            }
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.Completed += (obj, e) =>
            {
                LogFactory.Create().Info($"send ->{Common.BytesToString(buffer.ToArray())}<- send end");
                args.Dispose();
            };

            client.SendAsync(args);
        }

        protected virtual void OnReceiveHandler(byte[] obj)
        {
            ReceiveHandler?.Invoke(obj);
        }
    }
}
