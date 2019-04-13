using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SCM;

public class DescUI : UIBase
{
    // info
    public Text AttackType01;
    public Text AttackType02;
    public Text Cost;
    public Text GasCost;
    public Text ConstructingTime;
    public Text MaxHp;
    public Text Defence;
    public Text AttackPower;
    public Text Desc;

    public Transform Content;
    public GameObject Item;
    public GameObject HideBtn;

    public GameObject UnlockUnitUI;

    public CardUI CUI;
    public MainMenuUI MMUI;
    public UnitUnlockUI UUUI;

    private Dictionary<string, GameObject> ItemDic = null;

    private string OrgType;

    public void Refresh()
    {
        ClearContent();
        ClearDescInfo();
        Show(OrgType);
    }

    public void Show(string orgType)
    {
        base.Show();

        OrgType = orgType;

        BuildAllVariants(orgType);

        var meInfo = GameCore.Instance.MeInfo;
        ItemDic[meInfo.Variants[orgType]].GetComponent<Toggle>().isOn = true;
    }

    public void OnHideBtn()
    {
        ClearContent();
        ClearDescInfo();

        Hide();

        CUI.HideMark();
    }

    private void BuildAllVariants(string orgType)
    {
        ItemDic = new Dictionary<string, GameObject>();

        var cfgKeys = UnitConfiguration.AllUnitTypes;

        for (int i = 0; i < cfgKeys.Length; i++)
        {
            var info = UnitConfiguration.GetDefaultConfig(cfgKeys[i]);

            if (string.IsNullOrEmpty(info.OriginalType))
            {
                if (cfgKeys[i] == orgType)
                    CreateItem(cfgKeys[i]);
            }
            else if (info.OriginalType == orgType)
                CreateItem(cfgKeys[i]);
        }
    }

    private void CreateItem(string type)
    {
        var go = GameObject.Instantiate(Item) as GameObject;
        go.name = type;
        go.SetActive(true);
        go.transform.SetParent(Content);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var icon = go.transform.Find("Icon");

        Sprite img = Resources.Load<Sprite>(@"Texture\UnitCard\" + type);

        if (null != img)
        {
            icon.gameObject.SetActive(true);

            var image = icon.GetComponent<Image>();
            image.sprite = img;
            image.SetNativeSize();
        }
        else
        {
            icon.gameObject.SetActive(false);
        }

        go.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => OnVariantChanged(isOn, go));

        ItemDic[type] = go;

        var meInfo = GameCore.Instance.MeInfo;

        if (!meInfo.Units[type])
        {
            go.transform.Find("Lock").gameObject.SetActive(true);

            go.transform.Find("Lock").GetComponent<Button>().onClick.AddListener(() => OnClickLockBtn(type));

            go.GetComponent<Toggle>().enabled = false;
        }
    }

    public void OnClickLockBtn(string type)
    {
        UUUI.Show(type);
    }

    private void OnVariantChanged(bool isOn, GameObject go)
    {
        if (!isOn)
        {
            go.transform.Find("Mark").gameObject.SetActive(false);
            return;
        }

        ClearDescInfo();

        go.transform.Find("Mark").gameObject.SetActive(true);

        var type = go.name;

        ShowDescInfo(type);

        var info = GameCore.Instance.MeInfo;
        var orgType = info.Variants.ContainsKey(type) ? type : UnitConfiguration.GetDefaultConfig(type).OriginalType;
        info.Variants[orgType] = type;

        UserManager.SyncVariants2Server();
    }

    private void ShowDescInfo(string unitType)
    {
        var info = UnitConfiguration.GetDefaultConfig(unitType);

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

    private void ClearContent()
    {
        foreach (Transform t in Content)
            Destroy(t.gameObject);
    }
}
