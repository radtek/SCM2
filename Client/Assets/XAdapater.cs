using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XAdapater : MonoBehaviour {

    public Camera[] Cameras;
    public RectTransform[] UIAdapters;
    public GameObject TopBottonBanner;
    public bool DoTranslation = false;

	// Use this for initialization
	void Awake () {
        var w = Screen.width;
        var h = Screen.height;

        if (h / w >= 2)
            AdapterX();
        else
            AdapterNormal();

        SCMText.DoTranslation = DoTranslation;
    }

    void AdapterNormal()
    {
        TopBottonBanner.SetActive(false);

        foreach (var cam in Cameras)
            cam.orthographicSize = 56.8f;

        foreach (var rt in UIAdapters)
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    void AdapterX()
    {
        TopBottonBanner.SetActive(true);

        foreach (var cam in Cameras)
            cam.orthographicSize = 60;

        foreach (var rt in UIAdapters)
        {
            rt.offsetMin = new Vector2(0, 32);
            rt.offsetMax = new Vector2(0, -32);
        }
    }
}
