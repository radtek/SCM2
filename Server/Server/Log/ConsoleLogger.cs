using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swift;
using SCM;

namespace Server
{
    public class ConsoleLogger : ILog
    {
        // 日志信息
        public void Info(string str)
        {
            WriteInColor(str, ConsoleColor.Green);
        }

        // 日志错误
        public void Error(string str)
        {
            WriteInColor(str, ConsoleColor.Red);
        }

        // 警告
        public void Warn(string str)
        {
            WriteInColor(str, ConsoleColor.Yellow);
        }

        // 调试
        public void Debug(string str)
        {
            WriteInColor(str, ConsoleColor.Gray);
        }

        public void Close()
        {
        }

        void WriteInColor(string str, ConsoleColor fc)
        {
            var c = Console.ForegroundColor;
            Console.ForegroundColor = fc;
            Console.WriteLine(str);
            Console.ForegroundColor = c;
        }
    }
}
