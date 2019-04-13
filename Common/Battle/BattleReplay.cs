using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;

namespace SCM
{
    /// <summary>
    /// 战斗录像
    /// </summary>
    public class BattleReplay
    {
        public string ID;
        public string Usr1;
        public string Usr2;
        public string UsrName1;
        public string UsrName2;
        public ulong Length; // by 0.1 sec
        public DateTime Date;
        public bool IsPVP;
        public bool IsCrashReplay;
        public List<KeyValuePair<string, IReadableBuffer>> Msgs;

        // 序列化一个录像的基本信息
        public void SerializeHeader(IWriteableBuffer buff)
        {
            buff.Write(ID);
            buff.Write(Usr1);
            buff.Write(Usr2);
            buff.Write(UsrName1);
            buff.Write(UsrName2);
            buff.Write(Length);
            buff.Write(Date.Ticks);
            buff.Write(IsPVP);
            buff.Write(IsCrashReplay);
        }

        // 序列化一个录像
        readonly static byte[] EmptyBytesArray = new byte[0];
        public void Serialize(IWriteableBuffer buff)
        {
            SerializeHeader(buff);

            string lastMsgTitle = null;
            byte[] lastMsgData = null;

            var cnt = Msgs.Count;
            buff.Write(cnt);
            foreach (var msg in Msgs)
            {
                var title = msg.Key;
                var body = msg.Value;
                var len = body.Available;
                var msgData = len > 0 ? body.PeekBytes(len) : EmptyBytesArray;

                if (title == lastMsgTitle && AllSame(lastMsgData, msgData))
                    buff.Write(true);
                else
                {
                    buff.Write(false);
                    buff.Write(title);
                    buff.Write(len);
                    buff.Write(msgData);

                    lastMsgTitle = msg.Key;
                    lastMsgData = msgData;
                }
            }
        }

        // 反序列化一个录像的基本信息
        public static BattleReplay DeserializeHeader(IReadableBuffer data)
        {
            var replay = new BattleReplay();
            replay.ID = data.ReadString();
            replay.Usr1 = data.ReadString();
            replay.Usr2 = data.ReadString();
            replay.UsrName1 = data.ReadString();
            replay.UsrName2 = data.ReadString();
            replay.Length = data.ReadULong();
            replay.Date = new DateTime(data.ReadLong());
            replay.IsPVP = data.ReadBool();
            replay.IsCrashReplay = data.ReadBool();

            return replay;
        }

        // 反序列化一个录像
        public static BattleReplay Deserialize(IReadableBuffer data)
        {
            var replay = DeserializeHeader(data);

            string lastMsgTitle = null;
            byte[] lastMsgData = null;
            var msgs = new List<KeyValuePair<string, IReadableBuffer>>();
            var cnt = data.ReadInt();
            FC.For(cnt, (i) =>
            {
                // 这里生成 msg 必须用网络字节序，因为这里的数据等同于是直接伪装成了网络上的消息，
                // 对 Room4Client 来说是一致读取的

                var sameAsLastOne = data.ReadBool();
                if (!sameAsLastOne)
                {
                    var msgTitle = data.ReadString();
                    var msgLen = data.ReadInt();
                    var msgBody = data.ReadBytes(msgLen);
                    msgs.Add(new KeyValuePair<string, IReadableBuffer>(msgTitle, new RingBuffer(true, true, msgBody)));

                    lastMsgTitle = msgTitle;
                    lastMsgData = msgBody;
                }
                else
                    msgs.Add(new KeyValuePair<string, IReadableBuffer>(lastMsgTitle, new RingBuffer(true, true, lastMsgData)));
            });

            replay.Msgs = msgs;
            return replay;
        }

        static bool AllSame(byte[] d1, byte[] d2)
        {
            if (d1 == d2)
                return true;
            else if (d1 == null && d2 != null)
                return false;
            else if (d1 != null && d2 == null)
                return false;
            else if (d1.Length != d2.Length)
                return false;

            var n = d1.Length;
            for (var i = 0; i < n; i++)
            {
                if (d1[i] != d2[1])
                    return false;
            }

            return true;
        }
    }
}
