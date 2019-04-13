using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 构建服务对象
    /// </summary>
    public static class ServerBuilder
    {
        public static Action BuildConsoleAgent(GameServer gs, string srvAddr, int srvPort)
        {
            srv = gs;

            BuildBaseComponents(); // 基础模块
            var cia = BC<ConsoleInputAgent>();

            // 初始化所有模块
            srv.Initialize();

            return () =>
            {
                // 启动服务器
                cia.Start(srvAddr, srvPort);
                srv.Start();
            };
        }

        // 创建实验室用的服务器，返回启动操作
        public static Action BuildLibServer(GameServer gs, int port)
        {
            srv = gs;

            var ci = BC<ConsoleInput>();
            ci.Srv = gs;

            BuildBaseComponents(); // 基础模块
            BuildBussinessLoggers(gs); // 日志系统
            BuildQuestionnaire(gs); // 问卷系统
            BuildLogicComponents(gs); // 逻辑功能
            BuildGMComponent(); // GM 和调试功能

            // 初始化所有模块
            srv.Initialize();

            return () =>
            {
                // 启动服务器
                srv.Get<NetCore>().StartListening("0.0.0.0", port);
                srv.Get<ILog>().Info("GameServer started at: " + port);

                srv.Start();
            };
        }

        // 战斗数据分析用服务器
        public static Action BuildConsoleDataAnalysis(GameServer gs, int port)
        {
            srv = gs;

            var cida = BC<ConsoleInputDataAnalysis>();
            cida.Srv = gs;

            BuildBaseComponents(); // 基础模块
            BuildLogicComponents(gs); // 逻辑功能
            BuildDataAnalysisComponents(); // 数据分析功能

            // 初始化所有模块
            srv.Initialize();

            return () =>
            {
                // 启动服务器
                srv.Get<ILog>().Info("DataAnalysisServer started at: " + port);

                srv.Start();
            };
        }

        // 数据分析模块
        public static void BuildDataAnalysisComponents()
        {
            BC<DataAnalysisMgr>();
        }

        // 创建中的服务器对象
        static GameServer srv = null;
        
        // 默认方式创建给定模块
        static T BC<T>() where T : Component, new()
        {
            var c = new T();
            srv.Add(typeof(T).Name, c);
            return c;
        }

        // 通用基础模块
        public static void BuildBaseComponents()
        {
            BC<NetCore>(); // 网络
            BC<UserPort>(); // 消息端口
            UserConnectionExt.ClientMessageHandler = "ServerPort";

            // 动态编译组件
            var css = new CsScriptShell<ScriptObject>();
            var dsp = new DynamicScriptProvider<ScriptObject>();
            dsp.AddNamespace("SCM");
            dsp.AddNamespace("Server");
            dsp.AddAssembly(System.Reflection.Assembly.GetExecutingAssembly().Location);
            dsp.AddNamespace("MySql");
            dsp.AddNamespace("MySql.Data");
            dsp.AddNamespace("MySql.Data.MySqlClient");
            dsp.AddAssembly(System.Reflection.Assembly.GetAssembly(typeof(MySql.Data.MySqlClient.MySqlConnection)).Location);
            dsp.AddNamespace("System.Data");
            dsp.AddAssembly(System.Reflection.Assembly.GetAssembly(typeof(System.Data.IDbConnection)).Location);
            dsp.CsScriptShell = css;
            css.DSP = dsp;
            srv.Add("CsScriptShell", css);
        }

        // 游戏逻辑
        public static void BuildLogicComponents(GameServer gs)
        {
            BC<SessionContainer>(); // 会话容器
            BC<LoginManager>(); // 登录
            BC<UnitConfigManager>(); // 配置表
            BC<UserManager>();

            BC<BattleRoomManager>(); // 战斗房间管理
            BC<MatchBoard>(); // 对战匹配
            BC<UnitFactory>(); // 地图单位工厂
            BC<UnitConfiguration>(); // 战场单位配置管理

            var uc = new UserContainer(new MySqlDbPersistence<User, string>(
                "scm", "127.0.0.1", "root", "123456",
                @"Users", "CREATE TABLE Users(ID VARCHAR(100) BINARY, Data MediumBlob, PRIMARY KEY(ID ASC));", 
                null, (usr) =>
                {
                    var buff = new WriteBuffer();
                    usr.Serialize(buff);
                    return buff.Data;
                }, (data) =>
                {
                    var rb = new RingBuffer(data);
                    var usr = new User();
                    usr.Deserialize(rb);
                    return usr;
                }, null));
            srv.Add("UserContainer", uc);
        }

        public static void BuildGMComponent()
        {
            // 服务器日志
            var sysLog = new SystemLogger();
            srv.Add("SystemLogger", sysLog);
            sysLog.AddLogger("ConsoleLogger", new ConsoleLogger());
            sysLog.AddLogger("FileLogger", new FileLogger());

            BC<GMInLab>().Init();
            BC<CheatCode>().Init();
        }

        public static void BuildBussinessLoggers(GameServer srv)
        {
            ServerBusinessLoggerConfig.Config("scm_log", "127.0.0.1", "root", "123456");

            srv.Add("LoginLog", new ServerBusinessLogger<LoginInfo>()); // 登录日志
            srv.Add("BattleLog", new ServerBusinessLogger<BattleInfo>()); // 战斗日志
        }

        public static void BuildQuestionnaire(GameServer srv)
        {
            BC<QuestionnaireMgr>(); // 问卷调查
            BC<QuestionnaireResultMgr>(); // 问卷调查

            var qrc = new QuestionnaireResultContainer(new MySqlDbPersistence<QuestionnaireResult, string>(
                "scm_qr", "127.0.0.1", "root", "123456",
                @"Qa", "CREATE TABLE Qa(ID VARCHAR(100) BINARY, Data MediumBlob,"
                + "PRIMARY KEY(ID ASC));", null, (da) =>
                {
                    var buff = new WriteBuffer();
                    da.Serialize(buff);
                    return buff.Data;
                }, (data) =>
                {
                    var rb = new RingBuffer(data);
                    var qr = new QuestionnaireResult();
                    qr.Deserialize(rb);
                    return qr;
                }, null));
            srv.Add("QuestionnaireResultContainer", qrc);
        }
    }
}
