using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Swift.Math;
using System.Linq;
using SCM;
using Swift;

public class CardUI : UIBase
{
    public DescUI DUI;

    public GameObject Content;
    public List<GameObject> UnitList;

    public GameObject FortressItem;
    public GameObject BarrackItem;
    public GameObject BioTechItem;
    public GameObject FactoryItem;
    public GameObject VelTechItem;
    public GameObject AirportItem;
    public GameObject AirTechItem;

    public GameObject hideWithCPList1;
    public GameObject hideWithBPList1;
    public GameObject showWithBTPList1;
    public GameObject showWithBTPList2;
    public GameObject showWithBTPList3;
    public GameObject hideWithBTPList1;
    public GameObject hideWithBTPList2;
    public GameObject hideWithVTPList1;
    public GameObject hideWithFPList1;

    public GameObject CPList;    // 指挥中心解锁单位列表
    public GameObject BPList;    // 兵营解锁单位列表
    public GameObject BTPList;   // 生物科技解锁单位列表
    public GameObject FPList;    // 工厂解锁单位列表
    public GameObject VTPList;   // 车辆科技解锁单位列表
    public GameObject APList;    // 飞机场解锁列表
    public GameObject ATPList;   // 飞行科技解锁单位列表

    private bool showCPList = false;
    private bool showBPList = false;
    private bool showBTPList = false;
    private bool showFPList = false;
    private bool showVTPList = false;
    private bool showAPList = false;
    private bool showATPList = false;

    private string curSelUnit = null;

    protected override void StartOnlyOneTime()
    {
        UserManager.onSyncUnits2Server += SetAllUnitsLockState;
    }

    public void SetAllUnitsLockState()
    {
        var meInfo = GameCore.Instance.MeInfo;

        foreach (var go in UnitList)
        {
            if (!meInfo.Units[go.name])
                go.transform.Find("Lock").gameObject.SetActive(true);
            else
                go.transform.Find("Lock").gameObject.SetActive(false);
        }
    }

    public void OnCommandBtn()
    {
        showCPList = !showCPList;

        FortressItem.transform.Find("Mark").gameObject.SetActive(showCPList);

        CPList.SetActive(showCPList);
        hideWithCPList1.SetActive(!showCPList);

        StartCoroutine(Refresh());
    }

    public void OnBarrackBtn()
    {
        showBPList = !showBPList;

        BarrackItem.transform.Find("Mark").gameObject.SetActive(showBPList);

        if (showBTPList)
        {
            showWithBTPList1.SetActive(true);
            showWithBTPList2.SetActive(true);
            showWithBTPList3.SetActive(true);
            hideWithBTPList1.SetActive(false);
            hideWithBTPList2.SetActive(false);
        }

        BPList.SetActive(showBPList);
        hideWithBPList1.SetActive(!showBPList);

        StartCoroutine(Refresh());
    }

    public void OnBioTechBtn()
    {
        showBTPList = !showBTPList;

        BioTechItem.transform.Find("Mark").gameObject.SetActive(showBTPList);

        BTPList.SetActive(showBTPList);
        showWithBTPList1.SetActive(showBTPList);
        showWithBTPList2.SetActive(showBTPList);
        hideWithBTPList1.SetActive(!showBTPList);
        hideWithBTPList2.SetActive(!showBTPList);
        showWithBTPList3.SetActive(showBTPList);

        StartCoroutine(Refresh());
    }

    public void OnFactorBtn()
    {
        showFPList = !showFPList;

        FactoryItem.transform.Find("Mark").gameObject.SetActive(showFPList);

        FPList.SetActive(showFPList);
        hideWithFPList1.SetActive(!showFPList);

        StartCoroutine(Refresh());
    }

    public void OnVelTechBtn()
    {
        showVTPList = !showVTPList;

        VelTechItem.transform.Find("Mark").gameObject.SetActive(showVTPList);

        VTPList.SetActive(showVTPList);
        hideWithVTPList1.SetActive(!showVTPList);

        StartCoroutine(Refresh());
    }

    public void OnAirportBtn()
    {
        showAPList = !showAPList;

        AirportItem.transform.Find("Mark").gameObject.SetActive(showAPList);

        APList.SetActive(showAPList);

        StartCoroutine(Refresh());
    }

    public void OnAirTechBtn()
    {
        showATPList = !showATPList;

        AirTechItem.transform.Find("Mark").gameObject.SetActive(showATPList);

        ATPList.SetActive(showATPList);

        StartCoroutine(Refresh());
    }

    public void OnUnitBtn(string UnitType)
    {
        ShowMark(UnitType);
        DUI.Show(UnitType);

        curSelUnit = UnitType;
    }

    private IEnumerator Refresh()
    {
        yield return new WaitForEndOfFrame();
        var csf = Content.GetComponent<ContentSizeFitter>();
        csf.enabled = false;
        yield return new WaitForEndOfFrame();
        csf.enabled = true;
    }

    private void ShowMark(string name)
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            if (UnitList[i].name == name)
                UnitList[i].transform.Find("Mark").gameObject.SetActive(true);
        }
    }

    public void HideMark()
    {
        for (int i = 0; i < UnitList.Count; i++)
        {
            if (UnitList[i].name == curSelUnit)
                UnitList[i].transform.Find("Mark").gameObject.SetActive(false);
        }
    }
}
