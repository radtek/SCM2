using System.Collections;
using System.Collections.Generic;
using System;
using Swift.Math;

namespace Swift
{
    /// <summary>
    /// 数据序列化接口
    /// </summary>
    public interface ISerializable
    {
        void Serialize(IWriteableBuffer w);
        void Deserialize(IReadableBuffer r);
    }

    /// <summary>
    /// 具备对旧数据版本兼容的序列化数据对象
    /// </summary>
    public abstract class SerializableData : ISerializable
    {
        bool isWrite = true;
        public bool IsWrite { get { return isWrite; } }

        IWriteableBuffer w;
        public IWriteableBuffer W { get { return w; } }
        int wLenPos = 0, wAvailable1 = 0;

        // 深拷贝
        public virtual T Clone<T>() where T : SerializableData, new()
        {
            WriteBuffer buff = new WriteBuffer();
            Serialize(buff);
            T obj = new T();
            obj.Deserialize(new RingBuffer(buff.Data));
            return obj;
        }

        // 序列化
        public void Serialize(IWriteableBuffer w)
        {
            isWrite = true;
            this.w = w;
            MakeSureNotSyncing();
            Sync();
            MakeSureNotSyncing();
            this.w = null;
        }

        IReadableBuffer r;
        public IReadableBuffer R { get { return r; } }
        int rLength = 0, rAvaliable1 = 0;
        bool IsEnd()
        {
            return (rAvaliable1 - r.Available) >= rLength;
        }

        // 反序列化
        public void Deserialize(IReadableBuffer r)
        {
            isWrite = false;
            this.r = r;
            MakeSureNotSyncing();
            Sync();
            MakeSureNotSyncing();
            this.r = null;
        }

        enum State { None, Syncing, }
        State state = State.None;
        void MakeSureSyncing()
        {
            if (state != State.Syncing)
                throw new Exception("state != State.Syncing");
        }

        void MakeSureNotSyncing()
        {
            if (state != State.None)
                throw new Exception("state != State.None");
        }

        // 开始序列化段落
        protected void BeginSync()
        {
            MakeSureNotSyncing();
            state = State.Syncing;
            if (isWrite)
            {
                wLenPos = ((WriteBuffer)w).ReserveInt();
                wAvailable1 = w.Available;
            }
            else
            {
                rLength = r.ReadInt();
                rAvaliable1 = r.Available;
            }
        }

        // 序列化段落结束
        protected void EndSync()
        {
            MakeSureSyncing();
            state = State.None;

            if (isWrite)
            {
                int wAvailable2 = w.Available;
                ((WriteBuffer)w).UnreserveInt(wLenPos, wAvailable2 - wAvailable1);
            }
        }

        // 同步缓冲数据和对象数据
        abstract protected void Sync();

        #region 同步各种数据类型

        public void SyncInt(ref bool v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;

                v = r.ReadBool();
            }
        }

        public void SyncInt(ref int v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;
                
                v = r.ReadInt();
            }
        }

        protected void SyncListInt(ref List<int> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    for (int i = 0; i < v.Count; i++)
                        w.Write(v[i]);
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new List<int>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                        v.Add(r.ReadInt());
                }
            }
        }

        public void SyncFloat(ref float v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;

                v = r.ReadFloat();
            }
        }

        protected void SyncLong(ref long v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;
                
                v = r.ReadLong();
            }
        }

        protected void SyncFix64(ref Fix64 v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v.RawValue);
            else
            {
                if (IsEnd())
                    return;

                var rv = r.ReadLong();
                v = Fix64.FromRaw(rv);
            }
        }

        protected void SyncULong(ref ulong v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;
                
                v = r.ReadULong();
            }
        }

        protected void SyncString(ref string v)
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;

                v = r.ReadString();
            }
        }

        protected void SyncListString(ref List<string> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    for (int i = 0; i < v.Count; i++)
                        w.Write(v[i]);
                }
            }
            else
            {
                if (IsEnd())
                    return;

                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new List<string>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                        v.Add(r.ReadString());
                }
            }
        }

        protected void SyncObj<T>(ref T v) where T : class, ISerializable, new()
        {
            MakeSureSyncing();

            if (isWrite)
                w.Write(v);
            else
            {
                if (IsEnd())
                    return;

                v = r.Read<T>();
            }
        }

        protected void SyncListObj<T>(ref List<T> v) where T : class, ISerializable, new()
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    for (int i = 0; i < v.Count; i++)
                    {
                        T vi = v[i];
                        SyncObj(ref vi);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new List<T>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        T vi = null;
                        SyncObj(ref vi);
                        v.Add(vi);
                    }
                }
            }
        }

        protected void SyncDictII(ref Dictionary<int, int> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<int, int>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        int _k = r.ReadInt();
                        int _v = r.ReadInt();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictSI(ref Dictionary<string, int> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<string, int>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string _k = r.ReadString();
                        int _v = r.ReadInt();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictSU(ref Dictionary<string, ulong> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<string, ulong>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string _k = r.ReadString();
                        ulong _v = r.ReadULong();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictSS(ref Dictionary<string, string> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<string, string>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string _k = r.ReadString();
                        string _v = r.ReadString();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictSB(ref Dictionary<string, bool> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;

                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<string, bool>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string _k = r.ReadString();
                        bool _v = r.ReadBool();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictIB(ref Dictionary<int, bool> v)
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        w.Write(kv.Value);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;

                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<int, bool>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        int _k = r.ReadInt();
                        bool _v = r.ReadBool();
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictIObj<T>(ref Dictionary<int, T> v) where T : class, ISerializable, new()
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        var kvv = kv.Value;
                        SyncObj(ref kvv);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<int, T>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        int _k = r.ReadInt();
                        T _v = null;
                        SyncObj(ref _v);
                        v.Add(_k, _v);
                    }
                }
            }
        }

        protected void SyncDictSObj<T>(ref Dictionary<string, T> v) where T : class, ISerializable, new()
        {
            MakeSureSyncing();

            if (isWrite)
            {
                w.Write(v != null ? true : false);
                if (v != null)
                {
                    w.Write((int)v.Count);
                    foreach (var kv in v)
                    {
                        w.Write(kv.Key);
                        var kvv = kv.Value;
                        SyncObj(ref kvv);
                    }
                }
            }
            else
            {
                if (IsEnd())
                    return;
                
                bool hasValue = r.ReadBool();
                if (!hasValue)
                    v = null;
                else
                {
                    if (v == null)
                        v = new Dictionary<string, T>();
                    else
                        v.Clear();

                    int count = r.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        string _k = r.ReadString();
                        T _v = null;
                        SyncObj(ref _v);
                        v.Add(_k, _v);
                    }
                }
            }
        }

        #endregion
    }
}
