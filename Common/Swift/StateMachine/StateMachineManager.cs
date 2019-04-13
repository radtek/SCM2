using System;
using System.Collections.Generic;
using System.Linq;
using Swift;
using Swift.Math;

namespace Swift
{
    /// <summary>
    /// 状态机管理器，驱动管理所有状态机
    /// </summary>
    public class StateMachineManager : Component, IFrameDrived
    {
        // 所有状态机
        StableDictionary<string, StateMachine> sms = new StableDictionary<string, StateMachine>();

        public void OnTimeElapsed(int te)
        {
            var te64 = (Fix64)(te / 1000.0f);
            foreach (var sm in sms.ValueArray)
                sm.Run(te64);
        }

        // 获取已有状态机
        public StateMachine Get(string name)
        {
            return sms.ContainsKey(name) ? sms[name] : null;
        }

        // 创建新的状态机
        public void Add(StateMachine sm)
        {
            if (sms.ContainsKey(sm.Name))
                throw new Exception("state machine name conflict: " + name);

            sms[sm.Name] = sm;
            sm.Start();
        }

        // 删除状态机
        public void Del(string name)
        {
            if (!sms.ContainsKey(name))
                return;

            var sm = sms[name];
            sms.Remove(name);
            sm.Destroy();
        }

        // 移除所有状态机
        public void Clear()
        {
            foreach (var smName in sms.KeyArray)
                Del(smName);
        }
    }
}
