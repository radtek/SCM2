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
    /// 电脑对手 AI
    /// </summary>
    public abstract class AIComputerOpponent
    {
        // 所属房间
        public Room Room { get; private set; }

        // 对应 player
        public int Player { get; private set; }

        // 外部实现寻路功能
        public Action<Fix64, Vec2, Vec2, Action<Vec2[]>> FindPath = null;

        // 内部状态机
        protected StateMachine sm = null;

        public string Name { get { return sm.Name; } }

        public AIComputerOpponent(string name, Room room, int player)
        {
            Room = room;
            Player = player;
            sm = new StateMachine(name);
        }

        // 初始化 AI 状态机
        public abstract void Init();

        public void Start()
        {
            sm.Start();
        }

        public void Destroy()
        {
            sm.Destroy();
        }

        // 推动 AI
        public void OnTimeElapsed(int te)
        {
            var timeElapsed = (Fix64)te / 1000;
            sm.Run(timeElapsed);
        }

        #region GetUnit 一类

        protected Unit GetMyUnit(string type)
        {
            var us = GetMyUnits(type);
            return us == null || us.Length == 0 ? null : us[0];
        }

        protected Unit[] GetMyUnits(string type)
        {
            return Room.GetUnitsByType(type, Player);
        }

        protected Unit GetMyUnit(Func<Unit, bool> filter = null)
        {
            var us = GetMyUnits(filter);
            return us.Length == 0 || us.Length == 0 ? null : us[0];
        }

        protected Unit[] GetMyUnits(Func<Unit, bool> filter = null)
        {
            return Room.GetAllUnitsByPlayer(Player, filter);
        }

        protected Unit GetOpponentUnit(string type)
        {
            var us = GetOpponentUnits(type);
            return us == null || us.Length == 0 ? null : us[0];
        }

        protected Unit[] GetOpponentUnits(string type)
        {
            var oppoentPlayer = Player == 1 ? 2 : 1;
            return Room.GetUnitsByType(type, oppoentPlayer);
        }

        protected Unit GetOpponentUnit(Func<Unit, bool> filter = null)
        {
            var us = GetOpponentUnits(filter);
            return us.Length == 0 || us.Length == 0 ? null : us[0];
        }

        protected Unit[] GetOpponentUnits(Func<Unit, bool> filter = null)
        {
            var oppoentPlayer = Player == 1 ? 2 : 1;
            return Room.GetAllUnitsByPlayer(oppoentPlayer, filter);
        }

        #endregion
    }
}
