using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;
using System.Linq;

namespace SCM
{
    public class AvatarConfiguration : Component
    {
        static AvatarConfiguration Instance
        {
            get { return instance; }
        }
        static AvatarConfiguration instance = null;

        List<string> cfgs = new List<string>();

        public static List<string> Cfgs
        {
            get { return Instance.cfgs; }
        }

        public AvatarConfiguration()
        {
            if (instance != null)
                throw new Exception("only one AvatarConfiguration should be created.");

            instance = this;
            Build();
        }

        private void Build()
        {
            cfgs.Add("Default");
            cfgs.Add("Ghost");
            cfgs.Add("Warplane");
            cfgs.Add("SoldierCarrier");
            cfgs.Add("Hammer");
            cfgs.Add("MagSpider");
            cfgs.Add("Tank");
            cfgs.Add("Robot");
            cfgs.Add("Thor");
        }
    }
}