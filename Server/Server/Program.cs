using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swift;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = 9530;
            var srvAddr = "127.0.0.1";
            var noConsole = false;
            var onlyConsoleAgent = false;

            var runScripts = new List<string>();
            foreach (var p in args)
            {
                if (p == "-noconsole")
                    noConsole = true;
                else if (p.StartsWith("-p"))
                    port = int.Parse(p.Substring(2));
                else if (p == "-console-agent")
                    onlyConsoleAgent = true;
                else if (p.StartsWith("-h"))
                    srvAddr = p.Substring(2);
                else if (p.StartsWith("-r"))
                    runScripts.Add(Path.Combine("./init", p.Substring(2)));
                else
                    Console.WriteLine("unknown parameter: " + p);
            }

            var srv = new GameServer();

            try
            {
                if (!onlyConsoleAgent)
                {
                    var startSrv = ServerBuilder.BuildLibServer(srv, port);

                    var css = srv.Get<CsScriptShell<ScriptObject>>();
                    foreach (var s in runScripts)
                        css.RunScript(s, "Init", new object[] { srv });

                    if (!noConsole)
                        srv.Get<ConsoleInput>().Start(); // 启用控制台输入

                    startSrv();
                }
                else if (onlyConsoleAgent)
                {
                    ServerBuilder.BuildConsoleAgent(srv, srvAddr, port)();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("!!!! EXCEPTION: " + ex.Message);
                Console.WriteLine("!!!! STACK: " + ex.StackTrace);
                srv.Get<ILog>().Error("[!!!! EXCEPTION]:" + ex.Message);
                srv.Get<ILog>().Error("[!!!! STACK]:" + ex.StackTrace);
                srv.Stop();
            }
        }
    }
}
