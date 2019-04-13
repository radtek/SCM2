using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using System.Runtime;
using System.Reflection;
using Microsoft.CSharp;

namespace Swift
{
    /// <summary>
    /// 能够动态编译脚本的脚本对象提供者
    /// </summary>
    public class DynamicScriptProvider<T> : IScriptObjectProvider where T : ScriptObject
    {
        public ICsScriptShell CsScriptShell
        {
            get;
            set;
        }

        public ScriptObject GetByFile(string f)
        {
            f = f.ToLower();
            T so = null;
            if (!file2so.ContainsKey(f))
            {
                so = CompileFromFile(f);
                AddScriptObject(f, so);
            }
            else
                so = file2so[f];

            return so;
        }

        public ScriptObject CreateByFile(string f)
        {
            f = f.ToLower();
            T so = CompileFromFile(f);
            if (!file2so.ContainsKey(f))
                AddScriptObject(f, so);

            return so;
        }

        public ScriptObject GetByName(string name)
        {
            return name2so.ContainsKey(name) ? name2so[name] : null;
        }

        public bool RecompileFromFile(string f)
        {
            f = f.ToLower();
            T so = CompileFromFile(f);
            if (so == null)
                return false;

            if (file2so.ContainsKey(f))
            {
                ScriptObject oldOne = file2so[f];
                if (oldOne.Name != null)
                    name2so.Remove(oldOne.Name);

                file2so.Remove(f);
            }

            AddScriptObject(f, so);
            return true;
        }

        public DynamicScriptProvider()
        {
            LoadFile = LoadFileImpl;
        }

        // 临时目录名，如果为空，表示不生成临时文件。
        // 临时文件是为了在开发状态下生成 cs 和 pdb 文件进行调试，只在 pc 平台下有效
        public string TempDir
        {
            get;
            set;
        }

        // 为脚本添加名称空间
        public void AddNamespace(string ns)
        {
            if (customNamespace.IndexOf(ns) < 0)
                customNamespace.Add(ns);
        }
        
        // 增加脚本编译的引用库
        public void AddAssembly(string ass)
        {
            if (customAssembly.IndexOf(ass) < 0)
                customAssembly.Add(ass);
        }

        // 清除所有添加的名称空间
        public void ClearNamespace()
        {
            customNamespace.Clear();
        }

        // 将脚本内容包装成可编译的代码
        public string Wrap4Compile(string[] srcList, string[] className)
        {
            // 先将脚本代码包装成类
            string classSrc = "";
            for (int i = 0; i < srcList.Length; i++)
                classSrc += Warp2Class(srcList[i], out className[i]);

            // 再将类封装进名称空间，生成可编译代码
            string nsStr = "";
            foreach (string ns in customNamespace)
                nsStr += "using " + ns + ";\r\n";

            return strNamespace.Replace("[CustomNamespace_PlaceHolder]", nsStr).Replace("[ClassSource_PlaceHoder]", classSrc);
        }

        // 将脚本内容包装成可编译的代码
        public string Wrap4Compile(string src, out string className)
        {
            string[] cn = new string[1];
            string fullSource = Wrap4Compile(new string[] { src }, cn);
            className = cn[0];
            return fullSource;
        }

        // 将脚本内容包装成类
        public string Warp2Class(string src, out string className)
        {
            string scriptName = null;
            src = src.Trim(" \r\n".ToCharArray());
            if (src.StartsWith("#name("))
            {
                int endIndex = src.IndexOf(")");
                scriptName = src.Substring(6, endIndex - 6);
                src = src.Substring(endIndex + 1).Trim(" \r\n".ToCharArray());
            }

            className = GetValidClassName();
            string fullSource = strClass.Replace("[ClassName_PlaceHolder]", className).Replace("[ScriptCode_PlaceHolder]", src).Replace("[ScriptBaseClassName_PlaceHolder]", typeof(T).FullName);
            if (scriptName != null)
                fullSource = fullSource.Replace("[ScriptName_PlaceHolder]", "\"" + scriptName + "\"");
            else
                fullSource = fullSource.Replace("[ScriptName_PlaceHolder]", "null");

            return fullSource;
        }

        // 编译版本
        public string CompilerVersion
        {
            get
            {
                return compilerVersion;
            }
            set
            {
                compilerVersion = value;
            }
        }
        string compilerVersion = "v4.0";

        // 将指定目录下的所有脚本文件编译成为指定的 dll，并生成对应的静态 ObjectProvider 代码
        public string CompileAll2Dll(string path, string parten, string dllPath, string prividerClassName)
        {
            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
                path = path += Path.DirectorySeparatorChar;

            string[] files = Directory.GetFiles(path, parten, SearchOption.AllDirectories);

            // 读出所有脚本源码
            List<string> srcList = new List<string>();
            foreach (string fPath in files)
            {
                using (StreamReader r = new StreamReader(fPath))
                {
                    srcList.Add(r.ReadToEnd());
                }
            }

            // 编译到 dll
            string[] classNameList = new string[srcList.Count];
            string allSrc = Wrap4Compile(srcList.ToArray(), classNameList);
            Compile2Dll(allSrc, dllPath);

            // 编译完后，生成一份静态 ScriptObjectProvider 代码
            string caseSrc = "";
            for (int i = 0; i < files.Length; i++)
            {
                string f = files[i].Replace(path, "").ToLower();
                caseSrc += "case \"" + f + "\": return new " + classNameList[i] + "();\r\n";
            }

            // 再将类封装进名称空间，生成可编译代码
            string nsStr = "";
            foreach (string ns in customNamespace)
                nsStr += "using " + ns + ";\r\n";

            return strScriptProvider.Replace("[CustomNamespace_PlaceHolder]", nsStr).Replace("[ScriptProviderClassName_PlaceHolder]", prividerClassName).Replace("[CreateByFile_Case_PlaceHolder]", caseSrc);
        }

        // 利用当前设置，将给定代码编译到指定 dll 中
        public void Compile2Dll(string src, string dllPath)
        {
            CompilerResults cr = null;

            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateExecutable = false;
            compilerParams.TempFiles = null;
            compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add(Path.Combine(BinDir, "Swift.dll"));
            compilerParams.ReferencedAssemblies.Add(Path.Combine(BinDir, "SwiftIOSCompatible.dll"));
            foreach (string ass in customAssembly)
                compilerParams.ReferencedAssemblies.Add(ass);

            CSharpCodeProvider compiler = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", CompilerVersion } });
            compilerParams.CompilerOptions = "/target:library";
            compilerParams.IncludeDebugInformation = true;
            compilerParams.GenerateInMemory = false;
            compilerParams.OutputAssembly = dllPath;
            cr = compiler.CompileAssemblyFromSource(compilerParams, src);

            if (cr.Errors.HasErrors)
            {
                string errMsg = "";
                foreach (CompilerError error in cr.Errors)
                    errMsg += error.ErrorText + "(" + error.Line + ")\r\n";

                throw new Exception("Script compiling error: " + errMsg);
            }
        }

        // 将源码编译成脚本对象
        public T Compile(string src, string debugFile)
        {
            // 提取 #name 指令
            string className;
            string fullSource = Wrap4Compile(src, out className);

            CompilerParameters compilerParams = new CompilerParameters();
            compilerParams.GenerateExecutable = false;
            compilerParams.TempFiles = null;
            compilerParams.ReferencedAssemblies.Add("mscorlib.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            foreach (string ass in customAssembly)
                compilerParams.ReferencedAssemblies.Add(ass);

            CSharpCodeProvider compiler = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", CompilerVersion } });
            CompilerResults cr = null;

            if (debugFile != null)
            {
                StreamWriter w = new StreamWriter(debugFile + ".cs", false, Encoding.UTF8);
                w.WriteLine(fullSource);
                w.Close();

                compilerParams.CompilerOptions = "/target:library";
                compilerParams.IncludeDebugInformation = true;
                compilerParams.GenerateInMemory = false;
                compilerParams.OutputAssembly = debugFile + ".dll";

                cr = compiler.CompileAssemblyFromFile(compilerParams, debugFile + ".cs");
            }
            else
            {
                compilerParams.CompilerOptions = "/target:library /optimize";
                compilerParams.IncludeDebugInformation = false;
                compilerParams.GenerateInMemory = true;
                compilerParams.OutputAssembly = null;

                cr = compiler.CompileAssemblyFromSource(compilerParams, fullSource);
            }

            if (cr.Errors.HasErrors)
            {
                string errMsg = "";
                foreach (CompilerError error in cr.Errors)
                    errMsg += error.ErrorText + "(" + error.Line + ")\r\n";

                throw new Exception("Script compiling error: " + errMsg);
            }

            Assembly asm = cr.CompiledAssembly;
            T so = asm.CreateInstance("Swift.ScriptSpace." + className) as T;

            // 设置内置变量
            Dynamic.SetField(so, "_ssh", CsScriptShell);
            return so;
        }

        #region 保护部分

        // dll 所在目录，默认为当前目录
        string BinDir
        {
            get
            {
                if (binDir == null)
                {
                    FileInfo fInfo = new FileInfo(this.GetType().Assembly.Location);
                    binDir = fInfo.Directory.FullName;
                }

                return binDir;
            }
        } string binDir = null;

        // 尝试获取脚本文件的准确信息
        FileInfo GetFileInfo(string f)
        {
            return new FileInfo(f);
        }

        // 载入指定的脚本文件并创建脚本对象
        public T CompileFromFile(string f)
        {
            string debugFile = null;

            if (TempDir != null)
            {
                if (!Directory.Exists(TempDir))
                    Directory.CreateDirectory(TempDir);

                FileInfo fi = GetFileInfo(f);
                string fName = fi.Name;
                debugFile = Path.Combine(TempDir, fName + "_" + seqNo++);
            }

            string c = LoadFile(f);
            return Compile(c, debugFile);
        }

        // 获取一个不重复的合法的类名
        string GetValidClassName()
        {
            return "Script_Class_" + seqNo++;
        }

        // 添加一个脚本对象
        void AddScriptObject(string f, T so)
        {
            f = f.ToLower();
            file2so[f] = so;
            if (so.Name != null)
                name2so[so.Name] = so;
        }

        // 读取脚本文件的句柄
        public Func<string, string> LoadFile = null;
        string LoadFileImpl(string path)
        {
            FileStream fs = new FileStream(Path.Combine(".", path), FileMode.Open, FileAccess.Read);
            StreamReader r = new StreamReader(fs, Encoding.UTF8);
            string c = r.ReadToEnd();
            r.Close();
            fs.Close();
            return c;
        }

        // 卸载所有已经编译的脚本对象
        public void UnloadAll()
        {
            name2so.Clear();
            file2so.Clear();
        }

        // 手动设置类序号，用于预脚本
        public int SeqNo
        {
            set
            {
                seqNo = value;
            }
        }

        // 单增序号，用于保证类名不重复。10w以前留给预编译部分用，10w以后给即时编译用
        long seqNo = 100000;

        // 增加的脚本名称空间
        List<string> customNamespace = new List<string>();

        // 增加的程序集
        List<string> customAssembly = new List<string>();

        // 脚本文件路径与脚本对象的映射
        Dictionary<string, T> file2so = new Dictionary<string, T>();

        // 脚本对象名与脚本对象的映射
        Dictionary<string, T> name2so = new Dictionary<string, T>();

        // 拼装脚本文件的名称空间代码
        string strNamespace = @"
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Swift;
using Swift.ScriptSpace;

[CustomNamespace_PlaceHolder]

namespace Swift.ScriptSpace
{
    [ClassSource_PlaceHoder]
}
";
        // 拼装脚本的类代码
        string strClass = @"
public class [ClassName_PlaceHolder] : [ScriptBaseClassName_PlaceHolder]
{
    public override string Name
    {
        get
        {
            return [ScriptName_PlaceHolder];
        }
    }

    [ScriptCode_PlaceHolder]
}
";

        // 静态 ScriptObjectProvider 代码模板
        string strScriptProvider = @"
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Swift;
using Swift.ScriptSpace;

[CustomNamespace_PlaceHolder]

namespace Swift
{
    public class [ScriptProviderClassName_PlaceHolder] : IScriptObjectProvider
    {
        public ScriptObject GetByFile(string f)
        {
            f = f.ToLower();
            ScriptObject so = null;
            if (!file2so.ContainsKey(f))
            {
                so = CompileFromFile(f);
                AddScriptObject(f, so);
            }
            else
                so = file2so[f];

            return so;
        }

        public ScriptObject GetByName(string name)
        {
            return name2so.ContainsKey(name) ? name2so[name] : null;
        }

        public ScriptObject CreateByFile(string f)
        {
            f = f.ToLower();
            switch (f)
            {
[CreateByFile_Case_PlaceHolder]
default: return null;
            }
        }

        void AddScriptObject(string f, ScriptObject so)
        {
            f = f.ToLower();
            file2so[f] = so;
            if (so.Name != null)
                name2so[so.Name] = so;
        }

        Dictionary<string, ScriptObject> file2so = new Dictionary<string, ScriptObject>();
        Dictionary<string, ScriptObject> name2Obj = new Dictionary<string, ScriptObject>();
    }
}
";
        #endregion
    }
}
