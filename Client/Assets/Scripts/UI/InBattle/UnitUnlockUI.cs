using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SCM;

public class UnitUnlockUI : UIBase
{
    // info
    public Text Title;
    public Text AttackType01;
    public Text AttackType02;
    public Text Cost;
    public Text GasCost;
    public Text ConstructingTime;
    public Text MaxHp;
    public Text Defence;
    public Text AttackPower;
    public Text Desc;

    public Button ConfirmBtn;
    public Button CancelBtn;

    public MainMenuUI MMUI;
    public DescUI DUI;

    private string Type;
    private int Need;
    private int Stage;

    private bool IsEnough;

    protected override void StartOnlyOneTime()
    {
        base.StartOnlyOneTime();

        ConfirmBtn.onClick.AddListener(() => OnUnlockConfirmBtn());
        CancelBtn.onClick.AddListener(() => OnUnlockCancelBtn());
    }

    public void Show(string type)
    {
        base.Show();

        Type = type;

        IsEnough = CanUnlockUnit();
        ShowDescInfo(type);
    }

    private bool CanUnlockUnit()
    {
        var meInfo = GameCore.Instance.MeInfo;
        var ulcfgs = UnitConfiguration.Ulcfgs;

        for (int i = 0; i < ulcfgs.Count; i++)
        {
            if (!meInfo.UUnlocks[ulcfgs[i]])
            {
                var left = meInfo.Integration - meInfo.IntegrationCost;

                Stage = ulcfgs[i];

                if (i == 0)
                    Need = ulcfgs[i];
                else
                    Need = ulcfgs[i] - ulcfgs[i - 1];

                if (left >= Need)
                    return true;
                else
                    return false;
            }
        }

        return false;
    }

    // 确认解锁
    public void OnUnlockConfirmBtn()
    {
        if (!IsEnough)
        {
            AddTip("当前积分余额不足！", 28);
            return;
        }

        var meInfo = GameCore.Instance.MeInfo;

        meInfo.Units[Type] = true;
        meInfo.UUnlocks[Stage] = true;
        meInfo.IntegrationCost += Need;

        UserManager.SyncUnits2Server();
        UserManager.SyncUUnlocks2Server();
        UserManager.SyncIntegrationCost2Server();

        AddTip(string.Format("解锁新单位{0}", UnitConfiguration.GetDefaultConfig(Type).DisplayName), 28);

        DUI.Refresh();
        MMUI.ShowUserInfo();

        ClearDescInfo();
        Hide();
    }

    // 取消解锁
    public void OnUnlockCancelBtn()
    {
        ClearDescInfo();
        Hide();
    }

    private void ShowDescInfo(string unitType)
    {
        var info = UnitConfiguration.GetDefaultConfig(unitType);

		if(Need>1)
        Title.text = string.Format("{2} {0}{3} {1} {4}", SCMText.T(info.DisplayName+" "), Need,
            SCMText.T("解锁"), SCMText.T("将消耗"), SCMText.T("积分 "));
		else
			Title.text = string.Format("{2} {0}{3} {1} {4}", SCMText.T(info.DisplayName+" "), Need,
				SCMText.T("解锁"), SCMText.T("将消耗"), SCMText.T("积分"));

        AttackType01.text = info.CanAttackGround ? "<color=green>是</color>" : "<color=red>否</color>";
        AttackType02.text = info.CanAttackAir ? "<color=green>是</color>" : "<color=red>否</color>";
        Cost.text = info.Cost.ToString();
        GasCost.text = info.GasCost.ToString();

        AttackPower.text = info.CanAttackGround ? info.AttackPower[0].ToString() : "";
        AttackPower.text += info.CanAttackAir && info.CanAttackGround ? ", " : "";
        AttackPower.text += info.CanAttackAir ? info.AttackPower[1].ToString() : "";

        ConstructingTime.text = info.ConstructingTime.ToString() + "s";
        MaxHp.text = info.MaxHp.ToString();
        Defence.text = info.Defence.ToString();

        Desc.text = info.Desc;
    }

    private void ClearDescInfo()
    {
        AttackType01.text = "";
        AttackPower.text = "";
        AttackType02.text = "";
        MaxHp.text = "";
        Defence.text = "";
        ConstructingTime.text = "";
        Cost.text = "";
        GasCost.text = "";
    }
}
