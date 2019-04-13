using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Swift;
using UnityEngine.UI;

[ExecuteInEditMode]
[CanEditMultipleObjects]

public class TranslationHelper : ScriptableObject
{
    static Text tmp;
    static Dictionary<string, string> dict = new Dictionary<string, string>();

    [MenuItem("SCM/Translation")]
    static void TranslationPrepare()
    {
        tmp = (new GameObject("tmp")).AddComponent<Text>();
        SCMText.LoadDict("translation", dict);

        var xa = GameObject.Find("Root").GetComponentInChildren<XAdapater>();
        var doTranslation = !xa.DoTranslation;

        foreach (Transform c in GameObject.Find("Root").transform)
            TranslationInChildren(c.gameObject, doTranslation);

        DestroyImmediate(tmp.gameObject);
        tmp = null;

        xa.DoTranslation = doTranslation;

        Debug.Log("finished: " + (doTranslation ? "translated" : "untranslated"));
        AssetDatabase.SaveAssets();
    }

    static void TranslationInChildren(GameObject go, bool doTranslation)
    {
        foreach (Transform c in go.transform)
            TranslationInChildren(c.gameObject, doTranslation);

        var t = go.GetComponent<Text>();
        var st = go.GetComponent<SCMText>();
        if (t != null && st == null)
        {
            CopyTextProperties(t, tmp);
            DestroyImmediate(t);
            st = go.AddComponent<SCMText>();
            CopyTextProperties(tmp, st);
        }

        var img = go.GetComponent<Image>();
        if (img != null && img.sprite != null && img.sprite.texture != null)
        {
            var tex = AssetDatabase.GetAssetPath(img.sprite.texture);
            var n = tex.LastIndexOf(".");
            if (n < 0)
                return;

            var t1 = tex.Substring(0, n);
            var t2 = tex.Substring(n);
            if (t1.EndsWith("_en") && !doTranslation)
                t1 = t1.Substring(0, t1.Length - 3);
            else if (!t1.EndsWith("_en") && doTranslation)
                t1 += "_en";

            var stex = AssetDatabase.LoadAssetAtPath<Sprite>(t1 + t2);
            if (stex == null)
                return;

            img.sprite = stex;
        }
    }

    static void CopyTextProperties(Text from, Text to)
    {
        to.enabled = from.enabled;
        to.rectTransform.anchorMin = from.rectTransform.anchorMin;
        to.rectTransform.anchorMax = from.rectTransform.anchorMax;
        to.rectTransform.anchoredPosition = from.rectTransform.anchoredPosition;
        to.rectTransform.sizeDelta = to.rectTransform.sizeDelta;

        to.text = from.text;

        to.fontStyle = from.fontStyle;
        to.fontSize = from.fontSize;
        to.font = from.font;
        to.lineSpacing = from.lineSpacing;
        to.supportRichText = from.supportRichText;

        to.alignment = from.alignment;
        to.alignByGeometry = from.alignByGeometry;
        to.verticalOverflow = from.verticalOverflow;
        to.horizontalOverflow = from.horizontalOverflow;
        to.resizeTextForBestFit = from.resizeTextForBestFit;
        to.color = from.color;
        to.material = from.material;
        to.raycastTarget = false;
    }
}