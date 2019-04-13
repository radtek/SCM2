using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;
using Server;

namespace SCM
{
    /// <summary>
    /// PVE服务器端AI，普通模式
    /// </summary>
    public class AISimpleRush : AIComputerOpponent
    {
        private Room4Server room;
        private Unit barrackU;

        public AISimpleRush(string id, Room room, int player) : base(id, room, player)
        {
        }

        public override void Init()
        {
            room = (Room4Server)Room;

            sm.NewState("createBarrack").Run((st, te) =>
            {
                if (barrackU == null)
                    barrackU = room.SrvConstructBuilding(Player, "Barrack", new Vec2(30, 20));
            }).AsDefault();

            var cd = Fix64.Zero;

            sm.NewState("addSoldier").Run((st, te) =>
            {
                if (!barrackU.BuildingCompleted)
                    return;

                var sd = room.SrvAddBattltUnitAt(Player, "SoldierWithDog", new Vec2(30, 25));

                if (sd != null)
                    cd = UnitConfiguration.GetDefaultConfig("Soldier").ConstructingTime;
            });

            sm.NewState("addSoldierCD").Run((st, te) =>
            {
                cd -= te;
            });

            sm.Trans().From("createBarrack").To("addSoldier").When((st) => barrackU != null);
            sm.Trans().From("addSoldier").To("createBarrack").When((st) => barrackU.Hp <= 0);
            sm.Trans().From("addSoldier").To("addSoldierCD").When((st) => cd > 0);
            sm.Trans().From("addSoldierCD").To("addSoldier").When((st) => cd <= 0);
        }
    }
}