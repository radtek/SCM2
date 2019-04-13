using System;
using System.Collections.Generic;
using Swift;
using SCM;

namespace Server
{
    /// <summary>
    /// 用户对象
    /// </summary>
    public class User : DataItem<string>
    {
        // 用户密码
        public string Pwd;

        // 用户信息
        public UserInfo Info = new UserInfo();

        protected override void Sync()
        {
            BeginSync();
            SyncString(ref ID);
            SyncString(ref Pwd);
            SyncObj(ref Info);
            EndSync();
        }
    }
}
