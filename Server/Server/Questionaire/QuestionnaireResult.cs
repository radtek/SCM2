using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 数据对象
    /// </summary>
    public class QuestionnaireResult : DataItem<string>
    {
        // 数据信息
        public QuestionnaireResultInfo Info = new QuestionnaireResultInfo();

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref ID);
            SyncObj(ref Info);
            EndSync();
        }
    }
}
