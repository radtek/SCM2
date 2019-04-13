using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Swift;
using Swift.Math;
using Server;

namespace SCM
{
    /// <summary>
    /// PVE服务器端AI，困难模式
    /// </summary>
    public class AIDifficultRush : AIComputerOpponent
    {
        public AIDifficultRush(string id, Room room, int player) : base(id, room, player)
        {
        }

        public override void Init()
        {
            Room4Server room = (Room4Server)Room;

            Random random = new Random();
            Fix64 PosMinX = 1;
            Fix64 PosMaxX = room.MapSize.x - 1;
            Fix64 PosMinY = Player == 1 ? 10 : room.MapSize.y / 3;
            Fix64 PosMaxY = Player == 1 ? room.MapSize.y / 3 : room.MapSize.y - 10;

            bool isCrystalMachineEnough = false;
            bool isGasFactoryEnough = false;
            int curGasFactoryNum = 0;
            Unit curBarrackU = null;
            bool isFactoryEnough = false;
            int curFactoryNum = 0;
            Unit curVelTechU = null;
            bool dropOver = false;
            bool IsFireBatEnough = false;
            //long lastDefendTime = TimeUtils.NowSecond;


            // 建造矿机
            sm.NewState("CreateCrystalMachine").OnRunIn((st) =>
            {
                isCrystalMachineEnough = false;
            }).Run((st, te) =>
            {
                var bsU = room.GetUnitsByType("Base", Player, null);

                if (null == bsU)
                    return;

                if (bsU.Length < 1)
                    return;

                if (room.SrvConstructCrystalMachine(bsU[0]))
                    return;

                if (room.GetResource(Player, "Money") < UnitConfiguration.GetDefaultConfig("CrystalMachine").Cost)
                    return;

                isCrystalMachineEnough = true;
            }).AsDefault();

            sm.Trans().From("CreateCrystalMachine").To("CreateGasFactory").When((st) => isCrystalMachineEnough);

            // 建造瓦斯厂
            sm.NewState("CreateGasFactory").OnRunIn((st) =>
            {
                isGasFactoryEnough = false;
                curGasFactoryNum = room.GetUnitsByType("GasFactory", Player, null).Length;
            }).Run((st, te) =>
            {
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                var fU = room.GetUnitsByType("Factory", Player, null);

                int totalNum = vtU.Length >= 1 ? 3 : fU.Length >= 1 ? 2 : 1;

                if (curGasFactoryNum >= totalNum)
                {
                    isGasFactoryEnough = true;
                    return;
                }

                // 随机位置
                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(posX, posY);

                var u = room.SrvConstructBuilding(Player, "GasFactory", pos);

                if (null == u)
                    return;

                curGasFactoryNum++;
            });

            sm.Trans().From("CreateGasFactory").To("CreateBarrack").When((st) => isGasFactoryEnough);

            // 建造兵营
            sm.NewState("CreateBarrack").OnRunIn((st) =>
            {

            }).Run((st, te) =>
            {
                var bU = room.GetUnitsByType("Barrack", Player, null);

                if (null != curBarrackU)
                    return;

                if (bU.Length >= 1)
                    return;

                // 随机位置
                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(posX, posY);

                curBarrackU = room.SrvConstructBuilding(Player, "Barrack", pos);
                return;
            });

            sm.Trans().From("CreateBarrack").To("CreateFireGuard").When((st) => null != curBarrackU && curBarrackU.BuildingCompleted);

            // 建造守卫
            sm.NewState("CreateFireGuard").OnRunIn((st) =>
            {
                IsFireBatEnough = false;
            }).Run((st, te) =>
            {
                var fbU = room.GetUnitsByType("FireGuard", Player, null);

                if (fbU == null || fbU.Length < 3)
                {
                    int posX = 0;
                    int posY = 0;
                    Vec2 pos = Vec2.Zero;

                    if (fbU == null || fbU.Length == 0)
                    {
                        posX = random.Next(1, 20);
                        posY = random.Next(40, (int)PosMaxY);
                        pos = new Vec2(posX, posY);
                    }
                    else if (fbU.Length == 1)
                    {
                        if (fbU[0].Pos.x < 20)
                            posX = random.Next(20, 40);
                        else if (fbU[0].Pos.x < 40)
                            posX = random.Next(1, 20);
                        else
                            posX = random.Next(20, 40);

                        posY = random.Next(40, (int)PosMaxY);
                        pos = new Vec2(posX, posY);
                    }
                    else if (fbU.Length == 2)
                    {
                        if ((fbU[0].Pos.x < 20 && fbU[1].Pos.x < 40) || (fbU[0].Pos.x < 40 && fbU[1].Pos.x < 20))
                        {
                            posX = random.Next(40, 60);
                        }
                        else if ((fbU[0].Pos.x >= 20 && fbU[1].Pos.x >= 40) || (fbU[0].Pos.x >= 40 && fbU[1].Pos.x >= 20))
                        {
                            posX = random.Next(1, 20);
                        }
                        else
                        {
                            posX = random.Next(20, 40);
                        }
                        posY = random.Next(40, (int)PosMaxY);
                        pos = new Vec2(posX, posY);
                    }

                    var u = room.SrvConstructBuilding(Player, "FireGuard", pos);
                }
                else
                    IsFireBatEnough = true;
            });

            sm.Trans().From("CreateFireGuard").To("CreateFactory").When((st) => IsFireBatEnough);

            // 建造工厂
            sm.NewState("CreateFactory").OnRunIn((st) =>
            {
                isFactoryEnough = false;
                curFactoryNum = room.GetUnitsByType("Factory", Player, null).Length;
            }).Run((st, te) =>
            {
                var vtU = room.GetUnitsByType("VelTech", Player, null);

                int totalNum = vtU.Length >= 1 ? 2 : 1;

                if (curFactoryNum >= totalNum)
                {
                    isFactoryEnough = true;
                    return;
                }

                // 随机位置
                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(posX, posY);

                var u = room.SrvConstructBuilding(Player, "Factory", pos);

                if (null == u)
                    return;

                curFactoryNum++;
            });

            sm.Trans().From("CreateFactory").To("CreateVelTech").When((st) =>
            {
                isGasFactoryEnough = false;

                curGasFactoryNum = room.GetUnitsByType("GasFactory", Player, null).Length;
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                var fU = room.GetUnitsByType("Factory", Player, null);

                int totalNum = vtU.Length >= 1 ? 3 : fU.Length >= 1 ? 2 : 1;

                if (curGasFactoryNum >= totalNum)
                    isGasFactoryEnough = true;

                return isGasFactoryEnough && isFactoryEnough;
            });

            sm.Trans().From("CreateFactory").To("CreateGasFactory").When((st) =>
            {
                isGasFactoryEnough = false;

                curGasFactoryNum = room.GetUnitsByType("GasFactory", Player, null).Length;
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                var fU = room.GetUnitsByType("Factory", Player, null);

                int totalNum = vtU.Length >= 1 ? 3 : fU.Length >= 1 ? 2 : 1;

                if (curGasFactoryNum >= totalNum)
                    isGasFactoryEnough = true;

                return !isGasFactoryEnough && isFactoryEnough;
            });

            // 建造车辆工程
            sm.NewState("CreateVelTech").OnRunIn((st) =>
            {
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                curVelTechU = vtU.Length >= 1 ? vtU[0] : null;
            }).Run((st, te) =>
            {
                if (null != curVelTechU)
                    return;

                // 随机位置
                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(posX, posY);

                curVelTechU = room.SrvConstructBuilding(Player, "VelTech", pos);
            });

            sm.Trans().From("CreateVelTech").To("CreateFactoryAccessory").When((st) =>
            {
                isGasFactoryEnough = false;

                curGasFactoryNum = room.GetUnitsByType("GasFactory", Player, null).Length;
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                var fU = room.GetUnitsByType("Factory", Player, null);

                int totalNum = vtU.Length >= 1 ? 3 : fU.Length >= 1 ? 2 : 1;

                if (curGasFactoryNum >= totalNum)
                    isGasFactoryEnough = true;

                isFactoryEnough = false;
                curFactoryNum = room.GetUnitsByType("Factory", Player, null).Length;

                var vt1U = room.GetUnitsByType("VelTech", Player, null);

                int totalNum1 = vt1U.Length >= 1 ? 2 : 1;

                if (curFactoryNum >= totalNum1)
                    isFactoryEnough = true;

                return isGasFactoryEnough && isFactoryEnough && null != curVelTechU;
            });

            sm.Trans().From("CreateVelTech").To("CreateFactory").When((st) =>
            {
                isFactoryEnough = false;
                curFactoryNum = room.GetUnitsByType("Factory", Player, null).Length;

                var vt1U = room.GetUnitsByType("VelTech", Player, null);

                int totalNum1 = vt1U.Length >= 1 ? 2 : 1;

                if (curFactoryNum >= totalNum1)
                    isFactoryEnough = true;

                return !isFactoryEnough && null != curVelTechU;
            });

            sm.Trans().From("CreateVelTech").To("CreateGasFactory").When((st) =>
            {
                isGasFactoryEnough = false;

                curGasFactoryNum = room.GetUnitsByType("GasFactory", Player, null).Length;
                var vtU = room.GetUnitsByType("VelTech", Player, null);
                var fU = room.GetUnitsByType("Factory", Player, null);

                int totalNum = vtU.Length >= 1 ? 3 : fU.Length >= 1 ? 2 : 1;

                if (curGasFactoryNum >= totalNum)
                    isGasFactoryEnough = true;

                isFactoryEnough = false;
                curFactoryNum = room.GetUnitsByType("Factory", Player, null).Length;

                var vt1U = room.GetUnitsByType("VelTech", Player, null);

                int totalNum1 = vt1U.Length >= 1 ? 2 : 1;

                if (curFactoryNum >= totalNum1)
                    isFactoryEnough = true;

                return !isGasFactoryEnough && isFactoryEnough && null != curVelTechU;
            });

            // 建造工厂仓库
            sm.NewState("CreateFactoryAccessory").OnRunIn((st) =>
            {

            }).Run((st, te) =>
            {
                var bU = room.GetUnitsByType("Factory", Player, null);

                if (null == curVelTechU)
                    return;

                if (bU == null || bU.Length < 1)
                    return;

                if (bU.Length >= 1)
                {
                    if (bU[0].BuildingCompleted)
                        room.SrvConstructAccessory(bU[0].UID, "Accessory");

                    if (bU.Length >= 2)
                    {
                        if (bU[1].BuildingCompleted)
                            room.SrvConstructAccessory(bU[1].UID, "Accessory");
                    }
                }
            });

            sm.Trans().From("CreateFactoryAccessory").To("CreateTank").When((st) =>
            {
                var money = room.GetResource(Player, "Money");
                var gas = room.GetResource(Player, "Gas");

                var info = UnitConfiguration.GetDefaultConfig("Tank");

                return money >= info.Cost * 6 && gas >= info.GasCost * 6;
            });

            // 出坦克
            sm.NewState("CreateTank").OnRunIn((st) =>
            {

            }).Run((st, te) =>
            {
                // 随机位置
                int posX = random.Next((int)PosMinX, (int)PosMaxX);
                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(posX, posY);

                room.SrvAddBattltUnitAt(Player, "Tank", pos);
            });

            sm.Trans().From("CreateTank").To("CreateTank").When((st) => false);


            //// 出机器人
            //sm.NewState("CreateRobot").OnRunIn((st) =>
            //{

            //}).Run((st, te) =>
            //{
            //    // 随机位置
            //    int posX = random.Next((int)PosMinX, (int)PosMaxX);
            //    int posY = random.Next((int)PosMinY, (int)PosMaxY);
            //    Vec2 pos = new Vec2(posX, posY);

            //    room.SrvAddBattltUnitAt(Player, "Robot", pos);
            //});

            //// 出机枪兵
            //sm.NewState("CreateSoldier").OnRunIn((st) =>
            //{

            //}).Run((st, te) =>
            //{
            //    // 随机位置
            //    int posX = random.Next((int)PosMinX, (int)PosMaxX);
            //    int posY = random.Next((int)PosMinY, (int)PosMaxY);
            //    Vec2 pos = new Vec2(posX, posY);

            //    room.SrvAddBattltUnitAt(Player, "Soldier", pos);
            //});

            //// 出侦查犬
            //sm.NewState("CreateDog").OnRunIn((st) =>
            //{

            //}).Run((st, te) =>
            //{
            //    // 随机位置
            //    int posX = random.Next((int)PosMinX, (int)PosMaxX);
            //    int posY = random.Next((int)PosMinY, (int)PosMaxY);
            //    Vec2 pos = new Vec2(posX, posY);

            //    room.SrvAddBattltUnitAt(Player, "Dog", pos);
            //});

            bool isNeedDefend = false;
            Unit curDefendU = null;

            room.OnAddBattltUnitAt += (p, u) =>
            {
                if (p == Player)
                    return;

                curDefendU = u;
                isNeedDefend = true;
            };

            // 防守
            sm.NewState("Defend").OnRunIn((st) =>
            {
                dropOver = false;
                //lastDefendTime = TimeUtils.NowSecond;
            }).Run((st, te) =>
            {
                //// 随机位置
                //int posX = random.Next((int)PosMinX, (int)PosMaxX);
                //int posY = random.Next((int)PosMinY, (int)PosMaxY);
                //Vec2 pos = new Vec2(posX, posY);

                //var enemys = room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2);

                //if (null != enemys && enemys.Length > 0)
                //{
                //    // 随机一个敌军，在其位置放兵
                //    var target = enemys[random.Next(0, enemys.Length)];
                //    pos = new Vec2(target.Pos.x, target.Pos.y);
                //}

                //if (null != room.SrvAddBattltUnitAt(Player, "Tank", pos))
                //    return;

                //if (null != room.SrvAddBattltUnitAt(Player, "Robot", pos))
                //    return;

                //if (null != room.SrvAddBattltUnitAt(Player, "Soldier", pos))
                //    return;

                //if (null != room.SrvAddBattltUnitAt(Player, "Dog", pos))
                //    return;

                int posY = random.Next((int)PosMinY, (int)PosMaxY);
                Vec2 pos = new Vec2(curDefendU.Pos.x, posY);

                // 暂时没有单独的狗了
                if (curDefendU.cfg.TechLevel >= UnitConfiguration.GetDefaultConfig("Tank").TechLevel)
                {
                    if (null == room.SrvAddBattltUnitAt(Player, "Tank", pos))
                    {
                        if (null == room.SrvAddBattltUnitAt(Player, "Robot", pos))
                        {
                            room.SrvAddBattltUnitAt(Player, "SoldierWithDog", pos);
                        }
                    }
                }
                else if (curDefendU.cfg.TechLevel >= UnitConfiguration.GetDefaultConfig("Robot").TechLevel)
                {
                    if (null == room.SrvAddBattltUnitAt(Player, "Robot", pos))
                    {
                        if (null == room.SrvAddBattltUnitAt(Player, "Soldier", pos))
                        {
                            // room.SrvAddBattltUnitAt(Player, "Dog", pos);
                        }
                        else
                        {
                            room.SrvAddBattltUnitAt(Player, "Dog", pos);
                            room.SrvAddBattltUnitAt(Player, "Dog", pos);
                        }
                    }
                }
                else if (curDefendU.cfg.TechLevel >= UnitConfiguration.GetDefaultConfig("Soldier").TechLevel)
                {
                    if (null == room.SrvAddBattltUnitAt(Player, "Soldier", pos))
                    {
                        // room.SrvAddBattltUnitAt(Player, "Dog", pos);
                    }
                    else
                    {
                        room.SrvAddBattltUnitAt(Player, "Dog", pos);
                        room.SrvAddBattltUnitAt(Player, "Dog", pos);
                    }
                }
                else
                {
                    // room.SrvAddBattltUnitAt(Player, "Dog", pos);
                }
                
                isNeedDefend = false;
                curDefendU = null;

                dropOver = true;
            });

            sm.Trans().From("Defend").To("CreateCrystalMachine").When((st) => dropOver);

            // 其它状态迁移防守状态集合
            //sm.Trans().From("CreateCrystalMachine").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateGasFactory").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateBarrack").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateFactory").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateVelTech").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateFactoryAccessory").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);
            //sm.Trans().From("CreateTank").To("Defend").When((st) => TimeUtils.NowSecond - lastDefendTime > 10 && room.GetUnitsInArea(new Vec2(30, 30), 30, (u) => u.Player == 2).Length > 0);

            sm.Trans().From("CreateCrystalMachine").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateGasFactory").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateBarrack").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateFactory").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateVelTech").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateFactoryAccessory").To("Defend").When((st) => isNeedDefend);
            sm.Trans().From("CreateTank").To("Defend").When((st) => isNeedDefend);
        }
    }
}