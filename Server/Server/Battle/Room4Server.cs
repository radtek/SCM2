using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Swift;
using SCM;
using Swift.Math;

namespace Server
{
    /// <summary>
    /// 战斗房间
    /// </summary>
    public class Room4Server : Room
    {
        public static Action<Room4Server, string> OnBattleEnded;

        // 所有房间内的用户会话，包括玩家和观战用户
        List<Session> ss = new List<Session>();

        public int WinnerAward = 0; // 胜者奖励
        public int LoserAward = 0; // 败者奖励
        public string[] WinnerAwardUnits; // 胜者的奖励单位
        public bool IsPVP;

        // PVE room
        public Room4Server(string id, string aiID, Session s, Vec2 mapSize, string mapID)
        {
            var aiInfo = new UserInfo();
            aiInfo.Name = UnitConfigManager.RandomComputerOpponentName();
            Init(id, mapSize, mapID, aiID, s.ID, aiInfo, s.Usr.Info);
            AddSession(s);
        }

        // PVP room
        public Room4Server(string id, Session s1, Session s2, Vec2 mapSize, string mapID)
        {
            Init(id, mapSize, mapID, s1.ID, s2.ID, s1.Usr.Info, s2.Usr.Info);
            AddSession(s1);
            AddSession(s2);
        }

        // 进人
        public void AddSession(Session s)
        {
            if (ss.Contains(s))
                return;

            ss.Add(s);
        }

        // 走人
        public void RemoveSession(Session s)
        {
            if (!ss.Contains(s))
                return;

            ss.Remove(s);
        }

        // 广播消息给房间内所有人
        public void Broadcast(string op, Action<IWriteableBuffer> cb)
        {
            foreach (var s in ss)
            {
                if (s == null)
                    continue;

                s.Conn.Send2Usr(op, cb);
            }

            var wb = new WriteBuffer(true);
            cb.SC(wb);

            var rb = new RingBuffer(true, true);
            rb.Write(wb.Data, 0, wb.Available);
            currentBattleMsgHistory.Add(new KeyValuePair<string, IReadableBuffer>(op, rb));
        }

        // 战斗开始
        public override void BattleBegin(int randomSeed)
        {
            base.BattleBegin(randomSeed);
            timeElapsed = 0;
            currentBattleMsgHistory.Clear();
            Broadcast("BattleBegin", (buff) =>
            {
                buff.Write(randomSeed);
                buff.Write(Lv.LevelID);
                buff.Write(WinnerAward);
                buff.Write(LoserAward);
                buff.Write(WinnerAwardUnits);
                buff.Write(ID);
                buff.Write(UsrsID);
                buff.Write(UsrsInfo);
                buff.Write(Map.Size);
                buff.Write(IsPVP);
            });

            if (ComputerAI != null)
                ComputerAI.Start();
        }

        // 战斗结束
        public override void BattleEnd(string winner)
        {
            if (ComputerAI != null)
                ComputerAI.Destroy();

            base.BattleEnd(winner);

            // 记录胜负场次
            UserInfo winnerInfo = null;
            UserInfo loserInfo = null;
            if (winner != null /* null 是平局情况，算两人都输 */)
            {
                if (winner == UsrsID[1])
                {
                    winnerInfo = UsrsInfo[1];
                    loserInfo = UsrsInfo[2];
                }
                else if (winner == UsrsID[2])
                {
                    winnerInfo = UsrsInfo[2];
                    loserInfo = UsrsInfo[1];
                }

                winnerInfo.WinCount++;
                loserInfo.LoseCount++;
            }
            else
            {
                winnerInfo = UsrsInfo[1];
                loserInfo = UsrsInfo[2];
            }

            // 发放战斗奖励
            foreach (var s in ss)
            {
                if (s.ID != UsrsID[1] && s.ID != UsrsID[2])
                    continue;

                s.Usr.Update();
            }

            // 广播结果
            Broadcast("BattleEnd", (buff) =>
            {
                buff.Write(winner);
            });

            // 保存录像
            var replayID = SaveCurrentBattleReplay(UsrsID[1], UsrsID[2], UsrsInfo[1].Name, UsrsInfo[2].Name, FrameNo, IsPVP, currentBattleMsgHistory).ID;
            winnerInfo.AddMyReplay(replayID);
            loserInfo.AddMyReplay(replayID);
        }

        // 逻辑时间流逝
        Fix64 timeElapsed = 0;
        public void OnTimeElapsed(int te)
        {
            if (Finished)
                return;

            timeElapsed += te;
            if (timeElapsed >= FrameInterval)
            {
                timeElapsed -= FrameInterval;

                // MoveOneStep 和发送 "FrameMoveForward" 消息之间不可插入任何其它逻辑，否则
                // 插入在中间的逻辑，和客户端数据会差一帧
                try
                {
                    MoveOneStep();
                }
                catch (Exception ex)
                {
                    Broadcast("Crash", (buff) => { buff.Write(ex.Message); });
                    Broadcast("BattleEnd", (buff) => { buff.Write(UsrsID[1]); });
                    SaveCurrentBattleReplay(UsrsID[1], UsrsID[2], UsrsInfo[1].Name, UsrsInfo[2].Name, FrameNo, IsPVP, currentBattleMsgHistory, true);
                    Finished = true;
                }
                
                Broadcast("FF", null); // (buff) => { buff.Write(FrameNo); });

                if (Finished) // 可能已经结束了
                    return;

                if (ComputerAI != null)
                    ComputerAI.OnTimeElapsed(te);
            }
        }

        #region 处理客户端消息

        Dictionary<string, Action<int, IReadableBuffer>> messageHandlers = null;
        public void OnMessage(string op, string usr, IReadableBuffer data)
        {
            if (messageHandlers == null)
            {
                messageHandlers = new Dictionary<string, Action<int, IReadableBuffer>>()
                {
                    // { "SetPath", OnSetPath },
                    { "AddBattleUnitAt", OnAddBattleUnitAt },
                    { "ConstructBuilding", OnConstructBuilding },
                    { "ConstructAccessory", OnConstructAccessory },
                    { "ConstructCrystalMachine", OnConstructCrystalMachine },
                    { "ReconstructBuilding", OnReconstructBuilding },
                    { "CancelBuilding", OnCancelBuilding },
                    { "DestroyBuilding", OnDestroyBuilding },
                    { "DropSoldierFromCarrier", OnDropSoldierFromCarrier },
                    { "AddBattleUnit4TestAnyway", OnAddBattleUnit4TestAnyway },
                    { "AddBuildingUnit4TestAnyway", OnAddBuildingUnit4TestAnyway },
                    { "AddSoldierCarrierUnit4TestAnyway", OnAddSoldierCarrierUnit4TestAnyway },
                };
            }

            var player = GetNoByUser(usr);
            messageHandlers[op](player, data);
        }

        // 设置行进目标
        //void OnSetPath(int player, IReadableBuffer data)
        //{
        //    var uid = data.ReadString();
        //    var u = GetUnit(uid);
        //    if (u == null || u.Player != player)
        //        return;

        //    var path = data.ReadVec2Arr();
        //    SrvSetPath(u, path);
        //}
        //public void SrvSetPath(Unit u, Vec2[] path)
        //{
        //    u.ResetPath(path);
        //    Broadcast("SetPath", (buff) => { buff.Write(u.UID); buff.Write(path); });
        //}

        // 建造建筑
        void OnConstructBuilding(int player, IReadableBuffer data)
        {
            var constructUnitType = data.ReadString();
            var pos = data.ReadVec2();
            SrvConstructBuilding(player, constructUnitType, pos);
        }
        public Unit SrvConstructBuilding(int player, string constructUnitType, Vec2 pos)
        {
            var u = ConstructBuilding(constructUnitType, pos, player);
            if (u == null)
                return null;

            Broadcast("CosntructBuildingUnit", (buff) =>
            {
                buff.Write(constructUnitType);
                buff.Write(player);
                buff.Write(pos);
            });

            return u;
        }

        // 建造附件
        void OnConstructAccessory(int player, IReadableBuffer data)
        {
            var uid = data.ReadString();
            var type = data.ReadString();
            SrvConstructAccessory(uid, type);
        }
        public Unit SrvConstructAccessory(string uid, string type)
        {
            var u = GetUnit(uid);
            if (u == null)
                return null;

            var acc = ConstructAccessory(u, type);
            if (acc == null)
                return null;

            Broadcast("ConstructAccessory", (buff) =>
            {
                buff.Write(uid);
                buff.Write(type);
            });

            return acc;
        }

        // 建造矿机
        void OnConstructCrystalMachine(int player, IReadableBuffer data)
        {
            var baseUID = data.ReadString();
            var bs = GetUnit(baseUID);
            if (bs == null)
                return;

            SrvConstructCrystalMachine(bs);
        }

        public bool SrvConstructCrystalMachine(Unit bs)
        {
            if (ConstructingCrystalMachine(bs) == null)
                return false;

            Broadcast("ConstructCrystalMachine", (buff) =>
            {
                buff.Write(bs.UID);
            });

            return true;
        }

        // 改建建筑
        void OnReconstructBuilding(int player, IReadableBuffer data)
        {
            var uid = data.ReadString();
            var toNewType = data.ReadString();

            var u = GetUnit(uid);
            if (u == null || u.Player != player || u.cfg.ReconstructTo == null || u.cfg.ReconstructTo.FirstIndexOf(toNewType) < 0)
                return;

            SrvReconstructBuilding(u, toNewType);
        }

        void SrvReconstructBuilding(Unit u, string toNewType)
        {
            if (!ReconstructBuilding(u, toNewType))
                return;

            Broadcast("ReconstructBuilding", (buff) =>
            {
                buff.Write(u.UID);
                buff.Write(toNewType);
            });
        }

        // 取消建造
        void OnCancelBuilding(int player, IReadableBuffer data)
        {
            var uid = data.ReadString();
            var u = GetUnit(uid);
            if (u == null)
                return;

            SrvCancelBuilding(u);
        }
        public void SrvCancelBuilding(Unit u)
        {
            u.Room.CancelBuilding(u);
            Broadcast("CancelBuilding", (buff) =>
            {
                buff.Write(u.UID);
            });
        }

        // 拆毁建筑
        void OnDestroyBuilding(int player, IReadableBuffer data)
        {
            var uid = data.ReadString();
            var u = GetUnit(uid);
            if (u == null)
                return;

            SrvDestroyBuilding(u);
        }
        public void SrvDestroyBuilding(Unit u)
        {
            DestroyBuilding(u);
            Broadcast("DestroyBuilding", (buff) =>
            {
                buff.Write(u.UID);
            });
        }

        #region 测试用，不检查

        // 直接放置单位
        public void SrvAddUnitAt(int player, string type, int num, Vec2 pos)
        {
            FC.For(num, (i) => { AddNewUnit(null, type, pos, player); });
            Broadcast("AddUnitAt", (buff) =>
            {
                buff.Write(player);
                buff.Write(type);
                buff.Write(num);
                buff.Write(pos);
            });
        }

        // 直接加入战场兵
        void OnAddBattleUnit4TestAnyway(int _, IReadableBuffer data)
        {
            var player = data.ReadInt();
            var type = data.ReadString();
            var x = data.ReadInt();
            var y = data.ReadInt();

            var u = AddNewUnit(null, type, new Vec2(x, y), player);
            if (u != null)
            {
                Broadcast("AddBattleUnit4TestAnyway", (buff) =>
                {
                    buff.Write(player);
                    buff.Write(type);
                    buff.Write((int)u.Pos.x);
                    buff.Write((int)u.Pos.y);
                });
            }
        }

        // 直接建造建筑
        void OnAddBuildingUnit4TestAnyway(int _, IReadableBuffer data)
        {
            var player = data.ReadInt();
            var genType = data.ReadString();
            var x = data.ReadInt();
            var y = data.ReadInt();

            var building = AddNewUnit(null, genType, new Vec2(x, y), player, true);
            if (building != null)
            {
                building.Hp = building.cfg.MaxHp;

                Broadcast("AddBuildingUnit4TestAnyway", (buff) =>
                {
                    buff.Write(player);
                    buff.Write(genType);
                    buff.Write((int)building.Pos.x);
                    buff.Write((int)building.Pos.y);
                });
            }
        }

        // 直接添加空降兵
        void OnAddSoldierCarrierUnit4TestAnyway(int _, IReadableBuffer data)
        {
            var player = data.ReadInt();
            var genType = data.ReadString();
            var x = data.ReadInt();
            var y = data.ReadInt();

            Vec2 dropPt = new Vec2(x, y);
            var fromPt = player == 1 ? new Vec2(30, 5) : new Vec2(30, 235);
            var dir = dropPt - fromPt;
            var mr = MapSize.x > MapSize.y ? MapSize.x : MapSize.y;
            dir = dir.Length > 1 ? dir / dir.Length : Vec2.Zero;
            var toPt = dropPt + dir * mr;
            var stc = AddNewUnit(null, genType, fromPt, player);

            var cfg = UnitConfiguration.GetDefaultConfig(genType);
            var pType = cfg.Pets[0];
            var pCnt = int.Parse(cfg.Pets[1]);
            FC.For(pCnt, (i) => { stc.UnitCosntructingWaitingList.Add(pType); });

            stc.Dir = dir.Dir();
            stc.MovePath.Add(dropPt);
            stc.MovePath.Add(toPt);

            if (stc != null)
            {
                Broadcast("AddSoldierCarrierUnit4TestAnyway", (buff) =>
                {
                    buff.Write(player);
                    buff.Write(genType);
                    buff.Write((int)dropPt.x);
                    buff.Write((int)dropPt.y);
                });
            }
        }

        #endregion

        public Action<int, Unit> OnAddBattltUnitAt = null;

        void OnAddBattleUnitAt(int player, IReadableBuffer data)
        {
            var type = data.ReadString();
            var pos = data.ReadVec2();

            var cfg = UnitConfiguration.GetDefaultConfig(type);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;
            if (GetResource(player, "Money") < cost
                || GetResource(player, "Gas") < gasCost)
                return;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);

            var u = AddNewUnit(null, type, pos, player);
            if (u != null)
            {
                Broadcast("AddBattleUnitAt", (buff) =>
                {
                    buff.Write(player);
                    buff.Write(type);
                    buff.Write(u.Pos);
                });
            }

            if (null != OnAddBattltUnitAt)
                OnAddBattltUnitAt(player, u);
        }

        public Unit SrvAddBattltUnitAt(int player, string type, Vec2 pos)
        {
            if (!CheckPrerequisites(player, type))
                return null;

            var cfg = UnitConfiguration.GetDefaultConfig(type);

            var cost = cfg.Cost;
            var gasCost = cfg.GasCost;

            if (GetResource(player, "Money") < cost
                || GetResource(player, "Gas") < gasCost)
                return null;

            AddResource(player, "Money", -cost);
            AddResource(player, "Gas", -gasCost);

            var u = AddNewUnit(null, type, pos, player);
            if (u != null)
            {
                Broadcast("AddBattleUnitAt", (buff) =>
                {
                    buff.Write(player);
                    buff.Write(type);
                    buff.Write(u.Pos);
                });
            }

            return u;
        }

        // 投放伞兵
        void OnDropSoldierFromCarrier(int player, IReadableBuffer data)
        {
            var type = data.ReadString();
            var dropPt = new Vec2(data.ReadInt(), data.ReadInt());
            
            SrvDropSoldierFromCarrier(type, player, dropPt);
        }
        public void SrvDropSoldierFromCarrier(string type, int player, Vec2 dropPt)
        {
            if (!CreateSoldierCarrier(type, player, dropPt))
                return;

            Broadcast("DropSoldierFromCarrier", (buff) =>
            {
                buff.Write(type);
                buff.Write(player);
                buff.Write(dropPt);
            });
        }

        #endregion

        #region Replay related

        // 当前战斗的历史消息
        List<KeyValuePair<string, IReadableBuffer>> currentBattleMsgHistory = new List<KeyValuePair<string, IReadableBuffer>>();

        // 所有录像
        static StableDictionary<string, BattleReplay> replays = new StableDictionary<string, BattleReplay>();
        public static StableDictionary<string, BattleReplay> Replays
        {
            get
            {
                return replays;
            }
        }

        // 获取所有录像名称列表
        public static string[] AllReplayTitles { get { return replays.KeyArray; } }

        // 获取指定录像
        public static BattleReplay GetReplay(string r)
        {
            return replays.ContainsKey(r) ? replays[r] : null;
        }

        // 加载所有录像
        static int nextReplayNo = 0;
        public static void LoadAllPVPReplays()
        {
            if (!Directory.Exists(ReplayFolder))
                Directory.CreateDirectory(ReplayFolder);

            // 获取所有文件名并排序
            var fs = Directory.GetFiles(ReplayFolder, "*.*", SearchOption.TopDirectoryOnly).Where((f) => f.EndsWith(".scm")).ToArray();
            fs.SwiftSort((f) =>
            {
                var pureFileName = Path.GetFileNameWithoutExtension(f);
                return int.Parse(pureFileName);
            });

            if (fs.Length > 0)
            {
                var lastFile = Path.GetFileNameWithoutExtension(fs[fs.Length - 1]);
                nextReplayNo = int.Parse(lastFile);
            }

            nextReplayNo++;

            // 读取所有录像内容
            foreach (var f in fs)
            {
                byte[] data = null;
                using (var fr = new BinaryReader(new FileStream(f, FileMode.Open)))
                    data = fr.ReadBytes((int)fr.BaseStream.Length);

                var replay = BattleReplay.Deserialize(new RingBuffer(data));
                replays[Path.GetFileNameWithoutExtension(f)] = replay;
            }
        }

        // 战斗录像存放地址
        static string ReplayFolder = "Replays";
        public static BattleReplay SaveCurrentBattleReplay(string id1, string id2, string name1, string name2, ulong frames, bool isPVP, List<KeyValuePair<string, IReadableBuffer>> msgs, bool isCrashReplay = false)
        {
            if (!Directory.Exists(ReplayFolder))
                Directory.CreateDirectory(ReplayFolder);

            var replay = new BattleReplay();
            replay.ID = (nextReplayNo++).ToString().PadLeft(10, '0');
            replay.Usr1 = id1;
            replay.Usr2 = id2;
            replay.UsrName1 = name1;
            replay.UsrName2 = name2;
            replay.Length = frames;
            replay.Date = DateTime.Now;
            replay.IsPVP = isPVP;
            replay.IsCrashReplay = isCrashReplay;
            replay.Msgs = msgs;

            replays[replay.ID] = replay;

            var writer = new WriteBuffer();
            replay.Serialize(writer);
            var ext = isCrashReplay ? ".crash" : (isPVP ? ".scm" : ".pve");
            using (var fw = new BinaryWriter(new FileStream(Path.Combine(ReplayFolder, replay.ID + ext), FileMode.CreateNew)))
                fw.Write(writer.Data, 0, writer.Available);

            return replay;
        }
        
        #endregion

        #region 电脑 AI 相关

        public AIComputerOpponent ComputerAI { get; protected set; }

        public AIComputerOpponent CreateComputerAI(string aiType, int player)
        {
            ComputerAI = CreateAI(aiType, player);
            ComputerAI.Init();
            return ComputerAI;
        }

        AIComputerOpponent CreateAI(string aiType, int player)
        {
            var aiID = ID + "_ai_" + (aiType == null ? "null" : aiType);
            switch (aiType)
            {
                case "AIDifficultRush":
                    return new AIDifficultRush(aiID, this, player);
                case "AISimpleRushExt":
                    return new AISimpleRushExt(aiID, this, player);
                case "AISimpleRush":
                    return new AISimpleRush(aiID, this, player);
                default:
                    return new Dumb(aiID, this, player);
            }
        }

        #endregion
    }
}
