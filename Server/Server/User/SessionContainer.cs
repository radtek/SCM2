using System;
using System.Collections;
using System.Collections.Generic;
using Swift;

namespace Server
{
    /// <summary>
    /// 所有在线用户的会话容器
    /// </summary>
    public class SessionContainer : Component
    {
        Dictionary<string, Session> ss = new Dictionary<string, Session>();

        // 按 ID 索引所有 Session
        public Session this[string id]
        {
            get
            {
                return ss.ContainsKey(id) ? ss[id] : null;
            }

            set
            {
                if (ss.ContainsKey(id) && value != null)
                    throw new Exception("session id conflict: " + id);

                ss[id] = value;
            }
        }

        // 移除指定会话
        public Session Remove(string id)
        {
            var s = this[id];
            if (s != null)
                ss.Remove(s.ID);

            return s;
        }

        // 当前获取会话数
        public int Count
        {
            get
            {
                return ss.Count;
            }
        }

        // 按连接获取 Session
        public Session GetByConn(Connection conn)
        {
            if (conn == null)
                return null;

            foreach (var s in ss.Values)
            {
                if (s != null && s.Conn == conn)
                    return s;
            }

            return null;
        }

        // 按名称获取
        public Session[] GetByName(string name)
        {
            var lst = new List<Session>();
            foreach (var s in ss.Values)
            {
                if (s != null && s.Usr != null && s.Usr.Info.Name == name)
                    lst.Add(s);
            }

            return lst.ToArray();
        }
    }
}
