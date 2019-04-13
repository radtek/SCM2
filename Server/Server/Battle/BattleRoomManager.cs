using System;
using System.Collections.Generic;
using Swift;
using SCM;
using Swift.Math;

namespace Server
{
    public class BattleInfo
    {
        public string UserID1;
        public string UserID2;
        public string UserName1;
        public string UserName2;
        public ulong Frames;
        public int Winner;
        public bool IsPVP;

        public BattleInfo(string uid1, string uid2, string uname1, string uname2, ulong frms, int winner, bool isPVP)
        {
            UserID1 = uid1;
            UserID2 = uid2;
            UserName1 = uname1;
            UserName2 = uname2;
            Frames = frms;
            Winner = winner;
            IsPVP = isPVP;
        }
    }

    /// <summary>
    /// 战斗房间管理器
    /// </summary>
    public class BattleRoomManager : Component, IFrameDrived
    {
        UserPort UP;
        SessionContainer SC;

        // 所有活动的战斗房间
        List<Room4Server> rooms = new List<Room4Server>();
        public Room4Server[] AllRooms { get { return rooms.ToArray(); } }

        // 记录用户所在房间
        StableDictionary<string, Room4Server> usr2room = new StableDictionary<string, Room4Server>();

        // 战斗日志
        ServerBusinessLogger<BattleInfo> BattleLogger = null;

        private string AIType1 = "AIDifficultRush";
        private string AIType2 = "AISimpleRushExt";
        private string AIType3 = "AISimpleRush";
        private string AIType = "Dumb";

        public override void Init()
        {
            SC = GetCom<SessionContainer>();
            var lgMgr = GetCom<LoginManager>();
            lgMgr.OnUserDisconnecting += OnUserDisconnected;
            UP = GetCom<UserPort>();

            RedirectRoomMessage("AddBattleUnitAt");

            RedirectRoomMessage("ConstructBuilding");
            RedirectRoomMessage("ConstructCrystalMachine");
            RedirectRoomMessage("ConstructAccessory");
            RedirectRoomMessage("ReconstructBuilding");
            RedirectRoomMessage("CancelBuilding");
            RedirectRoomMessage("DropSoldierFromCarrier");
            RedirectRoomMessage("AddBattleUnit4TestAnyway");
            RedirectRoomMessage("AddBuildingUnit4TestAnyway");
            RedirectRoomMessage("AddSoldierCarrierUnit4TestAnyway");
            RedirectRoomMessage("DestroyBuilding");

            UP.OnMessage("Surrender", OnSurrender);
            UP.OnRequest("GetReplayList", OnGetReplayList);
            UP.OnRequest("GetMyReplayList", OnGetMyReplayList);
            UP.OnRequest("GetReplay", OnGetReplay);

            Room4Server.LoadAllPVPReplays();

            BattleLogger = GetCom<ServerBusinessLogger<BattleInfo>>();
        }

        // 创建 PVE 战斗房间
        public Room4Server CreatePVERoom(string usr, string aiType)
        {
            AIType = aiType;
            var s = SC[usr];

            // 创建新房间
            var roomID = usr + "_vs_AIRobot";

            // 创建 ai
            var aiID = roomID + "_" + aiType;
            var r = new Room4Server(roomID, aiID, s, new Vec2(60, 200), "PVP");
            r.IsPVP = false;

            if (s.Usr.Info.WinCount + s.Usr.Info.LoseCount != 0)
            {
                var rate = s.Usr.Info.WinCount*100 / (s.Usr.Info.WinCount + s.Usr.Info.LoseCount);
                if (rate >= 45)
                    AIType = AIType1;
                else if (rate >= 25)
                    AIType = AIType2;
                else
                    AIType = AIType3;
            }
            else AIType = AIType3;
            //AIType = "Dumb";//测试用傻瓜AI,不需要则注释.

            var ai = r.CreateComputerAI(AIType, r.GetNoByUser(aiID));

            ai.FindPath = (radius, src, dst, cb) => { RequestPathFromClient(s, radius, src, dst, cb); };

            rooms.Add(r);
            usr2room[usr] = r;
            return r;
        }

        // 从客户端请求寻路数据
        void RequestPathFromClient(Session s, Fix64 r, Vec2 src, Vec2 dst, Action<Vec2[]> cb)
        {
            var conn = s.Conn;
            if (conn == null)
                return;

            var buff = conn.BeginRequest("ServerPort", (data) =>
            {
                var path = data.ReadVec2Arr();
                if (path == null || path.Length == 0)
                    path = new Vec2[] { dst };

                cb(path);
            }, null);

            buff.Write("FindPath");
            buff.Write(src);
            buff.Write(dst);
            buff.Write(r);
            conn.End(buff);
        }

        // 创建 PVP 战斗房间
        public Room4Server CreatePVPRoom(string usr1, string usr2)
        {
            var s1 = SC[usr1];
            var s2 = SC[usr2];

            if (s1 == null || s2 == null)
                return null;

            // 创建新房间
            var roomID = usr1 + "_vs_" + usr2;
            var r = new Room4Server(roomID, s1, s2, new Vec2(60, 200), "PVP");
            r.IsPVP = true;
            rooms.Add(r);
            usr2room[usr1] = r;
            usr2room[usr2] = r;
            return r;
        }

        // 转递房间消息
        void RedirectRoomMessage(string op)
        {
            UP.OnMessage(op, (Session s, IReadableBuffer data) =>
            {
                var usr = s.ID;
                var r = usr2room.ContainsKey(usr) ? usr2room[usr] : null;
                if (r == null)
                    return;

                r.OnMessage(op, usr, data);
            });
        }

        // 推动游戏进度
        public void OnTimeElapsed(int te)
        {
            // 推动所有房间的游戏逻辑, 并记录已经结束的房间
            var roomsFinished = new List<Room4Server>();
            foreach (var r in rooms)
            {
                if (!r.Finished)
                {
                    var winner = r.CheckWinner();
                    if (winner >= 0) // 0 是平局
                        r.BattleEnd(r.UsrsID[winner]);
                }

                if (r.Finished)
                {
                    roomsFinished.Add(r);
                    BattleLogger.Log(new BattleInfo(r.UsrsID[1], r.UsrsID[2], r.UsrsInfo[1].Name, r.UsrsInfo[2].Name, r.FrameNo, 
                        r.Winner == r.UsrsID[1] ? 1 : (r.Winner == r.UsrsID[2] ? 2 : 0), r.IsPVP));
                }
                else
                    r.OnTimeElapsed(te);
            }

            // 丢弃已结束的房间
            foreach (var r in roomsFinished)
            {
                foreach (var usr in r.UsrsID)
                {
                    if (usr != null)
                        usr2room.Remove(usr);

                    rooms.Remove(r);
                }
            }
        }

        // 获取玩家当前房间
        public Room4Server GetUserRoom(string usr)
        {
            return usr2room.ContainsKey(usr) ? usr2room[usr] : null;
        }

        // 玩家退出或掉线
        void OnUserDisconnected(Session s)
        {
            OnSurrender(s, null);
        }

        // 玩家投降
        void OnSurrender(Session s, IReadableBuffer data)
        {
            if (!usr2room.ContainsKey(s.ID))
                return;

            var r = usr2room[s.ID];
            if (r == null)
                return;

            var loser = r.GetNoByUser(s.ID);
            var winner = loser == 1 ? 2 : 1;
            r.BattleEnd(r.UsrsID[winner]);
            r.RemoveSession(s);
        }

        // 调取最近录像列表
        void OnGetReplayList(Session s, IReadableBuffer data, IWriteableBuffer buff)
        {
            var maxNum = data.ReadInt();
            var arr = Room4Server.AllReplayTitles;
            var lst = new List<string>();
            for (var i = 0; lst.Count < maxNum && i < arr.Length; i++)
            {
                var n = arr.Length - 1 - i;
                var t = arr[n];
                var r = Room4Server.GetReplay(t);

                if (t.IndexOf(".crash.") < 0 && r.Length < 0 /* 两分钟 */)
                    continue;

                lst.Add(t);
            }

            buff.Write(lst.Count);
            foreach (var rid in lst)
            {
                var r = Room4Server.GetReplay(rid);
                r.SerializeHeader(buff);
            }
        }

        // 调取自己最近录像
        void OnGetMyReplayList(Session s, IReadableBuffer data, IWriteableBuffer buff)
        {
            var maxNum = data.ReadInt();
            var usrInfo = s.Usr.Info;

            var lst = new List<string>();
            var arr = usrInfo.MyReplays.ToArray();
            for (var i = 0; lst.Count < maxNum && i < arr.Length; i++)
            {
                var r = arr[arr.Length - i - 1];
                if (Room4Server.AllReplayTitles.FirstIndexOf(r) >= 0)
                    lst.Add(r);
                else
                    usrInfo.MyReplays.Remove(r);
            }

            buff.Write(lst.Count);
            foreach (var rid in lst)
            {
                var r = Room4Server.GetReplay(rid);
                r.SerializeHeader(buff);
            }
        }

        // 调取录像
        void OnGetReplay(Session s, IReadableBuffer data, IWriteableBuffer buff)
        {
            var replayName = data.ReadString();
            var r = Room4Server.GetReplay(replayName);
            buff.Write(r != null);
            if (r != null)
                r.Serialize(buff);
        }
    }
}
