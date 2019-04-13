using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Swift;
using Server;
using System.Data;
using System.Data.Common;
using System.Threading;

namespace Swift
{
    /// <summary>
    /// 数据容器，对外提供数据逻辑，给各个功能模块，内部完成存储功能。对于 DataContainer 来说，会假设所有数据项的 id
    /// 都在内存中，而数据内容可能被卸载
    /// </summary>
    public class DataContainer<T, IDType> : Component, IFrameDrived where T : DataItem<IDType>, new()
    {
        // 用户包装返回给外面用的数据，以定义和检查有效范围
        public class DataItemWrapper
        {
            IDType id = default(IDType);
            WeakReference<T> r = null;
            T rr = null; // 强引用避免失效

            public DataItemWrapper(T d)
            {
                id = d.ID;
                r = new WeakReference<T>(d);
                d._Update = () => { d.Status.Modified = true; Fix(); };
            }

            public static T operator !(DataItemWrapper w)
            {
                T target = null;
                if (w == null || !w.r.TryGetTarget(out target))
                    return null;

                return target;
            }

            public T t { get { return !this; } }

            public void Fix() { rr = t; }
            public void Unfix() { rr = null; }
        }

        // 构造器，需要给定持久化器
        public DataContainer(IAsyncPersistence<T, IDType> persistence)
        {
            p = persistence;
        }

        // 停止启动异步存储
        public override void Close()
        {
            ProcessAll();
            while (p.ProcessPendingCallback() > 0)
                Thread.Sleep(100);

            p.Wait4CompoleteAll();
        }

        // 新增数据
        public void AddNew(T it)
        {
            if (data.ContainsKey(it.ID))
                throw new Exception(it.ID + " already exists in data container");

            DataItemWrapper w = new DataItemWrapper(it);
            w.Fix();
            data[it.ID] = w;
            it.Status.NewAdd = true;
            it.Status.Modified = false;
        }

        // 删除数据
        public void Delete(IDType id)
        {
            if (data.ContainsKey(id))
                data.Remove(id);

            if (p != null)
                p.Delete(id);
        }

        // 如果内存中有数据，就从内存中取，否则尝试加载一个，并放入容器
        public void Retrieve(IDType id, Action<T> cb)
        {
            DataItemWrapper w = Get(id);
            if (!w != null)
            {
                if (cb != null)
                    cb(!w);
            }
            else
            {
                data.Remove(id);
                Load(id, (T d) =>
                {
                    w = Get(id);
                    if (d != null && !w == null)
                    {
                        w = new DataItemWrapper(d);
                        data[d.ID] = w;
                    }

                    if (cb != null)
                        cb(!w);
                });
            }
        }

        // 如果内存中有数据，就从内存中取，否则尝试加载一个，并放入容器
        public void Retrieve(IEnumerable<IDType> ids, Action<T[]> cb)
        {
            List<IDType> idLst = new List<IDType>();
            idLst.AddRange(ids);

            if (idLst.Count == 0)
            {
                cb(new T[] { });
                return;
            }

            T[] dArr = new T[idLst.Count];
            int cnt = 0;
            for (int i = 0; i < dArr.Length; i++)
            {
                int n = i;
                Retrieve(idLst[n], (T d) =>
                {
                    dArr[n] = d;
                    cnt++;

                    if (cnt == dArr.Length)
                        cb(dArr);
                });
            }
        }

        // 如果内存中有数据，就从内存中取，否则尝试加载一个，并放入容器
        public void Retrieve(Action<T[]> cb, params IDType[] ids)
        {
            Retrieve(ids, cb);
        }

        // 自动保存间隔（毫秒，默认 30000，即 30 秒），0 表示永远不进行自动存储
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }

        // 完成自动保存及推动回调等逻辑
        public void OnTimeElapsed(int te)
        {
            p.ProcessPendingCallback();

            if (interval == 0)
                return;

            elapsed += te;
            if (elapsed >= interval)
            {
                while (elapsed >= interval)
                    elapsed -= interval;

                ProcessAll();
            }
        }

        #region 保护部分

        // 从内存中获取指定 id 的数据
        protected virtual DataItemWrapper Get(IDType id)
        {
            if (id == null || !data.ContainsKey(id))
                return null;

            return data[id];
        }

        // 根据 ID 从磁盘载入指定数据项，并不放入容器
        protected virtual void Load(IDType id, Action<T> cb)
        {
            p.Load(id, (T it) =>
            {
                if (cb != null)
                    cb(it);
            });
        }

        // 处理所有等待的操作
        void ProcessAll()
        {
            // 尝试保存所有数据并刷新数据状态
            IDType[] arr = null;
            if (data.Count == 0)
                return;
            else
                arr = data.Keys.ToArray();

            // 将需要修改的数据都扔给持久化器进行操作
            foreach (var id in arr)
            {
                var w = Get(id);
                if (w == null)
                    continue;

                var it = w.t;
                if (it == null)
                {
                    data.Remove(id);
                    continue;
                }

                if (it.Status.NewAdd)
                    p.AddNew(it);
                else if (it.Status.Modified)
                    p.Update(it);

                it.Status.NewAdd = false;
                it.Status.Modified = false;
                w.Unfix();
            }
        }

        // 所有数据项
        Dictionary<IDType, DataItemWrapper> data = new Dictionary<IDType, DataItemWrapper>();

        // 持久化器
        protected IAsyncPersistence<T, IDType> p = null;

        // 自动保存间隔
        int interval = 30000;

        // 自动保存间隔累计时间
        int elapsed = 0;

        #endregion
    }
}
