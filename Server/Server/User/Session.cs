using System.Collections;
using System.Collections.Generic;
using Swift;

namespace Server
{
    /// <summary>
    /// 在线用户会话
    /// </summary>
    public class Session
    {
        // 用户连接
        public Connection Conn { get; set; }

        // 用户 ID
        public string ID { get { return Usr == null ? null : Usr.ID; } }

        // 用户对象
        public User Usr { get; set; }
    }
}