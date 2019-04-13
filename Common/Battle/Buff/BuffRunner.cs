using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 管理一场战斗的所有局内 buff/debuff
    /// </summary>
    public class BuffRunner
    {
        // 双方的局内 buff 对象
        List<Buff>[] allBuffs = new List<Buff>[] { null, new List<Buff>(), new List<Buff>() };

        // 所属房间
        public Room Room { get; set; }

        public void Clear()
        {
            allBuffs = new List<Buff>[] { null, new List<Buff>(), new List<Buff>() };
        }

        public void AddBuff(int player, Buff b)
        {
            allBuffs[player].Add(b);
            foreach (var u in Room.GetAllUnitsByPlayer(player))
                b.OnUnit(u);
        }

        public bool RemoveBuff(Buff b)
        {
            bool found = false;
            FC.For(allBuffs.Length, (player) =>
            {
                var buffs = allBuffs[player];
                if (buffs == null)
                    return;

                if (buffs.Contains(b))
                {
                    allBuffs[player].Add(b);
                    foreach (var u in Room.GetAllUnitsByPlayer(player))
                        b.OffUnit(u);

                    buffs.Remove(b);
                    found = true;
                }
            }, () => !found);

            return found;
        }

        public void OnUnitAdded(Unit u)
        {
            var buffs = allBuffs[u.Player];
            if (buffs == null)
                return;

            foreach (var b in buffs)
                b.OnUnit.SC(u);
        }

        public void OnUnitRemoved(Unit u)
        {
            var buffs = allBuffs[u.Player];
            if (buffs == null)
                return;

            foreach (var b in buffs)
                b.OffUnit.SC(u);
        }

        public void OnTimeElapsed(Fix64 te)
        {
            foreach (var buffs in allBuffs)
            {
                if (buffs == null)
                    continue;

                foreach (var b in buffs)
                    b.OnTimeElapsed.SC(te);
            }
        }
    }
}
