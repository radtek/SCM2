using System;
using System.Collections.Generic;
using Swift;

namespace Server
{
    /// <summary>
    /// 用户消息端口
    /// </summary>
    public class UserPort : Port
    {
        SessionContainer SC;

        public override void Init()
        {
            SC = GetCom<SessionContainer>();
        }

        // 注册无需应答的网络消息处理函数
        public void OnMessage(string op, Action<Session, IReadableBuffer> callback)
        {
            OnMessage(op, (Connection conn, IReadableBuffer data) =>
            {
                var s = SC.GetByConn(conn);
                if (s == null)
                    return;

                callback(s, data);
            });
        }

        // 注册需要应答的网络消息处理函数，这一类操作是需要异步完成的
        public void OnRequest(string op, Action<Session, IReadableBuffer, IWriteableBuffer, Action> callback)
        {
            OnRequest(op, (Connection conn, IReadableBuffer data, IWriteableBuffer buff, Action end) =>
            {
                var s = SC.GetByConn(conn);
                if (s == null)
                    return;

                callback(s, data, buff, end);
            });
        }

        // 注册需要应答的网络消息处理函数，这一类操作是同步完成的
        public void OnRequest(string op, Action<Session, IReadableBuffer, IWriteableBuffer> callback)
        {
            OnRequest(op, (Connection conn, IReadableBuffer data, IWriteableBuffer buff) =>
            {
                var s = SC.GetByConn(conn);
                if (s == null)
                    return;

                callback(s, data, buff);
            });
        }
    }

    /// <summary>
    /// 扩展用户连接方法，固定消息接受模块名称
    /// </summary>
    public static class UserConnectionExt
    {
        // 客户端接收消息的模块
        public static string ClientMessageHandler = null;

        // 发送消息给用户
        public static void Send2Usr(this Connection conn, string op, Action<IWriteableBuffer> fun = null)
        {
            var buff = conn.BeginSend(ClientMessageHandler);
            buff.Write(op);
            fun.SC(buff);
            conn.End(buff);
        }
    }
}
