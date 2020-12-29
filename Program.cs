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
                    CheckProcessAndBegin();
                    Thread.Sleep(3000);
                }            
            });
            thread.Start();

        }
        /// <summary>
        /// 检测指定程序是否启动
        /// </summary>
        public static bool CheckProcessAndBegin()
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
                        //设置目标程序端口
                        WinCapHelper.WinCapInstance.SetPort(listenPort);
                        //目标程序启动则直接开始侦听
                        WinCapHelper.WinCapInstance.Listen();
                        LogHelper.Info($"发现程序启动端口：{JsonConvert.SerializeObject(listenPort)}");
                    }
                    return true;
                }
                else
                {
                    if (isFindProcess)
                    {
                        isFindProcess = false;
                        WinCapHelper.WinCapInstance.StopAll();                       
                    }
                    LogHelper.Error($"目标程序未启动");
                    return false;
                }
            }
            catch (Exception ee)
            {
                LogHelper.Error("检测端口程序发生异常",ee);
                return false;
            }           
        }
    }
}
