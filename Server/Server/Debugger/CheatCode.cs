using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using SCM;

namespace Server
{
    public class CheatCode : Component
    {
        ConsoleInput ci;
        public override void Init()
        {
            ci = GetCom<ConsoleInput>();

            ci.OnCommand("showmethemoney", (ps) =>
            {
                if (ps == null || ps.Length < 1)
                    return "should specify the user id";

                var uid = ps[0];
                var brm = GetCom<BattleRoomManager>();
                foreach (Room4Server r in brm.AllRooms)
                {
                    var p = r.GetNoByUser(uid);
                    if (p >= 0)
                    {
                        var num = 100000;
                        r.Players[p].Resources["Money"] += num;
                        r.Broadcast("CheatCode", (buff) =>
                        {
                            buff.Write("ShowMeTheMoney");
                            buff.Write(uid);
                            buff.Write(num);
                        });
                        return uid + " + 100000 money";
                    }
                }

                return "can not find user: " + uid;
            });
        }
    }
}