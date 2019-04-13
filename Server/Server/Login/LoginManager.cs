using System;
using System.Collections.Generic;
using Swift;
using SCM;
using Swift.Math;

namespace Server
{
    public class LoginInfo
    {
        public string UserID;
        public string Name;
        public string IP;
        public bool FirstLogin;

        public LoginInfo(string uid, string name, string ip, bool first)
        {
            UserID = uid;
            Name = name;
            IP = ip;
            FirstLogin = first;
        }
    }

    /// <summary>
    /// 登录管理器
    /// </summary>
    public class LoginManager : Component
    {
        SessionContainer SC;
        UserPort UP;
        UserContainer UC;
        QuestionnaireResultContainer QRC;

        // 用户登录
        public Action<Session, bool> BeforeUserLogin = null;
        public Action<Session, bool> OnUserLogin = null;

        // 用户连接断开
        public Action<Session> OnUserDisconnecting = null;

        // 登录日志
        ServerBusinessLogger<LoginInfo> SrvLogger = null;

        // 初始化
        public override void Init()
        {
            SC = GetCom<SessionContainer>();
            UP = GetCom<UserPort>();
            UC = GetCom<UserContainer>();
            var nc = GetCom<NetCore>();
            nc.OnDisconnected += OnDisconnected;

            UP.OnRequest("Login", OnUserLoginMsg);

            // 登录日志
            SrvLogger = GetCom<ServerBusinessLogger<LoginInfo>>();

            //  问卷
            QRC = GetCom<QuestionnaireResultContainer>();
        }

        // 连接断开
        void OnDisconnected(Connection conn, string reason)
        {
            var s = SC.GetByConn(conn);
            if (s == null)
                return;

            KickOut(s.ID);
        }

        string SrvVersion = "1.0";
        string SrvBuildNo = "002"; 

        // 用户登录请求
        void OnUserLoginMsg(Connection conn, IReadableBuffer data, IWriteableBuffer buff, Action end)
        {
            var uid = data.ReadString();

            var deviceModel = "";

            // 检查版本

            var isNewVersion = false;
            var version = "";
            var buildNo = "";
            var platform = "";

            if (data.Available != 0)
            {
                version = data.ReadString();
                platform = data.ReadString();
            }

            if (data.Available != 0)
            {
                deviceModel = data.ReadString();
                buildNo = data.ReadString();
            }

            isNewVersion = (version == SrvVersion) && (buildNo == SrvBuildNo);

            buff.Write(isNewVersion);
            if (!isNewVersion)
            {
                if (platform == "IOS")
                    buff.Write("https://www.apple.com");
                else if (platform == "ANDROID")
                    buff.Write("https://www.google.com");
                else
                    buff.Write("https://www.baidu.com");

                end();
                return;
            }

            UC.Retrieve(uid, (usr) =>
            {
                if (SC[uid] != null)
                {
                    KickOut(uid);
                    end();
                    return;
                }

                var isNew = usr == null;
                if (isNew) // 用户不存在就创建新的
                {
                    usr = new User();
                    usr.ID = uid;
                    usr.Info = new UserInfo();
                    usr.Info.DeviceModel = deviceModel;
                    UC.AddNew(usr);

                    UserManager.SetDefaultInfo(usr);
                }

                // 创建会话
                var s = new Session();
                s.Usr = usr;
                s.Conn = conn;
                SC[uid] = s;

                // 登录日志
                SrvLogger.Log(new LoginInfo(uid, usr.Info.Name, conn.GetIP(), isNew));

                BeforeUserLogin.SC(s, isNew);

                // 通知登录成功
                buff.Write(true);
                buff.Write(usr.Info);

                // 问卷调查

                var totalCount = usr.Info.WinCount + usr.Info.LoseCount;
                if (totalCount >= 1 && totalCount < 5)
                {
                    buff.Write("1");
                    end();
                    OnUserLogin.SC(s, isNew);
                }
                else if (totalCount >= 5)
                {
                     QRC.Retrieve("1" + s.Usr.ID, (questionnaire) =>
                     {
                         if (questionnaire == null)
                         {
                             buff.Write("1");
                             end();
                             OnUserLogin.SC(s, isNew);
                         }
                         else
                         {
                             buff.Write("2");
                             end();
                             OnUserLogin.SC(s, isNew);
                         }
                     });
                }
                else
                {
                    buff.Write("0");
                    end();
                    OnUserLogin.SC(s, isNew);
                }
            });
        }

        // 踢掉用户，断开连接
        void KickOut(string uid)
        {
            var s = SC[uid];
            if (s == null)
                return;

            OnUserDisconnecting.SC(s);

            SC.Remove(uid);

            var conn = s.Conn;
            if (conn == null)
                return;

            conn.Close();
        }
    }
}
