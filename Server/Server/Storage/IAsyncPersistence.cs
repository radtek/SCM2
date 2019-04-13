using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Swift;

namespace Swift
{
    /// <summary>
    /// 异步持久化器
    /// </summary>
    public interface IAsyncPersistence<T, IDType> where T : DataItem<IDType>
    {
        // 保存新增的数据
        void AddNew(T it);

        // 保存数据
        void Update(T it);

        // 载入指定 id 对应的数据项
        void Load(IDType id, Action<T> cb);

        // 载入指定条件的数据项
        void LoadAll(Action<T[]> cb, string whereClouse);

        // 载入指定条件数据的ID
        void LoadAllID(Action<IDType[]> cb, string whereClouse);

        // 删除指定 id 对应数据
        void Delete(IDType id);

        // 处理等待的回调操作
        int ProcessPendingCallback();

        void Wait4CompoleteAll();
    }
}
