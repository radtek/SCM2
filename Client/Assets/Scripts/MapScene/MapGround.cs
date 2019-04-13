using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Swift;
using SCM;
using Swift.Math;
using System;
using UnityEngine.UI;

public class MapGround : SCMBehaviour, IEventHandler
{
    public UnitCreator UC;
    public EffectCreator EC;
    public UnitsUI UUIs;
    public UnitPosSelUI UPSUI;
    public MapScene MS;

    public GameObject RadarSelFlag;

    public MainCamera MainCam = null;

    public GameObject UnitPlaceOwnArea;
    public GameObject UnitPlaceMapArea;

    private Vec2 pt;
    private Vector3 wp;

    // 特定区域放置的单位
    private bool IsSpecificAreaUnit = false;
    private List<Unit> PlaceBuildingLst = null;

    protected override void StartOnlyOneTime()
    {
        Room4Client.OnBattleBegin += OnBattleBegin;
        Room4Client.OnBattleEnd += OnBattleEnd;
        UUIs.onSelUnit += UPSUI.Hide;
        UUIs.onSwitchUnits += UPSUI.Hide;
        UUIs.onShowUnitPlacedArea += OnShowUnitPlacedArea;
        UUIs.onHideUnitPlacedArea += OnHideUnitPlacedArea;
        UUIs.onHideUnitPlacedArea += UPSUI.Hide;

        PlaceBuildingLst = new List<Unit>();

        UnitPlaceOwnArea.transform.GetComponent<MeshRenderer>().material.SetFloat("_CellDensity", 0);
        UnitPlaceMapArea.transform.GetComponent<MeshRenderer>().material.SetFloat("_CellDensity", 0);
    }

    private void OnBattleBegin(Room4Client r, bool inReplay)
    {
        MapUnit.inReplay = inReplay;
        Room = r;
        transform.Find("MiddleLine").gameObject.SetActive(true);

        transform.Find("BGM").gameObject.SetActive(true);
    }

    private void OnBattleEnd(Room r, string winner, bool inReplay)
    {
        UnitPlaceOwnArea.SetActive(false);
        UnitPlaceMapArea.SetActive(false);
        transform.Find("BGM").gameObject.SetActive(false);
    }

    // 显示单位放置区域
    private void OnShowUnitPlacedArea(string unitType)
    {
        ClearHighlightBuildings();

        var lst = new List<Unit>();

        var info = UnitConfiguration.GetDefaultConfig(unitType);

        if (!info.IsBuilding)
        {
            IsSpecificAreaUnit = false;
            //ShowWholeMapArea();
        }
        else  if (unitType == "Base")
        {
            var us = Room.GetUnitsByType("BaseStub", 0);

            for (int i = 0; i < us.Length; i++)
            {
                var bs = Room.GetUnitsInArea(us[i].Pos, 1, (u) =>
                    u.UnitType == "Base" || u.UnitType == "CommanderCenter" || u.UnitType == "Fortress");

                if (bs != null && bs.Length > 0)
                    continue;

                if (GameCore.Instance.MePlayer == 1)
                {
                    if (us[i].Pos.y < Room.MapSize.y / 2)
                        lst.Add(us[i]);
                }
                else if (GameCore.Instance.MePlayer == 2)
                {
                    if (us[i].Pos.y > Room.MapSize.y / 2)
                        lst.Add(us[i]);
                }
            }

            IsSpecificAreaUnit = true;
            PlaceBuildingLst = lst;
            ShowPartArea(lst);
        }
        else if (unitType == "CrystalMachine" || unitType == "Accessory")
        {
            lst = GetMyPresUnitsByType(unitType);

            IsSpecificAreaUnit = true;
            PlaceBuildingLst = lst;
            ShowPartArea(lst);
        }
        else if (!string.IsNullOrEmpty(info.ReconstructFrom))
        {
            var us = Room.GetMyUnitsByType(info.ReconstructFrom);

            for (int i = 0; i < us.Length; i++)
            {
                if (us[i].BuildingCompleted)
                    lst.Add(us[i]);
            }

            IsSpecificAreaUnit = true;
            PlaceBuildingLst = lst;
            ShowPartArea(lst);
        }
        else
        {
            IsSpecificAreaUnit = false;
            ShowWholeOwnArea();
        }
    }

    private void ShowWholeMapArea()
    {
        UnitPlaceMapArea.gameObject.SetActive(true);
        UnitPlaceMapArea.transform.GetComponent<MeshRenderer>().material.SetColor("_MainTint", Color.green);
    }

    // 显示己方全部区域
    private void ShowWholeOwnArea()
    {
        UnitPlaceOwnArea.gameObject.SetActive(true);
        UnitPlaceOwnArea.transform.GetComponent<MeshRenderer>().material.SetColor("_MainTint", Color.green);
    }

    // 显示部分区域
    private void ShowPartArea(List<Unit> units)
    {
        UnitPlaceOwnArea.gameObject.SetActive(true);
        UnitPlaceOwnArea.transform.GetComponent<MeshRenderer>().material.SetColor("_MainTint", Color.red);

        ShowHighlightBuilding(units);
    }

    Dictionary<string, GameObject> uModels = new Dictionary<string, GameObject>();

    // 显示高亮建筑
    private void ShowHighlightBuilding(List<Unit> units)
    {
        foreach (var u in units)
        {
            var mu = UC.GetModel(u.UID);

            var go = GameObject.Instantiate(mu.gameObject) as GameObject;
            go.SetActive(true);
            go.transform.SetParent(MS.Units);

            SetHighlightBuildingPos(go);

            go.GetComponent<MapUnit>().AddOutLineEffect();
            UC.AddCoverArea(go.transform, Color.green);

            uModels[u.UID] = go;
        }
    }

    // 设置高亮建筑的位置
    private void SetHighlightBuildingPos(GameObject go)
    {
        var pos = go.transform.localPosition;
        var pt = UIManager.Instance.World2UI(pos);
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)pt.x, (float)pt.y, 0));
        var hitPt = pos - 18 * ray.direction;

        go.transform.localPosition = hitPt;
    }

    // 根据类型查找所有前置单位
    private List<Unit> GetMyPresUnitsByType(string type)
    {
        var lst = new List<Unit>();

        var pres = UnitConfiguration.GetDefaultConfig(type).Prerequisites;

        for (int i = 0; i < pres.Length; i++)
        {
            for (int j = 0; j < pres[i].Length; j++)
            {
                var us = Room.GetMyUnitsByType(pres[i][j]);

                for (int t = 0; t < us.Length; t++)
                {
                    if (us[t].BuildingCompleted)
                        lst.Add(us[t]);
                }
            }
        }

        return lst;
    }

    // 隐藏单位放置区域
    private void OnHideUnitPlacedArea()
    {
        UnitPlaceOwnArea.gameObject.SetActive(false);
        UnitPlaceMapArea.gameObject.SetActive(false);

        ClearHighlightBuildings();

        UC.DestroyExampleUnit();
    }

    private void ClearHighlightBuildings()
    {
        var ks = uModels.Keys.ToArray();

        for (int i = 0; i < ks.Length; i++)
        {
            Destroy(uModels[ks[i]]);
            uModels.Remove(ks[i]);
        }
    }

    public Room4Client Room
    {
        get { return room; }
        set
        {
            room = value;
            room.PathFinder = FindPath;
            var sz = room.MapSize;
            var w = sz.x;
            var h = sz.y;
            var cx = w / 2;
            var cy = h / 2;

            transform.localScale = new Vector3((float)w, 1, (float)h);
            transform.localPosition = new Vector3((float)cx, 0, (float)cy);
            transform.localRotation = GameCore.Instance.MePlayer == 2 ? Quaternion.identity : Quaternion.Euler(new Vector3(0, 180, 0));
        }
    }
    Room4Client room;

#if UNITY_EDITOR

    public void OnRightClick(Vec2 pt, Vector3 wp)
    {
        if (UUIs.CurUnitType == null)
            return;

        var cfg = UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType);

        // 右键全开
        if (!UUIs.IsOpenAllUUI)
            UUIs.IsOpenAllUUI = true;

        var conn = GameCore.Instance.ServerConnection;
        if (UUIs.CurUnitType == "SoldierCarrier" || UUIs.CurUnitType == "RobotCarrier")
        {
            var buff = conn.Send2Srv("AddSoldierCarrierUnit4TestAnyway");

            var p = wp.z >= room.MapSize.y / 2 ? 2 : 1;
            buff.Write(p);
            buff.Write(UUIs.CurUnitType);
            buff.Write((int)wp.x);
            buff.Write((int)wp.z);
            conn.End(buff);
        }
        else if (UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType).IsBuilding)
        {
            var buff = conn.Send2Srv("AddBuildingUnit4TestAnyway");

            var p = wp.z >= room.MapSize.y / 2 ? 2 : 1;
            buff.Write(p);
            buff.Write(UUIs.CurUnitType);
            buff.Write((int)wp.x);
            buff.Write((int)wp.z);
            conn.End(buff);
        }
        else
        {
            UUIs.ResetColdown(UUIs.CurUnitType);
            var buff = conn.Send2Srv("AddBattleUnit4TestAnyway");

            var p = wp.z >= room.MapSize.y / 2 ? 2 : 1;
            buff.Write(p);
            buff.Write(UUIs.CurUnitType);
            buff.Write((int)wp.x);
            buff.Write((int)wp.z);
            conn.End(buff);
        }
    }

#endif

    public void OnClick(Vec2 pt, Vector3 wp)
    {
        if (MapUnit.inReplay)
            return;

        if (UUIs.CurUnitType == null)
            return;

#if UNITY_EDITOR
        if (EventLayerHandler.CurrentClickBtn == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
        {
            OnRightClick(pt, wp);
            return;
        }
#endif

        this.pt = pt;
        this.wp = wp;

        var cp = new Vec2(wp.x, wp.z);

        var adaptWp = wp;
        var adaptCp = cp;

        var us = Room.GetUnitsInArea(cp, 1, (u) => PlaceBuildingLst.Contains(u));

        if (IsSpecificAreaUnit)
        {
            if (us.Length <= 0)
            {
                AddTip("该区域不可建造");
                UPSUI.Hide();
                return;
            }
            else
            {
                adaptCp = us[0].Pos;
                adaptWp = new Vector3((float)adaptCp.x, 0.0f, (float)adaptCp.y);
            }
        }

        if (UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType).IsBuilding)
        {
            if (adaptCp.y > 70 && GameCore.Instance.MePlayer == 1)
            {
                AddTip("只能在自己一侧建造");
                UPSUI.Hide();
                return;
            }
            else if (adaptCp.y < room.MapSize.y - 70 && GameCore.Instance.MePlayer == 2)
            {
                AddTip("只能在自己一侧建造");
                UPSUI.Hide();
                return;
            }

            UC.ShowExampleUnit(UUIs.CurUnitType, adaptWp);
            UPSUI.Show();
            UPSUI.ReSetPos(pt, adaptCp);
        }
        else
        {
            // 只能在各自那边建造，除了伞兵和雷达
            if (UUIs.CurUnitType != "SoldierCarrier" && UUIs.CurUnitType != "RobotCarrier" && UUIs.CurUnitType != "Radar")
            {
                if (adaptCp.y > 70 && GameCore.Instance.MePlayer == 1)
                    adaptCp.y = 70;
                else if (adaptCp.y < room.MapSize.y - 70 && GameCore.Instance.MePlayer == 2)
                    adaptCp.y = room.MapSize.y - 70;
            }

            OnClickMap(pt, adaptCp);

            //UUIs.CancelSelUnit();
        }
    }

    // 关闭建造或放兵提示界面，并销毁示例模型
    public void DestroyExampleUnit()
    {
        UC.DestroyExampleUnit();
    }

    // 单击地面放兵
    public void OnClickMap(Vec2 pt, Vec2 cp)
    {
        var cfg = UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType);

        if (!UUIs.CheckColdown(UUIs.CurUnitType))
        {
            AddTip("部队尚未就绪");
            return;
        }

        if (!CheckPrerequisitesAndTip(UUIs.CurUnitType)
            || !CheckResourceRequirementAndTip(UUIs.CurUnitType))
            return;

        UUIs.ResetColdown(UUIs.CurUnitType);
        var conn = GameCore.Instance.ServerConnection;
        if (UUIs.CurUnitType == "SoldierCarrier" || UUIs.CurUnitType == "RobotCarrier")
        {
            var buff = conn.Send2Srv("DropSoldierFromCarrier");
            buff.Write(UUIs.CurUnitType);
            buff.Write((int)cp.x);
            buff.Write((int)cp.y);
            conn.End(buff);
        }
        else if (UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType).IsBuilding)
        {
            if (!room.CheckSpareSpace(cp, cfg.SizeRadius)
                    && !room.FindNearestSpareSpace(cp, cfg.SizeRadius, 1, out cp))
            {
                UIManager.Instance.Tips.AddTip("没有足够的建造空间");
                return;
            }

            var buff = conn.Send2Srv("ConstructBuilding");
            buff.Write(UUIs.CurUnitType);
            buff.Write(cp);
            conn.End(buff);
        }
        else
        {
            var buff = conn.Send2Srv("AddBattleUnitAt");
            buff.Write(UUIs.CurUnitType);
            buff.Write(cp);
            conn.End(buff);
        }
    }

    public void OnDoubleClick(Vec2 pt, Vector3 wp)
    {
        OnClick(pt, wp);
    }

    // 客户端利用 unity 的 navmesh 寻路
    Vec2[] FindPath(Vec2 src, Vec2 dst, Fix64 size)
    {
        if (Room == null)
            return null;

        if (!src.InRect(Room.MapSize) || !dst.InRect(Room.MapSize))
            return null;

        var found = false;
        if (!found)
            return null;

        return new Vec2[0];
    }

    public void SelConstructBuilding(Vec2 pt, Func<Vec2> getConstructingPos)
    {
        var choices = UnitConfiguration.AllUnitTypes.ToArray((i, ut, skipAct) =>
        {
            var cfg = UnitConfiguration.GetDefaultConfig(ut);
            if (cfg.NoBody || cfg.NoCard || !cfg.IsBuilding || cfg.ReconstructFrom != null || ut == "Base")
                skipAct();

            return ut;
        });

        if (choices.Length == 0)
            return;

        // 空地上长按进行选择建造建筑
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = choices;
        ui.ChoicesName = ui.Choices.ToArray((i, c, skip) =>
        {
            var ccfg = UnitConfiguration.GetDefaultConfig(c);

            return ccfg.DisplayName;
        });
        ui.Refresh();
        ui.OnChoiceSel = (buildingType) =>
        {
            var cfg = UnitConfiguration.GetDefaultConfig(buildingType);

            if (!CheckPrerequisitesAndTip(buildingType)
                || !CheckResourceRequirementAndTip(buildingType))
                return;

            // 只能在各自那边建造
            var cp = getConstructingPos();
            if (cp.y > room.MapSize.y / 2 && GameCore.Instance.MePlayer == 1)
                return;
            else if (cp.y < room.MapSize.y / 2 && GameCore.Instance.MePlayer == 2)
                return;

            if (!room.CheckSpareSpace(cp, cfg.SizeRadius)
                    && !room.FindNearestSpareSpace(cp, cfg.SizeRadius, 1, out cp))
            {
                UIManager.Instance.Tips.AddTip("没有足够的建造空间");
                return;
            }

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ConstructBuilding");
            buff.Write(buildingType);
            buff.Write(cp);
            conn.End(buff);
        };
    }

    //void SendPath(Unit u, Vec2[] path)
    //{
    //    if (u.Player != GameCore.Instance.MePlayer)
    //        return;

    //    var uid = u.UID;
    //    var conn = GameCore.Instance.ServerConnection;
    //    var buff = conn.Send2Srv("SetPath");
    //    buff.Write(uid);
    //    buff.Write(path);
    //    conn.End(buff);
    //}

    public void OnPress(Vec2 pt, Vector3 wp)
    {
        //// 只能在各自那边建造
        //var cp = wp;
        //if (cp.z > room.MapSize.y / 2 && GameCore.Instance.MePlayer == 1)
        //{
        //    AddTip("只能在自己一侧建造");
        //    return;
        //}
        //else if (cp.z < room.MapSize.y / 2 && GameCore.Instance.MePlayer == 2)
        //{
        //    AddTip("只能在自己一侧建造");
        //    return;
        //}

        //SelConstructBuilding(pt, () => new Vec2(wp.x, wp.z));
    }

    public void Clear()
    {
        transform.Find("MiddleLine").gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
        transform.localPosition = Vector3.zero;
        MainCam.Clear();
    }

    void AddTip(string tip)
    {
        UIManager.Instance.Tips.AddTip(tip);
    }

    void AddSmallTip(string tip)
    {
        UIManager.Instance.Tips.AddSmallTip(tip);
    }

    Vec2 lastDraggingPos = Vec2.Zero;
    public void OnDragStarted(Vec2 pt, Vector3 wp)
    {
        scrollVelocity = 0f;
        lastDraggingPos = pt;
    }

    public void OnDragging(Vec2 from, Vector3 fromWp, Vec2 now, Vector3 nowWp)
    {
        var dd = now - lastDraggingPos;

        lastDraggingDis = dd;

        lastDraggingPos = now;
        MainCam.MoveCamera((float)dd.y);

        if (UPSUI.isActiveAndEnabled)
            UPSUI.WithGroundDraging();
    }

    private Vec2 lastDraggingDis = Vec2.Zero;
    private float scrollVelocity = 0f;
    private float timeTouchPhaseEnded = 0f;
    private float inertiaDuration = 0.5f;

    public void DoDragEnded(Vec2 from, Vector3 fromWp, Vec2 to, Vector3 toWp)
    {
        if (Mathf.Abs((float)lastDraggingDis.y) < 20.0f)
            return;

        scrollVelocity = (float)(lastDraggingDis.y * 0.5 / Time.deltaTime);
        timeTouchPhaseEnded = Time.time;
    }

    private void Update()
    {
        if (scrollVelocity != 0.0f)
        {
            // slow down
            float t = (Time.time - timeTouchPhaseEnded)/inertiaDuration;
            float frameVelocity = Mathf.Lerp(scrollVelocity, 0, t);
            lastDraggingDis.y += frameVelocity * Time.deltaTime;

            MainCam.MoveCamera((float)lastDraggingDis.y);

            if (t >= inertiaDuration)
                scrollVelocity = 0;
        }
    }

    public bool CheckPrerequisitesAndTip(string unitType)
    {
        var cfg = UnitConfiguration.GetDefaultConfig(UUIs.CurUnitType);
        if (!Room.CheckPrerequisites(GameCore.Instance.MePlayer, unitType))
        {
            var msg = "";
            foreach (var pres in cfg.Prerequisites)
            {
                foreach (var p in pres)
                {
                    var ut = p;
                    if (ut[0] == '-')
                    {
                        ut = p.Substring(1);
                        if (Room.GetMyFirstUnitByType(ut) != null)
                        {
                            msg = "已经建造了 " + UnitConfiguration.GetDefaultConfig(ut).DisplayName;
                            AddTip(msg);
                            return false;
                        }
                    }
                    else if (Room.GetMyFirstUnitByType(ut) == null)
                    {
                        msg = "需要先建造 " + UnitConfiguration.GetDefaultConfig(ut).DisplayName;
                        AddTip(msg);
                        return false;
                    }
                }
            }

            return false;
        }

        return true;
    }

    public bool CheckResourceRequirementAndTip(string unitType)
    {
        var cfg = UnitConfiguration.GetDefaultConfig(unitType);
        
        var cost = cfg.Cost;
        var gasCost = cfg.GasCost;

        if (GameCore.Instance.GetMyResource("Money") < cost)
        {
            AddTip("晶矿不足");
            return false;
        }
        else if (GameCore.Instance.GetMyResource("Gas") < gasCost)
        {
            AddTip("瓦斯不足");
            return false;
        }

        return true;
    }
}
