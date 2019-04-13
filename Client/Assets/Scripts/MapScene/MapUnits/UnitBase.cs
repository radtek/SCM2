using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SCM;
using Swift;
using Swift.Math;
using UnityEngine.AI;

/// <summary>
/// 主基地模型
/// </summary>
public class UnitBase : UnitBuilding
{
    // 点击生产单位
    public override void OnClick(Vec2 pt, Vector3 wp)
    {
        if (inReplay)
            return;

        if (U.Player != GameCore.Instance.MePlayer)
        {
            base.OnClick(pt, wp);
            return;
        }

        if (null != MG)
            MG.UPSUI.Hide();

        if (IsExampleUnit)
            return;
        
        if (!U.BuildingCompleted)
            base.OnClick(pt, wp);
        else
            ShowSels(pt);
    }

    void ShowSels(Vec2 pt)
    {
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = u.UnitType == "Base" ?
            (new string[] { "CrystalMachine" }).Concat(u.cfg.ReconstructTo).ToArray() :
            new string[] { "CrystalMachine" };

        ui.ChoicesName = ui.Choices.ToArray((i, t, skipAct) =>
        {
            var ccfg = t == "CrystalMachine" ?
                    UnitConfiguration.GetDefaultConfig("CrystalMachine") :
                    UnitConfiguration.GetDefaultConfig(t);

            return ccfg.DisplayName;
        });
        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            var unitType = toType == "CrystalMachine" ? "CrystalMachine" : toType;
            if (!U.BuildingCompleted ||
                    !MG.CheckPrerequisitesAndTip(unitType)
                    || !MG.CheckResourceRequirementAndTip(unitType))
                return;

            if (toType == "CrystalMachine")
            {
                if (U.Room.FindNextCrystalMachinePos(U) == Vec2.Zero)
                {
                    AddTip("没有多余的矿机位置");
                    return;
                }

                var conn = GameCore.Instance.ServerConnection;
                var buff = conn.Send2Srv("ConstructCrystalMachine");
                buff.Write(U.UID);
                conn.End(buff);
            }
            else // 升级基地
            {
                var conn = GameCore.Instance.ServerConnection;
                var buff = conn.Send2Srv("ReconstructBuilding");
                buff.Write(U.UID);
                buff.Write(toType);
                conn.End(buff);
            }
        };
    }

    //public override void OnDragStarted(Vec2 pt, Vector3 wp)
    //{
    //    // 指挥中心拖动释放雷达
    //    if (!u.Room.CheckPrerequisites(u.Player, "RadarSign") || u.Player != GameCore.Instance.MePlayer)
    //        return;

    //    MG.RadarSelFlag.SetActive(true);
    //    MG.RadarSelFlag.transform.localPosition = wp + Vector3.up;
    //}

    //public override void OnDragging(Vec2 from, Vector3 fromWp, Vec2 nowPt, Vector3 nowWp)
    //{
    //    if (!MG.RadarSelFlag.activeSelf)
    //        return;

    //    // UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", false);
    //    MG.RadarSelFlag.transform.localPosition = nowWp + Vector3.up;
    //}

    //public override void DoDragEnded(Vec2 from, Vector3 fromWp, Vec2 to, Vector3 toWp)
    //{
    //    MG.RadarSelFlag.SetActive(false);

    //    if (u.cfg.IsBuilding && !u.BuildingCompleted)
    //        return;

    //    // 指挥中心拖动释放雷达
    //    if (!u.Room.CheckPrerequisites(u.Player, "RadarSign") || u.Player != GameCore.Instance.MePlayer)
    //        return;

    //    if (!MG.CheckResourceRequirementAndTip("RadarSign"))
    //        return;

    //    var conn = GameCore.Instance.ServerConnection;
    //    var buff = conn.Send2Srv("ConstructBuilding");
    //    buff.Write("RadarSign");
    //    buff.Write(new Vec2(toWp.x, toWp.z));
    //    conn.End(buff);
    //}
}
