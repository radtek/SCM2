using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    /// <summary>
    /// 研究配置信息
    /// </summary>
    public class ResearchConfigInfo
    {
        public string BuffType; // buff 类型
        public Fix64[] BuffParameters; // buff 参数
        public string DisplayName; // 单位显示名称

        public int Cost; // 建造花费晶矿
        public int GasCost; // 建造花费气矿
        public Fix64 ConstructingTime; // 研发时间
    }

    /// <summary>
    /// 研究配置信息管理
    /// </summary>
    public class ResearchConfiguration : Component
    {
        static ResearchConfiguration Instance
        {
            get { return instance; }
        } static ResearchConfiguration instance = null;

        // 研究单位配置信息
        StableDictionary<string, ResearchConfigInfo> cfgs = new StableDictionary<string, ResearchConfigInfo>();

        public ResearchConfiguration()
        {
            if (instance != null)
                throw new Exception("only one ResearchConfiguration should be created.");

            instance = this;
            BuildAll();
        }

        public static ResearchConfigInfo GetResearchConfig(string type)
        {
            return Instance.cfgs.ContainsKey(type) ? Instance.cfgs[type] : null;
        }

        void BuildAll()
        {
            var addBioAttack = new ResearchConfigInfo();
            addBioAttack.DisplayName = "+生物攻击";
            addBioAttack.BuffType = "AddBiologicalAttack";
            addBioAttack.BuffParameters = new Fix64[] { 0.1 };
            addBioAttack.Cost = 100;
            addBioAttack.GasCost = 100;
            addBioAttack.ConstructingTime = 10;
            cfgs["addBioAttack"] = addBioAttack;

            var addBioDefence = new ResearchConfigInfo();
            addBioDefence.DisplayName = "+生物防御";
            addBioDefence.BuffType = "AddBiologicalDefence";
            addBioDefence.BuffParameters = new Fix64[] { 1 };
            addBioDefence.Cost = 100;
            addBioDefence.GasCost = 100;
            addBioDefence.ConstructingTime = 10;
            cfgs["addBioAttack"] = addBioAttack;

            var addMechAttack = new ResearchConfigInfo();
            addMechAttack.DisplayName = "+机械攻击";
            addMechAttack.BuffType = "AddMechanicalAttack";
            addMechAttack.BuffParameters = new Fix64[] { 0.1 };
            addMechAttack.Cost = 100;
            addMechAttack.GasCost = 100;
            addMechAttack.ConstructingTime = 10;
            cfgs["addMechAttack"] = addMechAttack;

            var addMechDefence = new ResearchConfigInfo();
            addMechDefence.DisplayName = "+机械防御";
            addMechDefence.BuffType = "AddMechanicalDefence";
            addMechDefence.BuffParameters = new Fix64[] { 1 };
            addMechDefence.Cost = 100;
            addMechDefence.GasCost = 100;
            addMechDefence.ConstructingTime = 10;
            cfgs["addMechDefence"] = addMechDefence;

            var addAirAttack = new ResearchConfigInfo();
            addAirAttack.DisplayName = "+飞行攻击";
            addAirAttack.BuffType = "AddAirAttack";
            addAirAttack.BuffParameters = new Fix64[] { 0.1 };
            addAirAttack.Cost = 100;
            addAirAttack.GasCost = 100;
            addMechAttack.ConstructingTime = 10;
            cfgs["addAirAttack"] = addAirAttack;

            var addAirDefence = new ResearchConfigInfo();
            addAirDefence.DisplayName = "+飞行防御";
            addAirDefence.BuffType = "AddAirDefence";
            addAirDefence.BuffParameters = new Fix64[] { 1 };
            addAirDefence.Cost = 100;
            addAirDefence.GasCost = 100;
            addAirDefence.ConstructingTime = 10;
            cfgs["addAirDefence"] = addAirDefence;
        }
    }
}
