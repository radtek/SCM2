using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Swift
{
    public interface ICsScriptShell
    {
        Component GetComponent(string name);
        Object Call(string scriptObjectName, string fun, params object[] args);
    }

    /// <summary>
    /// 脚本组件
    /// </summary>
    public abstract class ScriptObject
    {
        // 脚本对象名
        public abstract string Name
        {
            get;
        }

        // 根据名称获取组件对象
        public Component GetComponent(string name)
        {
            return _ssh.GetComponent(name);
        }

        // 脚本组件对象
        protected ICsScriptShell _ssh = null;
    }

    /// <summary>
    /// 提供脚本对象，或者从预编译 dll，或者从文件即时编译
    /// </summary>
    public interface IScriptObjectProvider
    {
        ScriptObject GetByFile(string f);

        ScriptObject GetByName(string name);

        ScriptObject CreateByFile(string f);

        ICsScriptShell CsScriptShell
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 提供一些动态特性的功能
    /// </summary>
    public class Dynamic : DynamicGeneral
    {
        // 获取指定名称的类型对象
        public static Type GetTypeByTypeName(string typeName)
        {
            Assembly asm = GetAssemblyByType(typeName);
            return asm.GetType(typeName);
        }

        // 创建给定类型的一个实例对象
        public static object CreateByType(Type type)
        {
            return type.Assembly.CreateInstance(type.FullName);
        }

        // 创建给定类型名称的一个实例对象
        public static object CreateByTypeName(string typeName)
        {
            Assembly asm = GetAssemblyByType(typeName);
            if (asm != null)
                return asm.CreateInstance(typeName);
            else
                return null;
        }
    }
}
