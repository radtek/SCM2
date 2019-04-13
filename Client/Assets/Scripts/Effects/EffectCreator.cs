using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using System.Linq;
using System;

public class EffectCreator : MonoBehaviour
{
    StableDictionary<AttackEffect, float> obj2LifeTime = new StableDictionary<AttackEffect, float>();

    // 创建一个指定类型的模型
    public void CreateEffect(string type, Transform parent, Vector3 toPos, float lifeTime)
    {
        GameObject go = null;
        var goTrans = transform.Find(type);
        if (goTrans == null)
            return;

        var model = goTrans.gameObject;
        go = Instantiate(model) as GameObject;
        var ae = go.GetComponent<AttackEffect>();
        ae.To = toPos;
        go.transform.SetParent(parent);
        if (!ae.DoNotScale)
            go.transform.localScale = parent.localScale;
        go.transform.localRotation = parent.localRotation;

        if (ae.MovePath == AttackEffect.MovePathType.OnAttacker)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
        }
        else if (ae.MovePath == AttackEffect.MovePathType.OnTarget)
        {
            go.transform.position = toPos;
        }
        else if (ae.MovePath == AttackEffect.MovePathType.FromGun2Target)
        {
            go.transform.position = parent.position;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.right, toPos - parent.position);
            ae.To = toPos;
        }

        obj2LifeTime[ae] = lifeTime;
        go.SetActive(true);
    }

    public void Clear()
    {
        foreach (var ae in obj2LifeTime.KeyArray)
        {
            if (ae != null)
                Destroy(ae.gameObject);
        }

        obj2LifeTime.Clear();
    }

    public void OnTimeElapsed(float te)
    {
        foreach (var ae in obj2LifeTime.KeyArray)
        {
            var lt = obj2LifeTime[ae];
            lt -= te;
            if (lt <= 0)
            {
                obj2LifeTime.Remove(ae);
                ae.transform.SetParent(null);
                Destroy(ae.gameObject);
            }
            else
            {
                obj2LifeTime[ae] = lt;
                if (ae.MovePath == AttackEffect.MovePathType.FromGun2Target)
                {
                    var d = ae.To - ae.transform.position;
                    var v = 80;
                    if (d.magnitude < v / 20)
                        obj2LifeTime[ae] = 0;
                    else
                    {
                        d.Normalize();
                        ae.transform.position += d * te * v;
                        ae.transform.rotation = Quaternion.FromToRotation(Vector3.right, ae.To - ae.transform.position);
                    }
                }
            }
        }
    }

    public void DestroyEffect(AttackEffect ae)
    {
        ae.transform.SetParent(null);
        ae.gameObject.SetActive(false);
        obj2LifeTime[ae] = 0;
    }
}
