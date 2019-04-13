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
/// 矿点
/// </summary>
public class UnitBaseStub : MapUnit
{
    // 点击生产单位
    public override void OnClick(Vec2 pt, Vector3 wp)
    {
        if (inReplay)
            return;

        if (!IsOwnerArea(pt, wp))
        {
            base.OnClick(pt, wp);
            return;
        }

        if (null != MG)
            MG.UPSUI.Hide();
        
        if (IsExampleUnit)
            return;
        
        ShowBaseConstructionSel(pt);
    }

    void ShowBaseConstructionSel(Vec2 pt)
    {
        var ui = UIManager.Instance.ShowTopUI("InBattleUI/SelectUnitUI", true) as SelectUnitUI;
        ui.Pos = pt;
        ui.Choices = new string[] { "Base" };
        ui.ChoicesName = new string[] { "基地"};

        ui.Refresh();
        ui.OnChoiceSel = (toType) =>
        {
            if (!MG.CheckPrerequisitesAndTip(toType) || !MG.CheckResourceRequirementAndTip(toType))
                return;

            var conn = GameCore.Instance.ServerConnection;
            var buff = conn.Send2Srv("ConstructBuilding");
            buff.Write(toType);
            buff.Write(U.Pos);
            conn.End(buff);
        };
    }

    // 是否在自己的区域
    private bool IsOwnerArea(Vec2 pt, Vector3 wp)
    {
        var cp = new Vec2(wp.x, wp.z);

        if (cp.y > U.Room.MapSize.y / 2 && GameCore.Instance.MePlayer == 1)
        {
            return false;
        }
        else if (cp.y < U.Room.MapSize.y / 2 && GameCore.Instance.MePlayer == 2)
        {
            return false;
        }

        return true;
    }
}
