using Helper;
using Newtonsoft.Json;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcessListener
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print SharpPcap version
            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("SharpPcap {0}, Example1.IfList.cs", ver);
            SystemHelper.SetAutoStart(true, "ProcessListener", @"C:\Users\Administrator\Source\Repos\ProcessListener\bin\Debug\ProcessListener.exe");
            var pro = SystemHelper.GetNetStatPorts();
            foreach (var item in pro)
            {
                Console.WriteLine($"{JsonConvert.SerializeObject(item)}");
            }
            string listenProcessName = "EveryThingTest";
            var listenPort = pro.Where(n => n.process_name == listenProcessName)?.Select(n=>n.port_number).ToList();

            WinCapHelper.WinCapInstance.Listen(listenPort);
            Console.ReadLine();
        }
    }
}
