using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class SelectUnitUI : UIBase {

    public Action OnHide = null;

    public Transform SelectArea;

    public Func<string, bool> IsChoiceValid;
    public Action<string> OnChoiceSel;

    public string[] Choices // 候选项
    {
        get
        {
            return choices;
        }
        set
        {
            choices = value;
        }
    } string[] choices = null;

    public string[] ChoicesName = null; // 候选项的显示文字

    private Dictionary<string, GameObject> UnitDic = new Dictionary<string, GameObject>();

    public GameObject ChoiceBtn = null; // 选择按钮
    public GameObject NewChoiceBtn = null; // 选择按钮
    List<GameObject> choiceBtns = new List<GameObject>();

    // 建造位置
    public Vec2 Pos
    {
        get { return pos; }
        set
        {
            pos = value;
            SelectArea.transform.localPosition = new Vector3((float)pos.x, (float)pos.y, 0);
        }
    } Vec2 pos;
	
    // 点击空白处
    public void OnSelectNothing()
    {
        Hide();
    }

    public override void Hide()
    {
        foreach (var btn in choiceBtns)
            Destroy(btn.gameObject);

        choiceBtns.Clear();
        UnitDic.Clear();
        base.Hide();

        if (OnHide != null)
            OnHide();
    }

    // 点击某个选项
    void OnSelectOne(string choice)
    {
        if (IsChoiceValid != null && !IsChoiceValid(choice))
            return;

        Hide();
        OnChoiceSel.SC(choice);
    }

    private GameObject SelChoiceBtn(string choice)
    {
        if (choice == "DestroyBuilding" || choice == "Confirm" || choice == "Cancel")
            return NewChoiceBtn;

        return ChoiceBtn;
    }

    // 重新排列所有选项
    public void Refresh()
    {
        // 销毁原有的按钮
        foreach (var btn in choiceBtns)
            Destroy(btn.gameObject);

        // 创建新按钮
        FC.For(choices.Length, (i) =>
        {
            var go = Instantiate(SelChoiceBtn(Choices[i]));
            go.transform.SetParent(SelectArea.transform);
            //go.GetComponentInChildren<Text>().text = ChoicesName[i];

            if (!(Choices[i] == "DestroyBuilding" || Choices[i] == "Confirm" || Choices[i] == "Cancel"))
            {
                UnitDic.Add(Choices[i], go);
                ShowUnitInfo(Choices[i], go);
            }

            var name = go.transform.Find("Name");
            name.GetComponent<Text>().text = ChoicesName[i];

            if (Choices[i] == "DestroyBuilding")
                name.GetComponent<Text>().color = Color.red;

            var choiceTag = choices[i];
            go.GetComponent<Button>().onClick.AddListener(() => { OnSelectOne(choiceTag); });
            go.SetActive(true);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            choiceBtns.Add(go);
        });

        // 重排列所有按钮
        var cnt = choiceBtns.Count;

        if (cnt == 0)
        {
            Hide();
            return;
        }

        // 菜单显示尺寸
        var w = choiceBtns[0].GetComponent<RectTransform>().rect.width;
        var h = choiceBtns[0].GetComponent<RectTransform>().rect.height;
        if (cnt == 1)
            choiceBtns[0].transform.localPosition = Vector3.zero;
        else if (cnt == 2)
        {
            choiceBtns[0].transform.localPosition = new Vector3(-w * 0.75f, 0, 0);
            choiceBtns[1].transform.localPosition = new Vector3(w * 0.75f, 0, 0);
            w *= 2.5f;
        }
        else if (cnt == 3)
        {
            choiceBtns[0].transform.localPosition = new Vector3(-w * 1.25f, 0, 0);
            choiceBtns[1].transform.localPosition = new Vector3(0, 0, 0);
            choiceBtns[2].transform.localPosition = new Vector3(w * 1.25f, 0, 0);
            w *= 3.75f;
        }
        else if (cnt < 6)
        {
            FC.For(cnt, (i) =>
            {
                var y = i < 3 ? h / 2 : -h / 2;
                var x = (i % 3) * w - w;
                choiceBtns[i].transform.localPosition = new Vector3(x, y, 0);
            });

            w *= 3;
            h *= 2;
        }
        else
        {
            FC.For(cnt, (i) =>
            {
                var y = i < 3 ? h : (i < 6 ? 0 : -h);
                var x = (i % 3) * w - w;
                choiceBtns[i].transform.localPosition = new Vector3(x, y, 0);
            });

            w *= 3;
            h *= 3;
        }

        // 检查边界调整位置
        var parentArea = SelectArea.parent.GetComponent<RectTransform>().rect;
        var uiPos = SelectArea.transform.localPosition + Vector3.up * 40f;
        var uiPosX = uiPos.x.Clamp(w / 2, parentArea.width - w / 2);
        var uiPosY = uiPos.y.Clamp(h / 2, parentArea.height - h / 2);
        SelectArea.transform.localPosition = new Vector3(uiPosX, uiPosY, 0);
    }


    private void ShowUnitInfo(string type, GameObject go)
    {
        var name = go.transform.Find("Name");
        var icon = go.transform.Find("Icon");
        var cost = go.transform.Find("Cost");
        var gasCost = go.transform.Find("GasCost");

        Sprite img = Resources.Load<Sprite>(@"Texture\UnitCard\" + type);

        if (null != img)
        {
            name.gameObject.SetActive(true);
            icon.gameObject.SetActive(true);
            icon.GetComponent<Image>().sprite = img;
            icon.GetComponent<Image>().SetNativeSize();
        }
        else
        {
            name.gameObject.SetActive(true);
            icon.gameObject.SetActive(false);
        }

        var cfg = UnitConfiguration.GetDefaultConfig(type);

        if (cfg == null)
            return;

        var myMoney = GameCore.Instance.GetMyResource("Money");
        var myGas = GameCore.Instance.GetMyResource("Gas");

        var costTxt = myMoney >= cfg.Cost ? "<color=#00D8FFFF>" + cfg.Cost + "</color>" : "<color=red>" + cfg.Cost + "</color>";
        var gasCostTxt = myGas >= cfg.GasCost ? "<color=#10EE25FF>" + cfg.GasCost + "</color>" : "<color=red>" + cfg.GasCost + "</color>";

        cost.GetComponent<Text>().text = costTxt;
        gasCost.GetComponent<Text>().text = gasCostTxt;
    }

    private void ShowAllUnitInfo()
    {
        if (UnitDic == null)
            return;

        foreach (var para in UnitDic)
        {
            ShowUnitInfo(para.Key, para.Value);
        }
    }

    public void Update()
    {
        ShowAllUnitInfo();
    }
}
