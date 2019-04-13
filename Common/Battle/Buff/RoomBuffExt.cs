using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 扩展 Room 的 Buff 行为
    /// </summary>
    public static class RoomBuffExt
    {
        static bool Biological(Unit u) { return u.cfg.IsBiological; }
        static bool Mechanical(Unit u) { return u.cfg.IsMechanical; }
        static bool AirUnit(Unit u) { return u.cfg.IsAirUnit; }

        public static void CreateBuff(this Room room, int player, string buffType, Fix64[] ps)
        {
            Buff b = null;
            switch(buffType)
            {
                case "AddBiologicalAttack":
                    b = AddAttackPower(Biological, ps[0]);
                    break;
                case "AddBiologicalDefence":
                    b = AddDefence(Biological, ps[0]);
                    break;
                case "AddMechanicalAttack":
                    b = AddAttackPower(Mechanical,ps[0]);
                    break;
                case "AddMechanicalDefence":
                    b = AddDefence(Mechanical, ps[0]);
                    break;
                case "AddAirUnitAttack":
                    b = AddAttackPower(AirUnit, ps[0]);
                    break;
                case "AddAirUnitDefence":
                    b = AddDefence(AirUnit, ps[0]);
                    break;
                default:
                    break;
            }

            if (b != null)
            {
                b.Type = buffType;
                room.BuffRunner.AddBuff(player, b);
            }
        }

        // 增加生物单位攻击
        static Buff AddAttackPower(Func<Unit, bool> filter, Fix64 addPrecentage)
        {
            Func<Unit, int, Fix64> calcDA = (u, i) =>
            {
                if (!filter(u))
                    return 0;

                var da = u.cfg.AttackPower[i] * addPrecentage;
                if (da < 1)
                    da = 1;

                return da;
            };

            var b = new Buff();
            b.OnUnit = (u) => { u.PowerAdd[0] += calcDA(u, 0); u.PowerAdd[1] += calcDA(u, 1); };
            b.OffUnit = (u) => { u.PowerAdd[0] -= calcDA(u, 0); u.PowerAdd[1] -= calcDA(u, 1); };

            return b;
        }

        // 增加生物单位防御
        static Buff AddDefence(Func<Unit, bool> filter, Fix64 addPoint)
        {
            var b = new Buff();
            b.OnUnit = (u) => { if (!filter(u)) return; u.DefenceAdd += addPoint; };
            b.OffUnit = (u) => { if (!filter(u)) return; u.DefenceAdd -= addPoint; };

            return b;
        }
    }
}
