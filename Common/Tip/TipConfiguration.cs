using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    public class TipConfiguration : Component
    {
        static TipConfiguration Instance
        {
            get { return instance; }
        } static TipConfiguration instance = null;

        public TipConfiguration()
        {
            if (instance != null)
                throw new Exception("only one TipConfiguration should be created.");

            instance = this;

            BuildTips();
        }

        StableDictionary<int, string> tips = new StableDictionary<int, string>();

        public static int[] AllTips
        {
            get { return Instance.tips.KeyArray; }
        }

        public static string GetDefaultConfig(int id)
        {
            return Instance.tips.ContainsKey(id) ? Instance.tips[id] : null;
        }

        void BuildTips()
        {
            int id = 0;

            tips[id++] = "多个出兵建筑能加快出兵速度并增加储兵上限。";
            tips[id++] = "可以利用建筑保护好你的资源点。";
            tips[id++] = "获取资源很重要，花掉资源更重要。";
            tips[id++] = "适时的侦查对方的动向避免措手不及。";
            tips[id++] = "混合兵种组成的部队更难被克制。";
            tips[id++] = "机枪兵廉价脆弱但能很早形成较大规模。";
            tips[id++] = "用喷火兵应对机枪兵很有效。";
            tips[id++] = "单位的阵型也能左右战局。";
            tips[id++] = "将关键建筑放在角落处避免暴露意图。";
            tips[id++] = "仓库能增加储兵上限但不能加快出兵速度。";
        }
    }
}
