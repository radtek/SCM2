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
    public class AISimpleRushExt : AIComputerOpponent
    {
        public AISimpleRushExt(string id, Room room, int player) : base(id, room, player)
        {
        }

        public override void Init()
        {
            Room4Server room = (Room4Server)Room;

            long lastTime = TimeUtils.NowSecond;

            Random random = new Random();

            string curUnitType = null;
            Vec2 curSelectPos = Vec2.Zero;

            Fix64 PosMinX = 1;
            Fix64 PosMaxX = room.MapSize.x - 1;

            Fix64 PosMinY = Player == 1 ? 10 : room.MapSize.y / 3;
            Fix64 PosMaxY = Player == 1 ? room.MapSize.y / 3 : room.MapSize.y - 10;

            var allUnitType = UnitConfiguration.AllOriginalUnitTypes;
            sm.NewState("SelectUnitTypeAndPos").Run((st, te) =>
            {
                int typeNum = random.Next(0, allUnitType.Length - 3);
                curUnitType = allUnitType[typeNum];

                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);

                curSelectPos = new Vec2(posX, posY);

            }).AsDefault();

            sm.NewState("CreateUnitOnGround").Run((st, te) =>
            {
                if (curUnitType == "Dog"
                || curUnitType == "Radar"
                || curUnitType == "Base"
                || curUnitType == "BaseStub"
                || curUnitType == "CrystalMachine"
                || curUnitType == "Accessory"
                || curUnitType == "CommanderCenter"
                || curUnitType == "Fortress"
                || curUnitType == "TreasureBoxCarrier"
                || curUnitType == "TreasureBox"
                || curUnitType == "AirTechUltimate"
                || curUnitType == "AirTech"
                || curUnitType == "VelTechSeige"
                || curUnitType == "VelTechRobot"
                || curUnitType == "VelTech"
                || curUnitType == "BioTechShot"
                || curUnitType == "BioTechAOE"
                || curUnitType == "BioTech"
                || curUnitType == "NeutralMonster"
                || curUnitType == "Blademaster"
                || curUnitType == "Velkoz")
                {
                    curUnitType = null;
                    return;
                }

                var cfg = UnitConfiguration.GetDefaultConfig(curUnitType);

                if (cfg.IsBuilding)
                    room.SrvConstructBuilding(Player, curUnitType, curSelectPos);
                else if (TimeUtils.NowSecond - lastTime >= 10)
                {
                    if (curUnitType == "SoldierCarrier")
                    {
                        lastTime = TimeUtils.NowSecond;
                        room.SrvDropSoldierFromCarrier("SoldierCarrier", Player, curSelectPos);
                    }
                    else if (room.SrvAddBattltUnitAt(Player, curUnitType, curSelectPos) != null)
                    {
                        lastTime = TimeUtils.NowSecond;
                    }
                }

                curUnitType = null;
            });

            sm.Trans().From("SelectUnitTypeAndPos").To("CreateUnitOnGround").When((st) => curUnitType != null);
            sm.Trans().From("CreateUnitOnGround").To("SelectUnitTypeAndPos").When((st) => curUnitType == null);
        }
    }
}