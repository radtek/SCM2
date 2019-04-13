using System;
using System.Collections.Generic;
using System.Linq;
using Swift;
using Swift.Math;
using SCM;
using RVO;

namespace SCM
{
    /// <summary>
    /// 战斗地图
    /// </summary>
    public class Map
    {
        // 所属房间
        public Room Room { get; set; }

        // 地图尺寸
        public Vec2 Size { get; private set; }
        public Vec2 HalfSize { get { return Size / 2; } }

        // 地图上的单位
        StableDictionary<string, Unit> units = new StableDictionary<string, Unit>();

        // RVO 模拟器处理碰撞躲避
        Simulator simulator = null;
        Simulator airforceSim = null; // 空中对象

        // RVOAgent 的 id 映射到 Unit 对象
        StableDictionary<int, Unit> rvoID2Units = new StableDictionary<int, Unit>();
        StableDictionary<int, Unit> rvoID2AirUnits = new StableDictionary<int, Unit>();

        // 0 是地面， 1 是空中
        MapGrid<string>[] grids = null;

        public Map(Vec2 sz, Fix64 frameInterval)
        {
            Size = sz;
            grids = new MapGrid<string>[] {
                    new MapGrid<string>((int)sz.x, (int)sz.y),
                    new MapGrid<string>((int)sz.x, (int)sz.y)
            };
            CreateRVOSimulator(frameInterval);
        }

        public void Clear()
        {
            foreach (var g in grids)
                g.Clear();

            DestroyRVOSimulator();
        }

        public void AddUnitAt(Unit u)
        {
            units[u.UID] = u;

            if (u.cfg.SizeRadius > 0 && !u.cfg.NoBody)
            {
                var aid = simulator.addAgent(u.Pos, u.cfg.SizeRadius, u.cfg.MaxVelocity);
                u.RVOAgentID = aid;
                rvoID2Units [aid] = u;
            }
            else
                u.RVOAgentID = 0;
            
            SettleUnit(u.UID);
        }

        public bool AddUnitInAir(Unit u)
        {
            units[u.UID] = u;

            if (u.cfg.SizeRadius > 0 && !u.cfg.NoBody)
            {
                var aid = airforceSim.addAgent(u.Pos, u.cfg.SizeRadius, u.cfg.MaxVelocity);
                u.RVOAgentID = aid;
                rvoID2AirUnits[aid] = u;
            }
            else
                u.RVOAgentID = 0;

            SettleUnit(u.UID);

            return true;
        }

        public void RemoveUnit(string uid)
        {
            var u = units[uid];
            if (u.RVOAgentID > 0)
            {
                var aid = u.RVOAgentID;

                if (u.cfg.IsAirUnit)
                {
                    rvoID2AirUnits.Remove(aid);
                    airforceSim.removeAgent(aid);
                }
                else
                {
                    rvoID2Units.Remove(aid);
                    simulator.removeAgent(aid);
                }
            }

            UnsettleUnit(uid);
            units.Remove(uid);
        }

        public void UnsettleUnit(string uid)
        {
            var u = units[uid];
            if (!u.cfg.IsFixed)
                return;

            var gd = u.cfg.IsAirUnit ? grids[1] : grids[0];
            gd.UnsetBlock((int)u.Pos.x, (int)u.Pos.y, u.cfg.SizeRadius, uid);
        }

        public void SettleUnit(string uid)
        {
            var u = units[uid];
            if (!u.cfg.IsFixed)
                return;

            var gd = u.cfg.IsAirUnit ? grids[1] : grids[0];
            gd.SetBlock((int)u.Pos.x, (int)u.Pos.y, u.cfg.SizeRadius, uid);
        }

        // 设置指定单位的行进速度
        public void SetPreferredVelocity(Unit u, Vec2 v)
        {
            var sim = u.cfg.IsAirUnit ? airforceSim : simulator;
            sim.setAgentPrefVelocity(u.RVOAgentID, v);
        }

        public void DoOneStep(int te)
        {
            DoOneStepOnGround(te);
            DoOneStepInAir(te);
        }

        void DoOneStepInAir(int te)
        {
            foreach (var agentID in airforceSim.getAllAgents())
            {
                var u = rvoID2AirUnits[agentID];

                var pos = airforceSim.getAgentPosition(agentID);
                if (u.Pos != pos)
                    airforceSim.setAgentPosition(agentID, u.Pos);

                if (u.cfg.MaxVelocity > 0)
                    airforceSim.setAgentPrefVelocity(agentID, u.PreferredVelocity * 1000);
            }

            // 检查各单位向目标行进的情况，如果向同一目标行进的一组单位的中心不再靠近
            airforceSim.setTimeStep(te / 1000.0f);
            airforceSim.doStep();

            foreach (var agentID in airforceSim.getAllAgents())
            {
                var pos = airforceSim.getAgentPosition(agentID);
                // var v = airforceSim.getAgentVelocity(agentID);
                var u = rvoID2AirUnits[agentID];

                if (!u.cfg.IsFixed)
                {
                    if (pos != u.Pos)
                    {
                        if (pos.x < 0)
                            pos.x = 0;
                        else if (pos.x >= Size.x)
                            pos.x = Size.x - 1;

                        if (pos.y < 0)
                            pos.y = 0;
                        else if (pos.y >= Size.y)
                            pos.y = Size.y - 1;

                        u.Pos = pos;
                    }
                }

                //if (u.PreferredVelocity.Length > float.Epsilon)
                //    u.Dir = v.Dir();
            }
        }

        // 推进游戏进度
        public void DoOneStepOnGround(int te)
        {
            foreach (var agentID in simulator.getAllAgents())
            {
                var u = rvoID2Units[agentID];

                var pos = simulator.getAgentPosition(agentID);
                if (u.Pos != pos)
                    simulator.setAgentPosition(agentID, u.Pos);

                if (u.cfg.MaxVelocity > 0)
                    simulator.setAgentPrefVelocity(agentID, u.PreferredVelocity * 10 /* PreferredVelocity 计算出的是 0.1s 的距离，但这里要的是 s 为单位的速度，差 10 倍 */);
            }

            // 检查各单位向目标行进的情况，如果向同一目标行进的一组单位的中心不再靠近
            simulator.setTimeStep(te / 1000.0f);
            simulator.doStep();

            foreach (var agentID in simulator.getAllAgents())
            {
                var pos = simulator.getAgentPosition(agentID);
                // var v = simulator.getAgentVelocity(agentID);
                var u = rvoID2Units[agentID];

                if (!u.cfg.IsFixed)
                {
                    if (pos != u.Pos)
                    {
                        if (pos.x < 0)
                            pos.x = 0;
                        else if (pos.x >= Size.x)
                            pos.x = Size.x - 1;

                        if (pos.y < 0)
                            pos.y = 0;
                        else if (pos.y >= Size.y)
                            pos.y = Size.y - 1;

                        u.Pos = pos;
                    }
                }

                //if (v.Length > float.Epsilon)
                //    u.Dir = v.Dir();
            }
        }

        void CreateRVOSimulator(Fix64 frameInterval)
        {
            simulator = new RVO.Simulator();
            simulator.setTimeStep(frameInterval);
            simulator.setAgentDefaults(1, 1, 1, 1, 0f, 0f, Vec2.Zero);
            simulator.processObstacles();

            airforceSim = new RVO.Simulator();
            airforceSim.setTimeStep(frameInterval);
            airforceSim.setAgentDefaults(1, 1, 1, 1, 0f, 0f, Vec2.Zero);
            airforceSim.processObstacles();
        }

        void DestroyRVOSimulator()
        {
            simulator.Clear();
            simulator = null;

            airforceSim.Clear();
            airforceSim = null;
        }

        public Unit Get(string uid)
        {
            return units.ContainsKey(uid) ? units[uid] : null;
        }

        public Unit[] AllUnits
        {
            get
            {
                return units.ValueArray;
            }
        }

        public List<Vec2> FindPath(Unit u, Vec2 dst, string asEmptyUID)
        {
            return (u.cfg.IsAirUnit ? grids[1] : grids[0]).FindPath(u.Pos, dst, u.cfg.SizeRadius, u.UID, asEmptyUID);
        }

        //public void OnUnitMovedFrom(Unit u, Vec2 srcPos)
        //{
        //    var gd = u.cfg.IsAirUnit ? grids[1] : grids[0];
        //    gd.UnsetBlock((int)srcPos.x, (int)srcPos.y, u.cfg.SizeRadius, u.UID);
        //    gd.SetBlock((int)u.Pos.x, (int)u.Pos.y, u.cfg.SizeRadius, u.UID);
        //}

        #region 地图位置占用相关

        public bool CheckSpareSpace(bool inAir, int cx, int cy, int radius, params string[] asEmptyUID) { return (inAir ? grids[1] : grids[0]).CheckSpareSpace(cx, cy, radius, asEmptyUID); }
        public bool FindNearestSpareSpace(bool inAir, Vec2 center, int radius, int fromDistance, out Vec2 pt)
        {
            var ox = 0;
            var oy = 0;
            var found = (inAir ? grids[1] : grids[0]).FindNearestSpareSpace((int)center.x, (int)center.y, radius, fromDistance, out ox, out oy);
            pt = new Vec2(ox, oy);
            return found;
        }

        #endregion
    }
}
