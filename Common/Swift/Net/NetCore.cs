using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Swift
{
    /// <summary>
    /// 链接对象，用来直接发送消息
    /// </summary>
    public class Connection
    {
        public Connection(INetHelper h, NetConnection nc)
        {
            this.h = h;
            this.nc = nc;
        }

        public IWriteableBuffer BeginSend(string componentName)
        {
            return h.BeginSend(componentName);
        }

        public IWriteableBuffer BeginRequest(string componentName, Action<IReadableBuffer> callback, Action<bool> expiredProcess = null)
        {
            return h.BeginRequest(componentName, callback, expiredProcess);
        }

        public void End(IWriteableBuffer buff)
        {
            h.End(nc, buff);
        }

        public bool IsConnected
        {
            get
            {
                return nc.IsConnected;
            }
        }

        public string GetIP()
        {
            return nc.RemoteAddress;
        }

        public void Close()
        {
            Close("close manually");
        }

        public void Close(string reason)
        {
            nc.Close(reason);
        }

        // 通信加密密钥
        public byte[] EncryptCode
        {
            get
            {
                return nc.EncryptCode;
            }
            set
            {
                nc.EncryptCode = value;
            }
        }

        // 通信解密密钥
        public byte[] DecryptCode
        {
            get
            {
                return nc.DecryptCode;
            }
            set
            {
                nc.DecryptCode = value;
            }
        }

        // 加密一个字节
        public byte Encrypt(byte data)
        {
            return nc.Encrypt(data);
        }

        // 解密一个字节
        public byte Decrypt(byte data)
        {
            return nc.Decrypt(data);
        }

        public object Tag;

        #region 保护部分

        public NetConnection nc = null;

        public INetHelper h = null;

        #endregion
    }

    /// <summary>
    /// 应答对象，用来产生应答消息
    /// </summary>
    public class Responser
    {
        public Responser(Connection conn)
        {
            this.conn = conn;
            responseID = conn.h.ResponseID;
        }

        public IWriteableBuffer BeginResponse()
        {
            return conn.h.BeginResponse(responseID);
        }

        public void End(IWriteableBuffer buff)
        {
            conn.End(buff);
        }

        #region 保护部分

        Connection conn = null;

        long responseID = 0;

        #endregion
    }

    /// <summary>
    /// 提供网络消息发送的辅助接口
    /// </summary>
    public interface INetHelper
    {
        IWriteableBuffer BeginSend(string componentName);
        IWriteableBuffer BeginRequest(string componentName, Action<IReadableBuffer> callback, Action<bool> expiredProcess);
        IWriteableBuffer BeginResponse(long responseID);
        long ResponseID
        {
            get;
        }
        void End(NetConnection nc, IWriteableBuffer buff);
    }

	class CallbackNode
	{
		public long time;
		public Action<IReadableBuffer> cb;
		public NetConnection nc;
		public bool expired = false;
	}

    /// <summary>
    /// 网络核心组件，提供网络收发及网络消息分发功能
    /// </summary>
    public class NetCore : Component, IFrameDrived
    {
        static Queue<WriteBuffer> WriteBufferPool = new Queue<WriteBuffer>();

        static WriteBuffer AllocWriteBuffer()
        {
            WriteBuffer buff = null;
            if (WriteBufferPool.Count > 0)
            {
                buff = WriteBufferPool.Dequeue() as WriteBuffer;
                buff.Clear();
            }
            else
                buff = new WriteBuffer(true);

            return buff;
        }

        static void Dealloc(WriteBuffer buff)
        {
            if (buff != null)
                WriteBufferPool.Enqueue(buff);
        }

        #region 辅助类型定义，实现 IMessageHelper 接口

        /// <summary>
        /// 提供网络消息发送的辅助对象
        /// </summary>
        class NetHelper4NetCore : INetHelper
        {
            // 在指定 id 的连接上开始发送消息
            public IWriteableBuffer BeginSend(string componentName)
            {
                var buff = AllocWriteBuffer();
                buff.IsUsing = true;

                buff.Reserve(sizeof(bool));
                buff.Reserve(sizeof(int));
                // long no = p();
                // buff.Write(no);
                buff.Write(false);
                buff.Write(componentName);

                return buff;
            }

            // 在指定 id 的连接上发送请求
            public IWriteableBuffer BeginRequest(string componentName, Action<IReadableBuffer> cb, Action<bool> expiredProcess)
            {
                var buff = AllocWriteBuffer();
                buff.IsUsing = true;

                buff.Reserve(sizeof(bool));
                buff.Reserve(sizeof(int));
                long no = p();
                buff.Write(true);
                buff.Write(no);
                buff.Write(componentName);

                // 记录回调处理句柄
                CallbackNode cbn = new CallbackNode();
                cbn.time = TimeUtils.NowSecond;
                cbn.cb = cb;
                callbacks[-no] = cbn;

                if (expiredProcess != null)
                    usrDefinedExpireProcess[-no] = expiredProcess;

                return buff;
            }

            // 应答号
            public long ResponseID
            {
                get
                {
                    return responseID;
                }
            }

            // 创建当前消息的应答
            public IWriteableBuffer BeginResponse(long responseID)
            {
                var buff = AllocWriteBuffer();
                buff.IsUsing = true;

                buff.Reserve(sizeof(bool));
                buff.Reserve(sizeof(int));
                buff.Write(true);
                buff.Write(responseID);

                return buff;
            }

            static byte[] TrueBytes = null;
            static byte[] FalseByte = null;
            static NetHelper4NetCore()
            {
                TrueBytes = BitConverter.GetBytes(true);
                FalseByte = BitConverter.GetBytes(false);
            }

            // 消息请求结束或发送
            public void End(NetConnection nc, IWriteableBuffer buff)
            {
                WriteBuffer wb = (buff as WriteBuffer);

                // 如果没有被使用说明这个buff已经被还回
                // 说明有什么地方调用了多次end
                if (!wb.IsUsing)
                    throw new Exception("call end more than once");

                // 检查是否和上一条消息完全一致
                var identifyWithLastOne = false;
                var rawData = wb.Data;
                var rawDataLen = wb.Available;
                if (nc.LastSent != null && nc.LastSent.Available == rawDataLen)
                {
                    identifyWithLastOne = true;
                    for (var i = 0; i < rawDataLen; i++)
                    {
                        if (nc.LastSent.Data[i] != rawData[i])
                        {
                            identifyWithLastOne = false;
                            break;
                        }
                    }
                }

                if (identifyWithLastOne)
                    nc.SendData(TrueBytes, 0, TrueBytes.Length);
                else
                {
                    wb.Unreserve(0, FalseByte);
                    int len = buff.Available - sizeof(bool) - sizeof(int);
                    wb.Unreserve(sizeof(bool), BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len)));

                    // if it's a request
                    long no = -wb.PeekLong(FalseByte.Length + sizeof(int));
                    CallbackNode cbn;
                    if (callbacks.TryGetValue(no, out cbn))
                        cbn.nc = nc;

                    // 长度信息不加密，余下加密
                    for (int i = sizeof(int); i < wb.Available; i++)
                        wb.Data[i] = nc.Encrypt(wb.Data[i]);

                    nc.SendData(wb.Data, 0, wb.Available);

                    Dealloc(nc.LastSent);

                    nc.LastSent = wb;
                }
                
                wb.IsUsing = false;
            }

            public delegate long SequenceNumberProvider();
            public SequenceNumberProvider p = null;
            public long responseID = 0;

			public Dictionary<long, CallbackNode> callbacks = null;
            public Dictionary<long, Action<bool>> usrDefinedExpireProcess = null;
            public Peer peer = null;
        };

        #endregion

        // 默认 request 请求超时时间（秒）
        public long RequestExpireTime = 5;

        // request 超时回调
        public Action<bool> OnRequestExpired = null;

		// 收到网络消息事件，表示网络是畅通的
		public Action OnMessageRecieved = null;

        // 连接断开事件
        public event Action<Connection, string> OnDisconnected = null;

        // 连接断开事件
        public event Action<string> OnNetDisconnected = null;

        // 初始化
        public override void Init()
        {
            helper.callbacks = callbacks;
            helper.usrDefinedExpireProcess = usrDefinedExpireProcess;
            helper.p = GenNextSeqNo;
            helper.peer = p;
            p.OnDisconnected += (NetConnection nc, string reason) =>
            {
                if (OnDisconnected != null)
                    OnDisconnected(GetConnectionWrap(nc), reason);

                if (OnNetDisconnected != null)
                    OnNetDisconnected(reason);

                netConn2Conn.Remove(nc);
            };
        }

        // 获取网络消息助手
        public INetHelper NetHelper
        {
            get
            {
                return helper;
            }
        }

        // 启动网络监听
        public void StartListening(string localAddr, int port)
        {
            p.StartListen(localAddr, port);
        }

        // 同步连接到指定节点，返回节点 ID
        public Connection Connect2Peer(string addr, int port)
        {
            return GetConnectionWrap(p.Connect2Peer(addr, port));
        }

        // 异步连接到指定节点，回调给出 connection，若 connection 为空，则表示连接失败
        public void Connect2Peer(string addr, int port, Action<Connection, string> callback, AddressFamily addrFamily = AddressFamily.InterNetwork)
        {
            p.Connect2Peer(addr, port, (NetConnection nc, string msg) =>
            {
				if (callback == null)
                    return;

                callback(nc == null ? null : GetConnectionWrap(nc), msg);
            }, addrFamily);
		}
		public void Connect2Peer(string addr, int port, AddressFamily addrFamily, Action<Connection, string> callback)
		{
			p.Connect2Peer(addr, port, addrFamily, (NetConnection nc, string msg) =>
			{
				if (callback == null)
					return;
				
				callback(nc == null ? null : GetConnectionWrap(nc), msg);
			});
		}

        // 不允许向 Net 组件发送网络消息
        public void OnMessage(string id, IReadableBuffer data, INetHelper s)
        {
            throw new Exception("Any message to Net component is invalid");
        }

        // 停止组件功能
        public override void Close()
        {
            // 停止所有网络监听，并断开所有连接
            p.Stop();

            // 重置运行时数据
            foreach (NetConnection conn in netConn2Conn.Keys)
            {
                if (conn != null && conn.IsConnected)
                    conn.Close("NetCore closing");
            }

			// 关闭网络模块前，先通知所有请求超时
			foreach (long no in usrDefinedExpireProcess.Keys)
			{
				NetConnection nc = null;
				if (callbacks.ContainsKey(no))
					nc = callbacks[no].nc;

				Action<bool> ep = usrDefinedExpireProcess[no];
				if (ep != null)
					ep(nc == null ? false : nc.IsConnected);
			}

			usrDefinedExpireProcess.Clear();
			callbacks.Clear();
            netConn2Conn.Clear();
			checkTimeoutLst.Clear();
        }

        // 时间驱动处理所有已经接收到的网络消息，并分发到其它各个组件对象
        public void OnTimeElapsed(int te)
        {
            // 每帧发送一次网络数据
            p.ProcessDoSend();

            // 处理所有未完成的连接操作
            p.ProcessPendingConnecting();

            // 处理所有网络消息
            NetConnection[] connections = p.AllConnections;
            foreach (NetConnection nc in connections)
            {
                IReadableBuffer data = nc.ReceivedData;

                // 处理已经完整接收的部分
                while (true)
                {
                    // 判断是否接收到重复上一条消息的指示头
                    if (data.Available < sizeof(bool))
                        break;

                    var repeatLastOne = false;
                    if (!data.PeekBool(ref repeatLastOne))
                        break;

                    Connection connWrap = GetConnectionWrap(nc);

                    if (repeatLastOne)
                    {
                        data.Skip(sizeof(bool));
                        var componentName = nc.LastData2Component;
                        if (componentName == null)
                        {
                            LogWarning("componentName repeated is null");
                            LogWarning("connected: " + nc.IsConnected + " : " + ((IPEndPoint)nc.Socket.RemoteEndPoint).Address.ToString());
                            nc.Close("componentName repeated is null");
                            break;
                        }

                        var msgBody = new RingBuffer(true, true);
                        NetComponent c = GetCom(componentName) as NetComponent;
                        msgBody.Write(nc.LastGetData, 0, nc.LastGetData.Length);
                        c.OnMessage(connWrap, msgBody);
                    }
                    else
                    {
                        // 判断消息是否已经完整接收
                        if (data.Available < sizeof(bool) + sizeof(int))
                            break;

                        int len = 0;
                        if (!data.PeekInt(sizeof(bool), ref len))
                            break;

                        if (len < 0 || len > NetConnection.MaxRecieveBufferSize)
                        {
                            string ex = "invalid message length: " + len;
                            nc.Close(ex);
                            throw new Exception(ex);
                            // break;
                        }

                        if (data.Available < len + sizeof(bool) + sizeof(int))
                            break;

                        data.Skip(sizeof(bool));
                        data.Skip(sizeof(int));

                        // 对该条消息解密
                        data.TravelReplaceBytes4Read(0, len, (byte d) => { return connWrap.Decrypt(d); });

                        if (OnMessageRecieved != null)
                            OnMessageRecieved();

                        // 消息序号，正数为普通消息，负数为应答消息
                        bool hasNo = data.ReadBool();
                        long no = hasNo ? data.ReadLong() : 0;
                        int offset = hasNo ? sizeof(long) + sizeof(bool) : sizeof(bool);

                        if (no >= 0)
                        {
                            helper.responseID = -no;
                            string componentName = data.ReadString();
                            if (componentName == null)
                                throw new Exception("componentName try to get is null");
                            NetComponent c = GetCom(componentName) as NetComponent;
                            byte[] msgData = data.ReadBytes(len - offset - sizeof(int) - componentName.Length);

                            nc.LastGetData = msgData;
                            nc.LastData2Component = componentName;

                            // 没找到对应的模块则忽略
                            if (c == null)
                            {
                                string ex = "no such a component named: " + componentName;
                                nc.Close(ex);
                                throw new Exception(ex);
                                // break;
                            }

                            // 投递消息
                            var msgBody = new RingBuffer(true, true);
                            msgBody.Write(msgData, 0, msgData.Length);
                            c.OnMessage(connWrap, msgBody);
                        }
                        else
                        {
                            byte[] msgData = data.ReadBytes(len - offset);
                            var msgBody = new RingBuffer(true, true);
                            msgBody.Write(msgData, 0, msgData.Length);

                            // 投递消息
                            CallbackNode tcb;
                            if (callbacks.TryGetValue(no, out tcb))
                            {
                                Action<IReadableBuffer> cb = tcb.cb;
                                callbacks.Remove(no);
                                usrDefinedExpireProcess.Remove(no);

                                if (cb != null)
                                    cb(msgBody);
                            }
                            else
                            {
                                string ex = "no request callback for " + no;
                                nc.Close(ex);
                                throw new Exception(ex);
                                // break;
                            }
                        }
                    }
                }
            }

			// 检查请求超时
			CheckNextCallbackTimeout();
        }

        #region 保护部分

        // 获取对 NetConnection 的包装
        Connection GetConnectionWrap(NetConnection nc)
        {
            if (!netConn2Conn.ContainsKey(nc))
                netConn2Conn[nc] = new Connection(helper, nc);

            return netConn2Conn[nc];
        }

        // 网络消息发送序号
        long seqNo = 1;

        // 网络节点，负责网络连接及收发数据
        Peer p = new Peer();

        // 缓存 Connection 对 NetConnection 的包装
        Dictionary<NetConnection, Connection> netConn2Conn = new Dictionary<NetConnection, Connection>();

        // 回调映射表
		Dictionary<long, CallbackNode> callbacks = new Dictionary<long, CallbackNode>();
        Dictionary<long, Action<bool>> usrDefinedExpireProcess = new Dictionary<long, Action<bool>>();
        NetHelper4NetCore helper = new NetHelper4NetCore();
        long GenNextSeqNo()
        {
            return seqNo++;
        }

        // 检查超时的回调等待
		List<KeyValuePair<long, CallbackNode>> checkTimeoutLst = new List<KeyValuePair<long, CallbackNode>>();
        void CheckNextCallbackTimeout()
        {
            if (checkTimeoutLst.Count == 0)
			{
				if (callbacks.Count > 0)
                	checkTimeoutLst.AddRange(callbacks.ToArray());

				return;
			}

            long checkPoint = TimeUtils.NowSecond - RequestExpireTime;
            KeyValuePair<long, CallbackNode> kv = checkTimeoutLst[0];
            checkTimeoutLst.RemoveAt(0);
            long no = kv.Key;
			CallbackNode cb = kv.Value;
			long requestTime = cb.time;
			NetConnection nc = cb.nc;
			if (!cb.expired && requestTime < checkPoint)
            {
                Action<bool> p = null;
                if (usrDefinedExpireProcess.TryGetValue(no, out p))
                {
                    usrDefinedExpireProcess.Remove(no);
                    p.SC(nc == null ? false : nc.IsConnected);
                }
                else if (OnRequestExpired != null)
                    OnRequestExpired(nc.IsConnected);

				// 每个消息只通知一次超时事件
				cb.expired = true;
            }
        }

        #endregion
    }
}
