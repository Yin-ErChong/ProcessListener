﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcessListener
{
    public class LogHelper
    {
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");//这里的 loginfo 和 log4net.config 里的名字要一样
        public static readonly log4net.ILog logerror = log4net.LogManager.GetLogger("logerror");//这里的 logerror 和 log4net.config 里的名字要一样
        public static void Info(string info)
        {
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }

        public static void Error(string info, Exception ex)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, ex);
            }
        }
    }
}
