using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using SCM;
using UnityEngine.AI;
using System.Linq;
using Swift.Math;
using System;

public class UnitCreator : MonoBehaviour
{
    public EffectCreator EC;
    public AudioCreator AC;

    // 表示视野范围
    public GameObject VisionRange;

    // 表示阴影
    public GameObject ShadowPlane;
    public GameObject RadarSignShaowPlane;

    // 选中光效
    public GameObject SelRing;

    // 地表
    public MapGround MG;

    public GameObject AttackRange;

    // 所有已经创建的单位模型
    StableDictionary<string, MapUnit> units = new StableDictionary<string, MapUnit>();

    private MapUnit exampleUnit = null;
    private string lastExampleUnitType = null;
    private string ExampleUnitUid = "ExampleUnitUid_01";    // 样例模型临时的uid

    // 创建一个指定类型的模型
    public MapUnit CreateModel(Unit u)
    {
        var isNeutral = u.Player == 0;
        var isMine = u.Player == GameCore.Instance.MePlayer;
        var withVision = u.cfg.VisionRadius > 0 && (isMine || isNeutral);

        return CreateModel(u.UnitType, u.UID, u.UnitType != "Radar", withVision);
    }

    // 显示示例模型
    public void ShowExampleUnit(string type, Vector3 pos)
    {
        // 类型改变,先销毁之前的模型
        if (lastExampleUnitType != type && exampleUnit != null)
        {
            DestroyExampleUnit();
            exampleUnit = CreateExampleUnit(type, pos);
            UpdateExampleUnitInfo(pos);
            return;
        }

        // 模型存在的话,更新坐标
        if (null != exampleUnit)
        {
            UpdateExampleUnitInfo(pos);
            return;
        }

        exampleUnit = CreateExampleUnit(type, pos);
        UpdateExampleUnitInfo(pos);
    }

    void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (var i = 0; i < obj.transform.childCount; i++)
            SetLayer(obj.transform.GetChild(i).gameObject, layer);
    }

    // 创建样例单位
    public MapUnit CreateExampleUnit(string type, Vector3 pos)
    {
        var model = transform.Find(type).gameObject;

        GameObject go = Instantiate(model) as GameObject;
        go.gameObject.SetActive(true);
        go.transform.SetParent(gameObject.transform.parent.GetComponent<MapScene>().Units);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        SetLayer(go, LayerMask.NameToLayer("BattleUnit"));

        var u = UnitFactory.Instance.Create(ExampleUnitUid);
        u.Room = GameCore.Instance.CurrentRoom;
        u.UnitType = type;
        u.Player = GameCore.Instance.MePlayer;
        u.Dir = u.Player == 2 ? 270 : 90;
        u.Pos = new Vec2(pos.x, pos.z);

        var mu = go.GetComponent<MapUnit>();

        // 添加阴影
//        AddShadow(go.transform);

        // 守卫和炮塔正式建造前显示其攻击范围
        if (type == "FireGuard" || type == "TowerGuard")
        {
            AddVisionRange(go.transform);
//            AddAttackRange(go.transform);
            mu.WithVision = true;
        }

        mu.IsExampleUnit = true;
        mu.U = u;

        lastExampleUnitType = type;

        AddCoverArea(go.transform, Color.blue);

        return mu;
    }

    //private void AddShadow(Transform parent)
    //{
    //    var shadow = Instantiate(ShadowPlane) as GameObject;

    //    shadow.SetActive(true);
    //    shadow.name = "Shadow";
    //    shadow.transform.SetParent(parent);
    //    shadow.transform.localPosition = Vector3.zero;
    //    shadow.transform.localScale = Vector3.one;
    //    shadow.transform.rotation = Quaternion.identity;
    //}

    // 添加占地面积区域
    public void AddCoverArea(Transform parent, Color c)
    {
        var model = transform.Find("CoverArea").gameObject;

        GameObject go = Instantiate(model) as GameObject;
        go.SetActive(true);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * 0.1f;

        SetLayer(go, LayerMask.NameToLayer("BattleUnit"));

        go.transform.GetComponent<MeshRenderer>().material.SetFloat("_CellDensity", 0);
        go.transform.GetComponent<MeshRenderer>().material.SetColor("_MainTint", c);
    }

    private void AddVisionRange(Transform parent)
    {
        var vision = Instantiate(VisionRange) as GameObject;
        vision.name = "Vision";
        vision.transform.SetParent(parent);
        vision.transform.localPosition = Vector3.zero;
        vision.transform.localRotation = Quaternion.identity;
        vision.SetActive(true);
    }

    // 显示攻击范围
    private void AddAttackRange(Transform parent)
    {
        var attackRange = Instantiate(AttackRange) as GameObject;

        attackRange.SetActive(true);
        attackRange.name = "AttackRange";
        attackRange.transform.SetParent(parent);
        attackRange.transform.localPosition = Vector3.zero;
        attackRange.transform.localScale = Vector3.one;
        attackRange.transform.rotation = Quaternion.identity;
    }

    // 更新示例模型信息，按需添加参数
    private void UpdateExampleUnitInfo(Vector3 pos)
    {
        var pt = UIManager.Instance.World2UI(pos);
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)pt.x, (float)pt.y, 0));
        var hitPt = pos - 18 * ray.direction;

        exampleUnit.gameObject.transform.localPosition = hitPt;
    }

    // 销毁样例单位
    public void DestroyExampleUnit()
    {
        if (null == exampleUnit)
            return;

        Destroy(exampleUnit.gameObject);
    }

    public MapUnit CreateModel(string type, string uid, bool withShadow, bool withVision)
    {
        GameObject go = null;
        var model = transform.Find(type).gameObject;
        go = Instantiate(model) as GameObject;

        var vision = Instantiate(VisionRange) as GameObject;
        vision.name = "Vision";
        vision.transform.SetParent(go.transform);
        vision.transform.localPosition = Vector3.zero;
        vision.transform.localRotation = Quaternion.identity;
        vision.SetActive(withVision);

        //if (withShadow)
        //{
        //    var shadow = (type == "RadarSign" ? Instantiate(RadarSignShaowPlane) : Instantiate(ShadowPlane))
        //         as GameObject;
        //    shadow.name = "Shadow";
        //    shadow.transform.SetParent(go.transform);
        //    shadow.transform.localPosition = Vector3.zero;
        //    shadow.transform.localScale = Vector3.one;
        //    shadow.transform.rotation = Quaternion.identity;
        //    shadow.SetActive(true);
        //}

        var ani = go.GetComponent<AnimationPlayer>();
        if (ani != null)
            ani.UC = this;

        var mu = go.GetComponent<MapUnit>();
        mu.WithVision = withVision;
        units[uid] = mu;
        
        return mu;
    }

    public MapUnit[] AllModels()
    {
        return units.Values.ToArray();
    }

    // 获取一个指定 UID 的模型
    public MapUnit GetModel(string uid)
    {
        return units.ContainsKey(uid) ? units[uid] : null;
    }

    // 解除 UnitCreator 对一个模型的托管，由外面自己管理
    public MapUnit UnhandleModel(string uid)
    {
        var mu = units[uid];
        units.Remove(uid);
        return mu;
    }

    // 移除一个模型
    public void RemoveModel(string uid, float destroyDelay = 0)
    {
        var mu = UnhandleModel(uid);

        if (destroyDelay <= 0)
            DestroyModel(mu);
        else
            StartCoroutine(DelayDestroyModel(mu, destroyDelay));
    }

    IEnumerator DelayDestroyModel(MapUnit mu, float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyModel(mu);
    }

    // 彻底销毁一个模型
    private void DestroyModel(MapUnit mu)
    {
        var lst = new List<Transform>();

        if (mu.Attack01Stub != null && mu.Attack01Stub.childCount > 0)
            FC.For(mu.Attack01Stub.childCount, (i) => { lst.Add(mu.Attack01Stub.GetChild(i)); });

        if (mu.Attack02Stub != null && mu.Attack02Stub.childCount > 0)
            FC.For(mu.Attack02Stub.childCount, (i) => { lst.Add(mu.Attack02Stub.GetChild(i)); });

        foreach (var c in lst)
        {
            var ae = c.GetComponent<AttackEffect>();
            if (ae != null)
                EC.DestroyEffect(ae);

            var aa = c.GetComponent<AttackAudio>();
            if (aa != null)
                AC.DestroyAudio(aa);
        }

        mu.MG = null;
        mu.transform.SetParent(null);
        mu.gameObject.SetActive(false);
        Destroy(mu.gameObject);
    }

    // 销毁所有模型
    public void DestroyAll()
    {
        foreach (var u in units.Values)
            Destroy(u.gameObject);

        units.Clear();

        MG.UPSUI.Hide();
    }
}
