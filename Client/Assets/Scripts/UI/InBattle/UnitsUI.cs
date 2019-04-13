using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swift;
using SCM;
using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Swift.Math;

public class UnitsUI : UIBase
{
    public GameObject UUI;
    public MapGround MG;
    public Button SwitchBtn;

    public Transform ArmyContent;                  // 军队
    public Transform BuildingContent;              // 建筑
    public Transform ThirdContent;                 // 其他

    public Action onSelUnit = null;                // 点击单位卡牌
    public Action onSwitchUnits = null;            // 点击切换按钮

    public Action<string> onShowUnitPlacedArea = null;
    public Action onHideUnitPlacedArea = null;

    private UnitCardUI curBtn = null;
    private bool needRefreshBtns = false;

    // 切换页签的滑动效果相关
    private bool isSwitch;                         // 是否切换
    private bool IsShowArmyList;                   // 是否显示部队
    private bool IsShowBuildingList;                   // 是否显示建筑
    private bool IsShowOtherList;                   // 是否显示其他
    private float elapsedTime;                     // 滑动已花费时间
    private float contentSlideTime;                // content滑动时间
    private Vector3 ShowContentPos;                // 显示区域的坐标
    private Vector3 HideContentPos;                // 隐藏区域的坐标
    private Vector3 contentSlideDis;               // 滑动距离
    private int SwitchTimes;                       // 切换次数

    private Dictionary<string, int> builder;
    private Dictionary<string, int> builderAccs;
    private Dictionary<string, float> coldown;
    private Dictionary<string, UnitCardUI> btns;

    public string CurUnitType { get; set; }        // 当前选择的unit类型

    public bool IsOpenAllUUI = false;

    private Vector3 hitPt;
    private Vector3 ptNow;

    public void OnSelUnit(string c)
    {
        // 再次点击取消选中
        if (CurUnitType == c)
        {
            CancelSelUnit();
            return;
        }

        if (curBtn != null)
        {
            var vcfg = UnitConfiguration.GetDefaultConfig(CurUnitType);

            curBtn.transform.Find(vcfg.IsBuilding ? "BuildingCheck" : "Check").gameObject.SetActive(false);
        }

        curBtn = btns[c];
        CurUnitType = c;

        var vcfg1 = UnitConfiguration.GetDefaultConfig(CurUnitType);

        curBtn.transform.Find(vcfg1.IsBuilding ? "BuildingCheck" : "Check").gameObject.SetActive(true);

        if (null != onSelUnit)
            onSelUnit();

        if (null != onHideUnitPlacedArea)
            onHideUnitPlacedArea();

        if (null != onShowUnitPlacedArea)
            onShowUnitPlacedArea(c);
    }

    public void CancelSelUnit()
    {
        var vcfg = UnitConfiguration.GetDefaultConfig(CurUnitType);

        curBtn.transform.Find(vcfg.IsBuilding ? "BuildingCheck" : "Check").gameObject.SetActive(false);

        curBtn = null;
        CurUnitType = null;

        if (null != onHideUnitPlacedArea)
            onHideUnitPlacedArea();
    }

    public bool CheckColdown(string type)
    {
        return coldown.ContainsKey(type) && coldown[type] >= 1;
    }

    public void ResetColdown(string type)
    {
        var mainBuilder = UnitConfiguration.GetMainBuilder(type);
        var cfg = UnitConfiguration.GetDefaultConfig(type);

        if (cfg.IsBuilding)
            return;

        // 同一建筑的单位一起重置 cd
        foreach (var t in coldown.Keys.ToArray())
        {
            var tcfg = UnitConfiguration.GetDefaultConfig(t);

            if (tcfg.IsBuilding)
                continue;

            if (UnitConfiguration.GetMainBuilder(t) == mainBuilder)
                coldown[t] = coldown[t] >= 1 ? coldown[t] - 1 : 0;
        }
    }

    public void SwitchUnits()
    {
        SwitchTimes++;

        var remainder = SwitchTimes % 3;
        
        switch (remainder)
        {
            case 0:
                IsShowArmyList = true;
                IsShowBuildingList = false;
                IsShowOtherList = false;
                break;
            case 1:
                IsShowBuildingList = true;
                IsShowOtherList = false;
                IsShowArmyList = false;
                break;
            case 2:
                IsShowOtherList = true;
                IsShowArmyList = false;
                IsShowBuildingList = false;
                break;
        }

        //IsShowArmyList = !IsShowArmyList;

        // 页签滑动相关
        isSwitch = true;
        elapsedTime = 0f;

        SwitchBtn.transform.Find("BigBuildingImg").gameObject.SetActive(!IsShowArmyList);
        SwitchBtn.transform.Find("SmallArmyImg").gameObject.SetActive(!IsShowArmyList);
        SwitchBtn.transform.Find("BigArmyImg").gameObject.SetActive(IsShowArmyList);
        SwitchBtn.transform.Find("SmallBuildingImg").gameObject.SetActive(IsShowArmyList);

        curBtn = null;
        CurUnitType = null;
        RefreshBtns();

        if (null != onSwitchUnits)
            onSwitchUnits();
    }

    protected override void StartOnlyOneTime()
    {
        builder = new Dictionary<string, int>();
        builderAccs = new Dictionary<string, int>();
        coldown = new Dictionary<string, float>();
        btns = new Dictionary<string, UnitCardUI>();

        isSwitch = false;
        SwitchTimes = 0;
        elapsedTime = 0f;
        contentSlideTime = 0.25f;                   // 按需调整滑动速度
        IsShowArmyList = true;
        IsShowBuildingList = false;
        IsShowOtherList = false;
        ShowContentPos = ArmyContent.parent.localPosition;
        HideContentPos = BuildingContent.parent.localPosition;
        contentSlideDis = HideContentPos - ShowContentPos;

        Room4Client.OnBattleBegin += Room4Client_OnBattleBegin;
        Room4Client.OnBattleEnd += Room4Client_OnBattleEnd;
        Room4Client.NotifyAddBuildingUnit += OnUnitAddOrRemovedOrConstructingComplete;
        Room4Client.NotifyUnitRemoved += OnUnitAddOrRemovedOrConstructingComplete;
        Room4Client.NotifyReconstructUnit += OnUnitReconstruct;
        Room4Client.OnConstructingCompleted += OnUnitAddOrRemovedOrConstructingComplete;

        UnitConfigUtil.OnBuildUnitCfgsFromServer += BuildAllUUIs;
        UnitConfigUtil.OnRefreshUnitCfgsFromServer += RefreshAllUUIs;

        gameObject.SetActive(false);
    }

    // 构造所有的UUI
    private void BuildAllUUIs()
    {
        var uts = UnitConfiguration.AllUnitTypes.Select((ut) =>
        {
            var meInfo = GameCore.Instance.MeInfo;

            var orgType = meInfo.Variants.ContainsKey(ut) ? ut : UnitConfiguration.GetDefaultConfig(ut).OriginalType;

            bool isUnlock = meInfo.Units[ut];
            bool isVarType = meInfo.Variants[orgType] == ut;
            bool isNoCard = UnitConfiguration.GetDefaultConfig(ut).NoCard;

            return isUnlock && isVarType && !isNoCard;
        });

        for (var i = 0; i < uts.Count; i++)
        {
            var ut = uts [i];
            var cfg = UnitConfiguration.GetDefaultConfig(ut);

            Transform parent = null;

            if (cfg.IsThirdType)
                parent = ThirdContent;
            else if (cfg.IsBuilding)
                parent = BuildingContent;
            else
                parent = ArmyContent;

            GameObject uui = CreateUUI(parent);
            SetUUIInfo(uui, ut);

            var btn = uui.GetComponent<UnitCardUI>();
            btn.OnPtDown += () => OnSelUnit(ut);
            btn.OnBeginDrag += OnBeginDrag;
            btn.OnDrag += OnDrag;
            btn.OnEndDrag += OnEndDrag;

            btns[ut] = btn;
            uui.SetActive(false);
        }
    }

    private Vector3 HitGroundPos()
    {
        ptNow = Input.touchCount == 0 ? Input.mousePosition :
            new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y, 0);

        EventLayerHandler.RayQueryHandler(ptNow, out hitPt);
        return hitPt;
    }

    private void OnBeginDrag()
    {
    }

    private void OnDrag()
    {
        if (string.IsNullOrEmpty(CurUnitType))
            return;

        HitGroundPos();

        Vec2 pt = UIManager.Instance.World2UI(hitPt);
        if (pt.y > 90)
            MG.UC.ShowExampleUnit(CurUnitType, hitPt);
        else
            MG.UPSUI.Hide();
    }

    private void OnEndDrag(bool cancelDrag)
    {
        if (string.IsNullOrEmpty(CurUnitType))
            return;

        if (cancelDrag || !UnitConfiguration.GetDefaultConfig(CurUnitType).IsBuilding)
            MG.UPSUI.Hide();

        if (cancelDrag)
            return;

        HitGroundPos();
        Vec2 pt = UIManager.Instance.World2UI(hitPt);
        MG.OnClick(pt, UIManager.Instance.View2Ground(ptNow));
    }

    private void RefreshAllUUIs()
    {
        foreach (var btn in btns)
        {
            var uui = btn.Value.gameObject;
            SetUUIInfo(uui, btn.Key);
        }
    }

    // 创建一个UUI
    private GameObject CreateUUI(Transform parent)
    {
        var uui = Instantiate(UUI) as GameObject;
        uui.SetActive(true);
        uui.transform.SetParent(parent);
        uui.transform.localPosition = Vector3.zero;
        uui.transform.localRotation = Quaternion.identity;
        uui.transform.localScale = Vector3.one;

        return uui;
    }

    // 设置UUI信息
    private void SetUUIInfo(GameObject uui, string type)
    {
        var cfg = UnitConfiguration.GetDefaultConfig(type);

        Sprite img = Resources.Load<Sprite>(@"Texture\UnitCard\" + type);

        if (null != img)
        {
            uui.transform.Find("Name").gameObject.SetActive(true);
            uui.transform.Find("Icon").gameObject.SetActive(true);
            uui.transform.Find("Icon").GetComponent<Image>().sprite = img;
            uui.transform.Find("Icon").GetComponent<Image>().SetNativeSize();
        }
        else
        {
            uui.transform.Find("Name").gameObject.SetActive(true);
            uui.transform.Find("Icon").gameObject.SetActive(false);
        }

        var okImg = uui.transform.Find("OK").GetComponent<Image>();

        uui.transform.Find("Name").GetComponent<Text>().text = cfg.DisplayName;
        uui.transform.Find("Cost").GetComponent<Text>().text = cfg.Cost.ToString();
        uui.transform.Find("GasCost").GetComponent<Text>().text = cfg.GasCost.ToString();
        uui.transform.Find("Num").GetComponent<Text>().text = "0";
        uui.transform.Find("OK").gameObject.SetActive(!cfg.IsBuilding);
        uui.transform.Find("BuildingOK").gameObject.SetActive(cfg.IsBuilding);

        if (!cfg.IsBuilding)
        {
            var mainBuilder = UnitConfiguration.GetMainBuilder(type);

            if (mainBuilder == "Barrack")
                okImg.sprite = Resources.Load<Sprite>(@"Texture\CombatUI\zhandou_27");
            else if (mainBuilder == "Factory")
                okImg.sprite = Resources.Load<Sprite>(@"Texture\CombatUI\zhandou_28");
            else if (mainBuilder == "Airport")
                okImg.sprite = Resources.Load<Sprite>(@"Texture\CombatUI\zhandou_29");
        }
    }

    private void OnUnitReconstruct(Unit u, string fromType)
    {
        if (!gameObject.activeSelf)
            return;

        RefreshBtns();
    }

    private void OnUnitAddOrRemovedOrConstructingComplete(Unit u)
    {
        if (!gameObject.activeSelf)
            return;

        RefreshBtns();
    }

    private void Room4Client_OnBattleBegin(Room4Client r, bool isReplay)
    {
        gameObject.SetActive(!isReplay);
        if (isReplay)
            return;

        UnitConfigUtil.GetUnitCfgsFromServer();

        curBtn = null;
        CurUnitType = null;
        IsShowArmyList = true;
        IsShowBuildingList = false;
        IsShowOtherList = false;
        SwitchTimes = 0;

        ArmyContent.parent.localPosition = ShowContentPos;
        BuildingContent.parent.localPosition = HideContentPos;
        ThirdContent.parent.localPosition = HideContentPos;

        SwitchBtn.transform.Find("Text1").GetComponent<Text>().text = IsShowArmyList ? "部队" : "建筑";
        SwitchBtn.transform.Find("Text2").GetComponent<Text>().text = IsShowArmyList ? "建筑" : "部队";

        foreach (var c in btns.Keys)
            btns[c].gameObject.SetActive(false);

        builder.Clear();
        builderAccs.Clear();
        coldown.Clear();

        btns.Clear();
        ContentClear(ArmyContent);
        ContentClear(BuildingContent);
        ContentClear(ThirdContent);
        BuildAllUUIs();

        needRefreshBtns = true;
    }

    private void Room4Client_OnBattleEnd(Room r, string winner, bool isReplay)
    {
        //  战斗结束时也刷新一下客户端配置表
        UnitConfigUtil.GetUnitCfgsFromServer();
    }

    private void RefreshColdown(float dt)
    {
        var myMoney = GameCore.Instance.GetMyResource("Money");
        var myGas = GameCore.Instance.GetMyResource("Gas");

        foreach (var c in btns.Keys)
        {
            var btn = btns[c];

            var cfg = UnitConfiguration.GetDefaultConfig(c);
            
            var dd = dt / (float)cfg.ConstructingTime;
            var rate = builder.ContainsKey(c) && MG.Room.CheckPrerequisites(GameCore.Instance.MePlayer, c) ? builder[c] : 0;

            if (rate == 0)
                continue;

            var accs = builderAccs.ContainsKey(c) ? builderAccs[c] : 0;
            dd *= rate;
            if (!coldown.ContainsKey(c))
                coldown[c] = cfg.IsBuilding ? 1 : 0;
            else
            {
                var maxStore = rate + accs;
                var cd = coldown[c] + dd;
                coldown[c] = cd > maxStore ? maxStore : cd;
            }

            var p = coldown[c] - (int)coldown[c];
            var okImg = btn.transform.Find(cfg.IsBuilding ? "BuildingOK" : "OK").GetComponent<Image>();
            okImg.fillAmount = p == 0 ? 1 : p;
            var num = btn.transform.Find("Num");
            if (cfg.IsBuilding)
                num.gameObject.SetActive(false);
            else
            {
                num.gameObject.SetActive(true);
                num.GetComponent<Text>().text = ((int)coldown[c]).ToString();
            }

            var costTxt = btn.transform.Find("Cost").GetComponent<Text>();
            var gasCostTxt = btn.transform.Find("GasCost").GetComponent<Text>();
            gasCostTxt.color = new Color(16 / 255.0f, 238 / 255.0f, 37 / 255.0f, 1);
            costTxt.color = new Color(0, 216 / 255.0f, 255 / 255.0f, 1);

            if (myMoney < cfg.Cost || myGas < cfg.GasCost)
            {
                okImg.color = new Color(187 / 255.0f, 187 / 255.0f, 187 / 255.0f, 1);
                if (myMoney < cfg.Cost)
                    costTxt.color = Color.red;

                if (myGas < cfg.GasCost)
                    gasCostTxt.color = Color.red;
            }
            else
                okImg.color = Color.white;
        }
    }

    // 刷新所有按钮状态
    private void RefreshBtns()
    {
        if (MG.Room == null)
            return;

        needRefreshBtns = false;
        foreach (var ut in btns.Keys)
        {
            var cfg = UnitConfiguration.GetDefaultConfig(ut);

            // 前置条件
            var ok = cfg.Prerequisites == null || MG.Room.CheckPrerequisites(GameCore.Instance.MePlayer, ut);

            if (IsOpenAllUUI)
                ok = true;

            var btn = btns[ut];
            btn.gameObject.SetActive(ok);
            btn.transform.Find(cfg.IsBuilding ? "BuildingCheck" : "Check").gameObject.SetActive(btn == curBtn);

            var coreBuilder = UnitConfiguration.GetMainBuilder(ut);
            if (coreBuilder != null)
            {
                builder[ut] = MG.Room.GetAllMyUnits((u) => (u.UnitType == coreBuilder || u.cfg.ReconstructFrom == coreBuilder) && u.BuildingCompleted).Length;
                builderAccs[ut] = MG.Room.GetMyUnitsByType("Accessory", (u) =>
                    u.Owner != null && u.BuildingCompleted &&
                    u.Owner.BuildingCompleted && (u.Owner.UnitType == coreBuilder || u.Owner.cfg.ReconstructFrom == coreBuilder)).Length;
            }
            else
            {
                builder[ut] = 1;
                builderAccs[ut] = 0;
            }

            if (ok)
                // 当前默认选定第一个
                SetDefaultSelBtn(ut);
        }
    }

    // 设置页签默认选定按钮 
    private void SetDefaultSelBtn(string ut)
    {
        if (!(null == curBtn || !curBtn.gameObject.activeSelf))
            return;

        var cfg = UnitConfiguration.GetDefaultConfig(ut);

        if ((cfg.IsBuilding && !IsShowArmyList) || (!cfg.IsBuilding && IsShowArmyList))
        {
            CurUnitType = ut;
            curBtn = btns[ut];
            btns[ut].OnPtDown();
        }
    }

    // 切换页签时的滑动效果
    private void SwitchAnimation()
    {
        if (!isSwitch)
            return;

        if (contentSlideTime <= 0f)
            return;

        if (elapsedTime >= contentSlideTime)
        {
            elapsedTime = contentSlideTime;
            isSwitch = false;
        }

        if (IsShowArmyList)
        {
            ArmyContent.parent.localPosition = Vector3.Lerp(HideContentPos, HideContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            ThirdContent.parent.localPosition = Vector3.Lerp(ShowContentPos, ShowContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            BuildingContent.parent.localPosition = HideContentPos;
        }
        else if (IsShowBuildingList)
        {
            BuildingContent.parent.localPosition = Vector3.Lerp(HideContentPos, HideContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            ArmyContent.parent.localPosition = Vector3.Lerp(ShowContentPos, ShowContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            ThirdContent.parent.localPosition = HideContentPos;
        }
        else if (IsShowOtherList)
        {
            ThirdContent.parent.localPosition = Vector3.Lerp(HideContentPos, HideContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            BuildingContent.parent.localPosition = Vector3.Lerp(ShowContentPos, ShowContentPos - contentSlideDis, elapsedTime / contentSlideTime);
            ArmyContent.parent.localPosition = HideContentPos;
        }
    }

    private void ContentClear(Transform parent)
    {
        foreach (Transform tran in parent)
            Destroy(tran.gameObject);
    }

    private void Update()
    {
        if (MG.Room == null)
            return;

        if (isSwitch)
        {
            elapsedTime += Time.deltaTime;
            SwitchAnimation();
        }

        if (needRefreshBtns && MG.Room != null)
            RefreshBtns();

        RefreshColdown(Time.deltaTime);
    }
}
