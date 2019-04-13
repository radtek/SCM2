using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SCMText : Text {

    public static void LoadDict(string trans, Dictionary<string, string> d)
    {
        var txtRes = Resources.Load(trans) as TextAsset;
        if (txtRes == null)
            return;

        var txt = txtRes.text;
        var ls = txt.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var l in ls)
        {
            var es = l.Split("\t".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
            if (es.Length >= 2)
            {
                es[0] = es[0].Replace("\\n", "\n");
                d[es[0]] = es[1].Replace("\\n", "\n");
            }
        }
    }

    public static bool DoTranslation = true;
    public static Dictionary<string, string> dict = new Dictionary<string, string>();

    public static string T(string txt)
    {
        return DoTranslation && dict.ContainsKey(txt) ? dict[txt] : txt;
    }

    public override string text
    {
        get
        {
            return T(base.text);
        }

        set
        {
            base.text = value;
        }
    }
}
