using Swift;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SCM;

/// <summary>
/// 战斗录像和回放工具
/// </summary>
public class BattleReplayer : Component, IFrameDrived
{
    // 消息记录
    List<KeyValuePair<string, IReadableBuffer>> msgHistroy = new List<KeyValuePair<string, IReadableBuffer>>();

    // 消息映射表
    Dictionary<string, Action<IReadableBuffer>> msgHandler = new Dictionary<string, Action<IReadableBuffer>>();

    public void Clear()
    {
        msgHistroy.Clear();
    }

    public void ReadFromBuffer(IReadableBuffer data)
    {
        var r = BattleReplay.Deserialize(data);
        msgHistroy = r.Msgs;
    }

    // 开始录像回放
    public void Start()
    {
        replayMsgIndex = 0;
        started = true;
    }

    // 录入一条消息
    public void Record(string op, IReadableBuffer data)
    {
        msgHistroy.Add(new KeyValuePair<string, IReadableBuffer>(op, (data as RingBuffer).Clone() as IReadableBuffer));
    }

    // 建立消息映射表
    public void OnMessage(string op, Action<IReadableBuffer> cb)
    {
        msgHandler[op] = cb;
    }

    bool started = false;
    int te = 0;
    int fact = 1;
    int replayMsgIndex = 0;

    public int SpeedUpFactor { set { if (fact >= 0) fact = value; } }

    // 是否正在回放录像
    public bool InReplaying { get { return started; } }

    // 当前录像播放进度
    public float Prograss
    {
        get
        {
            if (replayMsgIndex == msgHistroy.Count)
                return 1;
            else
                return (float)replayMsgIndex / msgHistroy.Count;
        }
    }

    public void Stop()
    {
        replayMsgIndex = msgHistroy.Count - 1;
    }

    public void OnTimeElapsed(int timeElapsed)
    {
        if (!started)
            return;

        te += timeElapsed;
        te *= fact;

        while (te >= Room.FrameInterval && started)
        {
            te -= Room.FrameInterval;
            var msg = msgHistroy[replayMsgIndex++];
            var op = msg.Key;
            var data = msg.Value;
            msgHandler[op]((data as RingBuffer).Clone() as IReadableBuffer);

            if (replayMsgIndex >= msgHistroy.Count)
                started = false;
        }
    }
}
