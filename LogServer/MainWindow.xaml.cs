using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

namespace LogServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public Socket server = null;
        private string address;
        private int port = 3000;
        private bool isStart = false;
        private byte[] data = new byte[256];
        private ConcurrentQueue<string> msgQueue = new ConcurrentQueue<string>();

        public MainWindow()
        {
            try
            {
                address = Environment.GetEnvironmentVariable("IOT_LOG_IP") ?? "192.168.1.100";
            }
            catch (Exception)
            {
                //
            }

            InitializeComponent();
            txtPort.Text = port.ToString();
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            server?.Shutdown(SocketShutdown.Both);
            server?.Dispose();
            isStart = false;
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


        public void RenderMsg(string msg, Brush color)
        {
            Dispatcher.Invoke(() =>
            {
                var tb = new TextBlock { Text = msg, Foreground = color };
                spMsg.Items.Add(tb);
                //                spMsg.ScrollIntoView(spMsg.Items[spMsg.Items.Count - 1]);
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPort.Text))
            {
                MessageBox.Show("端口不能为空");
                return;
            }
            StartUp();
            Task.Run(async () =>
            {
                while (true)
                {
                    var msg = "";
                    if (msgQueue.TryDequeue(out msg) && !string.IsNullOrWhiteSpace(msg))
                    {
                        WriteLog(msg);
                    }

                    await Task.Delay(10);
                }
            });

            RenderMsg("服务已开启...", Brushes.Gray);

            //开启新线程 避免阻塞主线程
            new Thread(() =>
            {
                while (true)
                {
                    Socket socket = server.Accept();
                    Dispatcher.Invoke(() =>
                    {
                        RenderMsg("客户端连接成功........", new SolidColorBrush(Colors.Red));
                    });
                    var bytes = new List<byte>();

                    //接受一个新的客户端 开启新线程
                    new Thread(() =>
                    {
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

                                var length = socket.Receive(data);

                                for (var i = 0; i < length; i++)
                                {
                                    bytes.Add(data[i]);
                                }

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
                                if (socket.Available == 0)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        msgQueue.Enqueue(Encoding.UTF8.GetString(bytes.ToArray()));
                                    });

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

        private void Test_OnClick(object sender, RoutedEventArgs e)
        {
            server?.Shutdown(SocketShutdown.Both);
            server?.Dispose();
            isStart = false;
        }

        private void WriteLog(string msg)
        {
            try
            {
                if (!Directory.Exists(@"E:/log"))
                {
                    Directory.CreateDirectory(@"E:/log");
                }

                using (var file = File.Open(@"E:/log/logs", FileMode.OpenOrCreate))
                {
                    file.Seek(0, SeekOrigin.End);
                    msg = msg.Replace("[", "\n[");
                    using (var sw = new StreamWriter(file))
                    {
                        sw.Write(msg);
                        Dispatcher.Invoke(() =>
                        {
                            txtMsg.Text = msg;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }
    }
}
