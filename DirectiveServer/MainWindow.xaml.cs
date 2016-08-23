using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DirectiveServer.libs.Enums;
using DirectiveServer.libs.Helper;

namespace DirectiveServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket server = null;
        private string address ;
        private int port = 8080;
        private bool isStart = false;
        private byte[] data = new byte[256];
        private double MULTI_PERCENT = 0.1;
        private double BAD_PERCENT = 0.0;

        private ConcurrentDictionary<int, bool> isRunning = new ConcurrentDictionary<int, bool>();
        private ConcurrentDictionary<int, bool> isPausing = new ConcurrentDictionary<int, bool>();
        private Dictionary<int, Timer> runTimers = new Dictionary<int, Timer>();
        private Dictionary<int, int> runSpeed = new Dictionary<int, int>();
        private ConcurrentQueue<Tuple<Socket, byte[]>> directiveQueue = new ConcurrentQueue<Tuple<Socket, byte[]>>();

        public MainWindow()
        {
            try
            {
                address = Environment.GetEnvironmentVariable("IOT_DIRECTIVE_IP") ?? "192.168.1.100";
            }
            catch (Exception)
            {
                //
            }

            isRunning.TryAdd(1, false);
            isRunning.TryAdd(2, false);
            isRunning.TryAdd(3, false);
            isRunning.TryAdd(4, false);
            isRunning.TryAdd(0x80, false);

            isPausing.TryAdd(1, false);
            isPausing.TryAdd(2, false);
            isPausing.TryAdd(3, false);
            isPausing.TryAdd(4, false);
            isPausing.TryAdd(0x80, false);

            runSpeed.Add(1, 0);
            runSpeed.Add(2, 0);
            runSpeed.Add(3, 0);
            runSpeed.Add(4, 0);

            InitializeComponent();
            txtPort.Text = port.ToString();
            this.Closed += MainWindow_Closed;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPort.Text))
            {
                MessageBox.Show("端口不能为空");
                return;
            }
            StartUp();

            Task.Run( async () =>
            {
                while (true)
                {
                    Tuple<Socket, byte[]> tuple;
                    if (directiveQueue.TryDequeue(out tuple) && tuple != null)
                    {
//                                                Send(tuple.Item1, tuple.Item2);
                                                resolveSendData(tuple.Item1, tuple.Item2.ToList());
                    }

                    await Task.Delay(10);
                }
            });

            Dispatcher.Invoke(() => {
                RenderMsg("服务已开启...", Brushes.Gray);
            });

            //开启新线程 避免阻塞主线程
            new Thread(() =>
            {
                while (true)
                {
                    Socket socket = server?.Accept();
                    Dispatcher.Invoke(() =>
                    {
                        RenderMsg("客户端连接成功........", new SolidColorBrush(Colors.Red));
                    });

                    //接受一个新的客户端 开启新线程
                    new Thread(() =>
                    {
                        var bytes = new List<byte>();
                        //持续接受客户端信息
                        while (true)
                        {
                            try
                            {
                                if (!CheckConnect(socket))
                                {
                                    socket?.Shutdown(SocketShutdown.Both);
                                    socket?.Close();
                                    socket?.Dispose();
                                    Dispatcher.Invoke(() =>
                                    {
                                        RenderMsg("CheckConnect is closed", new SolidColorBrush(Colors.Red));
                                    });
                                    throw new Exception("CheckConnect is closed");
                                };

                                var length = socket?.Receive(data) ?? 0;

                                for (var i = 0; i < length; i++)
                                {
                                    bytes.Add(data[i]);
                                }

                                //客户端关闭后会发送空数组
                                if (bytes.All(p => p == 0x00 || p == 0xff))
                                {
                                    socket?.Shutdown(SocketShutdown.Both);
                                    socket?.Close();
                                    socket?.Dispose();
                                    Dispatcher.Invoke(() =>
                                    {
                                        RenderMsg("CheckConnect is empty", new SolidColorBrush(Colors.Red));
                                    });

                                    throw new Exception("CheckConnect is empty");
                                }

                                //判断是否还有数据流需要接受
                                if (socket?.Available == 0)
                                {
                                    directiveQueue.Enqueue(new Tuple<Socket, byte[]>(socket, bytes.ToArray()));

                                    bytes.Clear();

                                }
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    socket?.Shutdown(SocketShutdown.Both);
                                    socket?.Close();
                                    socket?.Dispose();
                                }
                                catch (Exception)
                                {
                                    //ignore
                                }

                                Dispatcher.Invoke(() =>
                                {
                                    RenderMsg("Exception->" + ex.Message, new SolidColorBrush(Colors.Red));
                                });

                                break;
                            }
                        }

                    }).Start();
                }
            }).Start();
        }

        private bool CheckConnect(Socket s)
        {
            if (s == null || !s.Connected)
            {
                return false;
            }
            if (s.Poll(-1, SelectMode.SelectWrite) || s.Poll(-1, SelectMode.SelectRead))
            {
                return true;
            }
            else if (s.Poll(-1, SelectMode.SelectError))
            {
                return false;
            }
            return true;
        }

        private void Send(Socket socket, byte[] bytes)
        {
            try
            {
                if (!ValidateDirective(bytes)) return;
                var ret = processResolve(bytes);
        
                if (new Random().NextDouble() < MULTI_PERCENT)
                {
                    var pre = ret.Take(2).ToArray();
                    var post = ret.Skip(2).Take(ret.Length - 2).ToArray();
                    socket?.Send(pre);
                    socket?.Send(post);
                }
                else
                {
                    socket?.Send(ret);
                }
                AddMsg(bytes);
            }
            catch (Exception)
            {
                //
            }
        }

        private void resolveSendData(Socket socket, List<byte> dirtyData)
        {
            while (true)
            {
                if (dirtyData.Count <= 2) break;

                var len = ((DirectiveTypeEnum)dirtyData[1]).GetDirectiveLength();
                if (len == 0)
                {
                    dirtyData.RemoveAt(0);
                }
                else if (dirtyData.Count >= len)
                {
                    var directive = dirtyData.GetRange(0, len).ToArray();

                    if (ValidateDirective(directive))
                    {
                        Send(socket, directive.ToArray());
                        dirtyData.RemoveRange(0, len);
                    }
                    else
                    {
                        dirtyData.RemoveAt(0);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private bool ValidateDirective(byte[] bytes)
        {
            if (bytes.Length <= 4) return false;

            var content = bytes.Take(bytes.Length - 2).ToArray();
            var checkCode = bytes.Skip(bytes.Length - 2).Take(2).ToArray();
            var p = DirectiveHelper.GenerateCheckCode(content);

            return p[0] == checkCode[0] && p[1] == checkCode[1];
        }

        private byte[] processResolve(byte[] bytes)
        {
            byte[] xdata = null;
            switch (bytes[1])
            {
                case 0x00:
                    {
                        var rate = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(2).Take(2).ToArray());
                        var volume = DirectiveHelper.Parse2BytesToNumber(bytes.Skip(4).Take(2).ToArray());
                        xdata = resolveTryStartDirective(bytes);

                        var interval = (int) rate == 0 ? 1000 : (int) ((volume/rate)*60*1000);
                        runSpeed[bytes[0]] = (int)rate;

                        Timer timer;
                        if (runTimers.ContainsKey(bytes[0]))
                        {
                            timer = runTimers[bytes[0]];
                            timer.Dispose();
                            runTimers.Remove(bytes[0]);
                        }


                        isRunning[bytes[0]] = true;

                        timer = new Timer(new TimerCallback((p) =>
                        {
                            isRunning[bytes[0]] = false;
                        }), null, interval, 0);

                        runTimers.Add(bytes[0], timer);
                    }
                    break;

                case 0x01:
                    {
                        xdata = bytes;
                        isPausing[bytes[0]] = true;
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            isPausing[bytes[0]] = false;
                            isRunning[bytes[0]] = false;
                            if (runTimers.ContainsKey(bytes[0]))
                            {
                                var timer = runTimers[bytes[0]];
                                timer?.Dispose();
                                runTimers.Remove(bytes[0]);
                            }
                        });
                    }
                    break;

                case 0x02:
                    xdata = bytes;
                    break;

                case 0x03:
                    {
                        xdata = resolveIdleDirective(bytes);
                    }
                    break;

                case 0x04:
                    {
                        xdata = resolveRunningDirective(bytes);
                    }
                    break;

                case 0x05:
                    {
                        xdata = resolvePausingDirective(bytes);
                    }
                    break;

                default:
                    break;
            }

            if (new Random().NextDouble() < BAD_PERCENT && xdata != null)
            {
                xdata[xdata.Length - 1] = 0xff;
            }

            return xdata;
        }

        private byte[] GetDirectiveId(byte[] bytes)
        {
            var len = bytes.Length;
            var ret = new byte[2];
            ret[0] = bytes[len - 5];
            ret[1] = bytes[len - 4];

            return ret;
        }

        private byte GetDeviceType(byte[] bytes)
        {
            return bytes[bytes.Length - 3];
        }

        private byte[] resolveIdleDirective(byte[] bytes)
        {
            var ids = GetDirectiveId(bytes);
            var content = new byte[] { bytes[0], 0x03, 0x00, 0x00, ids[0], ids[1], GetDeviceType(bytes) };
            var checkCode = DirectiveHelper.GenerateCheckCode(content);

            return content.Concat(checkCode).ToArray();
        }

        private byte[] resolveRunningDirective(byte[] bytes)
        {
            var ids = GetDirectiveId(bytes);
            var rate = new byte[] { 0x00, 0x00 };
            if (isRunning[bytes[0]])
            {
                rate = DirectiveHelper.ParseNumberTo2Bytes(runSpeed[bytes[0]]);// bytes.Skip(4).Take(2).ToArray();
            }

            var content = new byte[] { bytes[0], 0x04, 0x00, 0x16, rate[0], rate[1], 0x00, 0x01, ids[0], ids[1], GetDeviceType(bytes) };
            var checkCode = DirectiveHelper.GenerateCheckCode(content);
            return content.Concat(checkCode).ToArray();
        }

        private byte[] resolvePausingDirective(byte[] bytes)
        {
            var ids = GetDirectiveId(bytes);
            var rate = new byte[] { 0x00, 0x00 };
            if (isPausing[bytes[0]])
            {
                rate = new byte[] { 0x00, 0x23, };
            }
            var content = new byte[] { bytes[0], 0x05, 0x00, 0x12, rate[0], rate[1], 0x01, ids[0], ids[1], GetDeviceType(bytes) };
            var checkCode = DirectiveHelper.GenerateCheckCode(content);
            return content.Concat(checkCode).ToArray();
        }

        private byte[] resolveTryStartDirective(byte[] bytes)
        {
            var ids = GetDirectiveId(bytes);
            var content = new byte[] { bytes[0], 0x00, ids[0], ids[1], GetDeviceType(bytes) };
            var checkCode = DirectiveHelper.GenerateCheckCode(content);
            return content.Concat(checkCode).ToArray();
        }

        private string BytesToString(byte[] bytes)
        {
            var temp = "";
            bytes.ToList().ForEach((t) =>
            {
                temp += "0X" + Convert.ToString(t, 16).PadLeft(2, '0') + " ";
            });

            return temp;
        }

        public Socket StartUp()
        {
            if (isStart) return server;

            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var ip = IPAddress.Parse(address);
            var endPoint = new IPEndPoint(ip, port);

            server.Bind(endPoint);
            server.Listen(10);
            isStart = true;

            btnOpen.IsEnabled = false;

            return server;
        }

        public void AddMsg(byte[] bytes)
        {
            if (bytes.Length <= 2) return;

            var msg = BytesToString(bytes);
            RenderMsg(msg, Brushes.Blue);
        }

        public void RenderMsg(string msg, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                var tb = new TextBlock { Text = msg, Foreground = color };
                if(spMsg.Items.Count>20) spMsg.Items.Clear();
                spMsg.Items.Add(tb);
                spMsg.ScrollIntoView(spMsg.Items[spMsg.Items.Count - 1]);
            });

        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            server?.Dispose();
            isStart = false;
        }

        private void Test_OnClick(object sender, RoutedEventArgs e)
        {

        }

    }
}
