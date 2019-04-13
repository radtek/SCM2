using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using Swift.Math;

public class UnitPosSelUI : UIBase
{
    public Action OnHide = null;

    public Transform SelectArea;

    public Func<string, bool> IsChoiceValid;
    public Action<string> OnChoiceSel;

    public MapGround MG;

    private float width;
    private float height;

    private Vec2 nowPos;
    private Vec2 nowWPos;

    protected override void StartOnlyOneTime()
    {
        OnHide += MG.DestroyExampleUnit;

        Choices = new string[] { "Confirm", "Cancel" };
        ChoicesName = new string[] { "是", "否" };
        Build();
    }

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

    public GameObject ChoiceBtn = null; // 选择按钮
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

    public override void Hide()
    {
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
        
    public void Build()
    {
        // 创建新按钮
        FC.For(choices.Length, (i) =>
        {
            var go = Instantiate(ChoiceBtn);
            go.transform.SetParent(SelectArea.transform);
            go.GetComponentInChildren<Text>().text = ChoicesName[i];
            var choiceTag = choices[i];
            go.GetComponent<Button>().onClick.AddListener(() => { OnSelectOne(choiceTag); });
            go.SetActive(true);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            choiceBtns.Add(go);
        });

        // 菜单显示尺寸
        width = choiceBtns[0].GetComponent<RectTransform>().rect.width;
        height = choiceBtns[0].GetComponent<RectTransform>().rect.height;

        choiceBtns[0].transform.localPosition = new Vector3(-width * 0.75f, 0, 0);
        choiceBtns[1].transform.localPosition = new Vector3(width * 0.75f, 0, 0);

        AdjustPos();
    }

    // 检查边界调整位置
    Vec2 AdjustPos()
    {
        var w = width * 2.5f;

        var parentArea = SelectArea.parent.GetComponent<RectTransform>().rect;
        var uiPos = SelectArea.transform.localPosition + Vector3.up * 40f;
        var uiPosX = uiPos.x.Clamp(w / 2, parentArea.width - w / 2);
        var uiPosY = uiPos.y.Clamp(height * 1.5f + 50f, parentArea.height - height / 2);
        SelectArea.transform.localPosition = new Vector3(uiPosX, uiPosY, 0);

        return new Vec2(uiPosX, uiPosY);
    }

    public void ReSetPos(Vec2 pt, Vec2 cp)
    {
        Pos = pt + Vec2.Up * 60;
        nowPos = AdjustPos();
        nowWPos = cp;

        OnChoiceSel = (toType) =>
        {
            if (toType == "Cancel")
            {
                MG.UUIs.CancelSelUnit();
                return;
            }

            var info = UnitConfiguration.GetDefaultConfig(MG.UUIs.CurUnitType);

            if (MG.UUIs.CurUnitType == "Base")
            {
                var bus = MG.Room.GetUnitsInArea(cp, 1, (u) => u.UnitType == "Base");

                if (bus != null && bus.Length > 0)
                    return;

                var us = MG.Room.GetUnitsInArea(cp, 1, (u) => u.UnitType == "BaseStub");

                if (us != null && us.Length > 0)
                {
                    if (!MG.CheckPrerequisitesAndTip("Base") || !MG.CheckResourceRequirementAndTip("Base"))
                        return;

                    var conn = GameCore.Instance.ServerConnection;
                    var buff = conn.Send2Srv("ConstructBuilding");
                    buff.Write("Base");
                    buff.Write(us[0].Pos);
                    conn.End(buff);
                }
            }
            else if (MG.UUIs.CurUnitType == "CrystalMachine")
            {
                var pres = UnitConfiguration.GetDefaultConfig("CrystalMachine").Prerequisites;

                var preLst = new List<string>();

                for (int i = 0; i < pres.Length; i++)
                    for (int j = 0; j < pres[i].Length; j++)
                        preLst.Add(pres[i][j]);

                var us = MG.Room.GetUnitsInArea(cp, 1, (u) => preLst.Contains(u.UnitType));

                if (us != null && us.Length > 0)
                {
                    if (MG.Room.FindNextCrystalMachinePos(us[0]) == Vec2.Zero)
                    {
                        AddTip("没有多余的矿机位置");
                        return;
                    }

                    if (!us[0].BuildingCompleted || !MG.CheckPrerequisitesAndTip(MG.UUIs.CurUnitType) || !MG.CheckResourceRequirementAndTip(MG.UUIs.CurUnitType))
                        return;

                    var conn = GameCore.Instance.ServerConnection;
                    var buff = conn.Send2Srv("ConstructCrystalMachine");
                    buff.Write(us[0].UID);
                    conn.End(buff);
                }
            }
            else if (MG.UUIs.CurUnitType == "Accessory")
            {
                var pres = UnitConfiguration.GetDefaultConfig("Accessory").Prerequisites;

                var preLst = new List<string>();

                for (int i = 0; i < pres.Length; i++)
                    for (int j = 0; j < pres[i].Length; j++)
                        preLst.Add(pres[i][j]);

                var us = MG.Room.GetUnitsInArea(cp, 1, (u) => preLst.Contains(u.UnitType));

                if (us != null && us.Length > 0)
                {
                    if (MG.Room.FindNextAccessoryPos(us[0], "Accessory") == Vec2.Zero)
                    {
                        AddTip("无法建造更多仓库");
                        return;
                    }

                    if (!us[0].BuildingCompleted || !MG.CheckPrerequisitesAndTip(MG.UUIs.CurUnitType) || !MG.CheckResourceRequirementAndTip(MG.UUIs.CurUnitType))
                        return;

                    var conn = GameCore.Instance.ServerConnection;
                    var buff = conn.Send2Srv("ConstructAccessory");
                    buff.Write(us[0].UID);
                    buff.Write("Accessory");
                    conn.End(buff);
                }
            }
            else if ((!string.IsNullOrEmpty(info.ReconstructFrom)))
            {
                var us = MG.Room.GetUnitsInArea(cp, 1, (u) => u.UnitType == info.ReconstructFrom);

                if (us != null && us.Length > 0)
                {
                    if (!us[0].BuildingCompleted || !MG.CheckPrerequisitesAndTip(MG.UUIs.CurUnitType) || !MG.CheckResourceRequirementAndTip(MG.UUIs.CurUnitType))
                        return;
                    else
                    {
                        var conn = GameCore.Instance.ServerConnection;
                        var buff = conn.Send2Srv("ReconstructBuilding");
                        buff.Write(us[0].UID);
                        buff.Write(MG.UUIs.CurUnitType);
                        conn.End(buff);
                    }
                }
            }
            else
            {
                MG.OnClickMap(pt, cp);
            }

            MG.UUIs.CancelSelUnit();
        };
    }

    public void WithGroundDraging()
    {
        var p = UIManager.Instance.World2UI(nowWPos);
        ReSetPos(p, nowWPos);
    }
}