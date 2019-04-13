using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;
using SCM;

public class Room4Client : Room
{
    ServerPort sp;
    BattleReplayer replayer;

    public static event Action<string, string, Vec2, bool> OnBeforeBattleBegin = null;
    public static event Action<Room4Client, bool> OnBattleBegin = null;
    public static event Action<Room, string, bool> OnBattleEnd = null;
    public static event Action<Room4Client> OnFrameElapsed = null;
    public static event Action<Unit, Unit> NotifyAddBattleUnit = null;
    public static event Action<Unit> NotifyAddBuildingUnit = null;
    public static event Action<Unit, string> NotifyReconstructUnit = null;
    public static event Action<Unit> OnConstructingCompleted = null;
    public static event Action<Unit> OnConstructingCanceled = null;
    public static event Action<int, string, Fix64> OnResourceChanged = null;
    public static event Action<Unit, string, Fix64> OnResourceProduced = null;
    public static event Action<Unit> NotifyUnitRemoved = null;
    public static event Action<Unit, Unit> OnDoAttack = null;

    public static event Action OnUsrsSwitched = null;

    // 当前执行的消息队列
    Queue<KeyValuePair<string, Action>> msgQ = new Queue<KeyValuePair<string, Action>>();

    // 寻路实现
    public Func<Vec2, Vec2, Fix64, Vec2[]> PathFinder = null;

    public void Init()
    {
        sp = GameCore.Instance.Get<ServerPort>();
        replayer = new BattleReplayer();
        GameCore.Instance.Add("Replayer", replayer);
        GameCore.Instance.Room = this;

        OnMessageDirectly("BattleBegin", OnBattleBeginMsg);
        OnMessageDirectly("FF", OnFrameMoveForwardMsg);
        OnMessageDirectly("BattleEnd", OnBattleEndMsg);

        // BufferMessage("SetPath", OnSetPathMsg);
        BufferMessage("CosntructBuildingUnit", OnCosntructBuildingUnitMsg);
        BufferMessage("ConstructCrystalMachine", OnConstructCrystalMachineMsg);
        BufferMessage("ConstructAccessory", OnConstructAccessoryMsg);
        BufferMessage("ReconstructBuilding", OnRecosntructBuildingMsg);
        BufferMessage("CancelBuilding", OnCancelBuildingMsg);
        BufferMessage("DestroyBuilding", OnDestroyBuildingMsg);
        BufferMessage("CheatCode", OnCheatCodeMsg);
        BufferMessage("DropSoldierFromCarrier", OnDropSoldierFromCarrierMsg);
        BufferMessage("AddUnitAt", OnAddUnitAtMsg);
        BufferMessage("PlayerSwitched", OnPlayersSwitchedMsg);
        BufferMessage("AddBattleUnitAt", OnAddBattleUnitAtMsg);
        BufferMessage("AddBattleUnit4TestAnyway", OnAddBattleUnit4TestAnyway);
        BufferMessage("AddBuildingUnit4TestAnyway", OnAddBuildingUnit4TestAnyway);
        BufferMessage("AddSoldierCarrierUnit4TestAnyway", OnAddSoldierCarrierUnit4TestAnyway);
        BufferMessage("Crash", OnCrashMsg);
    }

    // 是否存在满足指定条件的单位
    public bool ExistsMyUnit(Func<Unit, bool> filter)
    {
        foreach (var u in AllUnits)
        {
            if (u.Player == GameCore.Instance.MePlayer && filter(u))
                return true;
        }

        return false;
    }

    // 是否存在满足指定条件的单位
    public bool ExistsMyUnit(string unitType)
    {
        return ExistsMyUnit((u) => u.UnitType == unitType);
    }

    // 服务器寻路请求
    void OnSrvFindPath(IReadableBuffer data, IWriteableBuffer buff)
    {
        var src = data.ReadVec2();
        var dst = data.ReadVec2();
        var size = data.ReadFix64();

        Vec2[] path = PathFinder(src, dst, size);
        buff.Write(path);
    }

    // 立即响应的消息
    void OnMessageDirectly(string op, Action<IReadableBuffer> cb)
    {
        sp.OnMessage(op, (data) =>
        {
            replayer.Record(op, data);
            cb.SC(data);
        });

        replayer.OnMessage(op, cb);
    }

    ulong frameNoRecieved = 0; // 消息已经接收到了第几帧

    // 已经收到数据的帧编号和本地数据进度的帧编号的差值，如果太大，则应该加速推进
    public int WaitingFrameCount { get { return (int)(frameNoRecieved - FrameNo); } }

    void BufferMessage(string op, Action<IReadableBuffer> handler)
    {
        sp.OnMessage(op, (data) =>
        {
            replayer.Record(op, data);
            msgQ.Enqueue(new KeyValuePair<string, Action>(op, () => { handler(data); }));
        });

        replayer.OnMessage(op, (data)=>
        {
            msgQ.Enqueue(new KeyValuePair<string, Action>(op, () => { handler(data); }));
        });
    }

    // 逻辑帧前进
    void OnFrameMoveForwardMsg(IReadableBuffer data)
    {
        frameNoRecieved++;
        msgQ.Enqueue(new KeyValuePair<string, Action>("FrameEnd", null));
    }

    int te = 0;
    public void OnTimeElapsed(int timeElapsed)
    {
        if (Finished)
            return;

        te += timeElapsed;
        var fact = WaitingFrameCount < 1 ? 1 : WaitingFrameCount;
        te *= fact;
        MapUnit.SpeedUp = fact;

        while (te >= Room.FrameInterval)
        {
            te -= Room.FrameInterval;
            MoveOneStep();
        }
    }

    public override void MoveOneStep()
    {
        if (FrameNo >= frameNoRecieved || Finished)
            return;

        // 把这一帧所有的命令先执行了
        while (msgQ.Count > 0)
        {
            var kv = msgQ.Dequeue();

#if UNITY_EDITOR
            if (kv.Key == "FrameEnd" || kv.Key == "BattleEndFrame")
#else
            if (kv.Key == "FrameEnd")
#endif
            {
                base.MoveOneStep();
                OnFrameElapsed.SC(this);

#if UNITY_EDITOR
                if (kv.Key == "BattleEndFrame")
                    kv.Value();
#endif
                break;
            }
            else
                kv.Value();
        }
    }

    public int WinnerAward = 0;
    public int LoserAward = 0;
    public string[] WinnerAwardUnits = null;

    public bool IsPVP = false;

    // 战斗开始
    bool inReplay = false;
    void OnBattleBeginMsg(IReadableBuffer data)
    {
        frameNoRecieved = 0;
        msgQ.Clear();

        inReplay = replayer.InReplaying;
        if (!inReplay)
        {
            replayer.Clear();
            replayer.Record("BattleBegin", data);
        }

        Clear();

        var randomSeed = data.ReadInt();
        var lvID = data.ReadString();
        WinnerAward = data.ReadInt();
        LoserAward = data.ReadInt();
        WinnerAwardUnits = data.ReadStringArr();
        var roomID = data.ReadString();
        var usrs = data.ReadStringArr();
        var usrsInfo = data.ReadArr<UserInfo>();
        var sz = data.ReadVec2();
        IsPVP = data.ReadBool();

        // 设置当前用户信息
        var gc = GameCore.Instance;
        gc.CurrentRoom = this;
        var meID = gc.MeID;
        if (usrs[1] == meID)
            gc.MePlayer = 1;
        else 
            gc.MePlayer = 2; // 观战者也用 2 视角

        // 初始化房间信息
        var usr1 = usrs[1];
        var usr2 = usrs[2];
        OnBeforeBattleBegin.SC(usr1, usr2, sz, inReplay);
        Init(roomID, sz, lvID, usr1, usr2, usrsInfo[1], usrsInfo[2]);
        base.BattleBegin(randomSeed);

        OnBattleBegin.SC(this, inReplay);
    }

    // 战斗结束消息
    void OnBattleEndMsg(IReadableBuffer data)
    {
        var winner = data.ReadString();
        frameNoRecieved++;
        msgQ.Enqueue(new KeyValuePair<string, Action>("BattleEndFrame", () => { ProcessBattleEndFrame(winner); }));
    }

    // 战斗结束最后一帧
    void ProcessBattleEndFrame(string winner)
    {
        BattleEnd(winner);
        OnBattleEnd.SC(this, winner, inReplay);
    }

    public override void BattleEnd(string winner)
    {
        base.BattleEnd(winner);

        // 录像就不做战斗后结算
        if (inReplay)
            return;

        // 战斗后结算
        var meInfo = GameCore.Instance.MeInfo;

        // PVP 胜负场统计
        if (winner == GameCore.Instance.MeID)
            meInfo.WinCount++;
        else if (winner != null)
            meInfo.LoseCount++;
    }

    // 增加指定资源
    protected override void NotifyResourceChanged(int p, string resourceType, Fix64 delta, Fix64 total)
    {
        OnResourceChanged.SC(p, resourceType, total);
    }

    // 通知资源生产
    public override void OnProduceResource(Unit u, string resType, Fix64 num)
    {
        OnResourceProduced.SC(u, resType, num);
    }

    //// 设置行进目标
    //void OnSetPathMsg(IReadableBuffer data)
    //{
    //    var uid = data.ReadString();
    //    var path = data.ReadVec2Arr();
    //    var u = GetUnit(uid);
    //    u.ResetPath(path);
    //    OnSetPath.SC(u);
    //}

    // 开始建造一个建筑单位
    void OnCosntructBuildingUnitMsg(IReadableBuffer data)
    {
        var constructUnitType = data.ReadString();
        var player = data.ReadInt();
        var pos = data.ReadVec2();
        ConstructBuilding(constructUnitType, pos, player);
    }

    // 开始建造矿机
    void OnConstructCrystalMachineMsg(IReadableBuffer data)
    {
        var baseUID = data.ReadString();
        var bs = GetUnit(baseUID);
        ConstructingCrystalMachine(bs);
    }

    // 开始改建一个建筑单位
    void OnRecosntructBuildingMsg(IReadableBuffer data)
    {
        var uid = data.ReadString();
        var toNewType = data.ReadString();

        if (GetUnit(uid) == null)
            return;
        
        ReconstructBuilding(GetUnit(uid), toNewType);
    }

    // 开始建造附件
    void OnConstructAccessoryMsg(IReadableBuffer data)
    {
        var uid = data.ReadString();
        var type = data.ReadString();

        if (GetUnit(uid) == null)
            return;
        
        ConstructAccessory(GetUnit(uid), type);
    }

    // 取消建筑建造
    void OnCancelBuildingMsg(IReadableBuffer data)
    {
        var uid = data.ReadString();
        var u = GetUnit(uid);
        if (u == null)
            return;

        CancelBuilding(u);
    }

    // 拆毁建筑
    void OnDestroyBuildingMsg(IReadableBuffer data)
    {
        var uid = data.ReadString();
        var u = GetUnit(uid);
        if (u == null)
            return;

        DestroyBuilding(u);
    }

    // 投放伞兵
    void OnDropSoldierFromCarrierMsg(IReadableBuffer data)
    {
        var type = data.ReadString();
        var player = data.ReadInt();
        var dropPt = data.ReadVec2();

        if (!CreateSoldierCarrier(type, player, dropPt))
            return;
    }

#region 编辑器内测试使用

    // 直接添加单位
    void OnAddUnitAtMsg(IReadableBuffer data)
    {
        var player = data.ReadInt();
        var type = data.ReadString();
        var num = data.ReadInt();
        var pos = data.ReadVec2();
        FC.For(num, (i) => { AddNewUnit(null, type, pos, player); });
    }

    // 直接建造建筑
    void OnAddBuildingUnit4TestAnyway(IReadableBuffer data)
    {
        var player = data.ReadInt();
        var genType = data.ReadString();
        var pos = new Vec2(data.ReadInt(), data.ReadInt());

        var building = AddNewUnit(null, genType, pos, player, true);
        if (building != null)
            building.Hp = building.cfg.MaxHp;
    }

    // 直接建造空降兵
    void OnAddSoldierCarrierUnit4TestAnyway(IReadableBuffer data)
    {
        var player = data.ReadInt();
        var type = data.ReadString();
        var dropPt = new Vec2(data.ReadInt(), data.ReadInt());

        var fromPt = player == 1 ? new Vec2(30, 5) : new Vec2(30, 235);
        var dir = dropPt - fromPt;
        var mr = MapSize.x > MapSize.y ? MapSize.x : MapSize.y;
        dir = dir.Length > 1 ? dir / dir.Length : Vec2.Zero;
        var toPt = dropPt + dir * mr;
        var stc = AddNewUnit(null, type, fromPt, player);

        var cfg = UnitConfiguration.GetDefaultConfig(type);
        var pType = cfg.Pets[0];
        var pCnt = int.Parse(cfg.Pets[1]);
        FC.For(pCnt, (i) => { stc.UnitCosntructingWaitingList.Add(pType); });

        stc.Dir = dir.Dir();
        stc.MovePath.Add(dropPt);
        stc.MovePath.Add(toPt);
    }

    // 异常报错
    void OnCrashMsg(IReadableBuffer data)
    {
        var exMsg = data.ReadString();
        UIManager.Instance.Tips.AddErrorMsg("战斗报错：" + exMsg);
    }

    // 直接添加单位
    void OnAddBattleUnit4TestAnyway(IReadableBuffer data)
    {
        var player = data.ReadInt();
        var type = data.ReadString();
        var pos = new Vec2(data.ReadInt(), data.ReadInt());
        AddNewUnit(null, type, pos, player);
    }

#endregion

    void OnAddBattleUnitAtMsg(IReadableBuffer data)
    {
        var p = data.ReadInt();
        var t = data.ReadString();
        var pt = data.ReadVec2();

        var cfg = UnitConfiguration.GetDefaultConfig(t);
        
        AddResource(p, "Money", -cfg.Cost);
        AddResource(p, "Gas", -cfg.GasCost);
        AddNewUnit(null, t, pt, p);
    }

    // 交换 usr1/usr2 对应的 player number
    void OnPlayersSwitchedMsg(IReadableBuffer data)
    {
        var me = GameCore.Instance.MePlayer;
        GameCore.Instance.MePlayer = me == 1 ? 2 : 1;
        OnUsrsSwitched.SC();
    }

    // 作弊代码
    void OnCheatCodeMsg(IReadableBuffer data)
    {
        var code = data.ReadString();
        switch (code)
        {
            case "ShowMeTheMoney":
                {
                    var uid = data.ReadString();
                    var num = data.ReadInt();
                    var p = GetNoByUser(uid);
                    var money = Players[p].Resources["Money"];
                    money += num;
                    Players[p].Resources["Money"] = money;
                    OnResourceChanged.SC(p, "Money", money);
                }
                break;
            default:
                throw new Exception("do not support any cheat code yet");
        }
    }

    // 通知建筑完成
    public override void OnBuildingConstructingCompleted(Unit u)
    {
        base.OnBuildingConstructingCompleted(u);
        OnConstructingCompleted.SC(u);
    }

    // 通知建造取消
    protected override void OnUnitConstructingCanceled(Unit u)
    {
        base.OnUnitConstructingCanceled(u);
        OnConstructingCanceled.SC(u);
    }

    protected override void OnUnitAdded(Unit building, Unit u)
    {
        if (u.cfg.IsBuilding)
            NotifyAddBuildingUnit.SC(u);
        else
            NotifyAddBattleUnit.SC(building, u);
    }

    protected override void OnUnitReconstruct(Unit u, string fromType)
    {
        NotifyReconstructUnit.SC(u, fromType);
    }

    protected override void OnUnitRemoved(Unit u)
    {
        NotifyUnitRemoved.SC(u);
    }

    public bool IsMine(Unit u)
    {
        return u.Player != GameCore.Instance.MePlayer;
    }

    public Unit[] GetAllMyUnits(Func<Unit, bool> filter = null)
    {
        return GetAllUnitsByPlayer(GameCore.Instance.MePlayer, filter);
    }

    // 获取指定资源数量
    public Fix64 GetMyResource(string resourceType)
    {
        PlayerInfoInRoom pi = Players[GameCore.Instance.MePlayer];
        return pi.Resources.ContainsKey(resourceType) ? pi.Resources[resourceType] : 0;
    }

    public Unit[] GetMyUnitsByType(string unitType, Func<Unit, bool> filter = null)
    {
        return GetUnitsByType(unitType, GameCore.Instance.MePlayer, filter);
    }

    public Unit GetMyFirstUnitByType(string unitType)
    {
        foreach (var u in AllUnits)
        {
            if (u.Player != GameCore.Instance.MePlayer)
                continue;

            if (!u.cfg.IsBuilding)
            {
                if (u.UnitType == unitType)
                    return u;
            }
            else if (u.BuildingCompleted && u.UnitType == unitType)
                return u;
            else if (!u.BuildingCompleted && u.cfg.ReconstructFrom == unitType)
                return u;
        }

        return null;
    }

#region 战斗相关

    // 单体攻击
    public override Unit[] DoSingleAttack(Unit attacker, Unit target)
    {
        var tars = base.DoSingleAttack(attacker, target);
        OnDoAttack.SC(attacker, target);
        return tars;
    }

    // 圆形攻击
    public override Unit[] DoAOEAttackCircle(Unit attacker, Unit target)
    {
        var tars = base.DoAOEAttackCircle(attacker, target);
        OnDoAttack.SC(attacker, target);
        return tars;
    }

    // 扇形攻击
    public override Unit[] DoAOEAttackFan(Unit attacker, Unit target)
    {
        var tars = base.DoAOEAttackFan(attacker, target);
        OnDoAttack.SC(attacker, target);
        return tars;
    }

#endregion

    // 客户端在战局内添加额外的状态机
    public void AddStateMachineInRoom(StateMachine sm)
    {
        smm.Add(sm);
    }
}
