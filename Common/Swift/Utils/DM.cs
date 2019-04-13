using System;
using System.Collections.Generic;

namespace Swift
{
    /// <summary>
    /// Data Management
    /// </summary>
    public static class DM {}

    // 普通缓存器
    public class Cache<T>
    {
        Func<T> dc = null;

        public Cache(Func<T> defaultCreator)
        {
            dc = defaultCreator;
        }

        void Clear()
        {
            objs.Clear();
        }

        public T Get()
        {
            if (objs.Count > 0)
            {
                var obj = objs[0];
                objs.RemoveAt(0);
                return obj;
            }
            else
                return dc();
        }

        public void Reserve(int num)
        {
            FC.For(num, (i) => { objs.Add(dc()); });
        }

        public void Put(T obj)
        {
            objs.Add(obj);
        }

        List<T> objs = new List<T>();
    }

    // 带类型的缓存器
    public class ClassifiedCache<KT, VT>
    {
        // 对象缓存
        Dictionary<KT, Cache<VT>> cache = new Dictionary<KT, Cache<VT>>();

        Func<KT, VT> dc = null;

        public ClassifiedCache(Func<KT, VT> defaultCreator)
        {
            dc = defaultCreator;
        }

        Cache<VT> MakeSureCache(KT type)
        {
            if (!cache.ContainsKey(type))
                cache[type] = new Cache<VT>(() => { return dc(type); });

            return cache[type];
        }

        void Clear()
        {
            cache.Clear();
        }

        public VT Get(KT type)
        {
            var cc = MakeSureCache(type);
            return cc.Get();
        }

        public void Reserve(KT type, int num)
        {
            var cc = MakeSureCache(type);
            cc.Reserve(num);
        }

        public void Put(KT t, VT obj)
        {
            var cc = MakeSureCache(t);
            cc.Put(obj);
        }
    }
}
