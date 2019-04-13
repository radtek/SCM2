using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 问卷调查反馈信息
    /// </summary>
    public class QuestionnaireResultInfo : SerializableData
    {
        public string Id;
        public string Usr;

        public List<string> Answers = new List<string>();

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref Id);
            SyncString(ref Usr);
            SyncListString(ref Answers);
            EndSync();
        }
    }
}
