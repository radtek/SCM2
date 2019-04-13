using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Swift;
using Server;
using System.Data;
using System.Data.Common;

namespace Swift
{
    /// <summary>
    /// 数据容器，类似 DataContainer，但是全部缓存在内存，不卸载
    /// </summary>
    public class DataCacheContainer<T, IDType> : Component, IFrameDrived where T : DataItem<IDType>, new()
    {
        // 构造器，需要给定持久化器
        public DataCacheContainer(MySqlDbPersistence<T, IDType> persistence)
        {
            p = persistence;
        }

        // 停止异步存储，处理当前所有等待中的操作
        public override void Close()
        {
            ProcessAll();
        }

        // 新增数据
        public bool AddNew(T it)
        {
            if (data.ContainsKey(it.ID))
                return false;

            it._Update = () => { it.Status.Modified = true; };
            data[it.ID] = it;
            it.Status.NewAdd = true;
            it.Status.Modified = false;
            return true;
        }

        // 删除数据
        public bool Delete(IDType id)
        {
            bool exists = data.ContainsKey(id);

            if (exists)
                data.Remove(id);

            if (p != null)
                p.Delete(id);

            return exists;
        }

        // 同步获取数据
        public T Get(IDType id)
        {
            if (!data.ContainsKey(id))
                return null;
            else
                return data[id];
        }

        // 获取所有数据
        public IDType[] AllIDs
        {
            get { return data.Keys.ToArray(); }
        }

        // 从磁盘加载所有数据
        public void LoadAll(Action cb)
        {
            p.LoadAll((arr) =>
            {
                foreach (var d in arr)
                {
                    d._Update = () => { d.Status.Modified = true; };
                    data[d.ID] = d;
                }

                if (cb != null)
                    cb();
            }, null);
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

        // 序列化器
        MySqlDbPersistence<T, IDType> Persistence
        {
            get { return p; }
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
                var it = Get(id);
                if (it == null)
                    continue;

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
            }
        }

        // 所有数据项
        Dictionary<IDType, T> data = new Dictionary<IDType, T>();

        // 持久化器
        protected MySqlDbPersistence<T, IDType> p = null;

        // 自动保存间隔
        int interval = 30000;

        // 自动保存间隔累计时间
        int elapsed = 0;

        #endregion
    }
}
