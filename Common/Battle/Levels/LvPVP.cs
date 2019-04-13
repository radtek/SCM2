using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 普通 PVP 关卡
    /// </summary>
    public class LvPVP : Level
    {
        public override string LevelID { get { return "PVP"; } }
        public override string DisplayName { get { return "对战地图"; } }
        public override string Description { get { return "用于普通 PVP 对战"; } }

        public override void InitResource(Room r)
        {
            r.AddResource(1, "Money", 350);
            r.AddResource(2, "Money", 350);
        }

        Vec2[] poses = null;
        Fix64 ccOffset = Fix64.Zero;
        public override void InitMapSetting(Room r)
        {
            base.InitMapSetting(r);

            // 矿点位置
            ccOffset = 5;
            poses = new Vec2[]
            {
                new Vec2(MapHalfSize.x, ccOffset),
                new Vec2(MapHalfSize.x - 20, ccOffset + 15),
                new Vec2(MapHalfSize.x + 20, ccOffset + 30),
            };

            foreach (var p in poses)
            {
                r.AddNewUnit(null, "BaseStub", p, 0, true);
                r.AddNewUnit(null, "BaseStub", MapSize - p, 0, true);
            }
        }

        Unit cc1;
        Unit cc2;
        public override void InitBuilding(Room r)
        {
            // 双方地方基地
            cc1 = r.AddNewUnit(null, "Base", poses[0], 1, true);
            cc1.Hp = cc1.cfg.MaxHp;
            cc2 = r.AddNewUnit(null, "Base", MapSize - poses[0], 2, true);
            cc2.Hp = cc2.cfg.MaxHp;

            // 添加初始矿机
            FC.For(4, (i) =>
            {
                r.AddNewUnit(null, "CrystalMachine", r.FindNextCrystalMachinePos(cc1), 1, true).Owner = cc1;
                r.AddNewUnit(null, "CrystalMachine", r.FindNextCrystalMachinePos(cc2), 2, true).Owner = cc2;
            });
        }

        public override void InitBattleUnits(Room r)
        {
            // 初始雷达
            r.AddNewUnit(null, "Radar", cc1.Pos + (cc2.Pos - cc1.Pos) * 0.1f, 2, true);
            r.AddNewUnit(null, "Radar", cc2.Pos + (cc1.Pos - cc2.Pos) * 0.1f, 1, true);
        }
    }
}
