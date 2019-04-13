using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using System;
using System.Net.Sockets;

public class LoginUI : UIBase
{
    public InputField SrvAddr;
    public Text Tips;
    public Text DescTxt;
    public GameObject DownloadBtn;
    public GameObject LoginBtn;
    public QuestionnaireUI QAUI;

    private string platform = "";
    private string downloadURL = "";

    protected override void Starting()
    {
#if UNITY_EDITOR
        var ip = "127.0.0.1";
#else
        var ip = "119.23.110.78";
#endif
        var srvAddr = PlayerPrefs.GetString("ServerAddress", ip);
        SrvAddr.text = srvAddr;

        DescTxt.text = string.Format("<color=#00ff00ff>《星际对抗》</color>{0}版\n\n意见或建议请 qq 群反馈。\n<color=#00ff00ff>qq 水群：485938613</color>", Application.version);
    }

    public override void Show()
    {
        base.Show();
        SetTips("");
    }

    // 执行登录操作
    public void OnLogin()
    {
        var srvAddr = SrvAddr.text;
        var srvPort = 9530;
        var addrFamily = AddressFamily.InterNetwork;
        var acc = SystemInfo.deviceUniqueIdentifier;
        var version = Application.version;
        var deviceModel = SystemInfo.deviceModel.ToString();
        var buildNo = "002";
        GetPlatform();

    #if UNITY_IPHONE && !UNITY_EDITOR
        IPv6SupportMidleware.getIPType(srvAddr, srvPort.ToString(), out srvAddr, out addrFamily);
    #endif

        // 连接服务器
        var gc = GameCore.Instance;
        var nc = gc.Get<NetCore>();
        SetTips("连接服务器 ...");
        nc.Connect2Peer(srvAddr, srvPort, (conn, reason) =>
        {
            if (conn == null)
                SetTips(reason);
            else
            {
                gc.ServerConnection = conn;

                // 登录
                SetTips("请求登录 ...");
                var buff = conn.Request2Srv("Login", (data) =>
                {
                    var isNewVersion = data.ReadBool();
                    if (!isNewVersion)
                    {
                        downloadURL = data.ReadString();

                        SetTips("请更新到最新版本！");
                        DownloadBtn.SetActive(true);
                        LoginBtn.SetActive(false);
                        return;
                    }
                    else
                    {
                        DownloadBtn.SetActive(false);
                        LoginBtn.SetActive(true);
                    }

                    var ok = data.ReadBool();
                    if (ok)
                    {
                        PlayerPrefs.SetString("ServerAddress", srvAddr);

                        SetTips("登录成功");
                        GameCore.Instance.MeID = acc;
                        GameCore.Instance.MeInfo = data.Read<UserInfo>();
                        Hide();
                        UIManager.Instance.ShowTopUI("MainMenu", true);
                        UIManager.Instance.ShowTopUI("MainArea", true);

                        UnitConfigUtil.GetUnitCfgsFromServer();
                        UserManager.SyncAvatarsFromCfg();

                        var qName = data.ReadString();

                        if (!string.IsNullOrEmpty(qName))
                        {
                            QAUI.OnGetQuestionnaire(qName);
                        }
//                        StaticSoundMgr.Instance.PlaySound("Login");
                    }
                    else
                    {
                        SetTips("登录失败");
                        conn.Close();
                    }
                    
                }, (conntected) =>
                {
                    SetTips("登录超时");
                    if (conntected)
                        conn.Close();
                });

                buff.Write(acc);
                buff.Write(version);
                buff.Write(platform);
                buff.Write(deviceModel);
                buff.Write(buildNo);
                conn.End(buff);
            }
        }, addrFamily);
    }

    public void OnDownloadBtn()
    {
        Application.OpenURL(downloadURL);
    }

    void GetPlatform()
    {
        #if UNITY_IOS
        platform = "IOS";
        #endif

        #if UNITY_ANDROID
        platform = "ANDROID";
        #endif
    }

    void SetTips(string tips)
    {
        Tips.text = tips;
    }
}
