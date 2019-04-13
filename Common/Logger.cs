using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;

namespace SCM
{
    public abstract class Logger
    {
        public static Logger Instance { get; set; }

        public abstract void Info(string msg);
        public abstract void Err(string msg);
    }

    /// <summary>
    /// 一组 logger
    /// </summary>
    public class SystemLogger : Component, ILog
    {
        Dictionary<string, ILog> loggers = new Dictionary<string, ILog>();

        public void AddLogger(string name, ILog logger)
        {
            loggers[name] = logger;
        }

        public void RemoveLogger(string name)
        {
            loggers.Remove(name);
        }

        // 日志信息
        public void Info(string str)
        {
            foreach (var l in loggers.Values)
                l.Info(str);
        }

        // 日志错误
        public void Error(string str)
        {
            foreach (var l in loggers.Values)
                l.Error(str);
        }

        // 警告
        public void Warn(string str)
        {
            foreach (var l in loggers.Values)
                l.Warn(str);
        }

        // 调试
        public void Debug(string str)
        {
            foreach (var l in loggers.Values)
                l.Debug(str);
        }

        public override void Close()
        {
            foreach (var l in loggers.Values)
                l.Close();
        }
    }
}
