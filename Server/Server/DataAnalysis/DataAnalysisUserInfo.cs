using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace Server
{
    /// <summary>
    /// 数据分析统计User用
    /// </summary>
    public class DataAnalysisUserInfo : SerializableData
    {
        public string Id;
        public int Count;

        protected override void Sync()
        {
            BeginSync();

            SyncString(ref Id);
            SyncInt(ref Count);

            EndSync();
        }
    }
}
