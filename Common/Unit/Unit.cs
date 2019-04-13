using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 地图单位，包括兵、建筑、中立单位等
    /// </summary>
    public class Unit : SerializableData
    {
        public Action onDamage = null;
        public Action OnDead = null;

        // 整个战场生命周期内唯一 ID
        string uid;
        public string UID { get { return uid; } }
        Unit() { uid = null; } // 序列化用
        public Unit(string uniqueID) { uid = uniqueID; }

        // 单位类型名称
        string type;
        public string UnitType
        { 
            get 
            { 
                return type; 
            } 
            set
            { 
                type = value;

                _cfg = UnitConfiguration.GetDefaultConfig(type);

                IsInvisible = _cfg.InVisible;
            }
        }

        // 所处位置
        Vec2 pos;
        public Vec2 Pos { get { return pos; } set { pos = value; } }

        // 方向 [0, 360) degree
        int dir;
        public int Dir { get { return dir; } set { dir = value; } }
        public int ForwardDir { get { return player == 1 ? 90 : -90; } }

        // 血量
        Fix64 hp;
        public Fix64 Hp
        {
            get
            {
                if (hp <= 0)
                {
                    if (OnDead != null)
                    {
                        OnDead();

                        while (this.OnDead != null)
                            this.OnDead -= this.OnDead;
                    }
                }

                return hp; 
            }
            set
            {
                hp = MathEx.Clamp(value, Fix64.Zero, cfg.MaxHp);
            }
        }

        // 当前速度
        // Fix64 velocity;
        // public Fix64 Velocity { get { return velocity; } set { velocity = value; } }

        // 攻击力增加值
        public Fix64[] PowerAdd { get { return powerAdd; } }
        Fix64[] powerAdd = new Fix64[2];

        // 防御力增加值
        public Fix64 DefenceAdd { get; set; }

        // 实时攻击防御值
        public Fix64 GPower { get { return cfg.AttackPower[0] + PowerAdd[0]; } }
        public Fix64 APower { get { return cfg.AttackPower[1] + PowerAdd[1]; } }
        public Fix64[] Power { get { return new Fix64[] { GPower, APower }; } }
        public Fix64 Defence { get { return cfg.Defence + DefenceAdd; } }

        // 是否建造完成
        bool buildingCompleted;
        public bool BuildingCompleted { get { return buildingCompleted; } set { buildingCompleted = value; } }

        // 是否在拆毁过程
        public bool InDestroying { get; set; }

        // 所属房间
        public Room Room { get; set; }

        // 附件单位
        public List<Unit> Accessories = new List<Unit>();

        // RVOAgent ID
        public int RVOAgentID { get; set; }
		
		// 设置行进速度
        public Vec2 PreferredVelocity
        {
            get { return pv; }
            set { pv = value; }
        } Vec2 pv;

        // 所属主单位
        public Unit Owner
        {
            get { return owner; } 
            set
            {
                if (owner == value)
                    return;

                if (owner != null && owner.Accessories.IndexOf(this) >= 0)
                    owner.Accessories.Remove(this);

                owner = value;
                if (owner != null && owner.Accessories.IndexOf(this) < 0)
                    owner.Accessories.Add(this);
            }
        }
        Unit owner = null;

        // 建造列表
        public List<string> UnitCosntructingWaitingList { get { return unitCosntructingWaitingList; } }
        List<string> unitCosntructingWaitingList = new List<string>();

        // 所属玩家
        int player;
        public int Player { get { return player; } set { player = value; } }

        // 是否是中立单位
        public bool IsNeutral { get { return player == 0; } }

        // 剩余瘫痪时间
        public Fix64 TrappedTimeLeft = 0;

        // 剩余减速时间
        public Fix64 DecelerateTimeLeft = 0;

        // 减速值
        public Fix64 Deceleration = 0;

        // 是否隐身
        public bool IsInvisible = false;

        // 目标移动位置
        public List<Vec2> MovePath = new List<Vec2>();
        public void ResetPath(IEnumerable<Vec2> path)
        {
            MovePath.Clear();
            if (path != null)
                MovePath.AddRange(path);
        }

        // 一般性附加数据
        public object Tag { get; set; }

        // 配置信息
        public UnitConfigInfo cfg
        {
            get
            {
                return _cfg;
            }
        } UnitConfigInfo _cfg;

        // 承受伤害
        public void OnDamage(int damage)
        {
            // 伤害先转移到附件
            if (Accessories.Count > 0)
            {
                var a = Accessories[0];
                a.OnDamage(damage);
            }
            else
            {
                hp -= damage;
                hp = hp.Clamp(Fix64.MinValue, cfg.MaxHp);
            }

            if (null != onDamage)
                onDamage();
        }

        // 显隐
        public void ShowInvisible()
        {
            var ts = Room.GetUnitsInArea(Pos, cfg.VisionRadius, (tar) =>
            {
                // 敌方隐形单位
                return tar.IsInvisible && player != tar.player;
            });

            if (ts.Length > 0)
            {
                for (int i = 0; i < ts.Length; i++)
                    ts[i].IsInvisible = false;
            }
        }

        // 是否满足空地攻击条件
        public bool CanAttack(Unit target)
        {
            if (target == null || target.Hp <= 0 || target.cfg.UnAttackable || target.owner != null)
                return false;
            else if (!target.cfg.IsAirUnit)
            {
                if (!cfg.CanAttackGround)
                    return false;
                else if (cfg.AttackPower[0] > 0 && player == target.player) // 不能攻击自己人
                    return false;
                else if (cfg.AttackPower[0] < 0 && player != target.player) // 不能治疗敌人
                    return false;
            }
            else
            {
                if (!cfg.CanAttackAir)
                    return false;
                else if (cfg.AttackPower[1] > 0 && player == target.player) // 不能攻击自己人
                    return false;
                else if (cfg.AttackPower[1] < 0 && player != target.player) // 不能治疗敌人
                    return false;
            }

            return true;
        }

        // 是否在攻击范围内
        public bool InAttackRange(Unit target)
        {
            if (target == null)
                return false;

            var compD = cfg.AttackRange[target.cfg.IsAirUnit ? 1 : 0] + target.cfg.SizeRadius - 1;
            return (pos - target.pos).Length <= compD;
        }

        //// 获取在攻击范围内的目标
        //public Unit[] GetUnitsInAttackRange(Func<Unit, bool> filter)
        //{
        //    return FC.Select(Room.AllUnits,
        //        (tar) => (filter == null || filter(tar)) && InAttackRange(tar)).ToArray();
        //}

        public Unit GetFirstAttackableUnitsInAttackRange(Func<Unit, bool> filter)
        {
            return GetFirstAttackableUnitsInRange(filter, cfg.AttackRange);
        }

        public Unit GetFirstAttackableUnitsInChasingRange(Func<Unit, bool> filter)
        {
            return GetFirstAttackableUnitsInRange(filter, new int[] { (int)cfg.ChaseRadius, (int)cfg.ChaseRadius });
        }

        // 选择范围内的可攻击目标，选择最高优先级的攻击目标
        Unit GetFirstAttackableUnitsInRange(Func<Unit, bool> filter, int[] range)
        {
            // 这个是非常高频的部分，值得仔细优化

            Unit[] tarOnGround = null;
            Unit[] tarInAir = null;

            if (cfg.CanAttackGround)
                tarOnGround = Room.GetUnitsInArea(pos, range[0], (tar) => !tar.IsInvisible && CanAttack(tar) && (filter == null || filter(tar)));

            if (cfg.CanAttackAir)
                tarInAir = Room.GetUnitsInArea(pos, range[1], (tar) => !tar.IsInvisible && CanAttack(tar) && (filter == null || filter(tar)));

            Unit firstTargetOnGround = null;
            if (tarOnGround != null && tarOnGround.Length > 0)
            {
                firstTargetOnGround = tarOnGround.SwiftSort((t) =>
                {
                    return t.CanAttack(this) ? -10000 + (pos - t.pos).MaxAbsXOrY : (pos - t.pos).MaxAbsXOrY;
                })[0];
            }

            Unit firstTargetInAir = null;
            if (tarInAir != null && tarInAir.Length > 0)
            {
                firstTargetInAir = tarInAir.SwiftSort((t) =>
                {
                    return t.CanAttack(this) ? -10000 + (pos - t.pos).MaxAbsXOrY : (pos - t.pos).MaxAbsXOrY;
                })[0];
            }

            if (!CanAttack(firstTargetOnGround)) // 只可能有空中目标
                return firstTargetInAir;
            else if (!CanAttack(firstTargetInAir)) // 只有地面目标
                return firstTargetOnGround;
            else if (!firstTargetOnGround.CanAttack(this) && firstTargetInAir.CanAttack(this)) // 空中目标能攻击自己，优先打
                return firstTargetInAir;
            else if (!firstTargetInAir.CanAttack(this) && firstTargetOnGround.CanAttack(this)) // 地面目标能攻击自己，优先打
                return firstTargetOnGround;
            else // 都能攻击自己，挑距离近的打
                return ((firstTargetOnGround.pos - pos).MaxAbsXOrY <= (firstTargetInAir.pos - pos).MaxAbsXOrY) ?
                    firstTargetOnGround : firstTargetInAir;
        }

        override protected void Sync()
        {
            BeginSync();
            SyncString(ref uid);
            SyncString(ref type);
            SyncFix64(ref pos.x);
            SyncFix64(ref pos.y);
            SyncInt(ref dir);
            SyncFix64(ref hp);
            SyncInt(ref buildingCompleted);
            SyncListString(ref unitCosntructingWaitingList);
            SyncInt(ref player);
            EndSync();
        }
    }
}
