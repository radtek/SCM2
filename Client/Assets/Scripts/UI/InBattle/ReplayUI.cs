using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using Swift.Math;

public class ReplayUI : UIBase {

    public RectTransform BC;
    public Image PrograssBar;
    public Text SpeedUpText;
    public MainCamera MC;
    public MapGround MG;

    const int speedUpMax = 8;
    int speedUp = 1;

    BattleReplayer br;

    public void ResetAll(bool inReplay)
    {
        br = GameCore.Instance.Get<BattleReplayer>();
        PrograssBar.fillAmount = 0;
        speedUp = 1;
        br.SpeedUpFactor = speedUp;
        SpeedUpText.text = speedUp + "X";
        MC.TurnOnBattleFog = !inReplay;
        gameObject.SetActive(inReplay);
    }

    public void ChangeSpeedUp()
    {
        var s = speedUp == 0 ? 1 : speedUp * 2;
        if (s > speedUpMax)
            return;

        speedUp = s;
        SpeedUpText.text = s + "X";
        br.SpeedUpFactor = speedUp;
    }

    public void ReduceSpeed()
    {
        if (speedUp == 0)
            return;

        var s = speedUp == 1 ? 0 : speedUp / 2;

        speedUp = s;
        SpeedUpText.text = s + "X";
        br.SpeedUpFactor = speedUp;
    }

    private void Update()
    {
        PrograssBar.fillAmount = br.Prograss;
    }
}
