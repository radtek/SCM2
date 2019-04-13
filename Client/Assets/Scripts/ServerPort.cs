using System;
using System.Collections;
using System.Collections.Generic;
using Swift;

/// <summary>
/// 服务器消息端口
/// </summary>
public class ServerPort : Port
{
    // 注册网络消息处理函数
    public void OnMessage(string op, Action<IReadableBuffer> cb)
    {
        OnMessage(op, (Connection conn, IReadableBuffer data) =>
        {
            cb(data);
        });
    }

    // 接受服务器消息请求
    public void OnRequest(string op, Action<IReadableBuffer, IWriteableBuffer> cb)
    {
        OnRequest(op, (Connection conn, IReadableBuffer data, IWriteableBuffer buff) =>
        {
            cb(data, buff);
        });
    }
}

/// <summary>
/// 扩展服务器连接方法，固定消息接受模块名称
/// </summary>
public static class ServerConnectionExt
{
    // 服务器端接收消息的模块
    public static string ServerMessageHandler = null;

    // 发送消息给服务器
    public static IWriteableBuffer Send2Srv(this Connection conn, string op)
    {
        var buff = conn.BeginSend(ServerMessageHandler);
        buff.Write(op);
        return buff;
    }

    // 发送请求给服务器
    public static IWriteableBuffer Request2Srv(this Connection conn, string op, Action<IReadableBuffer> cb, Action<bool> onExpired = null)
    {
        var buff = conn.BeginRequest(ServerMessageHandler, cb, onExpired);
        buff.Write(op);
        return buff;
    }
}
