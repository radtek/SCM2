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
    public class QuestionnaireMgr : Component
    {
        UserPort UP;
        QuestionnaireResultContainer QRC;

        public override void Init()
        {
            UP = GetCom<UserPort>();
            QRC = GetCom<QuestionnaireResultContainer>();

            //LoadAllQuestionnaire();

            UP.OnRequest("GetQuestionnaire", OnGetQuestionnaire);
        }

        // 加载所有的问卷
        public void LoadAllQuestionnaire()
        {
            var questionnairesTest = new Questionnaire();

            var subject = "你喜欢玩游戏吗?";
            var answers = new List<string>();
            answers.Add("是");
            answers.Add("否");

            var subject1 = "你喜欢打篮球吗?";
            var answers1 = new List<string>();
            answers1.Add("是");
            answers1.Add("否");

            var subject2 = "建议?";
            var answers2 = new List<string>();

            questionnairesTest.Info.Id = "Test01";
            questionnairesTest.Info.Questions[subject] = answers;
            questionnairesTest.Info.Questions[subject1] = answers1;
            questionnairesTest.Info.Questions[subject2] = answers2;

            questionnaires[questionnairesTest.Info.Id] = questionnairesTest;
        }



        // 所有问卷
        static StableDictionary<string, Questionnaire> questionnaires = new StableDictionary<string, Questionnaire>();
        public static StableDictionary<string, Questionnaire> Questionnaires
        {
            get
            {
                return questionnaires;
            }
            set
            {
                questionnaires = value;
            }
        }

        // 所有问卷标题
        public static string[] AllQuestionnaireTitles { get { return questionnaires.KeyArray; } }

        // 获取指定问卷
        public static Questionnaire GetQuestionnaire(string q)
        {
            return questionnaires.ContainsKey(q) ? questionnaires[q] : null;
        }

        // 客户端请求问卷
        public void OnGetQuestionnaire(Session s, IReadableBuffer data, IWriteableBuffer buff, Action end)
        {
            var QuestionnaireName = data.ReadString();

            QRC.Retrieve(QuestionnaireName + s.Usr.ID, (questionnaire) =>
            {
                var isNew = questionnaire == null;

                buff.Write(isNew);

                if (isNew)
                {
                    var q = GetQuestionnaire(QuestionnaireName);

                    var isExists = q != null;

                    buff.Write(isExists);

                    if (isExists)
                    {
                        q.Serialize(buff);
                    }
                }

                end();
            });
        }
    }
}

