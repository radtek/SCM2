using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 宝箱管理
    /// </summary>
    public class TreasureBoxRunner
    {
        // 获取 buff 的显示名称
        static StableDictionary<string, string> displayName = new StableDictionary<string, string>();
        public static string GetDisplayName(string type)
        {
            return displayName[type];
        }

        // 所属房间
        public Room Room { get; set; }

        static TreasureBoxRunner()
        {
            // NeutralMonster
            tbWeight1["AddBiologicalAttack"] = 20;
            tbWeight1["AddBiologicalDefence"] = 10;
            tbWeight1["AddMechanicalAttack"] = 10;
            tbWeight1["AddMechanicalDefence"] = 5;
            tbWeight1["AddAirUnitAttack"] = 5;
            tbWeight1["AddAirUnitDefence"] = 2;
            tbWeight1["AddMoney200"] = 20;
            tbWeight1["AddGas100"] = 20;
            tbWeight1["AddMoney100Gas50"] = 20;

            foreach (var w in tbWeight1.Values)
                TotalWeight1 += w;

            // Blademaster
            tbWeight2["AddBiologicalAttack"] = 15;
            tbWeight2["AddBiologicalDefence"] = 8;
            tbWeight2["AddMechanicalAttack"] = 20;
            tbWeight2["AddMechanicalDefence"] = 10;
            tbWeight2["AddAirUnitAttack"] = 10;
            tbWeight2["AddAirUnitDefence"] = 2;
            tbWeight2["AddMoney150Gas75"] = 20;
            tbWeight2["AddGas150"] = 20;
            tbWeight2["AddMoney300"] = 20;

            foreach (var w in tbWeight2.Values)
                TotalWeight2 += w;

            // Velkoz
            tbWeight3["AddBiologicalAttack"] = 5;
            tbWeight3["AddBiologicalDefence"] = 2;
            tbWeight3["AddMechanicalAttack"] = 25;
            tbWeight3["AddMechanicalDefence"] = 10;
            tbWeight3["AddAirUnitAttack"] = 20;
            tbWeight3["AddAirUnitDefence"] = 8;
            tbWeight3["AddMoney200Gas100"] = 20;
            tbWeight3["AddGas200"] = 20;
            tbWeight3["AddMoney400"] = 20;

            foreach (var w in tbWeight3.Values)
                TotalWeight3 += w;

            displayName["AddBiologicalAttack"] = "生物攻击 +15%";
            displayName["AddBiologicalDefence"] = "生物防御 +1";
            displayName["AddMechanicalAttack"] = "机械攻击 +15%";
            displayName["AddMechanicalDefence"] = "机械防御 +1";
            displayName["AddAirUnitAttack"] = "空中攻击 +15%";
            displayName["AddAirUnitDefence"] = "空中防御 +1";
            displayName["SubHp20"] = "-20 hp";
            displayName["AddMoney200"] = "+200 晶矿";
            displayName["AddGas100"] = "+100 瓦斯";
            displayName["AddSoldier3"] = "+3 枪兵";
            displayName["AddMoney100Gas50"] = "+100 晶矿  +50 瓦斯";
            displayName["AddMoney150Gas75"] = "+150 晶矿  +75 瓦斯";
            displayName["AddMoney200Gas100"] = "+200 晶矿  +100 瓦斯";
            displayName["AddGas150"] = "+150 瓦斯";
            displayName["AddMoney300"] = "+300 晶矿";
            displayName["AddGas200"] = "+200 瓦斯";
            displayName["AddMoney400"] = "+400 晶矿";

            ProbabilitySet = new int[][] {
                new int[] { 70,30,0},
                new int[] { 60,40,0},
                new int[] { 50,50,0},
                new int[] { 40,60,0},
                new int[] { 30,70,0},
                new int[] { 20,80,0},
                new int[] { 10,90,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
                new int[] { 0,100,0},
            };
        }

        // 概率集
        private static int[][] ProbabilitySet = null;

        // 宝箱投放次数
        private int TBDropTime = 0;

        // 游戏时间流逝
        public void OnTimeElapsed(Fix64 te)
        {
            TryGenRandomTreasureBoxCarrier(te);
        }

        public void Clear()
        {
            tbActs.Clear();
        }

        // 随机产生一个宝箱投放机
        Fix64 nextTBCTime = 0;
        public void TryGenRandomTreasureBoxCarrier(Fix64 te)
        {
            // 期望平均每 90 产生一个宝箱，所以把 60s 作为下限，120s 作为上限
            if (nextTBCTime <= 0)
                nextTBCTime = Room.RandomNext(60, 120);
            
            nextTBCTime -= te / 1000;
            if (nextTBCTime <= 0)
            {
                var nx = Room.RandomNext(0, 5);
                var dropPt = new Vec2(
                    (nx + 0.5) * (Room.MapSize.x / 5),
                    Room.RandomNext((int)(Room.MapSize.y * 3 / 10), (int)(Room.MapSize.y * 7 / 10)));

                // 确定起止点和投放点
                nextTBCTime = 0;

                var mr = Room.MapSize.x > Room.MapSize.y ? Room.MapSize.x : Room.MapSize.y;
                var dir = Room.RandomNext(0, 360);
                var arc = dir * Fix64.Pi / 180;
                var d = mr * (new Vec2(Fix64.Cos(arc), Fix64.Sin(arc)));
                var fromPt = dropPt + d;
                var toPt = dropPt - d;

                var u = Room.AddNewUnit(null, "TreasureBoxCarrier", fromPt, 0);
                u.Dir = dir;
                u.MovePath.Add(dropPt);
                u.MovePath.Add(toPt);

                int index = Room.RandomNext(0, 100);

                if (0 <= index && index < ProbabilitySet[TBDropTime][0])
                    u.UnitCosntructingWaitingList.Add("NeutralMonster");
                else if ((100 - ProbabilitySet[TBDropTime][2]) <= index && index < 100)
                    u.UnitCosntructingWaitingList.Add("Velkoz");
                else
                    u.UnitCosntructingWaitingList.Add("Blademaster");

                TBDropTime++;
            }
        }

        // 每个宝箱的用途
        StableDictionary<string, Action<Unit>> tbActs = new StableDictionary<string, Action<Unit>>();

        // 触发宝箱
        public void TriggerOne(Unit u, string uid)
        {
            tbActs[uid].SC(u);
            tbActs.Remove(uid);
        }

        // 创建随机宝箱
        static int TotalWeight1 = 0;
        static int TotalWeight2 = 0;
        static int TotalWeight3 = 0;
        static StableDictionary<string, int> tbWeight1 = new StableDictionary<string, int>(); // 不同类型宝箱随机权重
        static StableDictionary<string, int> tbWeight2 = new StableDictionary<string, int>(); // 不同类型宝箱随机权重
        static StableDictionary<string, int> tbWeight3 = new StableDictionary<string, int>(); // 不同类型宝箱随机权重
        public string RandomTreasureBoxType(int index)
        {
            switch (index)
            {
                case 1:
                    var w1 = Room.RandomNext(0, TotalWeight1);
                    foreach (var tb in tbWeight1.Keys)
                    {
                        var tbW = tbWeight1[tb];
                        if (w1 <= tbW)
                            return tb;
                        else
                            w1 -= tbW;
                    }
                    break;
                case 2:
                    var w2 = Room.RandomNext(0, TotalWeight2);
                    foreach (var tb in tbWeight2.Keys)
                    {
                        var tbW = tbWeight2[tb];
                        if (w2 <= tbW)
                            return tb;
                        else
                            w2 -= tbW;
                    }
                    break;
                case 3:
                    var w3 = Room.RandomNext(0, TotalWeight3);
                    foreach (var tb in tbWeight3.Keys)
                    {
                        var tbW = tbWeight3[tb];
                        if (w3 <= tbW)
                            return tb;
                        else
                            w3 -= tbW;
                    }
                    break;
                default:
                    return null;
            }

            return null;
        }

        // 在指定位置创建宝箱
        public Unit CreateTreasureBox(string tbType, Vec2 pos)
        {
            Action<Unit> act = null;

            switch (tbType)
            {
                case "AddBiologicalAttack":
                case "AddMechanicalAttack":
                case "AddAirUnitAttack":
                    act = (u) => Room.CreateBuff(u.Player, tbType, new Fix64[] { 0.15 });
                    break;
                case "AddBiologicalDefence":
                case "AddMechanicalDefence":
                case "AddAirUnitDefence":
                    act = (u) => Room.CreateBuff(u.Player, tbType, new Fix64[] { 1 });
                    break;
                case "SubHp20":
                    act = (u) =>
                    {
                        var ts = Room.GetUnitsInArea(pos, 10, (tar) => !tar.cfg.UnAttackable);
                        foreach (var t in ts)
                            t.Hp -= 20;
                    };
                    break;
                case "AddHp10":
                    act = (u) =>
                    {
                        var ts = Room.GetUnitsInArea(pos, 10, (tar) => !tar.cfg.UnAttackable);
                        foreach (var t in ts)
                            t.Hp += 10;
                    };
                    break;
                case "AddMoney200":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 200);
                    };
                    break;
                case "AddGas100":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Gas", 100);
                    };
                    break;
                case "AddSoldier3":
                    act = (u) =>
                    {
                        FC.For(3, (i) =>
                        {
                            u.Room.AddNewUnit(null, "Soldier", u.Pos, u.Player);
                        });
                    };
                    break;
                case "AddMoney100Gas50":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 100);
                        u.Room.AddResource(u.Player, "Gas", 50);
                    };
                    break;
                case "AddMoney150Gas75":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 150);
                        u.Room.AddResource(u.Player, "Gas", 75);
                    };
                    break;
                case "AddMoney200Gas100":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 200);
                        u.Room.AddResource(u.Player, "Gas", 100);
                    };
                    break;
                case "AddGas150":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Gas", 150);
                    };
                    break;
                case "AddMoney300":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 300);
                    };
                    break;
                case "AddGas200":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Gas", 200);
                    };
                    break;
                case "AddMoney400":
                    act = (u) =>
                    {
                        u.Room.AddResource(u.Player, "Money", 400);
                    };
                    break;
            }

            if (act != null)
            {
                var u = Room.AddNewUnit(null, "TreasureBox", pos, 0);
                tbActs[u.UID] = act;
                u.Tag = tbType;
                return u;
            }

            return null;
        }
    }
}
