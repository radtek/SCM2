using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    class QuestionnaireResultContainer : DataContainer<QuestionnaireResult, string>
    {
        public QuestionnaireResultContainer(MySqlDbPersistence<QuestionnaireResult, string> persistence)
            : base(persistence)
        {
        }
    }
}