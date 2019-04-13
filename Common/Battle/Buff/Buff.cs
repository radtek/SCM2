using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    // 局内 buff
    public class Buff
    {
        public string Type;

        // 时间流逝
        public Action<Fix64> OnTimeElapsed = null;

        // 作用于一个单位上
        public Action<Unit> OnUnit = null;

        // 取消对一个单位的作用
        public Action<Unit> OffUnit = null;
    }
}
