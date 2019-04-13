using Swift.Math;
using System;
using System.Collections.Generic;

namespace Swift
{
    /// <summary>
    /// 状态机
    /// </summary>
    public class StateMachine
    {
        public string Name { get; private set; }

        // 状态集
        StableDictionary<string, State> states = new StableDictionary<string, State>();
        List<StateTransition> allTrans = new List<StateTransition>();

        // 迁移集
        StableDictionary<string, List<StateTransition>> trans = new StableDictionary<string, List<StateTransition>>();

        // 初始状态
        string StartState { get; set; }

        // 当前状态
        public string CurrentState { get { return curState; } }

        public StateMachine(string name)
        {
            Name = name;
        }

        // 获取指定名称的状态机
        public State this[string stName]
        {
            get { return states[stName]; }
        }

        // 添加状态
        public State NewState(string stateName)
        {            
            var s = new State(stateName);
            states[s.Name] = s;

            return s;
        }

        // 设置默认状态
        public void SetDefaultState(string stateName)
        {
            states[stateName].AsDefault();
        }

        // 添加迁移条件
        public StateTransition Trans()
        {
            var t = new StateTransition();
            allTrans.Add(t);
            return t;
        }

        bool running = false;
        string curState = null;

        // 启动状态机，只能启动一次
        public virtual void Start()
        {
            Prepare();
            running = true;
        }

        // 销毁状态机，不能再次启动
        public virtual void Destroy()
        {
            running = false;
            allTrans.Clear();
            curState = null;
        }

        public virtual void Pause()
        {
            running = false;
        }

        public virtual void Resume()
        {
            running = true;
        }

        public virtual void Run(Fix64 te)
        {
            if (!running)
                return;

            if (curState == null)
            {
                curState = StartState;
                CurSt.RunIn.SC(null);
            }

            // 刚刚变换到当前状态，则等待下一帧经过迁移条件检查后在执行，因为有可能立刻被迁移到别的状态去了
            if (running && !CheckTransition(te))
                CurSt.DoRun.SC(CurSt, te);
        }

        public virtual StateMachine Clone(string newName)
        {
            var sm = new StateMachine(newName);
            CopyTo(sm);
            sm.states[sm.StartState].AsDefault();
            return sm;
        }

        public virtual void LogAllStates(Action<string> logger)
        {
            foreach (var s in states.Values)
                s.Log(logger);
        }

        public virtual void MergeWith(StateMachine sm)
        {
            sm.CopyTo(this);
        }

        // prepare all transitions
        void Prepare()
        {
            trans.Clear();

            // 形如 "a|b" 的 FromState 要拆分一下
            foreach (var st in allTrans.ToArray())
            {
                if (st.FromState.Contains("|"))
                {
                    var fs = st.FromState.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in fs)
                    {
                        var t = new StateTransition();
                        t.From(s).To(st.ToState).OnReset(st.Reset).When(st.TransitionCondition).OnTimeElapsed(st.RunTime);
                        allTrans.Add(t);
                    }

                    allTrans.Remove(st);
                }
            }

            foreach (var st in allTrans)
            {
                // 状态迁移的两端不能为空，且状态表中必须存在
                if (st.FromState == null || st.ToState == null
                    || !states.ContainsKey(st.ToState) || !states.ContainsKey(st.FromState))
                    throw new Exception("invalid state transition: " + (st.FromState == null ? "null" : st.FromState) + " => " + (st.ToState == null ? "null" : st.ToState));

                if (!trans.ContainsKey(st.FromState))
                    trans[st.FromState] = new List<StateTransition>();

                trans[st.FromState].Add(st);
            }

            foreach (var s in states.Values)
            {
                if (s.IsDefault)
                {
                    // 后面的替代前面的
                    if (StartState != null)
                        states[StartState].AsDefault(false);

                    StartState = s.Name;
                }
            }

            if (StartState == null)
                throw new Exception("StartState is null since it's not set or the StateMachine has been destroyed.");
        }

        // 当前状态
        State CurSt { get { return states[curState]; } }

        // 检查状态迁移
        bool CheckTransition(Fix64 te)
        {
            // 要生效的迁移条件
            StateTransition tToWork = null;

            if (!running)
                return false;

            // 当前状态下的迁移条件
            foreach (var t in trans[curState])
            {
                if (t.RunTime != null)
                    t.RunTime(te);

                if (!running) // 状态可能已经迁移了
                    break;

                if (t.TransitionCondition != null && t.TransitionCondition(CurSt))
                {
                    tToWork = t;
                    break;
                }
            }

            // 没有满足条件的就算了
            if (tToWork == null)
                return false;

            // 执行迁移操作

            if (CurSt.RunOut != null)
                CurSt.RunOut(tToWork.ToState);

            if (!running) // 状态可能已经迁移了
                return true;

            var fromState = curState;
            curState = tToWork.ToState;

            if (CurSt.RunIn != null)
                CurSt.RunIn(fromState);

            if (!running) // 状态可能已经迁移了
                return true;

            // 新状态下的所有迁移条件重置一次
            if (trans.ContainsKey(curState))
            {
                foreach (var nt in trans[curState])
                {
                    if (nt.Reset != null)
                        nt.Reset();
                }
            }

            return true;
        }

        void CopyTo(StateMachine sm)
        {
            foreach (var s in states.Keys)
            {
                if (sm.states.ContainsKey(s))
                    throw new Exception("state " + s + " already exists");

                sm.states[s] = states[s].Clone();
                sm.states[s].IsDefault = false;
            }

            foreach (var st in allTrans)
            {
                var newSt = st.Clone();
                if (!trans.ContainsKey(newSt.FromState))
                    trans[newSt.FromState] = new List<StateTransition>();

                trans[newSt.FromState].Add(newSt);
                sm.allTrans.Add(newSt);
            }
        }
    }
}
