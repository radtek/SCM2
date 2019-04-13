using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 问卷对象
    /// </summary>
    public class Questionnaire
    {
        // 问卷信息
        public QuestionnaireInfo Info = new QuestionnaireInfo();

        // 序列化问卷信息
        public void Serialize(IWriteableBuffer buff)
        {
            buff.Write(Info.Id);

            for (int i = 0; i < Info.Questions.KeyArray.Length; i++)
            {
                buff.Write(Info.Questions.KeyArray[i]);

                var acnt = Info.Questions[Info.Questions.KeyArray[i]].Count;

                buff.Write(acnt);

                if (acnt != 0)
                {
                    for (int j = 0; j < acnt; j++)
                    {
                        buff.Write(Info.Questions[Info.Questions.KeyArray[i]][j]);
                    }
                }
            }
        }
    }
}
