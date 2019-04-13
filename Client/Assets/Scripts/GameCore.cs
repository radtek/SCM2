using System.Collections;
using System.Collections.Generic;
using Swift;
using SCM;
using Swift.Math;
using System;

public class GameCore : Core
{
    public static GameCore Instance { get { return gc; } }
    static GameCore gc = new GameCore();

    bool inited = false;
    public override void Initialize()
    {
        if (inited)
            return;

        BuildBaseComponents();
        BuildLogicComponents();

        // 载入翻译表
        SCMText.LoadDict("translation", SCMText.dict);

        // 初始化所有模块
        base.Initialize();
        inited = true;
    }

    // 默认方式创建给定模块
    T BC<T>() where T : Component, new()
    {
        var c = new T();
        Add(typeof(T).Name, c);
        return c;
    }

    // 通用基础模块
    public void BuildBaseComponents()
    {
        var nc = BC<NetCore>(); // 网络
        BC<ServerPort>(); // 消息端口
        ServerConnectionExt.ServerMessageHandler = "UserPort";
        nc.OnDisconnected += OnDisconnected;

        BC<CoroutineManager>(); // 协程
    }

    // 网络连接断开
    public event Action<Connection, string> OnMainConnectionDisconnected = null;
    private void OnDisconnected(Connection conn, string reason)
    {
        OnMainConnectionDisconnected.SC(conn, reason);
    }

    // 逻辑模块
    public void BuildLogicComponents()
    {
        BC<UnitConfiguration>(); // 单位配置管理
        BC<AvatarConfiguration>(); // 头像配置
        BC<UserManager>();
        BC<UnitFactory>();
        BC<TipConfiguration>();

        BC<GuideManager>();
    }

    // 服务器连接对象
    public Connection ServerConnection { get; set; }

    // 玩家自身信息
    public string MeID { get; set; }
    public UserInfo MeInfo { get; set; }

    // 自己在战斗中的 player 编号
    public int MePlayer { get; set; }

    // 当前战斗房间
    public Room4Client CurrentRoom { get; set; }

    // 本方资源
    public Fix64 GetMyResource(string type)
    {
        return CurrentRoom.GetResource(MePlayer, type);
    }

    public Room4Client Room = null;
    public override void RunOneFrame(int timeElapsed)
    {
        if (Room != null)
            Room.OnTimeElapsed(timeElapsed);

        base.RunOneFrame(timeElapsed);
    }
}
