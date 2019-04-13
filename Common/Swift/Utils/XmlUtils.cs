using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;

namespace Swift
{
    /// <summary>
    /// Xml工具类
    /// </summary>
    public static class XmlUtils
    {
        // 密钥码
        public static string KeyCode = "12348578902223367877723456789012";

        private static string UTF8ByteArrayToString(byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            string constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        private static byte[] StringToUTF8ByteArray(string pXmlString)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        // 内容加密
        private static string Encrypt(string toE)
        {
            // 加密和解密采用相同的key,具体自己填，但是必须为32位// 
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(KeyCode);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateEncryptor();

            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toE);
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        // 内容解密 
        private static string Decrypt(string toD)
        {
            // 加密和解密采用相同的key,具体值自己填，但是必须为32位// 
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(KeyCode);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateDecryptor();

            byte[] toEncryptArray = Convert.FromBase64String(toD);
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        // 保存为XML文件 
        private static void SaveXML(string path, string xmlStr)
        {
            string data = xmlStr;// Encrypt(xmlStr);
            StreamWriter writer;
            writer = File.CreateText(path);
            writer.Write(data);
            writer.Close();
        }

        // 读取XML文件 
        private static string LoadXML(string path)
        {
            StreamReader sReader = File.OpenText(path);
            string data = sReader.ReadToEnd();
            sReader.Close();
            string xmlStr = data;//Decrypt(dataString);
            return xmlStr;
        }

        // 序列化对象
        public static string ToXml<T>(T obj)
        {
            string xmlStr = null;
            MemoryStream memoryStream = new MemoryStream();
            XmlSerializer xs = new XmlSerializer(typeof(T));
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
            xs.Serialize(xmlTextWriter, obj);

            memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
            xmlStr = UTF8ByteArrayToString(memoryStream.ToArray());
            return xmlStr;
        }

        // 反序列化对象
        public static T ToObject<T>(string xmlStr)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(xmlStr));
            //XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
            return (T)xs.Deserialize(memoryStream);
        }

        // 序列化对象并保存为XML格式文件
        public static void ToXmlAndSave<T>(string path, T obj)
        {
            string xmlStr = ToXml(obj);
            SaveXML(path, xmlStr);
        }

        // 加载XML格式文件并反序列化为对象
        public static T ToObjectAndLoad<T>(string path)
        {
            string xmlStr = LoadXML(path);
            return ToObject<T>(xmlStr);
        }
    }
}
