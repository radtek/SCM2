using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using Microsoft.CSharp;

namespace Swift
{
    /// <summary>
    /// 脚本组件
    /// </summary>
    public class CsScriptShell<T> : Component, ICsScriptShell where T : ScriptObject
    {
        IScriptObjectProvider SP;
        public DynamicScriptProvider<T> DSP
        {
            get
            {
                return SP as DynamicScriptProvider<T>;
            }
            set
            {
                SP = value;
            }
        }

        public Component GetComponent(string name)
        {
            return GetCom(name);
        }

        public Object Call(string scriptObjectName, string fun, params object[] args)
        {
            var obj = DSP.GetByName(scriptObjectName);
            return Dynamic.Invoke(obj, fun, args);
        }

        // 执行一段给定的脚本源码
        public void RunCode(string src, params object[] args)
        {
            src = "void foo(object[] args) {\r\n" + src + "\r\n}";
            ScriptObject so = DSP.Compile(src, null);
            Dynamic.Invoke(so, "foo", new object[] { args });
        }

        // 执行一段给定的脚本源码，带返回值
        public object RunCodeR(string src, params object[] args)
        {
            src = "object foo(object[] args) {\r\n" + src + "\r\n}";
            ScriptObject so = DSP.Compile(src, null);
            return Dynamic.Invoke(so, "foo", new object[] { args });
        }

        // 执行指定脚本文件中的指定函数，并提供参数列表
        public Object RunScript(string f, string fun, object[] args)
        {
            ScriptObject so = SP.GetByFile(f);
            return Dynamic.Invoke(so, fun, args);
        }

        // 执行指定脚本文件中的指定函数，并提供参数列表
        public Object RunScriptS(string f, string fun, string[] args)
        {
            ScriptObject so = SP.GetByFile(f);
            return Dynamic.InvokeWithStringArgs(so, fun, args);
        }

        // 重新载入刷新脚本文件
        public bool RecompileFromFile(string f)
        {
            return DSP.RecompileFromFile(f);
        }
    }
}
