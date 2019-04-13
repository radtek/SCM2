using System;
using System.Collections.Generic;
using Swift;

namespace SCM
{
    /// <summary>
    /// 创建指定的单位对象
    /// </summary>
    public class UnitFactory : Component
    {
        public static UnitFactory Instance
        {
            get { return instance; }
        } static UnitFactory instance = null;

        public UnitFactory()
        {
            if (instance != null)
                throw new Exception("only one UnitFactory should be created.");

            instance = this;
        }

        // 创建基本单元
        Unit CreateUnit(string uid)
        {
            var u = new Unit(uid);
            return u;
        }

        // 创建一个地图单位，在加入地图后，才开始建造
        public Unit Create(string uid)
        {
            var u = CreateUnit(uid);
            return u;
        }
    }
}
