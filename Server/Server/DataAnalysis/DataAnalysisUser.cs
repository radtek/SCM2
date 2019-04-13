using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 数据对象
    /// </summary>
    public class DataAnalysisUser : DataItem<string>
    {
        // 数据信息
        public DataAnalysisUserInfo Info = new DataAnalysisUserInfo();

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref ID);
            SyncObj(ref Info);
            EndSync();
        }
    }
}
