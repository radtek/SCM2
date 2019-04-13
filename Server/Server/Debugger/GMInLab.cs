using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using SCM;

namespace Server
{
    public class GMInLab : NetComponent
    {
        ConsoleInput ci;
        public override void Init()
        {
            ci = GetCom<ConsoleInput>();

            ci.OnCommand("switchplayer", (ps) =>
            {
                var cnt = 0;
                var brm = GetCom<BattleRoomManager>();

                foreach (Room4Server r in brm.AllRooms)
                {
                    var usr1 = r.UsrsID[1];
                    var usr2 = r.UsrsID[2];
                    r.UsrsID[1] = usr2;
                    r.UsrsID[2] = usr1;

                    var p1 = r.Players[1];
                    var p2 = r.Players[2];
                    r.Players[1] = p2;
                    r.Players[2] = p1;

                    cnt++;

                    r.Broadcast("PlayerSwitched", null);
                }

                return "switched: " + cnt;
            });

            ci.OnCommand("win", (ps) =>
            {
                if (ps == null || ps.Length == 0)
                    return "need user id";

                var uid = ps[0];
                var brm = GetCom<BattleRoomManager>();

                foreach (Room4Server r in brm.AllRooms)
                {
                    if (r.UsrsID[1] == uid || r.UsrsID[2] == uid)
                    {
                        r.BattleEnd(uid);
                        return "battle ended with winner: " + uid;
                    }
                }

                return "find no Battle with userid: " + uid;
            });

            ci.OnCommand("lose", (ps) =>
            {
                if (ps == null || ps.Length == 0)
                    return "need user id";

                var uid = ps[0];
                var brm = GetCom<BattleRoomManager>();

                foreach (Room4Server r in brm.AllRooms)
                {
                    if (r.UsrsID[1] == uid || r.UsrsID[2] == uid)
                    {
                        var winner = r.UsrsID[1] == uid ? r.UsrsID[2] : r.UsrsID[1];
                        r.BattleEnd(winner);
                        return "battle ended with winner: " + winner;
                    }
                }

                return "find no Battle with userid: " + uid;
            });

            GetCom<LoginManager>().BeforeUserLogin += (Session s, bool isNew) =>
            {
                ////---- 这里加个 trick，方便内部测试
            };
        }
    }
}
