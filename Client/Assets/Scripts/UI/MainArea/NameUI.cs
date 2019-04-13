using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCM;
using Swift;

public class NameUI : UIBase
{
    public Text NameTxt;

    public MainMenuUI MMUI;

    public override void Show()
    {
        base.Show();

        NameTxt.text = "";
    }

    public void OnCancelBtn()
    {
        NameTxt.text = "";
        Hide();
    }

    public void OnConfirmBtn()
    {
        var name = NameTxt.text;

        if (name.Contains(".") || name.Contains("&"))
        {
            AddTip("名称包含非法字符");
            return;
        }

        var meInfo = GameCore.Instance.MeInfo;
        meInfo.Name = name;
        UserManager.SyncName2Server();

        MMUI.ShowUserInfo();
        Hide();
    }
}