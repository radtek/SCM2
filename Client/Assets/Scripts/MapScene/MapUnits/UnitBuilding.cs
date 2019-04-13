using System;
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
/// 建筑模型
/// </summary>
public class UnitBuilding : MapUnit
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

        if (u.InDestroying)
            return;
        else if (!U.BuildingCompleted)
            ShowCancelSel(pt);
        else if (U.cfg.ReconstructTo != null)
            ShowLevelUpSel(U.cfg.ReconstructTo, pt);
        else if (u.UnitType == "Barrack" || u.UnitType == "Factory" || u.UnitType == "Airport")
            ShowBuildAccessory(pt);
        else
            // ShowDestroyButton(pt);
            MG.OnClick(pt, wp);
    }

    //void ShowDestroyButton(Vec2 pt)
    //{
    //    var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
    //    ui.Pos = pt;
    //    ui.Choices = new string[] { "DestroyBuilding" };
    //    ui.ChoicesName = new string[] { "回收" };
    //    ui.Refresh();
    //    ui.OnChoiceSel = (toType) =>
    //    {
    //        ShowReDestroySel(pt);
    //    };
    //}

    void ShowBuildAccessory(Vec2 pt)
    {
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = new string[] { "Accessory", /* "DestroyBuilding" */ };
        ui.ChoicesName = new string[] { "+1 仓库", /* "回收" */ };
        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            if (toType == "Accessory")
            {
                if (u.Room.FindNextAccessoryPos(u, "Accessory") == Vec2.Zero)
                {
                    AddTip("无法建造更多仓库");
                    return;
                }
                else if (!MG.CheckResourceRequirementAndTip(toType))
                    return;

                var conn = GameCore.Instance.ServerConnection;
                var buff = conn.Send2Srv("ConstructAccessory");
                buff.Write(U.UID);
                buff.Write("Accessory");
                conn.End(buff);
            }
            //else if (toType == "DestroyBuilding")
            //{
            //    ShowReDestroySel(pt);
            //}
        };
    }

    void ShowCancelSel(Vec2 pt)
    {
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = new string[] { "Cancel" };
        ui.ChoicesName = new string[] { "取消" };
        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            if (U.BuildingCompleted)
                return;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("CancelBuilding");
            buff.Write(U.UID);
            conn.End(buff);
        };
    }

    void ShowReDestroySel(Vec2 pt)
    {
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = new string[] { "Confirm", "Cancel" };
        ui.ChoicesName = new string[] { "确认", "取消" };
        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            if (toType == "Cancel")
                return;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("DestroyBuilding");
            buff.Write(U.UID);
            conn.End(buff);
        };
    }

    void ShowLevelUpSel(string[] newTypes, Vec2 pt)
    {
        // var btns = new string[newTypes.Length + 1];
        // newTypes.CopyTo(btns, 0);
        // btns[newTypes.Length] = "DestroyBuilding";
        var btns = newTypes;

        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = btns;
        ui.ChoicesName = btns.ToArray((i, t, skipAct) =>
        {
            //if (t == "DestroyBuilding")
            //    return "回收";
            
            var ccfg = UnitConfiguration.GetDefaultConfig(t);
            return ccfg.DisplayName;
        });
        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            if (toType == "DestroyBuilding")
            {
                ShowReDestroySel(pt);
            }
            else if (!U.BuildingCompleted || !MG.CheckPrerequisitesAndTip(toType) || !MG.CheckResourceRequirementAndTip(toType))
                return;
            else
            {
                var conn = GameCore.Instance.ServerConnection;
                var buff = conn.Send2Srv("ReconstructBuilding");
                buff.Write(U.UID);
                buff.Write(toType);
                conn.End(buff);
            }
        };
    }
}
