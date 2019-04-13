using Swift.Math;
using System;
using System.Collections.Generic;

namespace Swift
{
    /// <summary>
    /// 多个状态机的组合
    /// </summary>
    public class StateMachineCombination : StateMachine
    {
        StateMachine[] sms = null;
        public StateMachineCombination(string name, params StateMachine[] subSms)
            : base(name)
        {
            sms = subSms;
        }

        // 默认的状态机组合，自身不做任何事情，只是子状态机在起作用
        public StateMachineCombination MakeSelfDumb()
        {
            NewState("dumb").AsDefault().Run(null);
            Trans().From("dumb").To("dumb").When(null);
            return this;
        }
        
        // 启动状态机，只能启动一次
        public override void Start()
        {
            base.Start();
            sms.Travel((sm) => { sm.Start(); });
        }

        // 销毁状态机，不能再次启动
        public override void Destroy()
        {
            sms.Travel((sm) => { sm.Destroy(); });
            base.Destroy();
        }

        public override void Pause()
        {
            sms.Travel((sm) => { sm.Pause(); });
            base.Pause();
        }

        public override void Resume()
        {
            sms.Travel((sm) => { sm.Resume(); });
            base.Resume();
        }

        public override void Run(Fix64 te)
        {
            sms.Travel((sm) => { sm.Run(te); });
            base.Run(te);
        }

        public override void LogAllStates(Action<string> logger)
        {
            base.LogAllStates(logger);
            sms.Travel((sm) => { sm.LogAllStates(logger); });
        }

        public override StateMachine Clone(string newName)
        {
            throw new Exception("StateMachineCombination does not support this feature");
        }

        public override void MergeWith(StateMachine sm)
        {
            throw new Exception("StateMachineCombination does not support this feature");
        }
    }
}
