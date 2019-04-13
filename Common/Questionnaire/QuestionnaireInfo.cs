using System;
using System.Collections.Generic;
using Swift;
using Swift.Math;

namespace SCM
{
    /// <summary>
    /// 问卷信息
    /// </summary>
    public class QuestionnaireInfo : SerializableData
    {
        public string Id;
        public StableDictionary<string, List<string>> Questions = new StableDictionary<string, List<string>>();

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref Id);
            EndSync();
        }
    }
}
