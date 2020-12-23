using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using SharpPcap;
using PacketDotNet;
using SharpPcap.LibPcap;
using ProcessListener;
using System.IO.Compression;
using System.Net;

namespace Helper
{
    public class WinCapHelper
    {
        private static object syncObj = new object();
        private static WinCapHelper _capInstance;
        public static WinCapHelper WinCapInstance
        {
            get
            {
                if (null == _capInstance)
                {
                    lock (syncObj)
                    {
                        if (null == _capInstance)
                        {
                            _capInstance = new WinCapHelper();
                        }
                    }
                }
                return _capInstance;
            }
        }
        private Thread _thread;
        private List<string> _listenPort;
        

        /// <summary>
        /// when get pocket,callback
        /// </summary>
        public Action<string> _logAction;


        /// <summary>
        /// 过滤条件关键字
        /// </summary>
        public string filter;


        private WinCapHelper()
        {


        }
        public void Listen(List<string> listenPort=null)
        {
            if (_thread != null && _thread.IsAlive)
            {
                return;
            }
            if (listenPort!=null)
            {
                _listenPort = listenPort;
            }
            //遍历网卡
            foreach (PcapDevice device in LibPcapLiveDeviceList.Instance)
            {
                Thread thread = new Thread(n=> {

                    //分别启动监听，指定包的处理函数
                    device.OnPacketArrival +=
                        new PacketArrivalEventHandler(device_OnPacketArrival);
                    device.Open(DeviceMode.Normal, 1000);
                    device.Capture();
                });
                thread.Start();
                //device.StartCapture();
            }
        } 

        
        /// <summary>
        /// 打印包信息，组合包太复杂了，所以直接把hex字符串打出来了
        /// </summary>
        /// <param name="str"></param>
        /// <param name="p"></param>
        private void PrintPacket(ref string str, Packet p)
        {
            if (p != null)
            {
                string s = p.ToString();
                if (!string.IsNullOrEmpty(filter) && !s.Contains(filter))
                {
                    return;
                }


                str += "\r\n" + s + "\r\n";


                //尝试创建新的TCP / IP数据包对象，
                //第一个参数为以太头长度，第二个为数据包数据块
                str += p.PrintHex() + "\r\n";
            }
        }
        public static string ReadGzip(byte[] bytes, string encoding = "GB2312")
        {
            string result = string.Empty;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (GZipStream decompressedStream = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(decompressedStream, Encoding.GetEncoding(encoding)))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }
            return result;
        }
        private static string ReadGzip2(byte[] buffer)
        {
            try
            {
                StringBuilder s = new StringBuilder(102400);
                //WebClient wr = new WebClient();
                //wr.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
                //byte[] buffer = wr.DownloadData(url);
                GZipStream g = new GZipStream((Stream)(new MemoryStream(buffer)), CompressionMode.Decompress);
                byte[] d = new byte[20480];
                int l = g.Read(d, 231, 20480-231);
                while (l > 0)
                {
                    s.Append(Encoding.Default.GetString(d, 0, l));
                    l = g.Read(d, 0, 20480);
                }
                return s.ToString();
            }
            catch (Exception ee)
            {

                return "";
            }


        }
        /// <summary>
        /// 接收到包的处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            //解析出基本包
            var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            //var ip = SystemHelper.GetIP(true);
            var ipPacket = (IpPacket)packet.Extract(typeof(IpPacket));
            if (ipPacket==null)
            {
                return;
            }
            switch (ipPacket.Protocol.ToString())
            {
                case "TCP":
                    var tcpPacket = (TcpPacket)packet.Extract(typeof(TcpPacket));
                    //if (_listenPort!=null&& !_listenPort.Contains(tcpPacket.SourcePort.ToString())&&! _listenPort.Contains(tcpPacket.DestinationPort.ToString()))
                    //{
                    //    //break;
                    //}
                    if (ipPacket.SourceAddress.ToString()== "47.103.92.119"|| ipPacket.DestinationAddress.ToString()== "47.103.92.119")
                    {
                        if (tcpPacket.PayloadData!=null)
                        {
                            Console.WriteLine($"源IP：{ipPacket.SourceAddress}:{tcpPacket.SourcePort},目标IP{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}");
                            Console.WriteLine($"内容1：{Encoding.UTF8.GetString(tcpPacket.PayloadData)}");
                            //byte[] by = new byte[] { };
                            //tcpPacket.PayloadData.CopyTo(by, 231);
                            Console.WriteLine($"内容2：{ReadGzip2(tcpPacket.PayloadData)}");
                        }
                        if (ipPacket.PayloadData!=null)
                        {
                            Console.WriteLine($"源IP：{ipPacket.SourceAddress}:{tcpPacket.SourcePort},目标IP{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}");
                            Console.WriteLine($"内容3：{Encoding.UTF8.GetString(ipPacket.PayloadData)}");
                            //byte[] by = new byte[] { };
                            //tcpPacket.PayloadData.CopyTo(by, 231);
                            Console.WriteLine($"内容4：{ReadGzip2(ipPacket.PayloadData)}");
                        }                       
                    }
                    
                    break;
                default:
                    break;
            }

            //Console.WriteLine(ipPacket.Protocol);

            //协议类别
           // var dlPacket = PacketDotNet.DataLinkPacket.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);


            //var ethernetPacket = PacketDotNet.EthernetPacket.GetEncapsulated(packet);


            //var internetLinkPacket = PacketDotNet.InternetLinkLayerPacket.Parse(packet.BytesHighPerformance.Bytes);
            //var internetPacket = PacketDotNet.InternetPacket.Parse(packet.BytesHighPerformance.Bytes);


            //var sessionPacket = PacketDotNet.SessionPacket.Parse(packet.BytesHighPerformance.Bytes);
            //var appPacket = PacketDotNet.ApplicationPacket.Parse(packet.BytesHighPerformance.Bytes);
            //var pppoePacket = PacketDotNet.PPPoEPacket.Parse(packet.BytesHighPerformance.Bytes);


            //var arpPacket = PacketDotNet.ARPPacket.GetEncapsulated(packet);
            //var ipPacket = PacketDotNet.IpPacket.GetEncapsulated(packet); //ip包
            //var udpPacket = PacketDotNet.UdpPacket.GetEncapsulated(packet);
            //var tcpPacket = PacketDotNet.TcpPacket.GetEncapsulated(packet);


            string ret = "";
           // PrintPacket(ref ret, packet);
            //ParsePacket(ref ret, ethernetPacket);
            //ParsePacket(ref ret, internetLinkPacket);
            //ParsePacket(ref ret, internetPacket);
            //ParsePacket(ref ret, sessionPacket);
            //ParsePacket(ref ret, appPacket);
            //ParsePacket(ref ret, pppoePacket);
            //ParsePacket(ref ret, arpPacket);
            //ParsePacket(ref ret, ipPacket);
            //ParsePacket(ref ret, udpPacket);
            //ParsePacket(ref ret, tcpPacket);




            //if (!string.IsNullOrEmpty(ret))
            //{
            //    string rlt = "\r\n时间 : " +
            //        DateTime.Now.ToLongTimeString() +
            //        "\r\n数据包: \r\n" + ret;
            //    _logAction(rlt);
            //}


        }

        public void StopAll()
        {
            foreach (PcapDevice device in SharpPcap.CaptureDeviceList.Instance)
            {
                if (device.Opened)
                {
                    Thread.Sleep(500);
                    device.StopCapture();
                }
                _logAction("device : " + device.Description + " stoped.\r\n");
            }
            _thread.Abort();
        }


    }
}