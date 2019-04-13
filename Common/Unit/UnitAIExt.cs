using System;
using System.Collections.Generic;
using Swift;
using SCM;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 扩展 Unit 的 AI 行为
    /// </summary>
    public static class UnitAIExt
    {
        // 保持单一状态
        public static StateMachine SimpleState(this Unit u, Action<State, Fix64> onRun = null)
        {
            var sm = new StateMachine(u.UID);
            sm.NewState("default").AsDefault().Run(onRun);
            sm.Trans().From("default").To("default").When((st) => false);
            return sm;
        }

        // 等待一段时间
        public static StateMachine StartWaiting4Time(this Unit u, Fix64 time, Action onEnd, Action<Fix64> onTimeElapsed = null)
        {
            var sm = new StateMachine(u.UID);
            sm.NewState("waiting").Run((st, dt) =>
            {
                time -= dt;
                if (time > 0)
                    onTimeElapsed.SC(dt);
                else
                    onEnd.SC();

                // 显隐
                if (u.cfg.IsObserver)
                    u.ShowInvisible();
            }).AsDefault();
            sm.NewState("ended").Run(null);
            sm.Trans().From("waiting").To("ended").When((st) => time <= 0);
            sm.Trans().From("ended").To("ended").When(null);
            return sm;
        }

        // 创建对应类型 AI
        public static StateMachine CreateAI(this Unit u)
        {
            // 建造中
            if (u.cfg.IsBuilding && !u.BuildingCompleted)
                return Constructing(u, u.cfg.ConstructingTime);
            else if (u.InDestroying)
                return Destroying(u);

            var cfg = u.cfg;
            if (cfg.AITypes == null || cfg.AITypes.Length == 0)
                return null;

            var subSms = new List<StateMachine>();
            FC.For(cfg.AITypes.Length, (i) =>
            {
                var ai = cfg.AITypes[i];
                var ps = cfg.AIParams == null || cfg.AIParams.Length <= i ? null : cfg.AIParams[i];
                StateMachine sm = null;
                switch (ai)
                {
                    case "Suicide": // 等待指定时间后消失
                        sm = Suicide(u, ps[0]);
                        break;
                    case "ObserverBuilding": // 显隐建筑
                        sm = ObserverBuilding(u);
                        break;
                    case "GenMoney":
                        sm = GenResource(u, "Money", ps[0]);
                        break;
                    case "GenGas":
                        sm = GenResource(u, "Gas", ps[0]);
                        break;
                    case "HoldAndAttack":
                        sm = HoldAndAttack(u, false, null);
                        break;
                    case "MoveAndAttack":
                        sm = MoveAndAttack(u, false, 0, false, 0, 0, null);
                        break;
                    case "MoveAndSelfExplode":
                        sm = MoveAndAttack(u, true, ps[0], false, 0, 0, null);
                        break;
                    case "MoveAndAttackBuilding":
                        sm = MoveAndAttack(u, false, 0, false, 0, 0, (tar) => tar.cfg.IsBuilding);
                        break;
                    case "MoveAndAttackWithDecelerate":
                        sm = MoveAndAttack(u, false, 0, false, ps[0], ps[1], null);
                        break;
                    case "Landmine": 
                        sm = MoveAndAttack(u, true, 0, true, 0, 0, null);
                        break;
                    case "Carrier":
                        sm = Carrier(u);
                        break;
                    case "TreasureBox":
                        sm = TreasureBox(u);
                        break;
                    case "Research":
                        sm = Research(u);
                        break;
                    case "NeutralMonster":
                        sm = NeutralMonster(u, null);
                        break;
                    default:
                        throw new Exception("unsupported ai type: " + ai);
                }

                subSms.Add(sm);
            });

            if (subSms.Count == 1)
                return subSms[0];
            else
                return (new StateMachineCombination(u.UID, subSms.ToArray())).MakeSelfDumb();
        }

        #region 各种独立功能的 ai 状态机

        // 等待指定时间自杀
        static StateMachine Suicide(Unit u, Fix64 t)
        {
            return u.StartWaiting4Time(t, () => { u.Hp = 0; });
        }

        static StateMachine ObserverBuilding(Unit u)
        {
            return u.SimpleState((st, te) => { u.ShowInvisible(); });
        }

        // 建造中
        static StateMachine Constructing(Unit u, Fix64 t)
        {
            var dHp = u.cfg.MaxHp / t;
            var sm = new StateMachine(u.UID);
            sm.NewState("constructing").AsDefault().Run((st, te) =>
            {
                t -= te;
                if (t <= 0)
                {
                    u.BuildingCompleted = true;
                    u.Room.OnBuildingConstructingCompleted(u);
                }
                else
                    u.Hp += dHp * te;
            });
            sm.NewState("idle").Run(null);
            sm.Trans().From("constructing").To("idle").When((st) => t <= 0);
            sm.Trans().From("idle").To("idle").When((st) => false);
            return sm;
        }

        // 拆毁中
        static readonly Fix64 TotalDestroyTime = 5; // 统一 5 秒拆毁
        static StateMachine Destroying(Unit u)
        {
            var sm = new StateMachine(u.UID);
            var destroyTime = TotalDestroyTime;
            sm.NewState("destroying").AsDefault().Run((st, te) =>
            {
                if (u.Hp <= 0)
                    return;

                destroyTime -= te;
                var dHp = te / TotalDestroyTime * u.cfg.MaxHp;
                u.Hp -= dHp;

                var money = dHp / u.cfg.MaxHp * u.cfg.Cost * 0.8f;
                var gas = dHp / u.cfg.MaxHp * u.cfg.GasCost * 0.8f;
                u.Room.OnBuildingDestoyingProcess(u, money, gas);
            });

            sm.Trans().From("destroying").To("destroying").When((st) => false);

            return sm;
        }

        // 生产资源
        static StateMachine GenResource(Unit u, string resType, Fix64 mountPerSecond)
        {
            return u.SimpleState((st, te) => { u.Room.AddResource(u.Player, resType, mountPerSecond * te); });
        }

        // 原地站立攻击
        static StateMachine HoldAndAttack(Unit u, bool suicideAfterAttack /* 自爆攻击 */, Func<Unit, bool> targetFilter)
        {
            var sm = new StateMachine(u.UID);

            Unit target = null; // 被锁定的攻击目标

            // 搜寻目标
            Func<Unit> findTarget = () =>
            {
                return u.GetFirstAttackableUnitsInAttackRange(targetFilter);
            };

            // 原地搜索目标
            sm.NewState("holding").Run((st, te) =>
            {
                u.PreferredVelocity = Vec2.Zero;
                target = findTarget();

                if (u.cfg.IsObserver)
                    u.ShowInvisible();
            }).AsDefault();

            // 攻击目标
            var attackCD = Fix64.Zero;
            sm.NewState("attacking").OnRunIn((st) => u.PreferredVelocity = Vec2.Zero).Run((st, te) =>
            {
                //                u.PreferredVelocity = Vec2.Zero;

                // 攻击状态中就不另寻目标
                u.Room.DoAttack(u, target);
                attackCD = u.cfg.AttackInterval[target.cfg.IsAirUnit ? 1 : 0];
                // 自爆攻击
                if (suicideAfterAttack)
                    u.Hp = 0;

                if (u.cfg.IsObserver)
                    u.ShowInvisible();
            });

            // 攻击间隔
            sm.NewState("attackingCD").Run((st, te) =>
            {
                attackCD -= te;

                if (u.cfg.IsObserver)
                    u.ShowInvisible();
            });

            sm.Trans().From("holding").To("attacking").When((st) => u.CanAttack(target));
            sm.Trans().From("attacking").To("holding").When((st) => !u.CanAttack(target));
            sm.Trans().From("attacking").To("attackingCD").When((st) => attackCD > 0);
            sm.Trans().From("attackingCD").To("attacking").When((st) => attackCD <= 0);

            return sm;
        }

        // 前进并试图攻击
        static StateMachine MoveAndAttack(Unit u,
            bool suicideAfterAttack /* 自爆攻击 */, 
            Fix64 trapTime /* 瘫痪时间 */, 
            bool isLandmine /* 地雷 */,
            Fix64 decelerateTime /* 减速时间 */,
            Fix64 deceleration /* 减速量 */, 
            Func<Unit, bool> targetFilter)
        {
            var v = u.cfg.MaxVelocity;
            var sm = new StateMachine(u.UID);

            List<Vec2> path = new List<Vec2>();
            Func<Fix64, bool> moveImpl = null;
            Unit target = null; // 被锁定的攻击目标

            // 搜寻目标
            Func<Unit> findTarget = () =>
            {
                return u.GetFirstAttackableUnitsInChasingRange(targetFilter);
            };

            Action findPath = () =>
            {
                if (u.Pos.y >= u.Room.MapSize.y - 25 && u.Player == 1)
                {
                    // path = u.Room.FindPath(u, new Vec2(u.Room.MapSize.x / 2, u.Room.MapSize.y - 5));
                    var tars = u.Room.GetAllUnitsByPlayer(2, (tar) => tar.UnitType == "Base" || tar.cfg.ReconstructFrom == "Base");
                    if (tars.Length > 0)
                        path = u.Room.FindPath(u, tars[0].Pos);
                    else
                        path.Clear();
                }
                else if (u.Pos.y <= 25 && u.Player != 1)
                {
                    // path = u.Room.FindPath(u, new Vec2(u.Room.MapSize.x / 2, 5));
                    var tars = u.Room.GetAllUnitsByPlayer(1, (tar) => tar.UnitType == "Base" || tar.cfg.ReconstructFrom == "Base");
                    if (tars.Length > 0)
                        path = u.Room.FindPath(u, tars[0].Pos);
                    else
                        path.Clear();
                }
                else
                    path = u.Room.FindPath(u, u.Player == 1 ? new Vec2(u.Pos.x, u.Room.MapSize.y - 20) : new Vec2(u.Pos.x, 20));

                var mv = v;

                if (u.DecelerateTimeLeft > 0)
                    mv = u.cfg.MaxVelocity - u.Deceleration;

                moveImpl = MakeMove(u, path, mv <= 0 ? 0 : mv);
                target = findTarget();
            };

            // 移动，寻路先到底线再到基地位置
            sm.NewState("moving").OnRunIn((st) =>
            {
                findPath();
            }).Run((st, te) =>
            {
                if (isLandmine)
                {
                    target = findTarget();
                    u.PreferredVelocity = Vec2.Zero;
                    return;
                }

                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                if (u.DecelerateTimeLeft > 0)
                    u.DecelerateTimeLeft -= te;
                else if (u.Deceleration != 0)
                {
                    u.Deceleration = 0;
                    findPath();
                }

                if (!moveImpl(te))
                {
                    findPath();
                    moveImpl(te);
                }

                target = findTarget();
            }).AsDefault();

            // 追赶目标
            Action<Fix64> chasingMoveImpl = null;
            sm.NewState("chasing").OnRunIn((st) =>
            {
                var mv = u.cfg.MaxVelocity;

                if (u.DecelerateTimeLeft > 0)
                    mv = u.cfg.MaxVelocity - u.Deceleration;
                chasingMoveImpl = MakeStraightMove(u, () => target.Pos, mv <= 0 ? 0 : mv);
            }).Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                if (u.DecelerateTimeLeft > 0)
                    u.DecelerateTimeLeft -= te;
                else if (u.Deceleration != 0)
                    u.Deceleration = 0;

                chasingMoveImpl(te);
                target = findTarget();
            });

            // 攻击目标
            var attackCD = Fix64.Zero;
            sm.NewState("attacking").OnRunIn((st) => u.PreferredVelocity = Vec2.Zero).Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                if (u.DecelerateTimeLeft > 0)
                    u.DecelerateTimeLeft -= te;
                else if (u.Deceleration != 0)
                    u.Deceleration = 0;

                // 攻击状态中就不另寻目标
                var tars = u.Room.DoAttack(u, target);

                // 暂时只瘫痪机械单位
                if (trapTime > 0)
                {
                    foreach (var tar in tars)
                    {
                        if (tar.cfg.IsMechanical)
                        {
                            tar.PreferredVelocity = Vec2.Zero;
                            tar.TrappedTimeLeft = trapTime;
                        }
                    }
                }

                if (decelerateTime > 0)
                {
                    foreach (var tar in tars)
                    {
                        tar.Deceleration = deceleration;
                        tar.DecelerateTimeLeft = decelerateTime;
                    }
                }

                attackCD = u.cfg.AttackInterval[target.cfg.IsAirUnit ? 1 : 0];

                // 自爆攻击
                if (suicideAfterAttack)
                    u.Hp = 0;
            });

            // 攻击间隔
            sm.NewState("attackingCD").Run((st, te) => { attackCD -= te; target = findTarget(); });

            sm.Trans().From("moving").To("chasing").When((st) => u.CanAttack(target) && !u.InAttackRange(target));
            sm.Trans().From("moving").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

            sm.Trans().From("chasing").To("moving").When((st) => !u.CanAttack(target));
            sm.Trans().From("chasing").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

            sm.Trans().From("attacking").To("moving").When((st) => attackCD <= 0 && !u.CanAttack(target));
            sm.Trans().From("attacking").To("chasing").When((st) => attackCD <= 0 && u.CanAttack(target) && !u.InAttackRange(target));
            sm.Trans().From("attacking").To("attackingCD").When((st) => attackCD > 0);
            sm.Trans().From("attackingCD").To("attacking").When((st) => attackCD <= 0);

            return sm;
        }

        // 投放机
        static StateMachine Carrier(Unit u)
        {
            var sm = new StateMachine(u.UID);
            
            var pts = new Vec2[] { Vec2.Zero, Vec2.Left, Vec2.Right, Vec2.Up, Vec2.Down };
            var dropped = false;
            var ended = false;
            var moveImpl = MakeMove(u, u.MovePath, u.cfg.MaxVelocity);
            sm.NewState("moving").Run((st, te) =>
            {
                ended = !moveImpl(te);

                u.Pos += u.PreferredVelocity;
                u.Dir = u.PreferredVelocity.Dir();

                if (!dropped && u.MovePath.Count < 2)
                {
                    dropped = true;
                    FC.ForEach(u.UnitCosntructingWaitingList, (i, gu) =>
                    {
                        var dropU = u.Room.AddNewUnit(null, gu, u.Pos + pts[i % pts.Length], u.Player);

                        // 中立怪身上放宝箱
                        if (dropU != null && (gu == "NeutralMonster" || gu == "Blademaster" || gu == "Velkoz"))
                        {
                            dropU.OnDead += () =>
                            {
                                string boxType = "";

                                switch (dropU.UnitType)
                                {
                                    case "NeutralMonster":
                                        boxType = dropU.Room.TBRunner.RandomTreasureBoxType(1);
                                        break;
                                    case "Blademaster":
                                        boxType = dropU.Room.TBRunner.RandomTreasureBoxType(2);
                                        break;
                                    case "Velkoz":
                                        boxType = dropU.Room.TBRunner.RandomTreasureBoxType(3);
                                        break;
                                }
                                dropU.Room.TBRunner.CreateTreasureBox(boxType, dropU.Pos);
                            };
                        }
                    });
                }
            }).AsDefault();
            sm.NewState("dead").OnRunIn((st) =>
            {
                u.Hp = 0;
            }).Run(null);

            // 到目的地就销毁
            sm.Trans().From("moving").To("dead").When((st) => ended);
            return sm;
        }
            
        // 中立怪
        static StateMachine NeutralMonster(Unit u, Func<Unit, bool> targetFilter)
        {
            var v = u.cfg.MaxVelocity;
            var sm = new StateMachine(u.UID);
            List<Vec2> path = null;
            Func<Fix64, bool> moveImpl = null;
            Unit target = null; // 被锁定的攻击目标

            Fix64 idleTime = 1;
            Fix64 moveTime = 5;

            bool isCounterattack = false;

            Fix64 PosMinX = 1;
            Fix64 PosMaxX = u.Room.MapSize.x - 1;

            Fix64 PosMinY = u.Room.MapSize.y / 10;
            Fix64 PosMaxY = u.Room.MapSize.y * 9 / 10;

            // 搜寻目标
            Func<Unit> findTarget = () =>
            {
                return u.GetFirstAttackableUnitsInChasingRange(targetFilter);
            };

            Action findPath = () =>
            {
                int posX = u.Room.RandomNext((int)PosMinX, (int)PosMaxX);
                int posY = u.Room.RandomNext((int)PosMinY, (int)PosMaxY);

                var dst = new Vec2(posX, posY);
                if (u.Tag != null && u.Tag is Unit)
                    dst = ((Unit)u.Tag).Pos;

                path = u.Room.FindPath(u, dst);

                moveImpl = MakeMove(u, path, v);
                target = findTarget();
            };

            sm.NewState("idle").OnRunIn((st) =>
            {
                idleTime = 1;
                u.PreferredVelocity = Vec2.Zero;
            }).Run((st, te) =>
            {
                idleTime -= te;
            });

            sm.NewState("moving").OnRunIn((st) =>
            {
                moveTime = u.Room.RandomNext(1, 6);
                findPath();
            }).Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                moveTime -= te;

                if (!moveImpl(te))
                {
                    findPath();
                    moveImpl(te);
                }

                target = findTarget();
            }).AsDefault();

            // 反击
            sm.NewState("counterattack").OnRunIn((st) =>
            {
                isCounterattack = true;
                findPath();
                u.Tag = null;
            }).Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                if (!moveImpl(te))
                    isCounterattack = false;
            });
            
            // 追赶目标
            var chasingMoveImpl = MakeStraightMove(u, () => target.Pos, u.cfg.MaxVelocity);
            sm.NewState("chasing").Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                chasingMoveImpl(te);
                target = findTarget();
            });

            // 攻击目标
            var attackCD = Fix64.Zero;
            sm.NewState("attacking").OnRunIn((st) => u.PreferredVelocity = Vec2.Zero).Run((st, te) =>
            {
                if (u.TrappedTimeLeft > 0)
                {
                    u.TrappedTimeLeft -= te;
                    return;
                }

                // 攻击状态中就不另寻目标
                u.Room.DoAttack(u, target);
                attackCD = u.cfg.AttackInterval[target.cfg.IsAirUnit ? 1 : 0];
            });

            // 攻击间隔
            sm.NewState("attackingCD").Run((st, te) => { attackCD -= te; target = findTarget(); });

            sm.Trans().From("moving").To("chasing").When((st) => u.CanAttack(target) && !u.InAttackRange(target));
            sm.Trans().From("moving").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

            sm.Trans().From("chasing").To("moving").When((st) => !u.CanAttack(target));
            sm.Trans().From("chasing").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

            sm.Trans().From("attacking").To("moving").When((st) => attackCD <= 0 && !u.CanAttack(target));
            sm.Trans().From("attacking").To("chasing").When((st) => attackCD <= 0 && u.CanAttack(target) && !u.InAttackRange(target));
            sm.Trans().From("attacking").To("attackingCD").When((st) => attackCD > 0);
            sm.Trans().From("attackingCD").To("attacking").When((st) => attackCD <= 0);

            sm.Trans().From("moving").To("idle").When((st) => moveTime <= 0);
            sm.Trans().From("idle").To("moving").When((st) => idleTime <= 0);

            sm.Trans().From("moving").To("counterattack").When((st) => u.Tag != null);
            sm.Trans().From("attacking").To("counterattack").When((st) => u.Tag != null);
            sm.Trans().From("chasing").To("counterattack").When((st) => u.Tag != null);
            sm.Trans().From("idle").To("counterattack").When((st) => u.Tag != null);
            sm.Trans().From("counterattack").To("idle").When((st) => isCounterattack == false);
            sm.Trans().From("counterattack").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

            return sm;
        }

        // 宝箱等人捡
        static StateMachine TreasureBox(Unit u)
        {
            var sm = new StateMachine(u.UID);

            sm.NewState("guarding").Run((st, te) =>
            {
                // 搜索附近可能触发宝箱的单位
                var ts = u.Room.GetUnitsInArea(u.Pos, u.cfg.VisionRadius, (tar) =>
                {
                    // 可移动的地面非中立单位
                    return !tar.cfg.IsAirUnit
                            && !tar.IsNeutral
                            && tar.cfg.MaxVelocity > 0;
                });

                if (ts.Length > 0)
                {
                    u.Room.TBRunner.TriggerOne(ts[0], u.UID);
                    u.Hp = 0;
                }
            }).AsDefault();

            sm.Trans().From("guarding").To("guarding").When((st) => false);

            return sm;
        }

        // 研究科技
        static StateMachine Research(Unit u)
        {
            var sm = new StateMachine(u.UID);

            // var researchingTime = 0;
            sm.NewState("idle").Run(null).AsDefault();
            sm.NewState("researching").Run((st, te) => { });

            sm.Trans().From("idle").To("researching").When((st) => u.UnitCosntructingWaitingList.Count > 0);
            sm.Trans().From("researching").To("idle").When((st) => u.UnitCosntructingWaitingList.Count == 0);

            return sm;
        }

        #endregion

        //// 建造中的建筑物
        //static StateMachine Constructing(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    var waitingTime = u.cfg.ConstructingTime;
        //    var hpAddPerSec = (u.cfg.MaxHp - 1) / waitingTime;
        //    sm.NewState("constructing").Run((st, dt) =>
        //    {
        //        waitingTime -= dt;
        //        u.Hp += hpAddPerSec * dt;
        //    }).AsDefault();
        //    sm.NewState("complete").OnRunIn((st) =>
        //    {
        //        u.BuildingComplete();
        //    }).Run(null);

        //    sm.Trans().From("constructing").To("complete").When((st) => u.BuildingCompleted || waitingTime <= 0);

        //    return sm;
        //}

        //// 资源生产，但一段时间后自毁
        //static StateMachine ResourceProducerWithSuicide(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);
        //    var rTypes = u.cfg.GenResourceType;
        //    var rates = u.cfg.GenResourceRate;
        //    Fix64 waitingTime = 0f;

        //    // 资源生产间隔时间为 1 秒
        //    sm.NewState("waiting").Run((st, dt) =>
        //    {
        //        waitingTime += dt;
        //    }).AsDefault();

        //    // 每秒产生指定数量的资源
        //    sm.NewState("addResource").Run((st, dt) =>
        //    {
        //        FC.For(rTypes.Length, (i) =>
        //        {
        //            var t = rTypes[i];
        //            var r = rates[i];
        //            u.Room.AddResource(u.Player, t, r);
        //        });

        //        waitingTime -= 1;
        //        u.Hp -= 1;
        //    });

        //    // 自毁
        //    sm.NewState("dead").Run(null);

        //    sm.Trans().From("waiting").To("addResource").When((st) => waitingTime >= 1);
        //    sm.Trans().From("addResource").To("waiting").When((st) => waitingTime < 1);
        //    sm.Trans().From("waiting|addResource").To("dead").When((st) => u.Hp <= 0);

        //    return sm;
        //}

        //// 要塞：生产资源的同时搜索攻击目标
        //static StateMachine Fortress(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);
        //    var rTypes = u.cfg.GenResourceType;
        //    var rates = u.cfg.GenResourceRate;

        //    Unit target = null;
        //    Fix64 resourceWatingTime = 0;
        //    Fix64 attackWaitngTime = 0;

        //    // 生产资源并搜索目标
        //    sm.NewState("guarding").Run((st, te) =>
        //    {
        //        resourceWatingTime += te;
        //        attackWaitngTime += te;

        //        // 生产资源
        //        if (resourceWatingTime >= 1)
        //        {
        //            resourceWatingTime -= 1;
        //            FC.For(rTypes.Length, (i) =>
        //            {
        //                u.Room.AddResource(u.Player, rTypes[i], rates[i]);
        //            });
        //        }

        //        // 搜索攻击目标
        //        target = u.GetFirstAttackableUnitsInRange(u.cfg.AttackRange);
        //    }).AsDefault();

        //    // 攻击目标
        //    sm.NewState("attacking").Run((st, te) =>
        //    {
        //        u.Room.DoAttack(u, target);
        //        attackWaitngTime = 0;
        //    });

        //    // 状态迁移
        //    sm.Trans().From("guarding").To("attacking").When((st) => target != null && target.Hp > 0 && attackWaitngTime >= u.cfg.AttackInterval);
        //    sm.Trans().From("attacking").To("guarding").When((st) => target == null || target.Hp <= 0 || attackWaitngTime < u.cfg.AttackInterval);

        //    return sm;
        //}

        //// 资源生产
        //static StateMachine ResourceProducer(Unit u) // , Dictionary<string, int> initialCount = null)
        //{
        //    var sm = new StateMachine(u.UID);
        //    var rTypes = u.cfg.GenResourceType;
        //    var rates = u.cfg.GenResourceRate;
        //    Fix64 waitingTime = 0f;

        //    // 生成初始资源量
        //    //bool initialized = false;
        //    //sm.NewState("initialResource").Run((st, dt) =>
        //    //{
        //    //    FC.For(rTypes.Length, (i) =>
        //    //    {
        //    //        var t = rTypes[i];
        //    //        var initValue = initialCount == null || !initialCount.ContainsKey(t) ? 0 : initialCount[t];
        //    //        u.Room.AddResource(u.Player, t, initValue);
        //    //    });
        //    //    initialized = true;
        //    //}).AsDefault();

        //    // 资源生产间隔时间为 1 秒
        //    sm.NewState("waiting").Run((st, dt) =>
        //    {
        //        waitingTime += dt;
        //    }).AsDefault();

        //    // 每秒产生指定数量的资源
        //    sm.NewState("addResource").Run((st, dt) =>
        //    {
        //        FC.For(rTypes.Length, (i) =>
        //        {
        //            var t = rTypes[i];
        //            var r = rates[i];

        //            // 生产的钱，从矿点消耗掉
        //            if (t == "Money")
        //            {
        //                var stubArr = u.Room.GetUnitsByType("CCStub", 0);
        //                var stubPos = u.Owner == null ? u.Pos : u.Owner.Pos;
        //                Unit stub = null;
        //                foreach (var s in stubArr)
        //                {
        //                    if (s.Pos == stubPos)
        //                    {
        //                        stub = s;
        //                        break;
        //                    }
        //                }

        //                // 找不到矿点就是矿采干了
        //                if (stub != null)
        //                {
        //                    r = stub.Hp < r ? stub.Hp : r;
        //                    stub.Hp -= r;
        //                }
        //                else
        //                    r = 0;
        //            }

        //            u.Room.AddResource(u.Player, t, r);
        //            u.Room.OnProduceResource(u, t, r);
        //        });

        //        waitingTime -= 1;
        //    });

        //    // sm.Trans().From("initialResource").To("waiting").When((st) => initialized);
        //    sm.Trans().From("waiting").To("addResource").When((st) => waitingTime >= 1);
        //    sm.Trans().From("addResource").To("waiting").When((st) => waitingTime < 1);

        //    return sm;
        //}

        //// 生产建筑
        //static StateMachine BattleUnitProducer(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);
        //    var genUnitType = "";

        //    // 尝试将建造列表的内容推给挂件执行
        //    Action try2Push2Accessories = () =>
        //    {
        //        if (u.UnitCosntructingWaitingList.Count <= 1)
        //            return;
                
        //        foreach (var acc in u.Accessories)
        //        {
        //            if (acc == null
        //                || !acc.BuildingCompleted
        //                || acc.Room.GetSM(acc).CurrentState != "idle")
        //                continue;

        //            var genType = u.UnitCosntructingWaitingList[0];
        //            u.UnitCosntructingWaitingList.RemoveAt(0);
        //            acc.UnitCosntructingWaitingList.Add(genType);
        //            u.Room.NotifyConstructingWaitingListChanged(u, genType);
        //            return;
        //        }
        //    };

        //    Fix64 waitingTime = 0f;
        //    sm.NewState("idle").Run(null).AsDefault();
        //    sm.NewState("constructingBattleUnit")
        //        .OnRunIn((st) =>
        //        {
        //            genUnitType = u.UnitCosntructingWaitingList[0];
        //            waitingTime = u.Room.GetPlayerUnitConfig(u.Player, genUnitType).ConstructingTime;
        //            u.Room.OnConstructingBattleUnitStarted(u, genUnitType);
        //            u.Room.NotifyConstructingWaitingListChanged(u, genUnitType);
        //        })
        //        .Run((st, dt) =>
        //        {
        //            waitingTime -= dt;
        //            if (waitingTime <= 0)
        //            {
        //                if (u.Room.OnBattleUnitConstructingCompleted(u, genUnitType) == null)
        //                    waitingTime += dt; // 可能因为人口上限卡住产兵，则一直等在最后一刻
        //                else
        //                {
        //                    u.UnitCosntructingWaitingList.RemoveAt(0); // 从建造列表移除
        //                    u.Room.NotifyConstructingWaitingListChanged(u, genUnitType);
        //                }
        //            }

        //            try2Push2Accessories();
        //        });

        //    sm.Trans().From("idle").To("constructingBattleUnit").When((st) => u.UnitCosntructingWaitingList.Count > 0);
        //    sm.Trans().From("constructingBattleUnit").To("idle").When((st) => waitingTime <= 0);

        //    return sm;
        //}

        //// 简单地面移动
        //static StateMachine Move(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    sm.NewState("idle").OnRunIn((st) =>
        //    {
        //        u.MovePath.Clear();
        //        u.PreferredVelocity = Vec2.Zero;
        //    }).Run(null).AsDefault();
        //    sm.NewState("moving").Run(MakeMove(u, u.MovePath));

        //    sm.Trans().From("idle").To("moving").When((st) => u.MovePath.Count > 0);
        //    sm.Trans().From("moving").To("idle").When((st) => u.MovePath.Count == 0);

        //    return sm;
        //}

        //// 地面固定位置攻击，不可移动
        //static StateMachine HoldAndAttack(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    // 攻击目标
        //    Unit target = null;

        //    // 原地寻找攻击目标
        //    sm.NewState("guarding").OnRunIn((st) =>
        //    {
        //        u.PreferredVelocity = Vec2.Zero;
        //    }).Run((st, dt) =>
        //    {
        //        target = u.GetFirstAttackableUnitsInRange(u.cfg.AttackRange);
        //    }).AsDefault();

        //    // 站立攻击目标
        //    var attackImpl = MakeHoldAndAttack(u, () => target);
        //    sm.NewState("attacking").Run((st, te) =>
        //    {
        //        attackImpl(st, te);
        //        target = u.GetFirstAttackableUnitsInRange(u.cfg.AttackRange); // 重新搜索目标
        //    });

        //    sm.Trans().From("guarding").To("attacking").When((st) => u.CanAttack(target));
        //    sm.Trans().From("attacking").To("guarding").When((st) => !u.CanAttack(target));

        //    return sm;
        //}

        //// 地面移动和攻击
        //static StateMachine MoveAndAttack(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    // 攻击目标
        //    Unit target = null;
        //    Func<Fix64, Unit> confirmTarget = (dt) =>
        //    {
        //        if (u.IgnoreTargetWhenMoving && u.MovePath.Count > 0)
        //            return null;

        //        // 原有攻击目标还可以攻击，并且是优先攻击类型（可以攻击自己的是优先攻击类型），就不更换目标
        //        if (u.CanAttack(target) && target.CanAttack(u) && u.InAttackRange(target))
        //            return target;
        //        else
        //            return u.GetFirstAttackableUnitsInRange(u.cfg.GuardingRange);
        //    };

        //    //// 原地寻找攻击目标
        //    //sm.NewState("guarding").OnRunIn((st) =>
        //    //{
        //    //    u.PreferredVelocity = Vec2.Zero;
        //    //}).Run((st, dt) =>
        //    //{
        //    //    target = confirmTarget(dt);
        //    //}).AsDefault();

        //    // 沿路径移动
        //    var moveImpl = MakeMove(u, u.MovePath);
        //    sm.NewState("moving").Run((st, dt) =>
        //    {
        //        moveImpl(st, dt);
        //        target = confirmTarget(dt);
        //    }).AsDefault();

        //    // 向目标直线移动
        //    var msImpl = MakeStraightMove(u, () => target.Pos);
        //    sm.NewState("chasing").Run((st, dt) =>
        //    {
        //        msImpl(st, dt);
        //        target = confirmTarget(dt);
        //    });

        //    // 站立攻击目标
        //    var attackingImpl = MakeHoldAndAttack(u, () => target);
        //    sm.NewState("attacking").OnRunIn((st) => u.PreferredVelocity = Vec2.Zero)
        //        .Run((st, dt) =>
        //        {
        //            attackingImpl(st, dt);
        //            target = confirmTarget(dt);
        //        });

        //    //sm.Trans().From("guarding").To("moving").When((st) => u.MovePath.Count > 0);
        //    //sm.Trans().From("guarding").To("chasing").When((st) => u.CanAttack(target) && !u.InAttackRange(target));
        //    //sm.Trans().From("guarding").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));

        //    //sm.Trans().From("moving").To("guarding").When((st) => u.MovePath.Count == 0);
        //    sm.Trans().From("moving").To("chasing").When((st) => u.CanAttack(target));

        //    // sm.Trans().From("chasing").To("guarding").When((st) => !u.CanAttack(target));
        //    sm.Trans().From("chasing").To("attacking").When((st) => u.CanAttack(target) && u.InAttackRange(target));
        //    sm.Trans().From("chasing").To("moving").When((st) => !u.CanAttack(target));

        //    sm.Trans().From("attacking").To("chasing").When((st) => u.CanAttack(target) && !u.InAttackRange(target));
        //    sm.Trans().From("attacking").To("moving").When((st) => !u.CanAttack(target));
        //    // sm.Trans().From("attacking").To("guarding").When((st) => !u.CanAttack(target));

        //    return sm;
        //}

        //// 存活一段时间后死亡
        //static StateMachine Suicide(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);
        //    sm.NewState("countingdown").Run((st, dt) => { u.Hp -= dt; }).AsDefault();
        //    sm.Trans().From("countingdown").To("countingdown").When((st) => false);
        //    return sm;
        //}

        //// 治疗塔，固定间隔给范围内的己方单位加血，按百分比
        //static StateMachine HealingTower(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    var t = Fix64.Zero;
        //    sm.NewState("waiting").Run((st, dt) =>
        //    {
        //        t += dt;
        //    }).AsDefault();
        //    sm.NewState("healing").Run((st, dt) =>
        //    {
        //        t = 0;
        //        u.Room.DoAttack(u, u);
        //    });

        //    sm.Trans().From("waiting").To("healing").When((st) => t >= u.cfg.AttackInterval);
        //    sm.Trans().From("healing").To("waiting").When((st) => t <= 0);

        //    return sm;
        //}

        //#region 基本行为实现

        // 沿路径移动，返回下一个目标路径点
        static Vec2 RunPath(Vec2 from, List<Vec2> path, Fix64 dist, out Vec2 ps, out Vec2 pe)
        {
            var dst = path.Count > 0 ? path[path.Count - 1] : from;
            ps = from;
            pe = from;

            while (dist > 0 && path.Count > 0)
            {
                pe = path[0];
                var d = (pe - ps).Length;
                if (dist >= d)
                {
                    dist -= d;
                    ps = pe;
                    path.RemoveAt(0);
                }
                else
                    return ps + (pe - ps) * dist / d;
            }

            return dst;
        }

        // 直线向目标移动
        static Action<Fix64> MakeStraightMove(Unit u, Func<Vec2> getDst, Fix64 v)
        {
            return (dt) =>
            {
                var dst = getDst();
                var maxDist = v * dt;
                var dir = dst - u.Pos;
                var dist = dir.Length;
                u.Dir = dir.Dir();

                u.PreferredVelocity = (maxDist >= dir.Length) ? dir : dir * maxDist / dist;
            };
        }

        // 沿路径移动
        static Func<Fix64, bool> MakeMove(Unit u, List<Vec2> path, Fix64 v)
        {
            Vec2 nowPos = u.Pos;
            return (dt) =>
            {
                // 计算当前位置
                var maxDist = v * dt;
                var ps = Vec2.Zero;
                var pe = Vec2.Zero;
                nowPos = RunPath(nowPos, path, maxDist, out ps, out pe);
                var dir = pe - ps;
                var dist = dir.Length;
                u.Dir = dist > 0 ? dir.Dir() : u.ForwardDir;

                u.PreferredVelocity = (maxDist >= dir.Length) ? dir : dir * maxDist / dist;

                return u.cfg.IsAirUnit ? path.Count > 0 : path.Count > 0 && u.Room.CheckSpareSpace(nowPos, 1, null);
            };
        }

        //// 站立攻击目标
        //static Action<State, Fix64> MakeHoldAndAttack(Unit u, Func<Unit> getTarget)
        //{
        //    Fix64 attackingInterval = 0f;
        //    return (st, dt) =>
        //    {
        //        if (attackingInterval > 0)
        //            attackingInterval -= dt;
        //        else
        //        {
        //            var target = getTarget();
        //            if (target == null)
        //                return;

        //            u.Room.DoAttack(u, target);
        //            attackingInterval = u.cfg.AttackInterval;
        //        }
        //    };
        //}

        //// 宝箱投放机
        //static StateMachine TreasureBoxCarrier(Unit u)
        //{
        //    var sm = new StateMachine(u.UID);

        //    var dropped = false;
        //    var moveImpl = MakeMove(u, u.MovePath);
        //    sm.NewState("moving").Run((st, te) =>
        //    {
        //        moveImpl(st, te);
        //        u.Pos += u.PreferredVelocity;
        //        u.Dir = u.PreferredVelocity.Dir();

        //        if (!dropped && u.MovePath.Count < 2)
        //        {
        //            dropped = true;
        //            u.Room.TBRunner.CreateRandomTreasureBox(u.Pos);
        //        }
        //    }).AsDefault();
        //    sm.NewState("dead").OnRunIn((st) =>
        //    {
        //        u.Hp = 0;
        //    }).Run(null);

        //    // 到目的地就销毁
        //    sm.Trans().From("moving").To("dead").When((st) => u.MovePath.Count == 0);

        //    return sm;
        //}

        //#endregion
    }
}
