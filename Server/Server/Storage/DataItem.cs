using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swift;

namespace Swift
{
    /// <summary>
    /// 数据项状态
    /// </summary>
    public class PersistenceStatus
    {
        public bool NewAdd = false;
        public bool Modified = false;
    }

    /// <summary>
    /// 数据项，放入数据容器进行管理
    /// </summary>
    public abstract class DataItem<IDType> : SerializableData
    {
        public IDType ID;

        public DataItem() { }

        // 构造器
        public DataItem(IDType dataID)
        {
            ID = dataID;
        }

        public void Update()
        {
            if (_Update != null)
                _Update();
        }

        // 通知数据更新
        public Action _Update = null;

        // 数据持久化状态
        public PersistenceStatus Status = new PersistenceStatus();
    }
}
