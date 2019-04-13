using System;
using System.Collections.Generic;
using Swift;
using SCM;
using Swift.Math;

namespace Server
{
    /// <summary>
    /// 配置管理器
    /// </summary>
    public class UnitConfigManager : Component
    {
        UserPort UP;

        // 初始化
        public override void Init()
        {
            UP = GetCom<UserPort>();
   
            UP.OnRequest("GetUnitCfgs", OnGetUnitCfgs);
        }

        void OnGetUnitCfgs(Connection conn, IReadableBuffer data, IWriteableBuffer buff, Action end)
        {
            buff.Write(UnitConfiguration.AllUnitTypes);

            for (int i = 0; i < UnitConfiguration.AllUnitTypes.Length; i++)
            {
                UnitUtils.WriteUnitInfo(UnitConfiguration.GetDefaultConfig(UnitConfiguration.AllUnitTypes[i]), buff);
            }

            end();
        }

        // 电脑对手伪装成玩家的名字
        public static string RandomComputerOpponentName()
        {
            var rand = new Random();
            var n = rand.Next(coNames.Length);
            return coNames[n];
        }

        static string[] coNames = new string[]
        {
            "PanzerVor", 
            "makikawaii",
            "阿莱克斯塔萨",
            "又双叒车俞了",
            "NodeJS",
            "UnKnown",
            "赢了就睡觉",
            "Megumi",
            "乌拉",
            "xopowo",
        };
    }
}
