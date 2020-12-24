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
        private List<string> _listenIPPort;
        private string Ip { get; set; } = SystemHelper.GetIP(true);

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
        public void SetPort(List<string> listenIPPort)
        {
            _listenIPPort= listenIPPort;
        }
        public void Listen(List<string> listenPort=null)
        {
            if (listenPort!=null)
            {
                _listenIPPort = listenPort;
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
        /// <summary>
        /// 接收到包的处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            if (_listenIPPort == null|| _listenIPPort.Count<1)
            {
                return;
            }
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
                //此处可以解析http，但是当http消息体被压缩、加密、拆分的情况下无法解析
                case "TCP":
                    var tcpPacket = (TcpPacket)packet.Extract(typeof(TcpPacket));
                    string sourceIpStr = $"{ipPacket.SourceAddress}:{tcpPacket.SourcePort}";
                    string destinationIpStr = $"{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort}";
                    //检测到指定的ip地址出现
                    if (_listenIPPort.Contains(sourceIpStr)|| _listenIPPort.Contains(destinationIpStr))
                    {
                        if(tcpPacket.PayloadData != null)
                        {
                            string content = Encoding.UTF8.GetString(tcpPacket.PayloadData);
                            if (!string.IsNullOrEmpty(content)&&! string.IsNullOrEmpty(content.Trim()))
                            {
                                LogHelper.Info($"\n【协议】：TCP，\n【程序名】：{Configer.Instance.ProcessName}，\n【源IP】：{ipPacket.SourceAddress}:{tcpPacket.SourcePort}\n,【目标IP】：{ipPacket.DestinationAddress}:{tcpPacket.DestinationPort},【内容】：{content}");
                            }                           
                        }
                    }                    
                    break;
                case "UDP":
                    var udpPacket = (UdpPacket)packet.Extract(typeof(UdpPacket));
                    sourceIpStr = $"{ipPacket.SourceAddress}:{udpPacket.SourcePort}";
                    destinationIpStr = $"{ipPacket.DestinationAddress}:{udpPacket.DestinationPort}";
                    //检测到指定的ip地址出现
                    if (_listenIPPort.Contains(sourceIpStr) || _listenIPPort.Contains(destinationIpStr))
                    {
                        if (udpPacket.PayloadData != null)
                        {
                            string content = Encoding.UTF8.GetString(udpPacket.PayloadData);
                            if (!string.IsNullOrEmpty(content) &&! string.IsNullOrEmpty(content.Trim()))
                            {
                                break;
                            }
                            LogHelper.Info($"\n【协议】：UDP，\n【程序名】：{Configer.Instance.ProcessName}，\n【源IP】：{ipPacket.SourceAddress}:{udpPacket.SourcePort}\n,【目标IP】：{ipPacket.DestinationAddress}:{udpPacket.DestinationPort},【内容】：{content}");
                        }
                    }
                    break;
                default:
                    break;
            }

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
        }


    }
}