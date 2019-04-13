using Swift.Math;
using System;
using System.Collections.Generic;

namespace Swift
{
    /// <summary>
    /// 状态迁移条件
    /// </summary>
    public class StateTransition
    {
        // 迁移的状态两端
        public string FromState { get; private set; }
        public string ToState { get; private set; }
        public Action<Fix64> RunTime { get; private set; }
        public Action Reset { get; private set; }

        // 迁移条件
        public Func<State, bool> TransitionCondition = null;

        public StateTransition From(string s)
        {
            FromState = s;
            return this;
        }

        public StateTransition To(string s)
        {
            ToState = s;
            return this;
        }

        public StateTransition When(Func<State, bool> condition)
        {
            TransitionCondition = condition;
            return this;
        }

        public StateTransition OnTimeElapsed(Action<Fix64> runTime)
        {
            RunTime = runTime;
            return this;
        }

        public StateTransition OnReset(Action reset)
        {
            Reset = reset;
            return this;
        }

        public StateTransition Clone()
        {
            var st = new StateTransition();
            st.TransitionCondition = TransitionCondition;
            st.FromState = FromState;
            st.ToState = ToState;
            st.RunTime = RunTime;
            st.Reset = Reset;

            return st;
        }
    }
}
