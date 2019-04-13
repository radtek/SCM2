using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swift;
using System.Linq;
using System;

public class AudioCreator : MonoBehaviour
{
    StableDictionary<AttackAudio, float> obj2LifeTime = new StableDictionary<AttackAudio, float>();

    public void CreateAudio(string type, Transform parent, float lifeTime)
    {
        GameObject go = null;
        var goTrans = transform.Find(type);
        if (goTrans == null)
            return;

        var model = goTrans.gameObject;
        go = Instantiate(model) as GameObject;
        var aa = go.GetComponent<AttackAudio>();
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        obj2LifeTime[aa] = lifeTime;
        go.SetActive(true);
    }
        
    public void CreateUnitCreateAudio(string type, Transform parent)
    {
        GameObject go = null;
        var goTrans = transform.Find(type);
        if (goTrans == null)
            return;

        var model = goTrans.gameObject;
        go = Instantiate(model) as GameObject;
        var aa = go.GetComponent<UnitCreateAudio>();
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;

        go.SetActive(true);
    }

    public void Clear()
    {
        foreach (var aa in obj2LifeTime.KeyArray)
        {
            if (aa != null)
                Destroy(aa.gameObject);
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
            }
        }
    }

    public void DestroyAudio(AttackAudio aa)
    {
        aa.transform.SetParent(null);
        aa.gameObject.SetActive(false);
        obj2LifeTime[aa] = 0;
    }

}
