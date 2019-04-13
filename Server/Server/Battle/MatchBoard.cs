using System;
using System.Collections;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 玩家实时匹配对手
    /// </summary>
    public class MatchBoard : Component, IFrameDrived
    {
        UserPort UP;

        // 战斗房间管理
        BattleRoomManager BtrMgr;

        SessionContainer SS;

        CoroutineManager CM;

        // 等待匹配的玩家及对应等待时间
        StableDictionary<string, int> waitingList = new StableDictionary<string, int>();

        // 等待匹配电脑的玩家及对应等待时间
        StableDictionary<string, int> PVEWaitingList = new StableDictionary<string, int>();

        public string AIType = "AIDifficultRush";

        public int WaitTime = 0;

        public override void Init()
        {
            BtrMgr = GetCom<BattleRoomManager>();
            UP = GetCom<UserPort>();
            SS = GetCom<SessionContainer>();
            CM = GetCom<CoroutineManager>();

            UP.OnMessage("MatchIn", OnIn);
            UP.OnRequest("CancelMatchIn", OnCancel);
            UP.OnMessage("PVEMatchIn", OnPVEIn);
            UP.OnRequest("CancelPVEMatchIn", OnPVECancel);

            GetCom<LoginManager>().OnUserDisconnecting += OnUserDisconnecting;
            ConsoleInput.OnChangePVEAI += ChangePVEAI;
        }

        void OnUserDisconnecting(Session s)
        {
            if (waitingList.ContainsKey(s.ID))
                waitingList.Remove(s.ID);

            if (PVEWaitingList.ContainsKey(s.ID))
                PVEWaitingList.Remove(s.ID);
        }

        // 加入匹配等待列表
        void OnIn(Session s, IReadableBuffer data)
        {
            if (!waitingList.ContainsKey(s.ID))
                waitingList[s.ID] = 0;
        }

        // 加入人机匹配等待列表
        void OnPVEIn(Session s, IReadableBuffer data)
        {
            if (!PVEWaitingList.ContainsKey(s.ID))
                PVEWaitingList[s.ID] = 0;
        }

        // 从人机匹配列表移除
        void OnPVECancel(Session s, IReadableBuffer data, IWriteableBuffer buff)
        {
            bool isSuccess = PVEWaitingList.Remove(s.ID);
            buff.Write(isSuccess);
        }

        // 从匹配列表移除
        void OnCancel(Session s, IReadableBuffer data, IWriteableBuffer buff)
        {
            bool isSuccess = waitingList.Remove(s.ID);
            buff.Write(isSuccess);
        }

        // 不断尝试匹配用户，匹配到了就开战
        public void OnTimeElapsed(int te)
        {
            var usrs = waitingList.KeyArray;

            // 玩家两两匹配
            while (usrs.Length >= 2)
            {
                var usr1 = usrs[0];
                var usr2 = usrs[1];
                waitingList.Remove(usr1);
                waitingList.Remove(usr2);
                PVEWaitingList.Remove(usr1);
                PVEWaitingList.Remove(usr2);

                var usrsInfo = new UserInfo[2];
                usrsInfo[0] = SS[usr1].Usr.Info;
                usrsInfo[1] = SS[usr2].Usr.Info;

                SS[usr1].Conn.Send2Usr("BattleReady", (buff) =>
                {
                    buff.Write(usrsInfo);
                });

                SS[usr2].Conn.Send2Usr("BattleReady", (buff) =>
                {
                    buff.Write(usrsInfo);
                });

                CM.StartCoroutine(DelayBeginPVPBattle(usr1, usr2), true);

                usrs = waitingList.KeyArray;
            }

            usrs = PVEWaitingList.KeyArray;
            foreach (var usr in usrs)
            {
                var wt = PVEWaitingList[usr];
                if (wt >= WaitTime && SS[usr] != null)
                {
                    PVEWaitingList.Remove(usr);
                    waitingList.Remove(usr);
                    var r = BtrMgr.CreatePVERoom(usr, AIType);
                    r.BattleBegin(RandomUtils.RandomNext());
                }
                else
                    PVEWaitingList[usr] = wt + te;
            }
        }

        private IEnumerator DelayBeginPVPBattle(string usr1, string usr2)
        {
            yield return new TimeWaiter(3500);

            var r = BtrMgr.CreatePVPRoom(usr1, usr2);

            if (r != null)
            {
                r.WinnerAward = 5;
                r.LoserAward = 0;
                r.BattleBegin(RandomUtils.RandomNext());
            }
        }

        private void ChangePVEAI(string type)
        {
            switch (type)
            {
                case "0":
                    AIType = "Dumb";
                    break;
                case "1":
                    AIType = "AISimpleRush";
                    break;
                case "2":
                    AIType = "AISimpleRushExt";
                    break;
            }
        }
    }
}
