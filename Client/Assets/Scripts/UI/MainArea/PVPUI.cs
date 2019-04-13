using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCM;
using System;
using Swift;

public class PVPUI : UIBase
{
    public Action onMatchingState = null;
    public Action onNormalState = null;

    // 匹配操作相关
    public Button PVPBtn;
    public Button PVEBtn;
    public Button PVPCancelBtn;
    public Button PVECancelBtn;
    public Button MatchingTipBtn;

    // 匹配特效相关
    public GameObject Dot1;
    public GameObject Dot2;
    public GameObject Dot3;
    public GameObject MapAni;

    public Button TipBtn;
    
    // Other
    public GameObject MainMenuUI;
    public BattleReadyUI BRUI;

    // 小提示点击效果相关
    private float tipEffectTime = 0.3f;
    private float tipEffectElapsedTime = 0f;
    private bool isShowTipEffect = false;
    private int TipLastId = -1;

    // 匹配特效相关
    private bool isShow = false;
    private bool isChange = false;
    private float elapsedTime = 0f;
    private float changeTime = 0.3f;

    ServerPort sp;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;
        GameCore.Instance.OnMainConnectionDisconnected += OnMainConnectionDisconnected;

        sp = GameCore.Instance.Get<ServerPort>();
        sp.OnMessage("BattleReady", OnBattleReady);
    }

    public override void Show()
    {
        base.Show();
        Change2NormalState(true);
    }

    private void OnMainConnectionDisconnected(Swift.Connection arg1, string arg2)
    {
        Change2NormalState(false);
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        Change2NormalState(false);
    }

    void OnBattleReady(IReadableBuffer data)
    {
        var infos = data.ReadArr<UserInfo>();

        BRUI.Show(infos);
    }

    // 匹配电脑
    public void OnSelPVE()
    {
        // 发送匹配请求
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Send2Srv("PVEMatchIn");
        conn.End(buff);

        Change2MatchingState("PVE");
    }

    // 取消匹配电脑
    public void OnCancelPVE()
    {
        // 发送匹配请求
        var conn = GameCore.Instance.ServerConnection;
        conn.End(conn.Request2Srv("CancelPVEMatchIn", (data) =>
        {
            var canceled = data.ReadBool();

            Change2NormalState(true);
        }));
    }

    // 匹配对手
    public void OnSelPVP()
    {
        // 发送匹配请求
        var conn = GameCore.Instance.ServerConnection;
        var buff = conn.Send2Srv("MatchIn");
        conn.End(buff);

        Change2MatchingState("PVP");
    }

    // 取消匹配对手
    public void OnCancelPVP()
    {
        // 发送匹配请求
        var conn = GameCore.Instance.ServerConnection;
        conn.End(conn.Request2Srv("CancelMatchIn", (data) =>
        {
            var canceled = data.ReadBool();

            Change2NormalState(true);
        }, (bool connected) => { Change2NormalState(true); }));
    }

    // 匹配状态...
    private void Change2MatchingState(string type)
    {
        PVEBtn.gameObject.SetActive(false);
        PVPBtn.gameObject.SetActive(false);

        switch (type)
        {
            case "PVE":
                PVECancelBtn.gameObject.SetActive(true);
                PVPCancelBtn.gameObject.SetActive(false);
                break;
            case "PVP":
                PVECancelBtn.gameObject.SetActive(false);
                PVPCancelBtn.gameObject.SetActive(true);
                MatchingTipBtn.gameObject.SetActive(true);
                break;
        }

        // Matching特效相关
        isChange = true;
        MapAni.SetActive(true);

        //// 主菜单
        //MainMenuUI.gameObject.SetActive(false);

        TipBtn.gameObject.SetActive(true);
        OnTipBtn();

        if (onMatchingState != null)
            onMatchingState();
    }

    // 正常状态...
    private void Change2NormalState(bool isShowMenuUI)
    {
        PVEBtn.gameObject.SetActive(true);
        PVPBtn.gameObject.SetActive(true);
        PVECancelBtn.gameObject.SetActive(false);
        PVPCancelBtn.gameObject.SetActive(false);

        // Matching特效相关
        isChange = false;
        SetImageAlpha(Dot1, 1);
        SetImageAlpha(Dot2, 1);
        SetImageAlpha(Dot3, 1);
        MapAni.SetActive(false);

        //// 主菜单
        //MainMenuUI.gameObject.SetActive(isShowMenuUI);

        // Other
        MatchingTipBtn.gameObject.SetActive(false);

        TipBtn.gameObject.SetActive(false);

        if (onNormalState != null)
            onNormalState();
    }

    public void OnMatchingTipBtn()
    {
        // 链接到Q群
        Application.OpenURL("https://jq.qq.com/?_wv=1027&k=5CNjLqc");
    }

    public void OnTipBtn()
    {
        isShowTipEffect = true;
        tipEffectElapsedTime = 0f;

        var tips = TipConfiguration.AllTips;

        if (tips.Length == 0)
            return;

        int id = UnityEngine.Random.Range(0, tips.Length);

        if (id == TipLastId)
        {
            OnTipBtn();
            return;
        }

        TipLastId = id;
        TipBtn.transform.Find("Text").GetComponent<Text>().text = TipConfiguration.GetDefaultConfig(id);
    }

    // 匹配效果相关
    private void AlphaValueTo1(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        Color c = img.color;
        c.a = Mathf.Lerp(0, 1, elapsedTime / changeTime);
        img.color = c;
    }

    private void AlphaValueTo0(GameObject go)
    {
        Image img = go.GetComponent<Image>();
        Color c = img.color;
        c.a = Mathf.Lerp(1, 0, elapsedTime / changeTime);
        img.color = c;
    }

    private void SetImageAlpha(GameObject go, float value)
    {
        Image img = go.GetComponent<Image>();
        Color c = img.color;
        c.a = 1;
        img.color = c;
    }

    private void Update()
    {
        if (isChange)
        {
            elapsedTime += Time.deltaTime;

            if (isShow)
            {
                if (elapsedTime >= changeTime)
                {
                    elapsedTime = changeTime;
                    isShow = !isShow; 
                }

                AlphaValueTo1(Dot1);
                AlphaValueTo0(Dot2);
                AlphaValueTo0(Dot3);

                if (!isShow)
                    elapsedTime = 0f;
            }
            else
            {
                if (elapsedTime >= changeTime)
                {
                    elapsedTime = changeTime;
                    isShow = !isShow; 
                }

                AlphaValueTo0(Dot1);
                AlphaValueTo1(Dot2);
                AlphaValueTo1(Dot3);

                if (isShow)
                    elapsedTime = 0f;
            }
        }

        if (isShowTipEffect)
        {
            tipEffectElapsedTime += Time.deltaTime;

            if (tipEffectElapsedTime >= tipEffectTime)
            {
                tipEffectElapsedTime = tipEffectTime;
                isShowTipEffect = false;
            }

            TipBtn.transform.Find("Text").GetComponent<Text>().fontSize = Mathf.FloorToInt(Mathf.Lerp(0.0f, 30.0f, tipEffectElapsedTime / tipEffectTime));
        }
    }
}