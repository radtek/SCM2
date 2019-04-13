/*
 * creator(s): chenm
 * reviser(s): chenm
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Swift;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.Text;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// SqlDb 序列化器基类
    /// </summary>
    public class MySqlDbPersistence<T, IDType> : IAsyncPersistence<T, IDType> where T : DataItem<IDType>
    {
        public ILog Logger = null;
        Func<T, byte[]> data2Buff = null;
        Func<byte[], T> buff2Data = null;

        public MySqlDbPersistence(string dbName, string dbServer, string username, string password,
            string tableName, string createTableCommand, string[] additionalCols,
            Func<T, byte[]> data2BuffHandler, Func<byte[], T> buff2DataHandler, Func<T, string, object> colValueMap)
        {
            tbName = tableName;
            cols = additionalCols;

            data2Buff = data2BuffHandler;
            buff2Data = buff2DataHandler;

            // cvm is not thread safety, need to be improved
            cvm = colValueMap;
            connStr = string.Format(@"Server={0};Database={1};User Id={2};Password={3};charset=utf8;pooling=true", dbServer, dbName, username, password);

            MakeSureTableExists(dbName, tbName, createTableCommand);
        }

        // 检查指定表是否存在
        void MakeSureTableExists(string dbName, string tableName, string createTableCommand)
        {
            using (var conn = GetConn())
            {
                bool exists = false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format("use {0}", dbName);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = string.Format("show tables like '{0}';", tableName);
                    var r = cmd.ExecuteReader();
                    exists = r.Read();
                    r.Close();
                }

                if (!exists)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = createTableCommand;
                        cmd.ExecuteNonQuery();
                    }
                }

                conn.Close();
            }
        }

        // 已经完成等待回掉的操作
        ConcurrentQueue<Action> opsCompleted = new ConcurrentQueue<Action>();

        // 任务队列
        ConcurrentQueue<Action> tasks = new ConcurrentQueue<Action>();
        ConcurrentDictionary<Action, Action> tasksCallback = new ConcurrentDictionary<Action, Action>();
        bool runningTask = false;
        void RunTask(Action act, Action onCompleted = null)
        {
            tasks.Enqueue(act);
            tasksCallback[act] = onCompleted;

            if (runningTask)
                return;

            runningTask = true;
            Task.Run(() =>
            {
                Action a = null;
                runningTask = tasks.TryDequeue(out a);
                while (runningTask)
                {
                    Thread.Sleep(1);
                    Action cb = null;
                    tasksCallback.TryRemove(a, out cb);
                    a();
                    cb.SC();
                    runningTask = tasks.TryDequeue(out a);
                }
            });
        }

        #region 同步方法

        public int QueryInt(string cmdText, int defaultValue)
        {
            MySqlConnection conn = null;
            MySqlDataReader r = null;
            MySqlCommand cmd = null;

            using (conn = GetConn())
            {
                using (cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    r = cmd.ExecuteReader();
                    if (!r.Read())
                        return defaultValue;
                    else
                    {
                        var v = r.GetValue(0);
                        if (v is DBNull)
                            return defaultValue;

                        return r.GetInt32(0);
                    }
                }
            }
        }

        public void ExecuteRawReader(string cmdText, Action<MySqlDataReader> cb)
        {
            MySqlConnection conn = null;
            MySqlDataReader r = null;
            MySqlCommand cmd = null;

            using (conn = GetConn())
            {
                using (cmd = conn.CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    r = cmd.ExecuteReader();
                    cb(r);
                    r.Close();
                }
            }
        }

        #endregion

        #region 需要继承实现的部分

        // 保存新增的数据
        public void AddNew(T it)
        {
            string addCols = "";
            string addNamedPs = "";

            if (cols != null && cols.Length > 0)
            {
                foreach (string c in cols)
                {
                    object v = cvm(it, c);
                    if (v != null)
                    {
                        addCols += ", " + c;
                        addNamedPs += ", " + NamedParam(c);
                    }
                }
            }

            MySqlConnection conn = null;
            MySqlCommand cmd = null;
            var buff = data2Buff == null ? null : data2Buff(it);

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            var id = it.ID;


                            cmd.CommandText = string.Format(@"insert into {0} (ID, Data{1}) values ({2}, {3}{4})", tbName, addCols, NamedParam("ID"), NamedParam("Data"), addNamedPs);

                            MySqlParameter idParam = cmd.CreateParameter();
                            idParam.ParameterName = RealParam("ID");
                            idParam.DbType = GetDbType(id, "ID");
                            idParam.Value = id;
                            cmd.Parameters.Add(idParam);

                            MySqlParameter dataParam = cmd.CreateParameter();
                            dataParam.ParameterName = RealParam("Data");
                            dataParam.DbType = DbType.Binary;
                            dataParam.Value = buff;
                            cmd.Parameters.Add(dataParam);

                            if (cols != null && cols.Length > 0)
                            {
                                foreach (string c in cols)
                                {
                                    object v = cvm(it, c);
                                    if (v == null)
                                        continue;

                                    MySqlParameter p = cmd.CreateParameter();
                                    p.ParameterName = RealParam(c);
                                    p.DbType = GetDbType(v, c);
                                    p.Value = v;
                                    cmd.Parameters.Add(p);
                                }
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("\r\n==========\r\n" + (cmd == null ? "null" : cmd.CommandText) + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");
                    throw ex;
                }
                finally
                {
                    if (conn != null)
                        conn.Close();
                }
            });
        }

        // 保存数据
        public void Update(T it)
        {
            string addCols = "";
            if (cols != null && cols.Length > 0)
            {
                foreach (string c in cols)
                {
                    object v = cvm(it, c);
                    if (v != null)
                        addCols += ", " + c + "=" + NamedParam(c);
                }
            }

            MySqlConnection conn = null;
            MySqlCommand cmd = null;

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            var id = it.ID;
                            var buff = data2Buff(it);

                            cmd.CommandText = string.Format(@"update {0} set Data = {1}{2} where ID = {3}", tbName, NamedParam("Data"), addCols, NamedParam("ID"));

                            MySqlParameter idParam = cmd.CreateParameter();
                            idParam.ParameterName = RealParam("ID");
                            idParam.DbType = GetDbType(id, "ID");
                            idParam.Value = id;
                            cmd.Parameters.Add(idParam);

                            MySqlParameter dataParam = cmd.CreateParameter();
                            dataParam.ParameterName = RealParam("Data");
                            dataParam.DbType = DbType.Binary;
                            dataParam.Value = buff;
                            cmd.Parameters.Add(dataParam);

                            if (cols != null && cols.Length > 0)
                            {
                                foreach (string c in cols)
                                {
                                    object v = cvm(it, c);
                                    if (v == null)
                                        continue;

                                    IDbDataParameter p = cmd.CreateParameter();
                                    p.ParameterName = RealParam(c);
                                    p.DbType = GetDbType(v, c);
                                    p.Value = v;
                                    cmd.Parameters.Add(p);
                                }
                            }

                            cmd.ExecuteNonQuery();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("\r\n==========\r\n" + (cmd == null ? "null" : cmd.CommandText) + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");
                    throw ex;
                }
                finally
                {
                    if (conn != null)
                        conn.Close();
                }
            });
        }

        // 载入指定 id 对应的数据项
        public void Load(IDType id, Action<T> cb)
        {
            MySqlConnection conn = null;
            MySqlDataReader r = null;
            MySqlCommand cmd = null;
            byte[] data = null;

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = string.Format(@"select Data from {0} where ID = {1}", tbName, NamedParam("ID"));

                            MySqlParameter idParam = cmd.CreateParameter();
                            idParam.ParameterName = RealParam("ID");
                            idParam.DbType = GetDbType(id, "ID");
                            idParam.Value = id;
                            cmd.Parameters.Add(idParam);

                            r = cmd.ExecuteReader();
                            var dataObj = r.Read() ? r.GetValue(0) : null;
                            data = dataObj is DBNull ? null : (byte[])dataObj;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("\r\n==========\r\n" + (cmd == null ? "null" : cmd.CommandText) + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");
                    throw ex;
                }
                finally
                {
                    if (r != null)
                        r.Close();

                    conn.Close();
                }
            }, () =>
            {
                opsCompleted.Enqueue(() =>
                {
                    cb.SC(data == null ? null : buff2Data(data));
                });
            });
        }

        // 载入指定条件的数据项
        public void LoadAll(Action<T[]> cb, string whereClause = null)
        {
            MySqlConnection conn = null;
            MySqlDataReader r = null;
            MySqlCommand cmd = null;
            List<byte[]> data = new List<byte[]>();

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = whereClause != null ? 
                                string.Format(@"select Data from {0} {1}", tbName, whereClause)
                                : string.Format(@"select Data from {0}", tbName);

                            r = cmd.ExecuteReader();
                            var dataLst = new List<T>();
                            while (r.Read())
                            {
                                object dataObj = r.GetValue(0);
                                if (!(dataObj is DBNull))
                                    data.Add((byte[])dataObj);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logger != null)
                        Logger.Error("\r\n==========\r\n" + (cmd == null ? "null" : cmd.CommandText) + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");

                    throw ex;
                }
                finally
                {
                    if (r != null)
                        r.Close();

                    conn.Close();
                }
            }, () =>
            {
                opsCompleted.Enqueue(() =>
                {
                    var objLst = new List<T>();
                    foreach (var d in data)
                        objLst.Add(buff2Data(d));

                    cb.SC(objLst.ToArray());
                });
            });
        }

        // 载入指定条件数据的ID
        public void LoadAllID(Action<IDType[]> cb, string whereClause)
        {
            MySqlConnection conn = null;
            MySqlDataReader r = null;
            MySqlCommand cmd = null;
            var idLst = new List<IDType>();

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = string.Format(@"select ID from {0} {1}", tbName, whereClause);

                            r = cmd.ExecuteReader();

                            while (r.Read())
                            {
                                IDType uid = (IDType)r.GetValue(0);
                                idLst.Add(uid);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("\r\n==========\r\n" + cmd.CommandText + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");
                    throw ex;
                }
                finally
                {
                    if (r != null)
                        r.Close();

                    conn.Close();
                }
            }, () =>
            {
                opsCompleted.Enqueue(() =>
                {
                    cb.SC(idLst.ToArray());
                });
            });
        }

        // 删除指定 id 对应数据
        public void Delete(IDType id)
        {
            MySqlConnection conn = null;
            MySqlCommand cmd = null;

            RunTask(() =>
            {
                try
                {
                    using (conn = GetConn())
                    {
                        using (cmd = conn.CreateCommand())
                        {

                            cmd.CommandText = string.Format(@"delete from {0} where where ID = {1}", tbName, NamedParam("ID"));

                            MySqlParameter idParam = cmd.CreateParameter();
                            idParam.ParameterName = RealParam("ID");
                            idParam.DbType = GetDbType(id, "ID");
                            idParam.Value = id;
                            cmd.Parameters.Add(idParam);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("\r\n==========\r\n" + (cmd == null ? "null" : cmd.CommandText) + "\r\n" + ex.Message + "\r\n==========\r\n" + ex.StackTrace + "\r\n==========\r\n");
                    throw ex;
                }
                finally
                {
                    conn.Close();
                }
            });
        }

        // 处理等待的回调操作
        public int ProcessPendingCallback()
        {
            Action cb = null;
            while (opsCompleted.TryDequeue(out cb))
                cb.SC();

            return opsCompleted.Count;
        }

        // 等待处理所有操作
        public void Wait4CompoleteAll()
        {
            while (runningTask)
                Thread.Sleep(100);

            while (ProcessPendingCallback() > 0)
                Thread.Sleep(100);
        }

        #endregion

        #region 保护部分

        // 连接字串
        protected string connStr = null;

        // 根据字段名取存放到数据库中的对应值
        protected Func<T, string, object> cvm = null;

        // 额外的列
        protected string[] cols = null;

        // 数据库连接
        public MySqlConnection GetConn()
        {
            var conn = new MySqlConnection(connStr);
            conn.Open();
            return conn;
        }

        // 包装形参名
        protected string NamedParam(string name)
        {
            return "?" + name;
        }

        // 包装实参名
        protected string RealParam(string name)
        {
            return name;
        }

        // 数据表名
        protected string tbName = null;

        // 获取对应的 MySql 类型
        public static DbType GetDbType(object d, string paraName)
        {
            if (d == null)
                throw new Exception("unsupported null type by GetDbType: " + paraName);
            else if (d is bool)
                return DbType.Boolean;
            else if (d is string)
                return DbType.String;
            else if (d is DateTime)
                return DbType.DateTime;
            else if (d is float)
                return DbType.Single;
            else if (d is Int16)
                return DbType.Int16;
            else if (d is Int32)
                return DbType.Int32;
            else if (d is Int64)
                return DbType.Int64;
            else if (d is UInt16)
                return DbType.UInt16;
            else if (d is UInt32)
                return DbType.UInt32;
            else if (d is UInt64)
                return DbType.UInt64;
            else if (d is byte)
                return DbType.Byte;
            else if (d is byte[])
                return DbType.Binary;
            else
                throw new Exception("unsupported type: " + paraName + ":" + d.GetType().Name);
        }

        // 获取对应的 MySql 类型名称
        public static string GetDbTypeName(Type t, string paraName)
        {
            if (t == null)
                throw new Exception("unsupported null type by GetDbTypeName: " + paraName);
            else if (t == typeof(bool)) // if (d is bool)
                return "Bool";
            else if (t == typeof(string)) // if (d is string)
                return "Blob";
            else if (t == typeof(DateTime)) // if (d is DateTime)
                return "DateTime";
            else if (t == typeof(float)) // else if (d is float)
                return "Float";
            else if (t == typeof(Int16)) // else if (d is Int16)
                return "SmallInt";
            else if (t == typeof(Int32)) // else if (d is Int32)
                return "Int";
            else if (t == typeof(Int64)) // else if (d is Int64)
                return "BigInt";
            else if (t == typeof(Int16)) // else if (d is UInt16)
                return "SmallInt";
            else if (t == typeof(UInt32)) // else if (d is UInt32)
                return "Int";
            else if (t == typeof(UInt64)) // else if (d == typeof(UInt64)) // else if (d is UInt64)
                return "BigInt";
            else if (t == typeof(byte)) // else if (d is byte)
                return "TinyInt";
            else if (t == typeof(byte[])) // else if (d is byte[])
                return "Blob";
            else
                throw new Exception("unsupported type: " + paraName + ":" + t.Name);
        }

        #endregion
    }
}
