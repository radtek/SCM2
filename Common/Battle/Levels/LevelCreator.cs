using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 创建关卡
    /// </summary>
    public class LevelCreator
    {
        static StableDictionary<string, Level> pves = new StableDictionary<string, Level>();
        static StableDictionary<string, Level> pvps = new StableDictionary<string, Level>();

        static LevelCreator()
        {
            pvps["PVP"] = new LvPVP();
        }

        public static Level[] AllPVELevels { get { return pves.ValueArray; } }

        public static Level GetLevel(string lvID)
        {
            if (pvps.ContainsKey(lvID))
                return pvps[lvID];
            else if (pves.ContainsKey(lvID))
                return pves[lvID];

            return null;
        }
    }
}
