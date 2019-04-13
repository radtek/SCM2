using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    class UserContainer : DataContainer<User, string>
    {
        public UserContainer(MySqlDbPersistence<User, string> persistence) : base(persistence)
        {
        }
    }
}