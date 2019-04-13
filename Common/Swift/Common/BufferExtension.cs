using System.Collections;
using System.Collections.Generic;
using System;

namespace Swift
{
    /// <summary>
    /// 扩展序列化对象在缓冲区上的操作
    /// </summary>
    public static class BufferExtension
    {
        public static void Write(this IWriteableBuffer w, ISerializable v)
        {
            w.Write(v != null ? true : false);
            if (v != null)
                v.Serialize(w);
        }

        public static void Write(this IWriteableBuffer w, ISerializable[] arr)
        {
            w.Write(arr != null ? true : false);
            if (arr != null)
            {
                w.Write(arr.Length);
                for (int i = 0; i < arr.Length; i++)
                    Write(w, arr[i]);
            }
        }

        public static T Read<T>(this IReadableBuffer r) where T : class, ISerializable, new()
        {
            bool hasValue = r.ReadBool();
            if (!hasValue)
                return null;
            else
            {
                T v = new T();
                v.Deserialize(r);
                return v;
            }
        }

        public static T[] ReadArr<T>(this IReadableBuffer r) where T : class, ISerializable, new()
        {
            bool hasValue = r.ReadBool();
            if (!hasValue)
                return null;
            else
            {
                int len = r.ReadInt();
                T[] arr = new T[len];
                for (int i = 0; i < len; i++)
                    arr[i] = Read<T>(r);
                return arr;
            }
        }
    }
}