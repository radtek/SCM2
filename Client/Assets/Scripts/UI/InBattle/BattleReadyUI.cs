using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCM;
using Swift;

public class BattleReadyUI : UIBase
{
    public RectTransform UserInfo1;
    public RectTransform UserInfo2;

    public Image UserAvatars1;
    public Image UserAvatars2;

    public Text UserName1;
    public Text UserName2;

    public Text UserRate1;
    public Text UserRate2;

    public GameObject Item;
    public Transform Content1;
    public Transform Content2;

    private Vector2 srcUserPos1;
    private Vector2 srcUserPos2;
    private Vector2 dstUserPos1;
    private Vector2 dstUserPos2;

    private float scrollTime = 0.5f;
    private float elapsedTime = 0f;
    private bool isScroll = false;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;

        srcUserPos1 = new Vector2(480, 150);
        srcUserPos2 = new Vector2(-480, 150);
        dstUserPos1 = new Vector2(-160, 150);
        dstUserPos2 = new Vector2(160, 150);
    }

    public void Show(UserInfo[] infos)
    {
        base.Show();

        Clear();
        ResetPos();

        UserName1.text = infos[0].Name;
        UserName2.text = infos[1].Name;

        var total1 = infos[0].WinCount + infos[0].LoseCount;
        var total2 = infos[1].WinCount + infos[1].LoseCount;

        UserRate1.text = SCMText.T("胜率") + string.Format("：{0}", total1 <= 0 ? " - " : (int)(infos[0].WinCount * 100 / total1) + "%");
        UserRate2.text = SCMText.T("胜率") + string.Format("：{0}", total2 <= 0 ? " - " : (int)(infos[1].WinCount * 100 / total2) + "%");

        SetAvatarInfo(infos);
        BuildAllItems(infos);

        isScroll = true;
        elapsedTime = 0;
    }

    private void SetAvatarInfo(UserInfo[] infos)
    {
        var name1 = infos[0].CurAvator;
        var name2 = infos[1].CurAvator;

        Sprite img1 = Resources.Load<Sprite>(@"Texture\AvatarUI\" + name1);
        Sprite img2 = Resources.Load<Sprite>(@"Texture\AvatarUI\" + name2);

        if (null != img1)
        {
            UserAvatars1.sprite = img1;
            UserAvatars1.SetNativeSize();
        }

        if (null != img2)
        {
            UserAvatars2.sprite = img2;
            UserAvatars2.SetNativeSize();
        }
    }

    private void BuildAllItems(UserInfo[] infos)
    {
        var info1 = infos[0];
        var info2 = infos[1];

        BuildItems(info1, Content1);
        BuildItems(info2, Content2);
    }

    private void BuildItems(UserInfo info, Transform content)
    {
        var variants = info.Variants;

        foreach (var v in variants)
        {
            var cfg = UnitConfiguration.GetDefaultConfig(v.Key);
            var isUnlock = info.Units[v.Key];

            if (v.Key == "Radar")
                continue;

            if (isUnlock && !cfg.IsBuilding && !cfg.NoCard)
            {
                var go = CreateItem(content);

                ShowItemInfo(go, v.Value);
            }
        }

        // 守卫
        if (info.Units["FireGuard"])
        {
            var vType = info.Variants["FireGuard"];

            var go = CreateItem(content);
            ShowItemInfo(go, vType);
        }
    }

    private GameObject CreateItem(Transform content)
    {
        var go = Instantiate(Item) as GameObject;
        go.SetActive(true);
        go.transform.SetParent(content);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go;
    }

    private void ShowItemInfo(GameObject go, string type)
    {
        var IconItem = go.transform.Find("Icon");
        Sprite img = Resources.Load<Sprite>(@"Texture\UnitCard\" + type);

        if (IconItem == null)
            return;

        if (img == null)
        {
            IconItem.gameObject.SetActive(false);
            return;
        }

        IconItem.gameObject.SetActive(true);
        IconItem.GetComponent<Image>().sprite = img;
        IconItem.GetComponent<Image>().SetNativeSize();
    }

    private void ResetPos()
    {
        UserInfo1.anchoredPosition = Vector2.zero;
        UserInfo2.anchoredPosition = Vector2.zero;
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        Hide();
    }

    private void Clear()
    {
        UserName1.text = "";
        UserName2.text = "";
        UserRate1.text = "";
        UserRate2.text = "";

        ContentClear(Content1);
        ContentClear(Content2);
    }

    private void ContentClear(Transform content)
    {
        foreach (Transform tran in content)
            Destroy(tran.gameObject);
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

            UserInfo1.anchoredPosition = Vector2.Lerp(srcUserPos1, dstUserPos1, elapsedTime / scrollTime);
            UserInfo2.anchoredPosition = Vector2.Lerp(srcUserPos2, dstUserPos2, elapsedTime / scrollTime);
        }
    }
}