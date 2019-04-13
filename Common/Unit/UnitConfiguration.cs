using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    /// <summary>
    /// 单位配置信息
    /// </summary>
    public class UnitConfigInfo
    {
        public string DisplayName; // 单位显示名称

        public int Cost; // 建造花费晶矿
        public int GasCost; // 建造花费气矿
        public Fix64 ConstructingTime; // 建造时间/冷却基准时间
        public int MaxNum; // 允许建造的最大数量，0 表示不限制
        public Fix64 MaxVelocity; // 最大速度

        public bool IsBuilding; // 是否是建筑
        public bool IsBiological; // 是否是生物
        public bool IsMechanical; // 是否是地面机械
        public bool IsAirUnit; // 是否是空中单位
        public bool UnAttackable; // 是否不可被攻击
        public Fix64 VisionRadius; // 视野半径
        public Fix64 ChaseRadius; // 追击半径
        public Fix64 Suppliment; // 占用人口
        public string[] AITypes; // AI 类型
        public Fix64[][] AIParams; // AI 参数
        public int SizeRadius; // 占位半径
        public bool NoBody; // 不占地方
        public bool NoCard; // 没有卡牌
        public bool IsFixed { get { return MaxVelocity == 0 && !NoBody; } } // 固定不动的
        public int MaxHp; // 最大血量

        // 攻击, 0 是对地， 1 是对空
        public bool CanAttackGround; // 是否能对地攻击
        public bool CanAttackAir; // 是否能对空攻击
        public string[] AttackType; // 攻击类型，light, normal, heavy
        public int[] AttackRange; // 射程
        public int[] AttackPower; // 攻击力
        public Fix64[] AttackInterval; // 攻击间隔
        public string[] AOEType; // 攻击 AOE 类型, line, fan, circle
        public Fix64[][] AOEParams; // 攻击 AOE 参数

        // 防御
        public string ArmorType; // 护甲类型, light, normal, heavy
        public int Defence; // 防御

        // 前置单位条件， 第一维之间是 or 关系，第二维之间是 and 关系。
        // 第二维的排列顺序，必须遵循以下要求：
        //     1、如果有 - 的前置条件，则必须排列在最前面
        //     2、核心建造单位，必须排列最后一个
        // 例如，Firebat 的核心建造建筑是 Barrack，是 Barrack 的数量决定其建造速度，所需科技建筑是 BioTech 或者 BioTechAOE，相斥的科技建筑是 BioTechShot
        // 则相应配置应为：
        //      Firebat.Prerequisites = new string[][] {
        //          { "-BioTechShot", "BioTech", "Barrack" }, 
        //          { "-BioTechShot", "BioTechAOE", "Barrack" }, 
        //      }
        // 又由于 BioTech, BioTechAOE, BioTechShot 之间有互斥关系，所以上述配置可以简化为：
        //      Firebat.Prerequisites = new string[][] {
        //          { "BioTech", "Barrack" }, 
        //          { "BioTechAOE", "Barrack" }, 
        //      }
        public string[][] Prerequisites;

        public string[] ReconstructTo; // 升级方向
        public string ReconstructFrom; // 从哪种单位升级而来
        public int TechLevel;//AI判断用科技等级
        public string Desc; // 详情
        public bool InVisible; // 隐身
        public bool IsObserver; // 显隐
        public Fix64 ReboundDamage; // 反伤 (比例)

        public string[] Pets; // 携带的宠物 （类型 + 数量）

        public string OriginalType; // 原始类型

        public bool IsThirdType;  // 非战斗单位&&非主建筑单位（科技 附件 仓库 改造）
    }

    /// <summary>
    /// 单位配置信息管理
    /// </summary>
    public class UnitConfiguration : Component
    {
        static UnitConfiguration Instance
        {
            get { return instance; }
        } static UnitConfiguration instance = null;

        // 所有单位配置信息
        StableDictionary<string, UnitConfigInfo> cfgs = new StableDictionary<string, UnitConfigInfo>();

        // 单位解锁触发条件 （积分）
        List<int> ulcfgs = new List<int>();

        public static List<int> Ulcfgs
        {
            get { return Instance.ulcfgs; }
        }

        public static string[] AllUnitTypes
        {
            get { return Instance.cfgs.KeyArray; }
        }

        // 所有原始单位配置信息
        public static string[] AllOriginalUnitTypes
        {
            get
            {
                List<string> orgKeys = new List<string>();

                var allKeys = Instance.cfgs.KeyArray;

                for (int i = 0; i < allKeys.Length; i++)
                {
                    if (string.IsNullOrEmpty(Instance.cfgs[allKeys[i]].OriginalType))
                    {
                        orgKeys.Add(allKeys[i]);
                    }
                }
                return orgKeys.ToArray();
            }
        }

        public static void GetUnitCfgsFromServer(IReadableBuffer data)
        {
            Instance.cfgs.Clear();

            var allUnitTypes = data.ReadStringArr();

            for (int i = 0; i < allUnitTypes.Length; i++)
            {
                Instance.cfgs[allUnitTypes[i]] = UnitUtils.ReadUnitInfo(data);
            }
        }

        public UnitConfiguration()
        {
            if (instance != null)
                throw new Exception("only one UnitConfiguration should be created.");

            instance = this;
            BuildAll();
            BuildUlc();
        }

        public static UnitConfigInfo GetDefaultConfig(string type)
        {
            return Instance.cfgs.ContainsKey(type) ? Instance.cfgs[type] : null;
        }

        public static void SetDefaultConfig(string type, UnitConfigInfo info)
        {
            if (!Instance.cfgs.ContainsKey(type))
                return;
            
            Instance.cfgs [type] = info;
        }

        public static string GetMainBuilder(string type)
        {
            var cfg = GetDefaultConfig(type);
            var pres = cfg.Prerequisites;
            if (pres == null)
                return null;

            return pres[0][pres[0].Length - 1];
        }

        void BuildUlc()
        {
            ulcfgs.AddRange(new int[] { 1, 2, 3, 4, 5, 7, 9, 11, 13, 15, 18, 21, 24, 27, 30, 34, 38, 42, 46, 50, 55, 60, 65, 70, 75 });
        }

        void BuildAll()
        {
            BuildBuildings();
            BuildBattleUnits();
            BuildOthers();
        }

        void BuildBuildings()
        {
            var bs = new UnitConfigInfo();
            bs.DisplayName = "基地";
            bs.Cost = 250;
            bs.ConstructingTime = 75;
            bs.IsBuilding = true;
            bs.SizeRadius = 3;
            bs.MaxHp = 800;
            bs.ArmorType = "heavy";
            bs.Defence = 0;
            bs.ReconstructTo = new string[] { "CommanderCenter", "Fortress" };
            bs.NoBody = true;
            //bs.NoCard = true;
            cfgs["Base"] = bs;

            var ft = new UnitConfigInfo();
            ft.DisplayName = "军事要塞";
            ft.Cost = 150;
            ft.GasCost = 100;
            ft.ConstructingTime = 25;
            ft.IsBuilding = true;
            ft.SizeRadius = 3;
            ft.MaxHp = 1000;
            ft.ArmorType = "heavy";
            ft.Defence = 0;
            ft.CanAttackGround = true;
            ft.AttackType = new string[] { "normal" };
            ft.AttackInterval = new Fix64[] { 1, 0 };
            ft.AttackPower = new int[] { 12, 0 };
            ft.AttackRange = new int[] { 30, 0 };
            ft.AITypes = new string[] { "HoldAndAttack" };
            ft.ReconstructFrom = "Base";
            ft.NoBody = true;
            ft.Prerequisites = new string[][] { new string[] { "Barrack" } };
            ft.Desc = "大型防御工事，具有很远的射程。";
            ft.IsThirdType = true;
            cfgs["Fortress"] = ft;
    
            var cc = new UnitConfigInfo();
            cc.DisplayName = "指挥中心";
            cc.Cost = 100;
            cc.GasCost = 50;
            cc.ConstructingTime = 25;
            cc.IsBuilding = true;
            cc.SizeRadius = 3;
            cc.MaxHp = 800;
            cc.ArmorType = "heavy";
            cc.Defence = 1;
            cc.ReconstructFrom = "Base";
            cc.NoBody = true;
            cc.AttackRange = new int[] { 25, 25 };
            cc.Prerequisites = new string[][] { new string[] { "Factory" } };
            cc.VisionRadius = 25;
            cc.AITypes = new string[] { "ObserverBuilding" };
            cc.Desc = "能侦查到一定范围内的隐形单位。";
            cc.IsObserver = true;
            cc.IsThirdType = true;
            cfgs["CommanderCenter"] = cc;

            var cm = new UnitConfigInfo();
            cm.DisplayName = "矿机";
            cm.Cost = 50;
            cm.ConstructingTime = 10;
            cm.IsBuilding = true;
            cm.SizeRadius = 1;
            cm.NoBody = true; // 不单独占地方
            //cm.NoCard = true;
            cm.MaxHp = 100;
            cm.ArmorType = "light";
            cm.Defence = 0;
            cm.AITypes = new string[] { "GenMoney" };
            cm.AIParams = new Fix64[][] { new Fix64[] { 2 } };
            cm.Prerequisites = new string[][] {
                new string[] { "Base" },
                new string[] { "CommanderCenter" },
                new string[] { "Fortress" },
            };
            cm.IsThirdType = true;
            cfgs["CrystalMachine"] = cm;

            var acc = new UnitConfigInfo();
            acc.DisplayName = "仓库";
            acc.Cost = 50;
            acc.ConstructingTime = 10;
            acc.IsBuilding = true;
            acc.SizeRadius = 1;
            acc.NoBody = true; // 不单独占地方
            //acc.NoCard = true;
            acc.MaxHp = 100;
            acc.ArmorType = "light";
            acc.Defence = 0;
            acc.Prerequisites = new string[][] {
                new string[] { "Barrack" },
                new string[] { "Factory" },
                new string[] { "Airport" },
            };
            acc.IsThirdType = true;
            cfgs["Accessory"] = acc;

            var gf = new UnitConfigInfo();
            gf.DisplayName = "瓦斯厂";
            gf.Cost = 200;
            gf.ConstructingTime = 18;
            gf.IsBuilding = true;
            gf.SizeRadius = 3;
            gf.MaxHp = 450;
            gf.ArmorType = "heavy";
            gf.Defence = 0;
            gf.AITypes = new string[] { "GenGas" };
            gf.AIParams = new Fix64[][] { new Fix64[] { 3 } };
            gf.Prerequisites = new string[][] {
                new string[] { "Base" },
                new string[] { "CommanderCenter" },
                new string[] { "Fortress" },
            };
            cfgs["GasFactory"] = gf;

            var brk = new UnitConfigInfo();
            brk.DisplayName = "兵营";
            brk.Cost = 150;
            brk.ConstructingTime = 35;
            brk.IsBuilding = true;
            brk.SizeRadius = 3;
            brk.MaxHp = 600;
            brk.ArmorType = "heavy";
            brk.Defence = 0;
            brk.Prerequisites = new string[][] {
                new string[] { "Base" },
                new string[] { "CommanderCenter" },
                new string[] { "Fortress" },
            };
            cfgs["Barrack"] = brk;

            var fg = new UnitConfigInfo();
            fg.DisplayName = "守卫";
            fg.IsBuilding = true;
            fg.Cost = 150;
            fg.ConstructingTime = 15;
            fg.SizeRadius = 2;
            fg.MaxHp = 125;
            fg.VisionRadius = 10;
            fg.CanAttackGround = true;
            fg.AttackType = new string[] { "normal" };
            fg.AttackInterval = new Fix64[] { 1, 0 };
            fg.AttackPower = new int[] { 18, 0 };
            fg.AttackRange = new int[] { 10, 0 };
            fg.ArmorType = "normal";
            fg.AITypes = new string[] { "HoldAndAttack" };
            fg.Prerequisites = new string[][] { new string[] { "Barrack" } };
            fg.Desc = "小型防御工事，能侦测到一定范围内的隐形单位。";
            fg.IsObserver = true;
            cfgs["FireGuard"] = fg;

            var fg1 = new UnitConfigInfo();
            fg1.DisplayName = "守卫";
            fg1.IsBuilding = true;
            fg1.Cost = 150;
            fg1.ConstructingTime = 15;
            fg1.SizeRadius = 2;
            fg1.MaxHp = 125;
            fg1.VisionRadius = 10;
            fg1.CanAttackAir = true;
            fg1.AttackType = new string[] { null,"normal" };
            fg1.AttackInterval = new Fix64[] { 0, 1 };
            fg1.AttackPower = new int[] { 0, 18 };
            fg1.AttackRange = new int[] { 0, 10 };
            fg1.ArmorType = "normal";
            fg1.AITypes = new string[] { "HoldAndAttack" };
            fg1.Prerequisites = new string[][] { new string[] { "Barrack" } };
            fg1.Desc = "防空型守卫，能攻击单个空中单位。";
            fg1.IsObserver = true;
            fg1.OriginalType = "FireGuard";
            cfgs["FireGuardAir"] = fg1;

            var fg2 = new UnitConfigInfo();
            fg2.DisplayName = "守卫";
            fg2.IsBuilding = true;
            fg2.Cost = 150;
            fg2.ConstructingTime = 15;
            fg2.SizeRadius = 2;
            fg2.MaxHp = 125;
            fg2.VisionRadius = 8;
            fg2.CanAttackGround = true;
            fg2.AttackType = new string[] { "light" };
            fg2.AOEType = new string[] { "fan" };
            fg2.AOEParams = new Fix64[][] { new Fix64[] { 9, 90 } };
            fg2.AttackInterval = new Fix64[] { 1.5, 0 };
            fg2.AttackPower = new int[] { 10, 0 };
            fg2.AttackRange = new int[] { 8, 0 };
            fg2.ArmorType = "normal";
            fg2.AITypes = new string[] { "HoldAndAttack" };
            fg2.Prerequisites = new string[][] { new string[] { "Barrack" } };
            fg2.Desc = "能攻击一定范围内的所有地面单位的守卫，但攻击力较低。";
            fg2.IsObserver = true;
            fg2.OriginalType = "FireGuard";
            cfgs["FireGuardAOE"] = fg2;

            var bioTech = new UnitConfigInfo();
            bioTech.DisplayName = "生物科技";
            bioTech.Cost = 150;
            //bioTech.GasCost = 75;
            bioTech.ConstructingTime = 35;
            bioTech.IsBuilding = true;
            bioTech.SizeRadius = 3;
            bioTech.MaxHp = 500;
            bioTech.ArmorType = "heavy";
            bioTech.Defence = 0;
            bioTech.Prerequisites = new string[][] { new string[] { "-BioTech", "-BioTechAOE", "-BioTechShot", "Barrack" } };
            bioTech.ReconstructTo = new string[] { "BioTechAOE", "BioTechShot" };
            cfgs["BioTech"] = bioTech;

            var bioTechAOE = new UnitConfigInfo();
            bioTechAOE.DisplayName = "磁暴科技";
            bioTechAOE.Cost = 150;
            bioTechAOE.GasCost = 100;
            bioTechAOE.ConstructingTime = 35;
            bioTechAOE.IsBuilding = true;
            bioTechAOE.SizeRadius = 3;
            bioTechAOE.MaxHp = 500;
            bioTechAOE.ArmorType = "heavy";
            bioTechAOE.Defence = 0;
            bioTechAOE.Prerequisites = new string[][] { new string[] { "-BioTechShot", "-BioTechAOE", "BioTech" } };
            bioTechAOE.ReconstructFrom = "BioTech";
            bioTechAOE.Desc = "研究后解锁磁暴蛛。";
            bioTechAOE.IsThirdType = true;
            cfgs["BioTechAOE"] = bioTechAOE;

            var bioTechShot = new UnitConfigInfo();
            bioTechShot.DisplayName = "狙击科技";
            bioTechShot.Cost = 150;
            bioTechShot.GasCost = 100;
            bioTechShot.ConstructingTime = 35;
            bioTechShot.IsBuilding = true;
            bioTechShot.SizeRadius = 3;
            bioTechShot.MaxHp = 500;
            bioTechShot.ArmorType = "heavy";
            bioTechShot.Defence = 0;
            bioTechShot.Prerequisites = new string[][] { new string[] { "-BioTechAOE", "-BioTechShot", "BioTech" } };
            bioTechShot.ReconstructFrom = "BioTech";
            bioTechShot.Desc = "研究后解锁狙击手。";
            bioTechShot.IsThirdType = true;
            cfgs["BioTechShot"] = bioTechShot;

            var fct = new UnitConfigInfo();
            fct.DisplayName = "工厂";
            fct.Cost = 150;
            fct.GasCost = 75;
            fct.ConstructingTime = 35;
            fct.IsBuilding = true;
            fct.SizeRadius = 3;
            fct.MaxHp = 850;
            fct.ArmorType = "heavy";
            fct.Defence = 0;
            fct.Prerequisites = new string[][] { new string[] { "Barrack" } };
            cfgs["Factory"] = fct;

            //var tg = new UnitConfigInfo();
            //tg.DisplayName = "炮塔";
            //tg.IsBuilding = true;
            //tg.Cost = 150;
            //tg.ConstructingTime = 15;
            //tg.SizeRadius = 2;
            //tg.VisionRadius = 20;
            //tg.MaxHp = 350;
            //tg.Defence = 1;
            //tg.CanAttackGround = true;
            //tg.AttackType = new string[] { "heavy", null };
            //tg.AttackInterval = new Fix64[] { 1, 0 };
            //tg.AttackPower = new int[] { 20, 0 };
            //tg.AttackRange = new int[] { 10, 0 };
            //tg.ArmorType = "normal";
            //tg.AITypes = new string[] { "HoldAndAttack" };
            //tg.Prerequisites = new string[][] { new string[] { "Factory" } };
            //cfgs["TowerGuard"] = tg;

            var velTech = new UnitConfigInfo();
            velTech.DisplayName = "车辆科技";
            velTech.Cost = 150;
            velTech.GasCost = 100;
            velTech.ConstructingTime = 35;
            velTech.IsBuilding = true;
            velTech.SizeRadius = 3;
            velTech.MaxHp = 700;
            velTech.ArmorType = "heavy";
            velTech.Defence = 0;
            velTech.Prerequisites = new string[][] { new string[] { "-VelTech", "-VelTechRobot", "-VelTechSeige", "Factory" } };
            velTech.ReconstructTo = new string[] { "VelTechRobot", "VelTechSeige" };
            cfgs["VelTech"] = velTech;

            var velTechRobot = new UnitConfigInfo();
            velTechRobot.DisplayName = "雷神科技";
            velTechRobot.Cost = 150;
            velTechRobot.GasCost = 100;
            velTechRobot.ConstructingTime = 35;
            velTechRobot.IsBuilding = true;
            velTechRobot.SizeRadius = 3;
            velTechRobot.MaxHp = 700;
            velTechRobot.ArmorType = "heavy";
            velTechRobot.Defence = 0;
            velTechRobot.Prerequisites = new string[][] { new string[] { "-VelTechRobot", "-VelTechSeige", "VelTech" } };
            velTechRobot.ReconstructFrom = "VelTech";
            velTechRobot.Desc = "研究后解锁雷神。";
            velTechRobot.IsThirdType = true;
            cfgs["VelTechRobot"] = velTechRobot;

            var velTechSeige = new UnitConfigInfo();
            velTechSeige.DisplayName = "攻城科技";
            velTechSeige.Cost = 150;
            velTechSeige.GasCost = 100;
            velTechSeige.ConstructingTime = 35;
            velTechSeige.IsBuilding = true;
            velTechSeige.SizeRadius = 3;
            velTechSeige.MaxHp = 700;
            velTechSeige.ArmorType = "heavy";
            velTechSeige.Defence = 0;
            velTechSeige.Prerequisites = new string[][] { new string[] { "-VelTechRobot", "-VelTechSeige", "VelTech" } };
            velTechSeige.ReconstructFrom = "VelTech";
            velTechSeige.Desc = "研究后解锁攻城车。";
            velTechSeige.IsThirdType = true;
            cfgs["VelTechSeige"] = velTechSeige;

            var ap = new UnitConfigInfo();
            ap.DisplayName = "机场";
            ap.Cost = 150;
            ap.GasCost = 100;
            ap.ConstructingTime = 35;
            ap.IsBuilding = true;
            ap.SizeRadius = 3;
            ap.MaxHp = 850;
            ap.ArmorType = "heavy";
            ap.Defence = 0;
            ap.Prerequisites = new string[][] { new string[] { "Factory" } };
            cfgs["Airport"] = ap;

            var airTech = new UnitConfigInfo();
            airTech.DisplayName = "飞行科技";
            airTech.Cost = 150;
            airTech.GasCost = 100;
            airTech.ConstructingTime = 35;
            airTech.IsBuilding = true;
            airTech.SizeRadius = 3;
            airTech.MaxHp = 700;
            airTech.ArmorType = "heavy";
            airTech.Defence = 0;
            airTech.Prerequisites = new string[][] { new string[] { "-AirTech", "-AirTechUltimate", "Airport" } };
            airTech.ReconstructTo = new string[] { "AirTechUltimate" };
            cfgs["AirTech"] = airTech;

            var airTechUltimate = new UnitConfigInfo();
            airTechUltimate.DisplayName = "巨舰科技";
            airTechUltimate.Cost = 150;
            airTechUltimate.GasCost = 150;
            airTechUltimate.ConstructingTime = 35;
            airTechUltimate.IsBuilding = true;
            airTechUltimate.SizeRadius = 3;
            airTechUltimate.MaxHp = 700;
            airTechUltimate.ArmorType = "heavy";
            airTechUltimate.Defence = 0;
            airTechUltimate.Prerequisites = new string[][] { new string[] { "-AirTechUltimate", "AirTech" } };
            airTechUltimate.ReconstructFrom = "AirTech";
            airTechUltimate.Desc = "研究后解锁巨舰。";
            airTechUltimate.IsThirdType = true;
            cfgs["AirTechUltimate"] = airTechUltimate;
        }

        void BuildBattleUnits()
        {
            var radar = new UnitConfigInfo();
            radar.DisplayName = "雷达";
            radar.Cost = 30;
            radar.ConstructingTime = 0;
            radar.SizeRadius = 12;
            radar.VisionRadius = 12;
            radar.MaxHp = 1;
            radar.AIParams = new Fix64[][] { new Fix64[] { 3 } }; // 雷达持续时间
            radar.AITypes = new string[] { "Suicide" };
            radar.NoBody = true;
            radar.UnAttackable = true;
            radar.Desc = "对一定范围内进行侦查，可观察该区域动态3秒。";
            radar.IsObserver = true;
            cfgs["Radar"] = radar;

            var dog = new UnitConfigInfo();
            dog.DisplayName = "侦查犬";
            dog.IsBiological = true;
            dog.Cost = 0; // 20;
            dog.ConstructingTime = 7;
            dog.SizeRadius = 1;
            dog.VisionRadius = 8;
            dog.ChaseRadius = 8;
            dog.MaxHp = 15;
            dog.MaxVelocity = 7;
            dog.Suppliment = 1;
            dog.CanAttackGround = true;
            dog.AttackType = new string[] { "normal" };
            dog.AttackInterval = new Fix64[] { 1, 0 };
            dog.AttackPower = new int[] { 3, 0 };
            dog.AttackRange = new int[] { 2, 0 };
            dog.ArmorType = "light";
            dog.AITypes = new string[] { "MoveAndAttack" };
            dog.TechLevel = 0;
            dog.NoCard = true;
            cfgs["Dog"] = dog;

            var sd = new UnitConfigInfo();
            sd.DisplayName = "机枪兵";
            sd.IsBiological = true;
            sd.Cost = 80;
            sd.ConstructingTime = 13;
            sd.SizeRadius = 1;
            sd.VisionRadius = 8;
            sd.ChaseRadius = 8;
            sd.MaxHp = 60;
            sd.MaxVelocity = 5;
            sd.Suppliment = 1;
            sd.CanAttackAir = true;
            sd.CanAttackGround = true;
            sd.AttackType = new string[] { "normal", "normal" };
            sd.AttackInterval = new Fix64[] { 1,1};
            sd.AttackPower = new int[] { 6, 6 };
            sd.AttackRange = new int[] { 5, 5 };
            sd.ArmorType = "light";
            sd.AITypes = new string[] { "MoveAndAttack" };
            sd.Prerequisites = new string[][] { new string[] { "Barrack" } };
            sd.TechLevel = 1;
            sd.Desc = "廉价但全能，在较早时期就能形成较大规模。";
            cfgs["Soldier"] = sd;

            var sd1 = new UnitConfigInfo();
            sd1.DisplayName = "机枪兵";
            sd1.IsBiological = true;
            sd1.Cost = 80;
            sd1.ConstructingTime = 13;
            sd1.SizeRadius = 1;
            sd1.VisionRadius = 8;
            sd1.ChaseRadius = 8;
            sd1.MaxHp = 25;
            sd1.MaxVelocity = 7;
            sd1.Suppliment = 1;
            sd1.CanAttackAir = true;
            sd1.CanAttackGround = true;
            sd1.AttackType = new string[] { "normal", "normal" };
            sd1.AttackInterval = new Fix64[] { 1, 1 };
            sd1.AttackPower = new int[] { 6, 6 };
            sd1.AttackRange = new int[] { 5, 5 };
            sd1.ArmorType = "light";
            sd1.AITypes = new string[] { "MoveAndAttack" };
            sd1.Prerequisites = new string[][] { new string[] { "Barrack" } };
            sd1.TechLevel = 1;
            sd1.Pets = new string[] { "Dog", "2" };
            sd1.Desc = "机枪兵带领着两只侦查犬的作战小队，行动迅速。";
            sd1.OriginalType = "Soldier";
            cfgs["SoldierWithDog"] = sd1;

            var sd2 = new UnitConfigInfo();
            sd2.DisplayName = "机枪兵";
            sd2.IsBiological = true;
            sd2.Cost = 80;
            sd2.ConstructingTime = 13;
            sd2.SizeRadius = 1;
            sd2.VisionRadius = 8;
            sd2.ChaseRadius = 8;
            sd2.MaxHp = 30;
            sd2.MaxVelocity = 5;
            sd2.Suppliment = 1;
            sd2.CanAttackAir = true;
            sd2.CanAttackGround = true;
            sd2.AttackType = new string[] { "normal", "normal" };
            sd2.AttackInterval = new Fix64[] { 0.5, 0.5 };
            sd2.AttackPower = new int[] { 6, 6 };
            sd2.AttackRange = new int[] { 5, 5 };
            sd2.ArmorType = "light";
            sd2.AITypes = new string[] { "MoveAndAttack" };
            sd2.Prerequisites = new string[][] { new string[] { "Barrack" } };
            sd2.TechLevel = 1;
            sd2.Desc = "经过改造拥有更快速的攻击效率，但相较之下更脆弱。";
            sd2.OriginalType = "Soldier";
            cfgs["SoldierFast"] = sd2;

            var fb = new UnitConfigInfo();
            fb.DisplayName = "喷火兵";
            fb.IsBiological = true;
            fb.Cost = 100;
            fb.GasCost = 50;
            fb.ConstructingTime = 16;
            fb.SizeRadius = 1;
            fb.VisionRadius = 8;
            fb.ChaseRadius = 8;
            fb.MaxHp = 100;
            fb.Defence =0;
            fb.MaxVelocity = 5;
            fb.Suppliment = 2;
            fb.CanAttackGround = true;
            fb.AttackType = new string[] { "light" };
            fb.AttackInterval = new Fix64[] { 1, 0 };
            fb.AttackPower = new int[] { 10, 0 };
            fb.AttackRange = new int[] { 5, 0 };
            fb.ArmorType = "heavy";
            fb.AITypes = new string[] { "MoveAndAttack" };
            fb.Prerequisites = new string[][] {
                new string[] { "BioTech", "Barrack" },
                new string[] { "BioTechShot", "Barrack" },
                new string[] { "BioTechAOE", "Barrack" }
            };
            fb.TechLevel = 2;
            fb.Desc = "令轻型单位闻风丧胆的敌人。";
            cfgs["Firebat"] = fb;

            var fb1 = new UnitConfigInfo();
            fb1.DisplayName = "喷火兵";
            fb1.IsBiological = true;
            fb1.Cost = 100;
            fb1.GasCost = 50;
            fb1.ConstructingTime = 16;
            fb1.SizeRadius = 1;
            fb1.VisionRadius = 8;
            fb1.ChaseRadius = 8;
            fb1.MaxHp = 100;
            fb1.Defence = 0;
            fb1.MaxVelocity = 5;
            fb1.Suppliment = 2;
            fb1.CanAttackGround = true;
            fb1.AttackType = new string[] { "light" };
            fb1.AOEType = new string[] { "fan" };
            fb1.AOEParams = new Fix64[][] { new Fix64[] { 6, 135 } };
            fb1.AttackInterval = new Fix64[] { 1.5, 0 };
            fb1.AttackPower = new int[] { 8, 0 };
            fb1.AttackRange = new int[] { 5, 0 };
            fb1.ArmorType = "heavy";
            fb1.AITypes = new string[] { "MoveAndAttack" };
            fb1.Prerequisites = new string[][] {
                new string[] { "BioTech", "Barrack" },
                new string[] { "BioTechShot", "Barrack" },
                new string[] { "BioTechAOE", "Barrack" }
            };
            fb1.TechLevel = 2;
            fb1.Desc = "改进后武器能造成范围杀伤，但攻击力和攻击速度有所削弱。";
            fb1.OriginalType = "Firebat";
            cfgs["FirebatAOE"] = fb1;

            var fb2 = new UnitConfigInfo();
            fb2.DisplayName = "喷火兵";
            fb2.IsBiological = true;
            fb2.Cost = 100;
            fb2.GasCost = 50;
            fb2.ConstructingTime = 16;
            fb2.SizeRadius = 1;
            fb2.VisionRadius = 8;
            fb2.ChaseRadius = 8;
            fb2.MaxHp = 50;
            fb2.Defence = 0;
            fb2.MaxVelocity = 5;
            fb2.Suppliment = 2;
            fb2.CanAttackGround = true;
            fb2.AttackType = new string[] { "light" };
            fb2.AttackInterval = new Fix64[] { 0.75, 0 };
            fb2.AttackPower = new int[] { 12, 0 };
            fb2.AttackRange = new int[] { 5, 0 };
            fb2.ArmorType = "heavy";
            fb2.AITypes = new string[] { "MoveAndAttack" };
            fb2.Prerequisites = new string[][] {
                new string[] { "BioTech", "Barrack" },
                new string[] { "BioTechShot", "Barrack" },
                new string[] { "BioTechAOE", "Barrack" }
            };
            fb2.TechLevel = 2;
            fb2.Desc = "经过改造拥有更高的攻击效率，但相较之下更脆弱。";
            fb2.OriginalType = "Firebat";
            cfgs["FirebatFast"] = fb2;

            // 磁暴蜘蛛
            var mspd = new UnitConfigInfo();
            mspd.DisplayName = "磁暴蛛";
            mspd.IsMechanical = true;
            mspd.Cost = 100;
            mspd.GasCost = 70;
            mspd.ConstructingTime = 15;
            mspd.SizeRadius = 1;
            mspd.VisionRadius = 8;
            mspd.ChaseRadius = 8;
            mspd.MaxHp = 50;
            mspd.Defence = 0;
            mspd.MaxVelocity = 8;
            mspd.Suppliment = 1;
            mspd.CanAttackGround = true;
            mspd.AttackType = new string[] { "heavy" };
            mspd.AOEType = new string[] { "circle" };
            mspd.AOEParams = new Fix64[][] { new Fix64[] { 6 /* 伤害范围 */ } };
            mspd.AttackInterval = new Fix64[] { 9999, 0 };
            mspd.AttackPower = new int[] { 50, 0 };
            mspd.AttackRange = new int[] { 3, 0 };
            mspd.ArmorType = "normal";
            mspd.AITypes = new string[] { "MoveAndSelfExplode" };
            mspd.AIParams = new Fix64[][] { new Fix64[] { 3 /* 瘫痪机械单位时间 */ } };
            mspd.Prerequisites = new string[][] {
                new string[] { "BioTechAOE", "Barrack" },
            };
            mspd.TechLevel = 3;
            mspd.Desc = "利用灵活的身躯快速靠近敌人，爆炸还能影响机械的正常运转。";
            cfgs["MagSpider"] = mspd;

            var mspd1 = new UnitConfigInfo();
            mspd1.DisplayName = "磁暴蛛";
            mspd1.IsMechanical = true;
            mspd1.Cost = 100;
            mspd1.GasCost = 70;
            mspd1.ConstructingTime = 15;
            mspd1.SizeRadius = 1;
            mspd1.VisionRadius = 8;
            mspd1.ChaseRadius = 8;
            mspd1.MaxHp = 50;
            mspd1.Defence = 0;
            mspd1.MaxVelocity = 8;
            mspd1.Suppliment = 1;
            mspd1.CanAttackGround = true;
            mspd1.AttackType = new string[] { "heavy" };
            mspd1.AOEType = new string[] { "circle" };
            mspd1.AOEParams = new Fix64[][] { new Fix64[] { 6 /* 伤害范围 */ } };
            mspd1.AttackInterval = new Fix64[] { 9999, 0 };
            mspd1.AttackPower = new int[] { 50, 0 };
            mspd1.AttackRange = new int[] { 3, 0 };
            mspd1.ArmorType = "normal";
            mspd1.AITypes = new string[] { "Landmine" };
            mspd1.AIParams = new Fix64[][] { new Fix64[] { 3 /* 瘫痪机械单位时间 */ } };
            mspd1.Prerequisites = new string[][] {
                new string[] { "BioTechAOE", "Barrack" },
            };
            mspd1.TechLevel = 3;
            mspd1.InVisible = true;
            mspd1.Desc = "地雷改造型，但能被反隐手段发现。";
            mspd1.OriginalType = "MagSpider";
            cfgs["MagSpiderLandmine"] = mspd1;

            var mspd2 = new UnitConfigInfo();
            mspd2.DisplayName = "磁暴蛛";
            mspd2.IsMechanical = true;
            mspd2.Cost = 50;
            mspd2.GasCost = 35;
            mspd2.ConstructingTime = 8;
            mspd2.SizeRadius = 1;
            mspd2.VisionRadius = 8;
            mspd2.ChaseRadius = 8;
            mspd2.MaxHp = 50;
            mspd2.Defence = 0;
            mspd2.MaxVelocity = 8;
            mspd2.Suppliment = 1;
            mspd2.CanAttackGround = true;
            mspd2.AttackType = new string[] { "heavy" };
            mspd2.AttackInterval = new Fix64[] { 9999, 0 };
            mspd2.AttackPower = new int[] { 50, 0 };
            mspd2.AttackRange = new int[] { 3, 0 };
            mspd2.ArmorType = "normal";
            mspd2.AITypes = new string[] { "MoveAndSelfExplode" };
            mspd2.AIParams = new Fix64[][] { new Fix64[] { 3 /* 瘫痪机械单位时间 */ } };
            mspd2.Prerequisites = new string[][] {
                new string[] { "BioTechAOE", "Barrack" },
            };
            mspd2.TechLevel = 3;
            mspd2.Desc = "轻量化改造，但只能对单个单位造成伤害和瘫痪。";
            mspd2.OriginalType = "MagSpider";
            cfgs["MagSpiderSingle"] = mspd2;

            // 狙击手
            var ghost = new UnitConfigInfo();
            ghost.DisplayName = "狙击手";
            ghost.IsBiological = true;
            ghost.Cost = 160;
            ghost.GasCost = 100;
            ghost.ConstructingTime = 23;
            ghost.SizeRadius = 1;
            ghost.VisionRadius = 10;
            ghost.ChaseRadius = 10;
            ghost.MaxHp = 120;
            ghost.Defence = 0;
            ghost.MaxVelocity = 5;
            ghost.Suppliment = 1;
            ghost.CanAttackGround = true;
            ghost.CanAttackAir = true;
            ghost.AttackType = new string[] { "normal", "light" };
            ghost.AttackInterval = new Fix64[] { 1, 1 };
            ghost.AttackPower = new int[] { 22,22 };
            ghost.AttackRange = new int[] { 10, 10 };
            ghost.ArmorType = "light";
            ghost.AITypes = new string[] { "MoveAndAttack" };
            ghost.Prerequisites = new string[][] {
                new string[] { "BioTechShot", "Barrack" },
            };
            ghost.TechLevel = 3;
            ghost.Desc = "超长的射程就是她最强的武器，敌人还未来得及靠近就可能倒下了。";
            cfgs["Ghost"] = ghost;

            var ghost1 = new UnitConfigInfo();
            ghost1.DisplayName = "狙击手";
            ghost1.IsBiological = true;
            ghost1.Cost = 160;
            ghost1.GasCost = 100;
            ghost1.ConstructingTime = 23;
            ghost1.SizeRadius = 1;
            ghost1.VisionRadius = 10;
            ghost1.ChaseRadius = 10;
            ghost1.MaxHp = 100;
            ghost1.Defence = 0;
            ghost1.MaxVelocity = 5;
            ghost1.Suppliment = 1;
            ghost1.CanAttackGround = true;
            ghost1.CanAttackAir = true;
            ghost1.AttackType = new string[] { "normal", "light" };
            ghost1.AttackInterval = new Fix64[] { 1, 1 };
            ghost1.AttackPower = new int[] { 22, 22 };
            ghost1.AttackRange = new int[] { 10, 10 };
            ghost1.ArmorType = "light";
            ghost1.AITypes = new string[] { "MoveAndAttack" };
            ghost1.Prerequisites = new string[][] {
                new string[] { "BioTechShot", "Barrack" },
            };
            ghost1.TechLevel = 3;
            ghost1.Desc = "身着隐形迷彩的狙击手，在没被发现时都是巨大的威胁。";
            ghost1.OriginalType = "Ghost";
            ghost1.InVisible = true;
            cfgs["GhostInvisible"] = ghost1;

            var ghost2 = new UnitConfigInfo();
            ghost2.DisplayName = "狙击手";
            ghost2.IsBiological = true;
            ghost2.Cost = 160;
            ghost2.GasCost = 100;
            ghost2.ConstructingTime = 23;
            ghost2.SizeRadius = 1;
            ghost2.VisionRadius = 10;
            ghost2.ChaseRadius = 10;
            ghost2.MaxHp = 100;
            ghost2.Defence = 0;
            ghost2.MaxVelocity = 5;
            ghost2.Suppliment = 1;
            ghost2.CanAttackGround = true;
            ghost2.CanAttackAir = true;
            ghost2.AttackType = new string[] { "normal", "light" };
            ghost2.AttackInterval = new Fix64[] { 1, 1 };
            ghost2.AttackPower = new int[] { 22, 22 };
            ghost2.AttackRange = new int[] { 10, 10 };
            ghost2.ArmorType = "light";
            ghost2.AITypes = new string[] { "MoveAndAttackWithDecelerate" };
            ghost2.AIParams = new Fix64[][] { new Fix64[] { 2 /* 减速单位时间 */, 5 /* 减速量 */} };
            ghost2.Prerequisites = new string[][] {
                new string[] { "BioTechShot", "Barrack" },
            };
            ghost2.TechLevel = 3;
            ghost2.Desc = "配备震荡弹的狙击手，能减缓敌方单位的行进速度。";
            ghost2.OriginalType = "Ghost";
            cfgs["GhostSlow"] = ghost2;


            //var ft = new UnitConfigInfo();
            //ft.DisplayName = "喷火车";
            //ft.IsMechanical = true;
            //ft.Cost = 100;
            //ft.ConstructingTime = 21;
            //ft.SizeRadius = 2;
            //ft.VisionRadius = 15;
            //ft.MaxHp = 90;
            //ft.MaxVelocity = 6;
            //ft.Suppliment = 2;
            //ft.CanAttackGround = true;
            //ft.AttackType = new string[] { "light" };
            //ft.AOEType = new string[] { "fan" };
            //ft.AOEParams = new Fix64[][] { new Fix64[] { 5, 135 } };
            //ft.AttackInterval = new Fix64[] { 1, 0 };
            //ft.AttackPower = new int[] { 8, 0 };
            //ft.AttackRange = new int[] { 5, 0 };
            //ft.ArmorType = "light";
            //ft.AITypes = new string[] { "MoveAndAttack" };
            //ft.Prerequisites = new string[][] { new string[] { "-VelTech", "-VelTechRobot", "-VelTechSeige", "Factory" } };
            //cfgs["FireTrunk"] = ft;

            var rb = new UnitConfigInfo();
            rb.DisplayName = "机器人";
            rb.IsMechanical = true;
            rb.Cost = 120;
            rb.GasCost = 60;
            rb.ConstructingTime = 20;
            rb.SizeRadius = 2;
            rb.VisionRadius = 8;
            rb.ChaseRadius = 8;
            rb.MaxHp = 120;
            rb.Defence = 0;
            rb.MaxVelocity = 5;
            rb.Suppliment = 2;
            rb.CanAttackAir = true;
            rb.CanAttackGround = true;
            rb.AttackType = new string[] { "normal", "light" };
            rb.AttackInterval = new Fix64[] { 1, 1 };
            rb.AttackPower = new int[] { 15,15 };
            rb.AttackRange = new int[] { 7, 7 };
            rb.ArmorType = "normal";
            rb.AITypes = new string[] { "MoveAndAttack" };
            rb.Prerequisites = new string[][] { new string[] { "Factory" } };
            rb.TechLevel = 2;
            rb.Desc = "从其极其简单的构造能看出这是专为战争制造的机械，方便且高效。";
            cfgs["Robot"] = rb;

            var rb1 = new UnitConfigInfo();
            rb1.DisplayName = "机器人";
            rb1.IsMechanical = true;
            rb1.Cost = 120;
            rb1.GasCost = 60;
            rb1.ConstructingTime = 20;
            rb1.SizeRadius = 2;
            rb1.VisionRadius = 8;
            rb1.ChaseRadius = 8;
            rb1.MaxHp = 120;
            rb1.Defence = 0;
            rb1.MaxVelocity = 5;
            rb1.Suppliment = 2;
            rb1.CanAttackGround = true;
            rb1.CanAttackAir = true;
            rb1.AttackType = new string[] { "normal", "light" };
            rb1.AttackInterval = new Fix64[] { 1, 1 };
            rb1.AttackPower = new int[] { 8, 18 };
            rb1.AttackRange = new int[] { 7, 7 };
            rb1.ArmorType = "normal";
            rb1.AITypes = new string[] { "MoveAndAttack" };
            rb1.Prerequisites = new string[][] { new string[] { "Factory" } };
            rb1.TechLevel = 2;
            rb1.Desc = "对空强化型机器人，但对地攻击降低。";
            rb1.OriginalType = "Robot";
            cfgs["RobotAir"] = rb1;

            var rb2 = new UnitConfigInfo();
            rb2.DisplayName = "机器人";
            rb2.IsMechanical = true;
            rb2.Cost = 120;
            rb2.GasCost = 60;
            rb2.ConstructingTime = 20;
            rb2.SizeRadius = 2;
            rb2.VisionRadius = 8;
            rb2.ChaseRadius = 8;
            rb2.MaxHp = 120;
            rb2.Defence = 0;
            rb2.MaxVelocity = 5;
            rb2.Suppliment = 2;
            rb2.CanAttackGround = true;
            rb2.AttackType = new string[] { "normal", null };
            rb2.AttackInterval = new Fix64[] { 1, 0 };
            rb2.AttackPower = new int[] { 18, 0 };
            rb2.AttackRange = new int[] { 7, 0 };
            rb2.ArmorType = "normal";
            rb2.AITypes = new string[] { "MoveAndAttack" };
            rb2.Prerequisites = new string[][] { new string[] { "Factory" } };
            rb2.TechLevel = 2;
            rb2.Desc = "对地强化型机器人，不再拥有对空攻击。";
            rb2.OriginalType = "Robot";
            cfgs["RobotGround"] = rb2;

            var tank = new UnitConfigInfo();
            tank.DisplayName = "坦克";
            tank.IsMechanical = true;
            tank.Cost = 215;
            tank.GasCost = 130;
            tank.ConstructingTime = 30;
            tank.SizeRadius = 2;
            tank.VisionRadius = 10;
            tank.ChaseRadius = 10;
            tank.MaxHp = 255;
            tank.Defence = 1;
            tank.MaxVelocity = 5;
            tank.Suppliment = 3;
            tank.CanAttackGround = true;
            tank.AttackType = new string[] { "heavy" };
            tank.AttackInterval = new Fix64[] { 1, 0 };
            tank.AttackPower = new int[] { 24, 0 };
            tank.AttackRange = new int[] { 8, 0 };
            tank.ArmorType = "heavy";
            tank.AITypes = new string[] { "MoveAndAttack" };
            tank.Prerequisites = new string[][] {
                new string[] { "VelTech", "Factory" },
                new string[] { "VelTechRobot", "Factory" },
                new string[] { "VelTechSeige", "Factory" }
            };
            tank.TechLevel = 3;
            tank.Desc = "在这片战场也依然是陆战的代表，以压倒一切的气势前进。";
            cfgs["Tank"] = tank;

            var tank1 = new UnitConfigInfo();
            tank1.DisplayName = "坦克";
            tank1.IsMechanical = true;
            tank1.Cost = 215;
            tank1.GasCost = 130;
            tank1.ConstructingTime = 30;
            tank1.SizeRadius = 2;
            tank1.VisionRadius = 10;
            tank1.ChaseRadius = 10;
            tank1.MaxHp = 270;
            tank1.Defence = 1;
            tank1.MaxVelocity = 5;
            tank1.Suppliment = 3;
            tank1.CanAttackGround = true;
            tank1.CanAttackAir = true;
            tank1.AttackType = new string[] { "heavy", "light" };
            tank1.AttackInterval = new Fix64[] { 1, 1 };
            tank1.AttackPower = new int[] { 10, 20 };
            tank1.AttackRange = new int[] { 8, 8 };
            tank1.ArmorType = "heavy";
            tank1.AITypes = new string[] { "MoveAndAttack" };
            tank1.Prerequisites = new string[][] {
                new string[] { "VelTech", "Factory" },
                new string[] { "VelTechRobot", "Factory" },
                new string[] { "VelTechSeige", "Factory" }
            };
            tank1.TechLevel = 3;
            tank1.Desc = "主要武器改为火箭炮，拥有对空火力但削弱了对地火力。";
            tank1.OriginalType = "Tank";
            cfgs["TankAir"] = tank1;

            var tank2 = new UnitConfigInfo();
            tank2.DisplayName = "坦克";
            tank2.IsMechanical = true;
            tank2.Cost = 215;
            tank2.GasCost = 130;
            tank2.ConstructingTime = 30;
            tank2.SizeRadius = 2;
            tank2.VisionRadius = 10;
            tank2.ChaseRadius = 10;
            tank2.MaxHp = 200;
            tank2.Defence = 1;
            tank2.MaxVelocity = 5;
            tank2.Suppliment = 3;
            tank2.CanAttackGround = true;
            tank2.AttackType = new string[] { "heavy", null };
            tank2.AOEType = new string[] { "circle" };
            tank2.AOEParams = new Fix64[][] { new Fix64[] { 5 } };
            tank2.AttackInterval = new Fix64[] { 1, 0 };
            tank2.AttackPower = new int[] { 18, 0 };
            tank2.AttackRange = new int[] { 8, 0 };
            tank2.ArmorType = "heavy";
            tank2.AITypes = new string[] { "MoveAndAttack" };
            tank2.Prerequisites = new string[][] {
                new string[] { "VelTech", "Factory" },
                new string[] { "VelTechRobot", "Factory" },
                new string[] { "VelTechSeige", "Factory" }
            };
            tank2.TechLevel = 3;
            tank2.Desc = "改造型加农炮，能对地面造成范围攻击。";
            tank2.OriginalType = "Tank";
            cfgs["TankGround"] = tank2;

            var thor = new UnitConfigInfo();
            thor.DisplayName = "雷神";
            thor.IsMechanical = true;
            thor.Cost = 300;
            thor.GasCost = 215;
            thor.ConstructingTime = 40;
            thor.SizeRadius = 2;
            thor.VisionRadius = 11;
            thor.ChaseRadius = 11;
            thor.MaxHp = 450;
            thor.Defence = 2;
            thor.MaxVelocity = 4;
            thor.Suppliment = 2;
            thor.CanAttackAir = true;
            thor.CanAttackGround = true;
            thor.AttackType = new string[] { "heavy", "light" };
            thor.AttackInterval = new Fix64[] { 1, 1 };
            thor.AttackPower = new int[] { 24, 24 };
            thor.AttackRange = new int[] { 8, 8 };
            thor.ArmorType = "heavy";
            thor.AITypes = new string[] { "MoveAndAttack" };
            thor.Prerequisites = new string[][] { new string[] { "VelTechRobot", "Factory" } };
            thor.TechLevel = 4;
            thor.Desc = "高科技机甲单位，作为陆战之王还拥有对空的攻击火力。";
            cfgs["Thor"] = thor;

            var thor1 = new UnitConfigInfo();
            thor1.DisplayName = "雷神";
            thor1.IsMechanical = true;
            thor1.Cost = 300;
            thor1.GasCost = 215;
            thor1.ConstructingTime = 40;
            thor1.SizeRadius = 2;
            thor1.VisionRadius = 11;
            thor1.ChaseRadius = 11;
            thor1.MaxHp = 450;
            thor1.Defence = 2;
            thor1.MaxVelocity = 4;
            thor1.Suppliment = 2;
            thor1.CanAttackAir = true;
            thor1.CanAttackGround = true;
            thor1.AttackType = new string[] { "heavy", "normal" };
            thor1.AttackInterval = new Fix64[] { 1, 1 };
            thor1.AOEType = new string[] { null, "circle" };
            thor1.AOEParams = new Fix64[][] { null, new Fix64[] { 6 } };
            thor1.AttackPower = new int[] { 16, 20 };
            thor1.AttackRange = new int[] { 8, 8 };
            thor1.ArmorType = "heavy";
            thor1.AITypes = new string[] { "MoveAndAttack" };
            thor1.Prerequisites = new string[][] { new string[] { "VelTechRobot", "Factory" } };
            thor1.TechLevel = 4;
            thor1.Desc = "对地攻击减弱，获得对空的范围攻击。";
            thor1.OriginalType = "Thor";
            cfgs["ThorAieAOE"] = thor1;

            var thor2 = new UnitConfigInfo();
            thor2.DisplayName = "雷神";
            thor2.IsMechanical = true;
            thor2.Cost = 300;
            thor2.GasCost = 215;
            thor2.ConstructingTime = 40;
            thor2.SizeRadius = 2;
            thor2.VisionRadius = 11;
            thor2.ChaseRadius = 11;
            thor2.MaxHp = 450;
            thor2.Defence = 2;
            thor2.MaxVelocity = 4;
            thor2.Suppliment = 2;
            thor2.CanAttackGround = true;
            thor2.AttackType = new string[] { "heavy", null };
            thor2.AttackInterval = new Fix64[] { 1, 0 };
            thor2.AttackPower = new int[] { 29, 0};
            thor2.AttackRange = new int[] { 8, 0};
            thor2.ArmorType = "heavy";
            thor2.AITypes = new string[] { "MoveAndAttack" };
            thor2.Prerequisites = new string[][] { new string[] { "VelTechRobot", "Factory" } };
            thor2.TechLevel = 4;
            thor2.Desc = "去除了对空攻击，提升对地攻击。";
            thor2.OriginalType = "Thor";
            cfgs["ThorGround"] = thor2;

            var hammer = new UnitConfigInfo();
            hammer.DisplayName = "攻城车";
            hammer.IsMechanical = true;
            hammer.Cost = 250;
            hammer.GasCost = 170;
            hammer.ConstructingTime = 30;
            hammer.SizeRadius = 2;
            hammer.VisionRadius = 8;
            hammer.ChaseRadius = 8;
            hammer.MaxHp = 150;
            hammer.Defence = 2;
            hammer.MaxVelocity = 7;
            hammer.Suppliment = 3;
            hammer.CanAttackGround = true;
            hammer.AttackType = new string[] { "heavy" };
            hammer.AttackInterval = new Fix64[] { 1.5 };
            hammer.AttackPower = new int[] { 60, 0 };
            hammer.AttackRange = new int[] { 3, 0 };
            hammer.ArmorType = "heavy";
            hammer.AITypes = new string[] { "MoveAndAttackBuilding" };
            hammer.Prerequisites = new string[][] { new string[] { "VelTechSeige", "Factory" } };
            hammer.TechLevel = 4;
            hammer.Desc = "以无法与作战单位交战为代价，换来无与伦比的攻城利器。";
            cfgs["Hammer"] = hammer;

            var hammer1 = new UnitConfigInfo();
            hammer1.DisplayName = "攻城车";
            hammer1.IsMechanical = true;
            hammer1.Cost = 250;
            hammer1.GasCost = 170;
            hammer1.ConstructingTime = 30;
            hammer1.SizeRadius = 2;
            hammer1.VisionRadius = 8;
            hammer1.ChaseRadius = 8;
            hammer1.MaxHp = 200;
            hammer1.Defence = 2;
            hammer1.MaxVelocity = 5;
            hammer1.Suppliment = 3;
            hammer1.CanAttackGround = true;
            hammer1.AttackType = new string[] { "heavy" };
            hammer1.AttackInterval = new Fix64[] { 1.5 };
            hammer1.AttackPower = new int[] { 60, 0 };
            hammer1.AttackRange = new int[] { 3, 0 };
            hammer1.ArmorType = "heavy";
            hammer1.AITypes = new string[] { "MoveAndAttackBuilding" };
            hammer1.Prerequisites = new string[][] { new string[] { "VelTechSeige", "Factory" } };
            hammer1.TechLevel = 4;
            hammer1.Desc = "周身的护盾能反弹受到的伤害，但移动得更慢了。";
            hammer1.OriginalType = "Hammer";
            hammer1.ReboundDamage = 0.5;
            cfgs["HammerCounter"] = hammer1;

            var hammer2 = new UnitConfigInfo();
            hammer2.DisplayName = "攻城车";
            hammer2.IsMechanical = true;
            hammer2.Cost = 250;
            hammer2.GasCost = 170;
            hammer2.ConstructingTime = 30;
            hammer2.SizeRadius = 2;
            hammer2.VisionRadius = 8;
            hammer2.ChaseRadius = 8;
            hammer2.MaxHp = 120;
            hammer2.Defence = 2;
            hammer2.MaxVelocity = 10;
            hammer2.Suppliment = 3;
            hammer2.CanAttackGround = true;
            hammer2.AttackType = new string[] { "heavy" };
            hammer2.AttackInterval = new Fix64[] { 1.5 };
            hammer2.AttackPower = new int[] { 60, 0 };
            hammer2.AttackRange = new int[] { 3, 0 };
            hammer2.ArmorType = "heavy";
            hammer2.AITypes = new string[] { "MoveAndAttackBuilding" };
            hammer2.Prerequisites = new string[][] { new string[] { "VelTechSeige", "Factory" } };
            hammer2.TechLevel = 4;
            hammer2.Desc = "拥有能快速突防的机动，但相对脆弱。";
            hammer2.OriginalType = "Hammer";
            cfgs["HammerFast"] = hammer2;

            var sc = new UnitConfigInfo();
            sc.DisplayName = "空降兵";
            sc.IsAirUnit = true;
            sc.Cost = 150;
            sc.GasCost = 90;
            sc.ConstructingTime = 30;
            sc.SizeRadius = 2;
            sc.NoBody = true;
            sc.VisionRadius = 8;
            sc.ChaseRadius = 8;
            sc.MaxHp = 100;
            sc.MaxVelocity = 25;
            sc.ArmorType = "normal";
            sc.AttackPower = new int[] { 1,1 };
            sc.AITypes = new string[] { "Carrier" };
            sc.Prerequisites = new string[][] { new string[] { "Airport" } };
            sc.TechLevel = 3;
            sc.Desc = "搭载3个机枪兵的运输机，可以对敌方的防守薄弱处给予沉重打击。";
            sc.Pets = new string[] { "Soldier", "3" };
            cfgs["SoldierCarrier"] = sc;

            var sc1 = new UnitConfigInfo();
            sc1.DisplayName = "空降兵";
            sc1.IsAirUnit = true;
            sc1.Cost = 120;
            sc1.GasCost = 70;
            sc1.ConstructingTime = 20;
            sc1.SizeRadius = 2;
            sc1.NoBody = true;
            sc1.VisionRadius = 8;
            sc1.ChaseRadius = 8;
            sc1.MaxHp = 100;
            sc1.MaxVelocity = 25;
            sc1.ArmorType = "normal";
            sc1.AttackPower = new int[] { 1,1 };
            sc1.AITypes = new string[] { "Carrier" };
            sc1.Prerequisites = new string[][] { new string[] { "Airport" } };
            sc1.TechLevel = 3;
            sc1.Desc = "搭载1个机器人的运输机，可以对敌方的防守薄弱处给予沉重打击。";
            sc1.Pets = new string[] { "Robot","1"};
            sc1.OriginalType = "SoldierCarrier";
            cfgs["RobotCarrier"] = sc1;

            //var wp = new UnitConfigInfo();
            //wp.DisplayName = "直升机";
            //wp.IsAirUnit = true;
            //wp.Cost = 160;
            //wp.GasCost = 100;
            //wp.ConstructingTime = 23;
            //wp.SizeRadius = 2;
            //wp.VisionRadius = 10;
            //wp.ChaseRadius = 10;
            //wp.MaxHp = 225;
            //wp.Defence = 1;
            //wp.MaxVelocity = 6;
            //wp.Suppliment = 1;
            //wp.CanAttackAir = true;
            //wp.CanAttackGround = true;
            //wp.AttackType = new string[] { "normal", "normal" };
            //wp.AttackInterval = new Fix64[] { 1,1 };
            //wp.AttackPower = new int[] { 15, 15 };
            //wp.AttackRange = new int[] { 6, 6 };
            //wp.ArmorType = "light";
            //wp.AITypes = new string[] { "MoveAndAttack" };
            //wp.Prerequisites = new string[][] { new string[] { "-AirTechUltimate", "-AirTech", "Airport" } };
            //cfgs["Helicopter"] = wp;

            var bwp = new UnitConfigInfo();
            bwp.DisplayName = "战斗机";
            bwp.IsAirUnit = true;
            bwp.Cost = 190;
            bwp.GasCost = 145;
            bwp.ConstructingTime = 25;
            bwp.SizeRadius = 2;
            bwp.VisionRadius = 11;
            bwp.ChaseRadius = 11;
            bwp.MaxHp = 165;
            bwp.Defence = 2;
            bwp.MaxVelocity = 8;
            bwp.Suppliment = 1;
            bwp.CanAttackAir = true;
            bwp.CanAttackGround = true;
            bwp.AttackType = new string[] { "normal", "light" };
            bwp.AttackInterval = new Fix64[] { 1,1 };
            bwp.AttackPower = new int[] { 20,20 };
            bwp.AttackRange = new int[] { 8, 8 };
            bwp.ArmorType = "light";
            bwp.AITypes = new string[] { "MoveAndAttack" };
            bwp.Prerequisites = new string[][] {
                new string[] { "AirTech", "Airport" },
                new string[] { "AirTechUltimate", "Airport" }
            };
            bwp.Desc = "隐形战机，无论强袭骚扰都是优秀的选择。";
            bwp.TechLevel = 4;
            bwp.InVisible = true;
            cfgs["Warplane"] = bwp;
            
            var bwp1 = new UnitConfigInfo();
            bwp1.DisplayName = "战斗机";
            bwp1.IsAirUnit = true;
            bwp1.Cost = 190;
            bwp1.GasCost = 145;
            bwp1.ConstructingTime = 25;
            bwp1.SizeRadius = 2;
            bwp1.VisionRadius = 11;
            bwp1.ChaseRadius = 11;
            bwp1.MaxHp = 225;
            bwp1.Defence = 2;
            bwp1.MaxVelocity = 8;
            bwp1.Suppliment = 1;
            bwp1.CanAttackGround = true;
            bwp1.AOEType = new string[] { "circle"};
            bwp1.AOEParams = new Fix64[][] { new Fix64[] { 5} };
            bwp1.AttackType = new string[] { "heavy", null };
            bwp1.AttackInterval = new Fix64[] { 1, 0 };
            bwp1.AttackPower = new int[] { 22, 0 };
            bwp1.AttackRange = new int[] { 3, 0 };
            bwp1.ArmorType = "light";
            bwp1.AITypes = new string[] { "MoveAndAttack" };
            bwp1.Prerequisites = new string[][] {
                new string[] { "AirTech", "Airport" },
                new string[] { "AirTechUltimate", "Airport" }
            };
            bwp1.Desc = "抛弃隐形能力的轰炸机，能对地面造成范围伤害。";
            bwp1.TechLevel = 4;
            bwp1.OriginalType = "Warplane";
            cfgs["WarplaneBomb"] = bwp1;

            var bwp2 = new UnitConfigInfo();
            bwp2.DisplayName = "战斗机";
            bwp2.IsAirUnit = true;
            bwp2.Cost = 190;
            bwp2.GasCost = 145;
            bwp2.ConstructingTime = 25;
            bwp2.SizeRadius = 2;
            bwp2.VisionRadius = 11;
            bwp2.ChaseRadius = 11;
            bwp2.MaxHp = 225;
            bwp2.Defence = 2;
            bwp2.MaxVelocity = 8;
            bwp2.Suppliment = 1;
            bwp2.CanAttackAir = true;
            bwp2.CanAttackGround = true;
            bwp2.AttackType = new string[] { "normal", "light" };
            bwp2.AttackInterval = new Fix64[] { 1, 1 };
            bwp2.AttackPower = new int[] { 10, 28 };
            bwp2.AttackRange = new int[] { 1, 8 };
            bwp2.ArmorType = "light";
            bwp2.AITypes = new string[] { "MoveAndAttack" };
            bwp2.Prerequisites = new string[][] {
                new string[] { "AirTech", "Airport" },
                new string[] { "AirTechUltimate", "Airport" }
            };
            bwp2.Desc = "对空强化改造，能有效打击敌方空中单位。";
            bwp2.TechLevel = 4;
            bwp2.OriginalType = "Warplane";
            cfgs["WarplaneAir"] = bwp2;

            var ms = new UnitConfigInfo();
            ms.DisplayName = "巨舰";
            ms.IsAirUnit = true;
            ms.Cost = 360;
            ms.GasCost = 300;
            ms.ConstructingTime = 45;
            ms.SizeRadius = 3;
            ms.VisionRadius = 12;
            ms.ChaseRadius = 12;
            ms.MaxHp = 500;
            ms.Defence = 3;
            ms.MaxVelocity = 4;
            ms.Suppliment = 1;
            ms.CanAttackAir = true;
            ms.CanAttackGround = true;
            ms.AttackType = new string[] { "heavy", "light" };
            ms.AttackInterval = new Fix64[] { 1, 1 };
            ms.AttackPower = new int[] { 35, 35 };
            ms.AttackRange = new int[] { 10, 10 };
            ms.ArmorType = "light";
            ms.AITypes = new string[] { "MoveAndAttack" };
            ms.Prerequisites = new string[][] { new string[] { "AirTechUltimate", "Airport" } };
            ms.TechLevel = 5;
            ms.Desc = "飞行的堡垒，昂贵的造价带来的是毁灭性的军事力量。";
            cfgs["MotherShip"] = ms;

            var ms1 = new UnitConfigInfo();
            ms1.DisplayName = "巨舰";
            ms1.IsAirUnit = true;
            ms1.Cost = 360;
            ms1.GasCost = 300;
            ms1.ConstructingTime = 45;
            ms1.SizeRadius = 3;
            ms1.VisionRadius = 12;
            ms1.ChaseRadius = 12;
            ms1.MaxHp = 500;
            ms1.Defence = 3;
            ms1.MaxVelocity = 4;
            ms1.Suppliment = 1;
            ms1.CanAttackAir = true;
            ms1.CanAttackGround = true;
            ms1.AttackType = new string[] { "heavy", "light" };
            ms1.AttackInterval = new Fix64[] { 0.5, 0.5 };
            ms1.AttackPower = new int[] { 17, 17 };
            ms1.AttackRange = new int[] { 10, 10 };
            ms1.ArmorType = "light";
            ms1.AITypes = new string[] { "MoveAndAttack" };
            ms1.Prerequisites = new string[][] { new string[] { "AirTechUltimate", "Airport" } };
            ms1.TechLevel = 5;
            ms1.Desc = "快速攻击改造型巨舰，但单次伤害降低。";
            ms1.OriginalType = "MotherShip";
            cfgs["MotherShipFast"] = ms1;

            var ms2 = new UnitConfigInfo();
            ms2.DisplayName = "巨舰";
            ms2.IsAirUnit = true;
            ms2.Cost = 360;
            ms2.GasCost = 300;
            ms2.ConstructingTime = 45;
            ms2.SizeRadius = 3;
            ms2.VisionRadius = 12;
            ms2.ChaseRadius = 12;
            ms2.MaxHp = 500;
            ms2.Defence = 3;
            ms2.MaxVelocity = 4;
            ms2.Suppliment = 1;
            ms2.CanAttackAir = true;
            ms2.CanAttackGround = true;
            ms2.AttackType = new string[] { "heavy", "light" };
            ms2.AttackInterval = new Fix64[] { 1.5, 1.5 };
            ms2.AttackPower = new int[] { 50, 50 };
            ms2.AttackRange = new int[] { 10, 10 };
            ms2.ArmorType = "light";
            ms2.AITypes = new string[] { "MoveAndAttack" };
            ms2.Prerequisites = new string[][] { new string[] { "AirTechUltimate", "Airport" } };
            ms2.TechLevel = 5;
            ms2.Desc = "慢速攻击改造型巨舰，但单次伤害增加。";
            ms2.OriginalType = "MotherShip";
            cfgs["MotherShipSlow"] = ms2;

            var nm = new UnitConfigInfo();
            nm.DisplayName = "中立怪";
            nm.IsMechanical = true;
            nm.SizeRadius = 1;
            nm.VisionRadius = 8;
            nm.ChaseRadius = 8;
            nm.MaxHp = 70;
            nm.Defence = 0;
            nm.MaxVelocity = 5;
            nm.CanAttackAir = true;
            nm.CanAttackGround = true;
            nm.AttackType = new string[] { "normal", "normal" };
            nm.AttackInterval = new Fix64[] { 1, 1 };
            nm.AttackPower = new int[] { 7, 7 };
            nm.AttackRange = new int[] { 5, 5 };
            nm.ArmorType = "normal";
            nm.AITypes = new string[] { "NeutralMonster" };
            nm.NoCard = true;
            cfgs["NeutralMonster"] = nm;

            // 中立怪
            var bm = new UnitConfigInfo();
            bm.DisplayName = "剑圣";  
            bm.IsMechanical = true;
            bm.SizeRadius = 2;
            bm.VisionRadius = 8;
            bm.ChaseRadius = 8;
            bm.MaxHp = 110;
            bm.Defence = 1;
            bm.MaxVelocity = 5;
            bm.CanAttackAir = true;
            bm.CanAttackGround = true;
            bm.AttackType = new string[] { "normal", "normal" };
            bm.AttackInterval = new Fix64[] { 1, 1 };
            bm.AttackPower = new int[] { 14, 14 };
            bm.AttackRange = new int[] { 6, 6 };
            bm.ArmorType = "normal";
            bm.AITypes = new string[] { "NeutralMonster" };
            bm.NoCard = true;
            cfgs["Blademaster"] = bm;

            // 中立怪
            var vk = new UnitConfigInfo();
            vk.DisplayName = "维克兹";
            vk.IsMechanical = true;
            vk.SizeRadius = 2;
            vk.VisionRadius = 10;
            vk.ChaseRadius = 10;
            vk.MaxHp = 400;
            vk.Defence = 3;
            vk.MaxVelocity = 5;
            vk.CanAttackAir = true;
            vk.CanAttackGround = true;
            vk.AttackType = new string[] { "heavy", "heavy" };
            vk.AttackInterval = new Fix64[] { 1, 1 };
            vk.AOEType = new string[] { null, "circle" };
            vk.AOEParams = new Fix64[][] { null, new Fix64[] { 3 } };
            vk.AttackPower = new int[] { 20, 15 };
            vk.AttackRange = new int[] { 6, 6 };
            vk.ArmorType = "heavy";
            vk.AITypes = new string[] { "NeutralMonster" };
            vk.NoCard = true;
            cfgs["Velkoz"] = vk;
        }

        void BuildOthers()
        {
            
            var bsStub = new UnitConfigInfo();
            bsStub.SizeRadius = 3;
            bsStub.NoCard = true;
            bsStub.IsBuilding = true;
            bsStub.DisplayName = "矿点";
            bsStub.MaxHp = 9999;
            bsStub.UnAttackable = true;
            cfgs["BaseStub"] = bsStub;

            var tbc = new UnitConfigInfo();
            tbc.DisplayName = "宝箱投放机";
            tbc.IsAirUnit = true;
            tbc.UnAttackable = true;
            tbc.SizeRadius = 2;
            tbc.VisionRadius = 3;
            tbc.MaxHp = 1;
            tbc.MaxVelocity = 30;
            tbc.AITypes = new string[] { "Carrier" };
            tbc.NoBody = true;
            tbc.NoCard = true;
            cfgs["TreasureBoxCarrier"] = tbc;

            var tb = new UnitConfigInfo();
            tb.DisplayName = "宝箱";
            tb.UnAttackable = true;
            tb.SizeRadius = 1;
            tb.VisionRadius = 3;
            tb.MaxHp = 1;
            tb.MaxVelocity = 30;
            tb.AITypes = new string[] { "TreasureBox" };
            tb.NoCard = true;
            cfgs["TreasureBox"] = tb;
        }
    }
}
