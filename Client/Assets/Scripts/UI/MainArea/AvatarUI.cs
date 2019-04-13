using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCM;
using Swift;

public class AvatarUI : UIBase
{
    public GameObject Item;
    public Transform Content;

    private List<string> lockLst;
    private List<string> unLockLst;

    public MainMenuUI MMUI;

    public override void Show()
    {
        base.Show();

        BuildItemList();
        BuildAllItem();
    }

    public void OnExitBtn()
    {
        Hide();
    }

    public void OnClickLockBtn()
    {
        UIManager.Instance.Tips.AddTip("每次比赛取得胜利均有几率解锁一个随机头像", 28);
    }

    private void OnValueChanged(GameObject go, bool isOn)
    {
        if (!isOn)
        {
            go.transform.Find("Mark").gameObject.SetActive(false);
            return;
        }
        else
        {
            go.transform.Find("Mark").gameObject.SetActive(true);

            // 修改头像
            var meInfo = GameCore.Instance.MeInfo;
            meInfo.CurAvator = go.name;

            // 同步服务器
            UserManager.SyncCurAvatar2Server();

            // 表现
            MMUI.ShowUserInfo();
        }
    }

    private GameObject CreateItem(string name)
    {
        var go = Instantiate(Item) as GameObject;
        go.SetActive(true);
        go.name = name;
        go.transform.SetParent(Content);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        go.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => OnValueChanged(go, isOn));

        return go;
    }

    private void SetItemInfo(GameObject go, bool isLock)
    {
        Sprite img = Resources.Load<Sprite>(@"Texture\AvatarUI\" + go.name);

        if (null != img)
            go.transform.Find("Icon").GetComponent<Image>().sprite = img;
        else
            go.transform.Find("Icon").gameObject.SetActive(false);

        go.transform.Find("Lock").gameObject.SetActive(isLock);

        go.GetComponent<Toggle>().enabled = !isLock;
    }

    private void BuildItemList()
    {
        lockLst = new List<string>();
        unLockLst = new List<string>();

        var lst = AvatarConfiguration.Cfgs;
        var meInfo = GameCore.Instance.MeInfo;

        for (int i = 0; i < lst.Count; i++)
        {
            if (meInfo.Avatars[lst[i]])
                unLockLst.Add(lst[i]);
            else
                lockLst.Add(lst[i]);
        }
    }

    private void BuildAllItem()
    {
        ClearContent(Content);

        foreach (var para in unLockLst)
        {
            var go = CreateItem(para);
            SetItemInfo(go, false);

            if (para == GameCore.Instance.MeInfo.CurAvator)
                go.transform.GetComponent<Toggle>().isOn = true;
        }

        foreach (var para in lockLst)
        {
            var go = CreateItem(para);
            SetItemInfo(go, true);
        }
    }

    private void ClearContent(Transform content)
    {
        foreach (Transform trans in content)
            Destroy(trans.gameObject);
    }
}