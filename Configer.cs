﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ProcessListener
{
    public class Configer
    {
        public static ConfigModel Instance = InitConfig();
        private static ConfigModel InitConfig()
        {
            string jsonfile = $"{AppDomain.CurrentDomain.BaseDirectory}\\jsconfig.json";//JSON文件路径
            string json = System.IO.File.ReadAllText(jsonfile);
            ConfigModel configModel = JsonConvert.DeserializeObject<ConfigModel>(json);
            return configModel;
        }
        public class ConfigModel
        {
            public string ProcessName { get; set; }
        }

    }
}
