using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 静默对手，不会任何发展
    /// </summary>
    public class Dumb : AIComputerOpponent
    {
        public Dumb(string id, Room room, int player) : base(id, room, player)
        {
        }

        public override void Init()
        {
            sm.NewState("dumb").Run(null).AsDefault();
            sm.Trans().From("dumb").To("dumb").When((st) => false);
        }
    }
}