using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 关卡设置
    /// </summary>
    public abstract class Level
    {
        // 关卡 ID，显示名称
        public abstract string LevelID { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        // 检查结束条件，返回 winner player，默认胜利条件为拆光对方基地
        public virtual int CheckWinner(Room r)
        {
            if (r.GetAllUnitsByPlayer(1, (u) => u.UnitType == "Base" || u.cfg.ReconstructFrom == "Base").Length == 0)
                return 2;
            if (r.GetAllUnitsByPlayer(2, (u) => u.UnitType == "Base" || u.cfg.ReconstructFrom == "Base").Length == 0)
                return 1;
            else
                return -1;
        }

        // 地图尺寸
        Vec2 mapSize = Vec2.Zero;
        public Vec2 MapSize { get { return mapSize; } }
        public Vec2 MapHalfSize { get { return MapSize / 2; } }

        // 地图设定，包括矿点，障碍
        public virtual void InitMapSetting(Room r)
        {
            mapSize = r.MapSize;
        }

        // 初始建筑，包括初始主基地
        public virtual void InitBuilding(Room r)
        {
        }

        // 初始战斗单位
        public virtual void InitBattleUnits(Room r)
        {
        }

        // 初始资源，包括人口上限等
        public virtual void InitResource(Room r)
        {
        }

        // 初始化全部内容
        public virtual void Init(Room r)
        {
            InitMapSetting(r);
            InitBuilding(r);
            InitBattleUnits(r);
            InitResource(r);
        }
    }
}
