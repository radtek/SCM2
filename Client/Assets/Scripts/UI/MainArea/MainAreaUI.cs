using SCM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainAreaUIPage
{
    public const int DescPage = -1;
    public const int PVPPage = 0;
    public const int ReplayPage = 1;
}

public class MainAreaUI : UIBase
{
    public UIBase PVPUI;
    public UIBase CardUI;
    public UIBase ReplayListUI;

    // UI切换效果相关
    private float scrollTime = 0.3f;
    private float elapsedTime = 0f;
    private bool isScroll = false;
    private Vector2 srcOffsetMin;
    private Vector2 srcOffsetMax;
    private Vector2 dstOffsetMin;
    private Vector2 dstOffsetMax;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;
        UnitConfigUtil.OnBuildUnitCfgsFromServer += ShowCardUI;

        ShowAll();
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        Hide();
    }

    private void ShowAll()
    {
        PVPUI.Show();
        CardUI.Show();
        ReplayListUI.Show();
    }

    private void ShowCardUI()
    {
        CardUI.Show();
    }

    public void Scroll(int dstPage)
    {
        isScroll = true;
        elapsedTime = 0f;

        dstOffsetMin = srcOffsetMin = GetComponent<RectTransform>().offsetMin;
        dstOffsetMax = srcOffsetMax = GetComponent<RectTransform>().offsetMax;

        dstOffsetMin.x = -dstPage * 640;
        dstOffsetMax.x = -dstPage * 640;
    }

    private void Update()
    {
        if (isScroll)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= scrollTime)
            {
                elapsedTime = scrollTime;
                isScroll = false;
            }

            GetComponent<RectTransform>().offsetMin = Vector2.Lerp(srcOffsetMin, dstOffsetMin, elapsedTime / scrollTime);
            GetComponent<RectTransform>().offsetMax = Vector2.Lerp(srcOffsetMax, dstOffsetMax, elapsedTime / scrollTime);
        }
    }
}
