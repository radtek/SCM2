using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Swift;
using UnityEngine.UI;

[ExecuteInEditMode]
[CanEditMultipleObjects]
public class EffectHelper : MonoBehaviour {

    [MenuItem("SCM/AutoProcessUnitModel")]
    static void AutoProcessUnitModel()
    {
        var cnt = 0;
        foreach (var go in Selection.gameObjects)
        {
            var mu = go.GetComponent<MapUnit>();
            if (mu == null)
                continue;

            var a01 = FindFirstChildren(go.transform, "attack01");
            var a02 = FindFirstChildren(go.transform, "attack02");

            if (a01 != null)
            {
                mu.Attack01Stub = a01;
                Debug.Log("find attack01 in " + mu.transform.name);
            }

            if (a02 != null)
            {
                mu.Attack02Stub = a02;
                Debug.Log("find attack02 in " + mu.transform.name);
            }

            cnt = (a01 != null || a01 != null) ? cnt + 1 : cnt;

            var normalUnitShader = Shader.Find("SCM/NormalUnit");
            Debug.Log("normalUnitShader is null ? " + normalUnitShader == null);
            foreach (var sr in mu.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (var mat in sr.sharedMaterials)
                    mat.shader = normalUnitShader;

                sr.receiveShadows = false;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
            {
                var node = mr.gameObject;
                var mt = mr.sharedMaterial;
                var mf = mr.GetComponent<MeshFilter>();
                var mesh = mf.sharedMesh;
                DestroyImmediate(mr);
                DestroyImmediate(mf);
                var smr = node.AddComponent<SkinnedMeshRenderer>();
                smr.sharedMesh = mesh;
                smr.sharedMaterial = mt;
            }

            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                smr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                smr.receiveShadows = true;
            }
        }

        Debug.Log(cnt + " nodes processed");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Transform FindFirstChildren(Transform t, string name)
    {
        Transform theChild = null;
        FC.For(t.childCount, (i) =>
        {
            var c = t.GetChild(i);
            if (c.name == name)
                theChild = c;
            else
                theChild = FindFirstChildren(c, name);
        }, () => theChild == null);

        return theChild;
    }
}
