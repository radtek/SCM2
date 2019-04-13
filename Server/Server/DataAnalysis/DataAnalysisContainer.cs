using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    class DataAnalysisContainer : DataContainer<DataAnalysis, string>
    {
        public DataAnalysisContainer(MySqlDbPersistence<DataAnalysis, string> persistence)
            : base(persistence)
        {
        }

        public MySqlDbPersistence<DataAnalysis, string> P
        {
            get
            {
                return p as MySqlDbPersistence<DataAnalysis, string>;
            }
        }
    }
}