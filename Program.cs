using Helper;
using Newtonsoft.Json;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ProcessListener
{
    class Program
    {
        static void Main(string[] args)
        {
            // 打印SharpPcap版本
            string ver = SharpPcap.Version.VersionString;
            LogHelper.Info("测试");
            Console.WriteLine("SharpPcap {0}, Example1.IfList.cs", ver);
            string exePath  = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            SystemHelper.SetAutoStart(true, "ProcessListener", exePath);
            //启动检测线程
            Thread thread = new Thread(n=> {
                while (true)
                {
                    CheckProcess();
                    Thread.Sleep(3000);
                }            
            });
            thread.Start();
            //开始侦听
            WinCapHelper.WinCapInstance.Listen() ;
            Console.ReadLine();
        }
        /// <summary>
        /// 检测指定程序是否启动
        /// </summary>
        public static void CheckProcess()
        {
            try
            {
                var pro = SystemHelper.GetNetStatPorts();
                string listenProcessName = Configer.Instance.ProcessName;
                var listenPort = pro.Where(n => n.process_name == listenProcessName)?.Select(n => $"{n.ip_number}:{n.port_number}").ToList();
                if (listenPort != null && listenPort.Count > 0)
                {
                    Console.WriteLine($"发现程序启动端口：{JsonConvert.SerializeObject(listenPort)}");
                    WinCapHelper.WinCapInstance.SetPort(listenPort);
                }
                else
                {
                    Console.WriteLine($"程序关闭");
                    WinCapHelper.WinCapInstance.SetPort(null);
                }
            }
            catch (Exception ee)
            {
                Console.WriteLine("发生异常");
            }           
        }
    }
}
