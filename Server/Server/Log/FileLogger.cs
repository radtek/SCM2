using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Swift;
using SCM;

namespace Server
{
    public class FileLogger : ILog
    {
        // 日志信息
        public void Info(string str)
        {
            WriteMsg("info", str);
        }

        // 日志错误
        public void Error(string str)
        {
            WriteMsg("err", str);
        }

        // 警告
        public void Warn(string str)
        {
            WriteMsg("warn", str);
        }

        // 调试
        public void Debug(string str)
        {
            WriteMsg("dbg", str);
        }

        bool closed = false;
        public void Close()
        {
            if (w != null)
                w.Close();

            closed = true;
            w = null;
        }

        string lastLogOnDate = null;
        StreamWriter w = null;
        void WriteMsg(string prefix, string msg)
        {
            if (!Directory.Exists("./log"))
                Directory.CreateDirectory("./log");

            var now = DateTime.Now;
            var currentLogOnDate = now.ToString("yyyy-MM-dd");
            if (w == null || lastLogOnDate != currentLogOnDate)
            {
                if (w != null)
                    w.Close();

                lastLogOnDate = currentLogOnDate;
                w = new StreamWriter(Path.Combine("./log", currentLogOnDate + ".log"), true);
            }

            w.WriteLine("[" + prefix + "](" + now.ToString("hh:mm:ss") + "):" + msg);
            w.Flush();

            // 可能是关闭服务器之后的最后几条日志
            if (closed)
            {
                w.Close();
                w = null;
            }
        }
    }
}
