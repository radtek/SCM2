using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HistoryCamera : MonoBehaviour {

    public RenderTexture BuildingTex;
    public RenderTexture VisionTex;
    public Material CameraMat;

    public void Clear()
    {
        clearFlag = true;
    }

    bool clearFlag = false;
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        CameraMat.SetTexture("_VisionTex", VisionTex);
        CameraMat.SetTexture("_BuildingTex", BuildingTex);
        Graphics.Blit(source, destination, CameraMat, clearFlag ? 1 : 0);
        clearFlag = false;
    }
}
