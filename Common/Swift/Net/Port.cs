using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swift
{
    /// <summary>
    /// 接口类，用于通信过程中的消息收发
    /// </summary>
    public class Port : NetComponent
    {
        // 注册无需应答的网络消息处理函数
        public void OnMessage(string op, Action<Connection, IReadableBuffer> callback)
        {
            MakeSureNoDuplicate(op);
            noResponseMap[op] = callback;
        }

        // 注册需要应答的网络消息处理函数，这一类操作是需要异步完成的
        public void OnRequest(string op, Action<Connection, IReadableBuffer, IWriteableBuffer, Action> callback)
        {
            MakeSureNoDuplicate(op);
            responseMap[op] = callback;
        }

        // 注册需要应答的网络消息处理函数，这一类操作是同步完成的
        public void OnRequest(string op, Action<Connection, IReadableBuffer, IWriteableBuffer> callback)
        {
            MakeSureNoDuplicate(op);
            responseMapImmediately[op] = callback;
        }

        // 处理网络消息的映射
        public override void OnMessage(Connection conn, IReadableBuffer data)
        {
            string op = data.ReadString();
            if (noResponseMap.ContainsKey(op))
                noResponseMap[op](conn, data);
            else if (responseMap.ContainsKey(op))
            {
                Responser r = CreateResponser(conn);
                IWriteableBuffer buff = r.BeginResponse();
                responseMap[op](conn, data, buff, () => { r.End(buff); });
            }
            else if (responseMapImmediately.ContainsKey(op))
            {
                Responser r = CreateResponser(conn);
                IWriteableBuffer buff = r.BeginResponse();
                responseMapImmediately[op](conn, data, buff);
                r.End(buff);
            }
            else
            {
                conn.Close("unhandled message '" + op + "' in " + Name);
                throw new Exception("unhandled message '" + op + "' in " + Name);
            }
        }

        #region 保护部分

        // 检查重复注册的消息映射
        void MakeSureNoDuplicate(string op)
        {
            if (noResponseMap.ContainsKey(op)
                || responseMap.ContainsKey(op))
                throw new Exception("message: " + op + " has already register");
        }

        // 无需回复的消息映射表
        Dictionary<string, Action<Connection, IReadableBuffer>> noResponseMap = new Dictionary<string, Action<Connection, IReadableBuffer>>();

        // 需要回复的消息映射表
        Dictionary<string, Action<Connection, IReadableBuffer, IWriteableBuffer, Action>> responseMap = new Dictionary<string, Action<Connection, IReadableBuffer, IWriteableBuffer, Action>>();
        Dictionary<string, Action<Connection, IReadableBuffer, IWriteableBuffer>> responseMapImmediately = new Dictionary<string, Action<Connection, IReadableBuffer, IWriteableBuffer>>();

        #endregion
    }

    /// <summary>
    /// 接口代理类，用于通信过程中的消息收发
    /// </summary>
    public class PortAgent : NetComponentAgent
    {
        // 重制连接 id
        public void Reset(Connection conn)
        {
            Setup(conn, compName);
        }

		// 发送无回执消息
        public void Send(string op, Action<IWriteableBuffer> fun)
        {
            NetUtils.Send(op, compName, fun, conn);
        }

		HashSet<string> pending = null;

		// Request 一条消息，在服务器回复前，或超时前，如果再发这条消息，直接忽略。
		public void RequestNoRepeat(string op, Action<IWriteableBuffer> fun, Action<IReadableBuffer> callback)
		{
			if (pending == null) 
			{
				pending = new HashSet<string>();
			}

			if (pending.Contains(op))
			{
				return;
			}

			pending.Add(op);

			NetUtils.Request(op, compName, fun, 
			(r) => {
				// 这一句放前面，否则如果callback出异常就悲剧了
				pending.Remove(op);
				if (callback != null)
					callback(r);
            }, 
            (connected) => {
				pending.Remove(op);
			}, 
			conn);
		}

		// 发送请求并等待请求结果
        public void Request(string op, Action<IWriteableBuffer> fun, Action<IReadableBuffer> callback)
        {
            NetUtils.Request(op, compName, fun, callback, conn);
        }

		// 发送请求并等待请求结果
        public void Request(string op, Action<IWriteableBuffer> fun, Action<IReadableBuffer> callback, Action<bool> onExpired)
		{
			NetUtils.Request(op, compName, fun, callback, onExpired, conn);
		}
    }
}
