using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Swift;
using SCM;
using Swift.Math;

namespace Server
{
    /// <summary>
    /// 接受控制台输入
    /// </summary>
    public class ConsoleInput : Component, IFrameDrived
    {
        // 游戏服务器
        public GameServer Srv = null;

        Thread thr; // 等待输入的工作线程
        bool running = false;

        // 等待执行的指令
        ConcurrentQueue<Action> cmdQ = new ConcurrentQueue<Action>();

        public static Action<string> OnChangePVEAI = null;

        private Dictionary<string, Dictionary<string, string>> dic;

        public override void Init()
        {
            dic = new Dictionary<string, Dictionary<string, string>>();

            var up = GetCom<UserPort>();
            up.OnRequest("GMCmd", (Connection conn, IReadableBuffer data, IWriteableBuffer buff, Action end) =>
            {
                var strs = data.ReadString().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var cmd = strs[0];
                var ps = strs.Length == 1 ? null : new string[strs.Length - 1];
                if (ps != null)
                    Array.Copy(strs, 1, ps, 0, ps.Length);

                if (!cmdHandlers.ContainsKey(cmd))
                {
                    buff.Write("unknown command: " + cmd);
                    end();
                }
                else
                {
                    PushCommand((r) =>
                    {
                        buff.Write(r);
                        end();
                    }, cmd, ps);
                }
            });

            OnCommand("stop", (ps) => { running = false; Srv.Stop(); return "stopped"; });

            OnCommand("runfile", (ps) =>
            {
                if (ps == null || ps.Length < 1)
                    return "no file specified";
                else if (ps.Length < 2)
                    return "no function name specified";

                var f = ps[0];
                var fun = ps[1];

                try
                {
                    var css = GetCom<CsScriptShell<ScriptObject>>();
                    css.RunScript(f, fun, new object[] { Srv });
                }
                catch (Exception ex)
                {
                    return "run file " + f + "::" + fun + "[ex]\r\n" + ex.Message + "\r\n" + ex.StackTrace;
                }

                return "run file " + f + "::" + fun + "[ok]\r\n";
            });

            OnCommand("cfg", (ps) =>
            {
                if (ps == null)
                    return "parameter error!, try cfg --help";

                string result = "";

                switch (ps[0])
                {
                    case "get":
                        if (ps.Length != 2)
                            return "parameter error!, try cfg --help";

                        switch (ps[1])
                        {
                            case "-a":
                                var allUnitTypes = UnitConfiguration.AllUnitTypes;
                                for (int i = 0; i < allUnitTypes.Length; i++)
                                    result += "\n" + GetCfgInfo(allUnitTypes[i]);
                                break;
                            default:
                                result = GetCfgInfo(ps[1]);
                                break;
                        }
                        break;
                    case "set":
                        if (ps.Length < 4)
                            return "parameter error!, try cfg --help";

                        return result = SetCfgInfo(ps) ? "successful!" : "failed!";
                    case "count":
                        return result = UnitConfiguration.AllUnitTypes.Length.ToString();
                    case "-m":
                        foreach (var para in dic)
                        {
                            result += para.Key + "\n    ";
                            foreach (var p in para.Value)
                            {
                                result += p.Key + " : ";
                                result += p.Value + "\n    ";
                            }
                            result += "\n";
                        }
                        break;
                    case "--help":
                        result = "count" + " : " + "查看配置表数量";
                        result += "\n";
                        result += "get" + " : " + "查看配置表信息";
                        result += "\n";
                        result += "    get -a" + " : " + "查看所有配置表信息";
                        result += "\n";
                        result += "    get type" + " : " + "查看类型为type的配置表信息";
                        result += "\n";
                        result += "set" + " : " + "设置配置表信息";
                        result += "\n";
                        result += "    set type field value" + " : " + "设置类型为type的配置表信息,将field字段的值设置为value";
                        result += "\n";
                        return result;
                    default:
                        return "parameter error!, try cfg --help";
                }

                result += "\n";
                return result;
            });

            OnCommand("pve", (ps) =>
            {
                if (ps == null)
                    return "parameter error!";
                if (ps.Length != 2)
                    return "parameter error!";

                switch(ps[0])
                {
                    case "ai":
                        if (ps[1] != "0" && ps[1] != "1" && ps[1] != "2")
                            return "not exist!";
                        else
                        {
                            if (OnChangePVEAI != null)
                                OnChangePVEAI(ps[1]);
                            return "successful!";
                        }
                }

                return "";
            });

            OnCommand("onlines", (ps) =>
            {
                var ss = GetCom<SessionContainer>();
                return ss.Count.ToString();
            });

            OnCommand("set_auth_lv", (ps) =>
            {
                if (ps.Length < 1)
                    return "no name specified";

                var name = ps[0];
                var lv = ps.Length > 1 ? int.Parse(ps[1]) : 0;
                var ss = GetCom<SessionContainer>();
                var sArr = ss.GetByName(name);
                foreach (var s in sArr)
                {
                    s.Usr.Info.AuthLv = lv;
                    s.Usr.Update();
                }

                return "set " + sArr.Length + " to " + lv;
            });
        }

        private string GetCfgInfo(string type)
        {
            string result = "";
            object info = UnitConfiguration.GetDefaultConfig(type);

            if (info == null)
                return result = "not exist!";

            var fields = info.GetType().GetFields();

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i].Name;

                if (fields[i].GetValue(info) == null)
                    continue;

                string fieldValue = "";
                var fieldType = fields[i].FieldType;

                switch (fieldType.Name)
                {
                    case "Int32[]":
                        string result1 = "   ";
                        int[] value1 = (int[])fields[i].GetValue(info);
                        for (int t1 = 0; t1 < value1.Length; t1++)
                        {
                            var para = value1[t1];

                            result1 += " " + para.ToString();
                        }
                        fieldValue += "\n" + result1;
                        break;
                    case "String[]":
                        string result2 = "   ";
                        string[] value2 = (string[])fields[i].GetValue(info);
                        for (int t2 = 0; t2 < value2.Length; t2++)
                        {
                            var para = value2[t2];
                            if (para == null)
                            {
                                result2 += " " + "null";
                                continue;
                            }
                            result2 += " " + para.ToString();
                        }
                        fieldValue += "\n" + result2;
                        break;
                    case "String[][]":
                        string result3 = "   ";
                        string[][] value3 = (string[][])fields[i].GetValue(info);
                        for (int t3 = 0; t3 < value3.Length; t3++)
                        {
                            var para = value3[t3];
                            if (para == null)
                            {
                                result3 += " " + "null";
                                result3 += "\n   ";
                                continue;
                            }

                            foreach (var p in para)
                            {
                                if (p == null)
                                    continue;
                                result3 += " " + p.ToString();
                            }
                            result3 += "\n   ";
                        }
                        fieldValue += "\n" + result3;
                        break;
                    case "Fix64[]":
                        string result4 = "   ";
                        Fix64[] value4 = (Fix64[])fields[i].GetValue(info);
                        for (int t4 = 0; t4 < value4.Length; t4++)
                        {
                            var para = value4[t4];
                            if (para == null)
                            {
                                result4 += " " + "null";
                                continue;
                            }
                            result4 += " " + para.ToString();
                        }
                        fieldValue += "\n" + result4;
                        break;
                    case "Fix64[][]":
                        string result5 = "   ";
                        Fix64[][] value5 = (Fix64[][])fields[i].GetValue(info);
                        for (int t5 = 0; t5 < value5.Length; t5++)
                        {
                            var para = value5[t5];
                            if (para == null)
                            {
                                result5 += " " + "null";
                                result5 += "\n   ";
                                continue;
                            }

                            foreach (var p in para)
                            {
                                if (p == null)
                                    continue;
                                result5 += " " + p.ToString();
                            }
                            result5 += "\n   ";
                        }
                        fieldValue += "\n" + result5;
                        break;
                    default:
                        fieldValue = fields[i].GetValue(info).ToString();
                        break;
                }
               
                result += "\n" + fieldName + " : " + fieldValue;
            }

            return result;
        }

        private bool SetCfgInfo(string[] ps)
        {
            object info = UnitConfiguration.GetDefaultConfig(ps[1]);

            if (info == null)
                return false;

            var field = info.GetType().GetField(ps[2]);

            if (field == null)
                return false;

            switch (field.FieldType.Name)
            {
                case "Fix64":
                    float v1 = 0.0f;

                    if (!float.TryParse(ps[3], out v1))
                        return false;

                    Fix64 value1 = v1;
                    field.SetValue(info, value1);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], ps[3]);
                    }
                    dic[ps[1]][ps[2]] = ps[3];
                    break;
                case "Fix64[]":
                    string str2 = "";
                    Fix64[] value2 = new Fix64[ps.Length - 3];
                    for (int i = 3; i < ps.Length; i++)
                    {
                        float v2 = 0.0f;

                        if (!float.TryParse(ps[i], out v2))
                            return false;

                        str2 += ps[i] + " ";
                        value2[i - 3] = v2;
                    }
                    field.SetValue(info, value2);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], str2);
                    }
                    dic[ps[1]][ps[2]] = str2;
                    break;
                case "Fix64[][]":
                    string str3 = "";
                    int v3 = 0;
                    if (!int.TryParse(ps[3], out v3))
                        return false;

                    int row = v3;
                    Fix64[][] value3 = new Fix64[row][];
                    int t = 0;
                    for (int i = 4; i < ps.Length; i++)
                    {
                        int n = 0;
                        if (!int.TryParse(ps[i], out n))
                            return false;

                        str3 += "\n";
                        int num = n;
                        if (num == 0)
                        {
                            value3[t++] = null;
                            str3 += "null";
                            continue;
                        }

                        Fix64[] value = new Fix64[num];
                        for (int j = 0; j < num; j++)
                        {
                            if (i == ps.Length - 1)
                                return false;

                            float v = 0.0f;
                            if (!float.TryParse(ps[++i], out v))
                                return false;

                            str3 += ps[i] + " ";
                            value[j] = v;
                        }

                        value3[t++] = value;
                    }
                    field.SetValue(info, value3);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], str3);
                    }
                    dic[ps[1]][ps[2]] = str3;
                    break;
                case "Int32[]":
                    string str4 = "";
                    Int32[] value4 = new Int32[ps.Length - 3];
                    for (int i = 3; i < ps.Length; i++)
                    {
                        int v4 = 0;

                        if (!int.TryParse(ps[i], out v4))
                            return false;

                        str4 += ps[i] + " ";
                        value4[i - 3] = v4;
                    }
                    field.SetValue(info, value4);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], str4);
                    }
                    dic[ps[1]][ps[2]] = str4;
                    break;
                case "String[]":
                    string str5 = "";
                    String[] value5 = new String[ps.Length - 3];
                    for (int i = 3; i < ps.Length; i++)
                    {
                        value5[i - 3] = ps[i];
                        str5 += ps[i] + " ";
                    }
                    field.SetValue(info, value5);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], str5);
                    }
                    dic[ps[1]][ps[2]] = str5;
                    break;
                case "String[][]":
                    string str6 = "";
                    int v5 = 0;
                    if (!int.TryParse(ps[3], out v5))
                        return false;

                    int row1 = v5;
                    String[][] value6 = new String[row1][];
                    int t1 = 0;
                    for (int i = 4; i < ps.Length; i++)
                    {
                        str6 += "\n";
                        int n = 0;
                        if (!int.TryParse(ps[i], out n))
                            return false;

                        int num = n;
                        if (num == 0)
                        {
                            value6[t1++] = null;
                            str6 += "null";
                            continue;
                        }

                        String[] value = new String[num];
                        for (int j = 0; j < num; j++)
                        {
                            if (i == ps.Length - 1)
                                return false;

                            value[j] = ps[++i];
                            str6 += ps[i] + " ";
                        }

                        value6[t1++] = value;
                    }
                    field.SetValue(info, value6);
                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], str6);
                    }
                    dic[ps[1]][ps[2]] = str6;
                    break;
                default:
                    if (ps.Length != 4)
                        return false;

                    field.SetValue(info, DynamicGeneral.ParseBaseTypeValueFromString(ps[3], field.FieldType));

                    if (!dic.ContainsKey(ps[1]))
                    {
                        dic.Add(ps[1], new Dictionary<string, string>());
                    }                    
                    if (!dic[ps[1]].ContainsKey(ps[2]))
                    {
                        dic[ps[1]].Add(ps[2], ps[3]);
                    }
                    dic[ps[1]][ps[2]] = ps[3];
                    break;
            }

            return true;
        }

        public void Start()
        {
            running = true;
            thr = new Thread(new ThreadStart(WorkingThread));
            thr.Start();
        }

        void WorkingThread()
        {
            var cmd = "";
            while (running)
            {
                cmd = Console.ReadLine();
                var ins = cmd.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                cmd = ins.Length > 0 ? ins[0] : "";
                var ps = ins.Length > 1 ? ins.SubArray(1, ins.Length - 1) : null;
                lock (cmdHandlers)
                {
                    if (!cmdHandlers.ContainsKey(cmd))
                        cmdQ.Enqueue(() => { Console.WriteLine("unknown command: " + cmd); });
                    else
                        cmdQ.Enqueue(() => { Console.WriteLine(cmdHandlers[cmd](ps)); });
                }
            }
        }

        public void PushCommand(Action<string> cb, string cmd, params string[] ps)
        {
            cmdQ.Enqueue(() => { cb(cmdHandlers[cmd](ps)); });
        }

        public void OnTimeElapsed(int te)
        {
            Action cmd = null;
            while (cmdQ.TryDequeue(out cmd))
                cmd();
        }

        Dictionary<string, Func<string[], string>> cmdHandlers = new Dictionary<string, Func<string[], string>>();
        public void OnCommand(string cmd, Func<string[], string> op)
        {
            lock(cmdHandlers)
                cmdHandlers[cmd] = op;
        }
    }
}

// 代理服务器后台输入
public class ConsoleInputAgent : Component
{
    Thread thr;
    bool running = false;
    Connection conn = null;
    public void Start(string srvAddr, int ciPort)
    {
        running = true;
        thr = new Thread(new ThreadStart(WorkingThread));
        thr.Start();

        var nc = GetCom<NetCore>();
        conn = nc.Connect2Peer(srvAddr, ciPort);
        Console.WriteLine("server connected.");
    }

    void WorkingThread()
    {
        while (running)
        {
            var str = Console.ReadLine();
            var buff = conn.BeginRequest("UserPort", (data) =>
            {
                var r = data.ReadString();
                Console.WriteLine(r);
            });

            buff.Write("GMCmd");
            buff.Write(str);
            conn.End(buff);
        }
    }
}

namespace Server
{
    // 数据分析服务器后台输入
    public class ConsoleInputDataAnalysis : Component, IFrameDrived
    {
        public GameServer Srv = null;

        Thread thr;
        bool running = false;
        ConcurrentQueue<Action> cmdQ = new ConcurrentQueue<Action>();

        public void Start()
        {
            running = true;
            thr = new Thread(new ThreadStart(WorkingThread));
            thr.Start();
        }

        void WorkingThread()
        {
            var cmd = "";
            while (running)
            {
                cmd = Console.ReadLine();
                var ins = cmd.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                cmd = ins.Length > 0 ? ins[0] : "";
                var ps = ins.Length > 1 ? ins.SubArray(1, ins.Length - 1) : null;
                lock (cmdHandlers)
                {
                    if (!cmdHandlers.ContainsKey(cmd))
                        cmdQ.Enqueue(() => { Console.WriteLine("unknown command: " + cmd); });
                    else
                        cmdQ.Enqueue(() => { Console.WriteLine(cmdHandlers[cmd](ps)); });
                }
            }
        }

        public void PushCommand(Action<string> cb, string cmd, params string[] ps)
        {
            cmdQ.Enqueue(() => { cb(cmdHandlers[cmd](ps)); });
        }

        public void OnTimeElapsed(int te)
        {
            Action cmd = null;
            while (cmdQ.TryDequeue(out cmd))
                cmd();
        }

        Dictionary<string, Func<string[], string>> cmdHandlers = new Dictionary<string, Func<string[], string>>();
        public void OnCommand(string cmd, Func<string[], string> op)
        {
            lock (cmdHandlers)
                cmdHandlers[cmd] = op;
        }
    }
}