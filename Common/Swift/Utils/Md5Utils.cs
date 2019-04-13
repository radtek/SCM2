using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Swift
{
    public class Md5Utils
    {
        // 计算一个 hash 字符串
        public static string MD5(string src)
        {
            byte[] data = Encoding.UTF8.GetBytes(src);
            byte[] code = p.ComputeHash(data);
            string r = "";
            foreach (byte b in code)
                r += b;

            return r;
        }
        // 计算一个 hash 字符串
        // 返回值是32位字符串，每个字符是一个16进制数
        public static string MD5Hex(string src)
        {
            byte[] data = Encoding.UTF8.GetBytes(src);
            return MD5Hex(data);
        }

        public static string MD5Hex(byte[] data)
        {
            byte[] code = p.ComputeHash(data);

            StringBuilder sb = new StringBuilder();
            foreach (var c in code)
                sb.Append(c.ToString("x2"));

            return sb.ToString();
        }

        // md5 加密器
        static MD5CryptoServiceProvider p = new MD5CryptoServiceProvider();
    }
}
