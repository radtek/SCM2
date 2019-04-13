using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 房间内的玩家数据
    /// </summary>
    public class PlayerInfoInRoom
    {
        // 各种资源数量
        public StableDictionary<string, Fix64> Resources = new StableDictionary<string, Fix64>();

        public Fix64 this[string resourceType]
        {
            get
            {
                if (!Resources.ContainsKey(resourceType))
                    Resources[resourceType] = 0;

                return Resources[resourceType];
            }
            set
            {
                Resources[resourceType] = value;
            }
        }
    }

    /// <summary>
    /// 一场战斗一个房间
    /// </summary>
    public class Room
    {
        #region 帧同步管理

        SRandom rand = null;
        public const int FrameInterval = 100; // 一帧逻辑帧的时间长度(毫秒)

        // 帧编号
        ulong frameSeqNo = 0;
        public ulong FrameNo { get { return frameSeqNo; } }

        // 随机种子
        public int RandomSeed { get { return rand.Seed; } }

        // 获取下一个随机数
        public int RandomNext(int min, int max) { return rand.Next(min, max); }

        // 获取下一个序列 ID
        protected string NextUniqueID { get { return ID + "_" + (++idSeq); } }
        int idSeq = 0;

        #endregion

        // 每个 Room 自己单独一个状态机管理器
        protected StateMachineManager smm = new StateMachineManager();

        // 房间 ID，全球唯一
        public string ID { get; protected set; }

        // 对战双方的 UseID
        public string[] UsrsID { get; protected set; }

        // 双方 UserInfo
        public UserInfo[] UsrsInfo { get; protected set; }

        // 双方的局内信息
        public PlayerInfoInRoom[] Players { get; protected set; }

        // 战斗地图
        protected Map Map { get; private set; }
        public Vec2 MapSize { get { return Map == null ? Vec2.Zero : Map.Size; } }

        #region 房间流程

        // 战斗是否结束
        public bool Finished { get; protected set; }
        public string Winner { get; protected set; }

        // 关卡信息
        public Level Lv { get; protected set; }

        public bool IsPVP { get; protected set; }

        public virtual void Init(string roomID, Vec2 mapSize, string mapID, string usr1, string usr2, UserInfo usrInfo1, UserInfo usrInfo2)
        {
            ID = roomID;

            var num = 3;

            UsrsID = new string[num];
            UsrsID[0] = null; // 0 表示中立单位
            UsrsID[1] = usr1;
            UsrsID[2] = usr2;

            UsrsInfo = new UserInfo[num];
            UsrsInfo[0] = new UserInfo();
            UsrsInfo[1] = usrInfo1;
            UsrsInfo[2] = usrInfo2;

            Players = new PlayerInfoInRoom[num];
            Players[0] = null;
            Players[1] = new PlayerInfoInRoom();
            Players[2] = new PlayerInfoInRoom();

            Lv = LevelCreator.GetLevel(mapID);

            BuffRunner = new BuffRunner();
            BuffRunner.Room = this;

            TBRunner = new TreasureBoxRunner();
            TBRunner.Room = this;

            Map = new Map(mapSize, FrameInterval / 1000.0f);
            Map.Room = this;
            frameSeqNo = 0;

            smm.Clear();
        }

        // 推进一步数据逻辑
        public const int MaxFrameNum = 9000; // 最多 15 分钟一局
        public virtual void MoveOneStep()
        {
            if (frameSeqNo >= MaxFrameNum)
            {
                BattleEnd(null);
                return;
            }

            frameSeqNo++;

            // 目标已死亡就从地图移除
            foreach (var u in AllUnits)
            {
                if (u.Hp <= 0)
                {
                    RemoveUnit(u.UID);
                    foreach (var acc in u.Accessories.ToArray())
                        RemoveUnit(acc.UID);
                }
            }

            smm.OnTimeElapsed(FrameInterval);

            Map.DoOneStep(FrameInterval);

            // 宝箱和 buff

            if (TBRunner != null)
                TBRunner.OnTimeElapsed(FrameInterval);

            if (BuffRunner != null)
                BuffRunner.OnTimeElapsed(FrameInterval);
        }

        // 战斗开始
        public virtual void BattleBegin(int randomSeed)
        {
            rand = new SRandom(randomSeed);
            Winner = null;
            Finished = false;
            frameSeqNo = 0;

            InitBattle();
        }

        // 初始化战斗内容
        protected virtual void InitBattle()
        {
            AddResource(1, "Money", 0);
            AddResource(2, "Money", 0);
            AddResource(1, "Suppliment", 0);
            AddResource(2, "Suppliment", 0);
            AddResource(1, "SupplimentLimit", 30);
            AddResource(2, "SupplimentLimit", 30);

            Lv.Init(this);
        }

        public UserInfo GetUserInfo(string uid)
        {
            if (uid == UsrsID[1])
                return UsrsInfo[1];
            else if (uid == UsrsID[2])
                return UsrsInfo[2];
            else
                return null;
        }

        // 检查结束条件，返回 winner player
        public virtual int CheckWinner()
        {
            return Lv.CheckWinner(this);
        }

        // 战斗结束
        public virtual void BattleEnd(string winner)
        {
            Winner = winner;
            Finished = true;
        }

        // 刷新指定单位的 AI
        public void RefreshAI(Unit u)
        {
            smm.Del(u.UID);
            var sm = u.CreateAI();
            if (sm != null)
                smm.Add(sm);
        }

        // 获取指定单位的 AI 状态机
        public StateMachine GetSM(Unit u)
        {
            return smm.Get(u.UID);
        }

        #endregion

        #region 房间内信息查询

        // 获取所有指定玩家指定类型的单位
        public Unit[] GetUnitsByType(string unitType, int player, Func<Unit, bool> filter = null)
        {
            return FC.Select(AllUnits, (u) => u.Player == player && u.UnitType == unitType
                && (filter == null || filter(u))).ToArray();
        }

        // 获取对应 user 的编号
        public int GetNoByUser(string usr)
        {
            return Array.IndexOf(UsrsID, usr);
        }

        // 获取指定用户的所有场景单位
        public Unit[] GetAllUnitsByUser(string usr, Func<Unit, bool> filter = null) { return GetAllUnitsByPlayer(GetNoByUser(usr), filter); }
        public Unit[] GetAllUnitsByPlayer(int player, Func<Unit, bool> filter = null)
        {
            var lst = new List<Unit>();
            foreach (var u in Map.AllUnits)
            {
                if (u.Player == player && (filter == null || filter(u)))
                    lst.Add(u);
            }

            return lst.ToArray();
        }

        // 是否存在满足指定条件的单位
        public bool ExistsUnit(int player, Func<Unit, bool> filter)
        {
            foreach (var u in AllUnits)
            {
                if (u.Player == player && filter(u))
                    return true;
            }

            return false;
        }

        // 获取指定 uid 的单位
        public Unit GetUnit(string uid)
        {
            return Map.Get(uid);
        }

        // 所有房间内单位
        public Unit[] AllUnits
        {
            get
            {
                return Map.AllUnits;
            }
        }

        // 获取指定资源数量
        public Fix64 GetResource(int p, string resourceType)
        {
            PlayerInfoInRoom pi = p == 1 ? Players[1] : Players[2];
            return pi.Resources.ContainsKey(resourceType) ? pi.Resources[resourceType] : 0;
        }

        // 获取指定圆形范围内满足条件的单位
        public Unit[] GetUnitsInArea(Vec2 center, Fix64 r, Func<Unit, bool> filter)
        {
            return FC.Select(AllUnits,
                (u) => (filter == null || filter(u))
                    && (u.Pos - center).Length - u.cfg.SizeRadius <= r).ToArray();
        }

        // 获取指定矩形范围内满足条件的单位
        public Unit[] GetUnitInFanArea(Vec2 fanCenter, Fix64 fanR, Fix64 fanDir, Fix64 fanAngle, Func<Unit, bool> filter)
        {
            return FC.Select(AllUnits,
                (u) => (filter == null || filter(u))
                    && MU.IsFunOverlappedCircle(u.Pos, u.cfg.SizeRadius, fanCenter, fanR, fanDir, fanAngle)).ToArray();
        }

        public bool CheckSpareSpace(Vec2 center, int radius, string asEmptyUID = null) { return CheckSpareSpace((int)center.x, (int)center.y, radius, asEmptyUID); }
        public bool CheckSpareSpace(int cx, int cy, int radius, string asEmptyUID = null)
        {
            return Map.CheckSpareSpace(false, cx, cy, radius, asEmptyUID);
        }

        // 寻找离指定位置最近的一个可容纳指定半径单位的点，从离中心 fromDistance 的距离开始找
        public bool FindNearestSpareSpace(Vec2 center, int radius, int fromDistance, out Vec2 pt)
        {
            return Map.FindNearestSpareSpace(false, center, radius, fromDistance, out pt);
        }

        #endregion

        #region 房间内信息修改

        // 销毁房间内所有对象
        public virtual void Clear()
        {
            if (Map != null)
            {
                foreach (var u in AllUnits)
                {
                    smm.Del(u.UID);
                    u.Room = null;
                }

                Map.Clear();
            }

            if (BuffRunner != null)
                BuffRunner.Clear();

            if (TBRunner != null)
                TBRunner.Clear();

            UsrsID = null;
            Players = null;
            frameSeqNo = 0;
            rand = null;
            idSeq = 0;
        }

        // 设置资源数量
        void SetResource(int p, string resourceType, Fix64 num)
        {
            PlayerInfoInRoom pi = Players[p];
            pi.Resources[resourceType] = num;
        }

        // 增加指定资源
        protected virtual void NotifyResourceChanged(int p, string resourceType, Fix64 delta, Fix64 total) { }
        public Fix64 AddResource(int p, string rType, Fix64 delta)
        {
            // 检查一下上下限
            var r = GetResource(p, rType) + delta;
            var maxKey = rType + "_Max";
            var max = GetResource(p, maxKey);
            if (max > 0 && r > max)
                r = max;
            else if (r < 0)
                r = 0;

            SetResource(p, rType, r);
            NotifyResourceChanged(p, rType, delta, r);
            return r;
        }

        Unit NewUnit(string type, Vec2 pos, int player)
        {
            if (pos.x < 0)
                pos.x = 0;
            else if (pos.x >= MapSize.x)
                pos.x = MapSize.x - 1;

            if (pos.y < 0)
                pos.y = 0;
            else if (pos.y >= MapSize.y)
                pos.y = MapSize.y - 1;

            var u = UnitFactory.Instance.Create(NextUniqueID);
            u.Room = this;
            u.Player = player;
            u.UnitType = type;
            u.Hp = u.cfg.MaxHp;
            u.Pos = pos;
            u.Dir = player == 2 ? 270 : 90;
            return u;
        }

        // 创建一个新的单位并放置在指定位置
        public Unit AddNewUnit(Unit building, string type, Vec2 pos, int player, bool buildingCompleted = false)
        {
            var u = NewUnit(type, pos, player);
            u.BuildingCompleted = buildingCompleted;
            if (buildingCompleted)
                u.Hp = u.cfg.MaxHp;

            if (type != "SoldierCarrier" && type != "RobotCarrier" && u.cfg.Pets != null)
            {
                var pt = u.cfg.Pets[0];
                var cnt = int.Parse(u.cfg.Pets[1]);

                for (var i = 0; i < cnt; i++)
                {
                    AddNewUnit(null, pt, pos, player);
                }
            }

            return AddUnitAt(building, u) ? u : null;
        }

        // 添加单位到指定位置
        protected virtual void OnUnitAdded(Unit building, Unit u) { }
        public bool AddUnitAt(Unit building, Unit u)
        {
            if (!u.cfg.NoBody && !u.cfg.NoCard && !Map.CheckSpareSpace(u.cfg.IsAirUnit, (int)u.Pos.x, (int)u.Pos.y, u.cfg.SizeRadius))
            {
                Vec2 newPt = Vec2.Zero;
                if (!Map.FindNearestSpareSpace(u.cfg.IsAirUnit, u.Pos, u.cfg.SizeRadius, 0, out newPt))
                    return false;

                u.Pos = newPt;
            }

            u.Dir = u.ForwardDir;

            if (u.cfg.IsAirUnit)
                Map.AddUnitInAir(u);
            else
                Map.AddUnitAt(u);

            if (!u.IsNeutral)
            {
                // 人口数也要调整
                var suppliment = u.cfg.Suppliment;
                AddResource(u.Player, "Suppliment", suppliment);
            }

            u.Room = this;
            var sm = u.CreateAI();
            if (sm != null)
                smm.Add(sm);

            BuffRunner.OnUnitAdded(u);
            OnUnitAdded(building, u);
            return true;
        }

        // 移除指定单位
        protected virtual void OnUnitRemoved(Unit u) { }
        Unit RemoveUnit(string uid)
        {
            smm.Del(uid);
            var u = GetUnit(uid);
            u.Room = null;
            Map.RemoveUnit(uid);

            u.Owner = null;

            // 人口数也要调整
            if (!u.IsNeutral)
            {
                var suppliment = u.cfg.Suppliment;
                AddResource(u.Player, "Suppliment", -suppliment);
            }

            BuffRunner.OnUnitRemoved(u);
            OnUnitRemoved(u);
            return u;
        }

        #endregion

        #region 建造相关        

        // 检查前置单位条件是否具备
        public bool CheckPrerequisites(int player, string unitType)
        {
            var cfg = UnitConfiguration.GetDefaultConfig(unitType);

            if (cfg.Prerequisites == null || cfg.Prerequisites.Length == 0)
                return true;

            foreach (var rs in cfg.Prerequisites)
            {
                var satisfied = true;
                foreach (var r in rs)
                {
                    if (r[0] == '-') // 指示 “必须没有某单位” 这种条件，建筑的互斥条件不需要等造完
                    {
                        if (ExistsUnit(player, (u) => ((u.BuildingCompleted || cfg.IsBuilding) && r.EndsWith(u.UnitType))
                                || (u.cfg.ReconstructFrom != null && !u.BuildingCompleted && r.EndsWith(u.cfg.ReconstructFrom))))
                        {
                            satisfied = false;
                            break;
                        }
                    } // 默认是 “必须有某单位” 这种条件
                    else if (!ExistsUnit(player, (u) => (u.UnitType == r && u.BuildingCompleted)
                                || (u.cfg.ReconstructFrom != null && !u.BuildingCompleted && u.cfg.ReconstructFrom == r)))
                    {
                        satisfied = false;
                        break;
                    }
                }

                if (satisfied)
                    return true;
            }

            return false;
        }

        // 确定下一个矿机的位置
        public Vec2 FindNextAccessoryPos(Unit ownerBuilding, string type)
        {
            var poses = new Vec2[]
            {
                new Vec2(Fix64.Cos(Fix64.Pi / 4),  Fix64.Sin(Fix64.Pi / 4)), 
                new Vec2(Fix64.Cos(Fix64.Pi * 5 / 4),  Fix64.Sin(Fix64.Pi * 5 / 4)), 
            };

            foreach (var p in poses)
            {
                var pos = ownerBuilding.Pos + p * ownerBuilding.cfg.SizeRadius;
                if (GetUnitsInArea(pos, 1, (u) => u.UnitType == type).Length == 0)
                    return pos;
            }

            return Vec2.Zero;
        }

        // 开始建造挂件单位
        public Unit ConstructAccessory(Unit ownerBuilding, string type)
        {
            var player = ownerBuilding.Player;

            var cfg = UnitConfiguration.GetDefaultConfig(type);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;
            if (GetResource(player, "Money") < cost || GetResource(player, "Gas") < gasCost)
                return null;
            else if (cfg.Prerequisites != null && GetAllUnitsByPlayer(player,
                    (u) => u.BuildingCompleted && CheckPrerequisites(player, u.UnitType)).Length == 0)
                return null;

            var pt = FindNextAccessoryPos(ownerBuilding, type);
            if (pt == Vec2.Zero)
                return null;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);
            var acc = AddNewUnit(null, type, pt, player);
            if (acc != null)
                acc.Hp = acc.cfg.MaxHp * 0.1; // 从 10% 的血量开始建造

            acc.Owner = ownerBuilding;
            return acc;
        }

        // 开始建造建筑
        public Unit ConstructBuilding(string genType, Vec2 pos, int player)
        {
            var cfg = UnitConfiguration.GetDefaultConfig(genType);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;
            if (GetResource(player, "Money") < cost || GetResource(player, "Gas") < gasCost)
                return null;
            else if (cfg.Prerequisites != null && GetAllUnitsByPlayer(player,
                    (u) => u.BuildingCompleted && CheckPrerequisites(player, u.UnitType)).Length == 0)
                return null;
            else if (!cfg.NoBody && !cfg.NoCard && !CheckSpareSpace(pos, cfg.SizeRadius))
                return null;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);
            var building = AddNewUnit(null, genType, pos, player);
            if (building != null)
                building.Hp = building.cfg.MaxHp * 0.1; // 从 10% 的血量开始建造

            return building;
        }

        // 开始改建建筑
        protected virtual void OnUnitReconstruct(Unit u, string fromType) { }
        public bool ReconstructBuilding(Unit u, string toNewType)
        {
            var player = u.Player;

            if (!CheckPrerequisites(player, toNewType))
                return false;

            var cfg = UnitConfiguration.GetDefaultConfig(toNewType);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;
            if (GetResource(player, "Money") < cost || GetResource(player, "Gas") < gasCost)
                return false;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);

            // 改变建筑类型
            u.BuildingCompleted = false;
            var oldType = u.UnitType;
            u.UnitType = toNewType;
            RefreshAI(u);
            OnUnitReconstruct(u, oldType);
            return true;
        }

        // 取消建造
        protected virtual void OnUnitConstructingCanceled(Unit u) { }
        public void CancelBuilding(Unit u)
        {
            if (u.BuildingCompleted)
                return;

            var cost = u.cfg.Cost;
            var gasCost = u.cfg.GasCost;

            var refund = (int)(cost * 0.8f);
            var gasRefund = (int)(gasCost * 0.8f);

            AddResource(u.Player, "Money", refund);
            AddResource(u.Player, "Gas", gasRefund);

            if (u.cfg.ReconstructFrom != null)
            {
                u.UnitType = u.cfg.ReconstructFrom;
                u.BuildingCompleted = true;
                RefreshAI(u);
            }
            else
                u.Hp = 0;

            OnUnitConstructingCanceled(u);
        }

        // 确定下一个矿机的位置
        public Vec2 FindNextCrystalMachinePos(Unit bs)
        {
            var poses = new Vec2[] { 
                new Vec2(1.4f * Fix64.Cos(0), 1.4f * Fix64.Sin(0)),
                new Vec2(1.4f * Fix64.Cos(Fix64.Pi / 3), 1.4f * Fix64.Sin(Fix64.Pi / 3)),
                new Vec2(1.4f * Fix64.Cos(Fix64.Pi * 2 / 3), 1.4f * Fix64.Sin(Fix64.Pi * 2 / 3)),
                new Vec2(1.4f * Fix64.Cos(Fix64.Pi), 1.4f * Fix64.Sin(Fix64.Pi)),
                new Vec2(1.4f * Fix64.Cos(Fix64.Pi * 4 / 3), 1.4f * Fix64.Sin(Fix64.Pi * 4 / 3)),
                new Vec2(1.4f * Fix64.Cos(Fix64.Pi * 5 / 3), 1.4f * Fix64.Sin(Fix64.Pi * 5 / 3)),
            };
            foreach (var p in poses)
            {
                var pos = bs.Pos + p * bs.cfg.SizeRadius;
                if (GetUnitsInArea(pos, 1, (u) => u.UnitType == "CrystalMachine").Length == 0)
                    return pos;
            }

            return Vec2.Zero;
        }

        // 开始建造矿机
        public Unit ConstructingCrystalMachine(Unit u)
        {
            // 有数量限制
            var pos = FindNextCrystalMachinePos(u);
            if (pos == Vec2.Zero)
                return null;

            var cm = ConstructBuilding("CrystalMachine", pos, u.Player);
            if (cm == null)
                return null;

            cm.Owner = u;
            return cm;
        }

        // 通知部队单位进入建造列表
        public virtual void NotifyConstructingWaitingListChanged(Unit building, string genType) { }

        // 空投伞兵到指定位置，路径随机
        public bool CreateSoldierCarrier(string type, int player, Vec2 dropPt)
        {
            var cfg = UnitConfiguration.GetDefaultConfig(type);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;
            if (GetResource(player, "Money") < cost || GetResource(player, "Gas") < gasCost)
                return false;
            else if (cfg.Prerequisites != null && GetAllUnitsByPlayer(player,
                    (u) => u.BuildingCompleted && CheckPrerequisites(player, u.UnitType)).Length == 0)
                return false;

            var aps = GetUnitsByType(UnitConfiguration.GetMainBuilder(type), player);
            if (aps.Length == 0)
                return false;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);

            // 随机从一个机场出发
            var fromPt = aps[RandomNext(0, aps.Length)].Pos;
            var dir = dropPt - fromPt;
            var mr = MapSize.x > MapSize.y ? MapSize.x : MapSize.y;
            dir = dir.Length > 1 ? dir / dir.Length : Vec2.Zero;
            var toPt = dropPt + dir * mr;
            var stc = AddNewUnit(null, type, fromPt, player);

            var pType = cfg.Pets[0];
            var pCnt = int.Parse(cfg.Pets[1]);
            FC.For(pCnt, (i) => { stc.UnitCosntructingWaitingList.Add(pType); });
            stc.Dir = dir.Dir();
            stc.MovePath.Add(dropPt);
            stc.MovePath.Add(toPt);

            return true;
        }

        // 通知生产资源
        public virtual void OnProduceResource(Unit u, string resType, Fix64 num) { }

        // 通知建筑建造完成
        public virtual void OnBuildingConstructingCompleted(Unit u) { u.Room.RefreshAI(u); }

        // 通知建筑销毁过程
        public virtual void DestroyBuilding(Unit u)
        {
            u.InDestroying = true;
            RefreshAI(u);
        }

        public virtual void OnBuildingDestoyingProcess(Unit u, Fix64 dMoney, Fix64 dGas)
        {
            AddResource(u.Player, "Money", dMoney);
            AddResource(u.Player, "Gas", dGas);
        }

        #endregion

        #region 攻击相关

        // 执行攻击行为
        public virtual Unit[] DoAttack(Unit attacker, Unit target)
        {
            var AOEType = attacker.cfg.AOEType == null ? null :
                (target.cfg.IsAirUnit ? attacker.cfg.AOEType[1] : attacker.cfg.AOEType[0]);

            if (AOEType == null)
                return DoSingleAttack(attacker, target);
            else if (AOEType == "fan")
                return DoAOEAttackFan(attacker, target);
            else if (AOEType == "circle")
                return DoAOEAttackCircle(attacker, target);
            else // if (AOEType == "line")
                throw new Exception("not implemented yet");
        }

        // 单体攻击
        public virtual Unit[] DoSingleAttack(Unit attacker, Unit target)
        {
            var tars = new Unit[] { target };
            MakeDamage(attacker, tars);
            return tars;
        }

        // 圆形攻击
        public virtual Unit[] DoAOEAttackCircle(Unit attacker, Unit target)
        {
            var ps = target.cfg.IsAirUnit ? attacker.cfg.AOEParams[1] : attacker.cfg.AOEParams[0];
            var targets = new List<Unit>();
            targets.Add(target); // place main target as first one
            var ts = GetUnitsInArea(target.Pos, ps[0], (u) => attacker.CanAttack(u) && u.Hp > 0 && u.cfg.IsAirUnit == target.cfg.IsAirUnit);
            foreach (Unit t in ts)
                if (!targets.Contains(t))
                    targets.Add(t);

            var tars = targets.ToArray();
            MakeDamage(attacker, tars);
            return tars;
        }

        // 扇形攻击
        public virtual Unit[] DoAOEAttackFan(Unit attacker, Unit target)
        {
            var ps = target.cfg.IsAirUnit ? attacker.cfg.AOEParams[1] : attacker.cfg.AOEParams[0];
            var dir = MU.v2Degree(target.Pos.x - attacker.Pos.x, target.Pos.y - attacker.Pos.y);
            var targets = new List<Unit>();
            targets.Add(target); // place main target as first one
            var ts = GetUnitInFanArea(attacker.Pos,
                ps[0], dir, ps[1], (u) => attacker.CanAttack(u) && u.Hp > 0 && u.cfg.IsAirUnit == target.cfg.IsAirUnit);
            foreach (Unit t in ts)
                if (!targets.Contains(t))
                    targets.Add(t);

            var tars = targets.ToArray();
            MakeDamage(attacker, targets.ToArray());
            return tars;
        }

        // 计算伤害
        public virtual void MakeDamage(Unit attacker, Unit[] targets)
        {
            foreach (var t in targets)
            {
                var pn = t.cfg.IsAirUnit ? 1 : 0;
                var attackType = attacker.cfg.AttackType[pn];
                var power = attacker.Power[pn];
                var d = (power - t.Defence).Clamp(1, Fix64.MaxValue);

                var armorType = t.cfg.ArmorType;
                if (attackType == "light" && armorType == "light") // 穿刺-轻甲 加成
                    d = d * 1.5f;
                else if (attackType == "light" && armorType == "heavy") // 穿刺-重甲 削弱
                    d = d * 0.5f;
                else if (attackType == "heavy" && armorType == "heavy") // 重击-重甲 加成
                    d = d * 1.5f;

                t.Tag = attacker;

                d = d.Clamp(1, Fix64.MaxValue);

                var td = (int)d;

                t.OnDamage(td);

                var trd = t.cfg.ReboundDamage;

                if (trd > 0)
                {
                    var rd = (int)(td * trd);
                    attacker.OnDamage(rd);
                }
            }
        }

        #endregion

            #region 宝箱和 buff 相关

            // 局内 buff 管理
        public BuffRunner BuffRunner { get; protected set;}

        // 宝箱投放
        public TreasureBoxRunner TBRunner { get; protected set; }

        #endregion

        // 寻路
        public List<Vec2> FindPath(Unit u, Vec2 dst, string targetUID = null)
        {
            return Map.FindPath(u, dst, targetUID);
        }

        //public void OnUnitMovedFrom(Unit u, Vec2 srcPos)
        //{
        //    Map.OnUnitMovedFrom(u, srcPos);
        //}
    }
}
