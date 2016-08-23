using System;
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
using Windows.Networking.Connectivity;
using Windows.Storage.Streams;
using Shunxi.Infrastructure.Common.Configuration;

namespace Shunxi.Infrastructure.Common.Log
{
    public  class NetLog : ILog
    {
        private string address;
        private int port = 3000;
        private Socket client = null;
        private bool isConnect => null != client && client.Connected;
        private bool isNew = true;
        private ConcurrentQueue<string> waitForSendMsg = new ConcurrentQueue<string>();
        private static object _locker = new object();
        private string localIp;

        public NetLog()
        {
            localIp = Utility.Common.GetLocalIp();
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                address = Environment.GetEnvironmentVariable("IOT_LOG_IP") ?? Config.IOT_LOG_IP;
                Task.Run(async () =>
                {
                    await Send();
                });
            }
            catch (Exception)
            {
                //
            }
        }

        public void Info(string msg)
        {
            Open();
            System.Diagnostics.Debug.WriteLine(msg);

            //防止log服务端没开导致的内存泄漏
            if (waitForSendMsg.Count < 1000)
                waitForSendMsg.Enqueue($"[{localIp}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} {LogLevel.Info}] {msg}");
            
        }

        public void Warnning(string msg)
        {
            Open();
            System.Diagnostics.Debug.WriteLine(msg);

            //防止log服务端没开导致的内存泄漏
            if (waitForSendMsg.Count < 1000)
                waitForSendMsg.Enqueue($"[{localIp}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} {LogLevel.Warnning}] {msg}");
        }

        public void Error(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);

            //防止log服务端没开导致的内存泄漏
            if (waitForSendMsg.Count < 1000)
                waitForSendMsg.Enqueue($"[{localIp}|{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} {LogLevel.Error}] {msg}");
        }

        public void Dispose()
        {
            client?.Shutdown(SocketShutdown.Both);
            client?.Dispose();
        }

        public void Close()
        {
            client?.Shutdown(SocketShutdown.Both);
            client?.Dispose();
        }

        public void Open()
        {
            lock (_locker)
            {
                try
                {
                    if (isConnect) return;

                    var ip = IPAddress.Parse(address);
                    var endPoint = new IPEndPoint(ip, port);
                    var args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = endPoint;

                    if (!isNew)
                    {
                        isNew = true;
                        client.Dispose();
                        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    }
                    
                    args.Completed += (obj, e) =>
                    {
                        isNew = false;
                        
                    };

                    client.ConnectAsync(args);
                }
                catch (Exception)
                {
                    //
                }
                
            }
        }

        public async Task Send()
        {
            try
            {
                while (true)
                {
                    if (!isConnect)
                    {
                        await Task.Delay(10);
                        continue;
                    }
                    var msg = "";
                    if (!waitForSendMsg.TryDequeue(out msg))
                    {
                        continue;
                    }

                    var data = Encoding.UTF8.GetBytes(msg);

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(data, 0, data.Length);
                    args.Completed += (sender, eventArgs) =>
                    {
                        args.Dispose();
                    };

                    client.SendAsync(args);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("send log" + ex.Message);
            }
        }
    }
}
