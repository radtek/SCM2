using System;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// 生成唯一 ID
    /// </summary>
    public class UniqueID
    {
        public static UniqueID Instance { get { return instance; } }
        static UniqueID instance = new UniqueID();

        // 序号
        long seqNo = 0;

        // 上一次的生成时间
        DateTime lastGenTime = DateTime.Now;

        // 生成一个全局唯一 ID
        public string GenID()
        {
            var now = DateTime.Now;
            if (now != lastGenTime)
            {
                lastGenTime = now;
                seqNo = 0;
            }
            else
                seqNo++;

            var prefix = now.Ticks.ToString();
            return prefix + "_" + seqNo;
        }
    }
}
