using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Swift;
using Swift.Math;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.Text;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using SCM;

namespace Server
{
    class QuestionnaireResultMgr : Component
    {
        UserPort UP;
        QuestionnaireResultContainer QRC;

        public override void Init()
        {
            UP = GetCom<UserPort>();
            QRC = GetCom<QuestionnaireResultContainer>();

            UP.OnMessage("SubmitQuestionnaireResult", OnSubmitQuestionnaireResult);
        }

        static StableDictionary<string, QuestionnaireResult> qrs = new StableDictionary<string, QuestionnaireResult>();
        public static StableDictionary<string, QuestionnaireResult> QuestionnaireResults
        {
            get
            {
                return qrs;
            }
        }

        void OnSubmitQuestionnaireResult(Session s, IReadableBuffer data)
        {
            var qr = new QuestionnaireResult();
            qr.Info.Usr = s.Usr.ID;

            qr.Info.Id = data.ReadString();
            while (data.Available > 0)
            {
                qr.Info.Answers.Add(data.ReadString());
            }

            qr.ID = qr.Info.Id + qr.Info.Usr;

            QRC.Retrieve(qr.ID, (questionnaire) =>
            {
                if (questionnaire != null)
                    return;

                QRC.AddNew(qr);
            });
        }
    }
}
