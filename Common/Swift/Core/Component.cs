using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Swift
{
    /// <summary>
    /// 将 ComponentContainer 功能反向粘结到 Component 上
    /// </summary>
    public interface IComponentFetcher
    {
        Component GetByName(string name);
        Component Remove(string name);
        T Get<T>() where T : class;
        void Add(string name, Component com);
    }

    /// <summary>
    /// 组件接口
    /// </summary>
    public class Component
    {
        // 组件对象名
        public string Name { get; set; }

        public IComponentFetcher ComFetcher = null;

        public Component GetCom(string name)
		{
            return ComFetcher.GetByName(name);
		}

        public Component RemoveCom(string name)
        {
            return ComFetcher.Remove(name);
        }

        public T GetCom<T>() where T : class
        {
            return ComFetcher.Get<T>();
        }

        public void AddCom(string name, Component com)
        {
            ComFetcher.Add(name, com);
        }

        // 组件初始化
        public virtual void Init()
        {

        }

        // 本对象被加入容器
        public virtual void OnAdded()
        {
        }

        // 本对象被移出容器
        public virtual void OnRemoved()
        {
        }

        // 关闭组件对象
        public virtual void Close()
        {
        }

        #region log 部分

        protected void LogInfo(string str)
        {
            var l = GetCom<ILog>();
            if (l != null)
                l.Info(str);
        }

        protected void LogWarning(string str)
        {
            var l = GetCom<ILog>();
            if (l != null)
                l.Warn(str);
        }

        protected void LogError(string str)
        {
            var l = GetCom<ILog>();
            if (l != null)
                l.Error(str);
        }

        #endregion

        #region 保护部分

        // 组件对象名称
        protected string name = null;

        #endregion
    }
}
