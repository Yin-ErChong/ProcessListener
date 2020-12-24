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
        private static bool isFindProcess = false;
        static void Main(string[] args)
        {
            // 打印SharpPcap版本
            string ver = SharpPcap.Version.VersionString;
            LogHelper.Info($"SharpPcap版本号{ver}，ProcessListener启动中..");
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
                    if (!isFindProcess)
                    {
                        isFindProcess = true;
                        LogHelper.Info($"发现程序启动端口：{JsonConvert.SerializeObject(listenPort)}");
                    }
                    WinCapHelper.WinCapInstance.SetPort(listenPort);
                }
                else
                {
                    if (isFindProcess)
                    {
                        isFindProcess = false;
                        LogHelper.Error($"程序关闭");
                    }                   
                    WinCapHelper.WinCapInstance.SetPort(null);
                }
            }
            catch (Exception ee)
            {
                LogHelper.Error("检测端口程序发生异常",ee);
            }           
        }
    }
}
