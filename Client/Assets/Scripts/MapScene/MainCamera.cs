using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SCM;
using Swift.Math;

public class MainCamera : SCMBehaviour
{
    public int Oblique = 55;

    RenderTexture VisionTex;
    RenderTexture GroundTex;
    RenderTexture BuildingHistoryTex;
    RenderTexture UIIndicatorTex;

    public Light MainLight;
    public Camera VisionCamera1;
    public Camera VisionCamera2;
    public Camera GroundCamera;
    public Camera BulidingCamera1;
    public Camera BulidingCamera2;
    public Camera BuildingHistoryCam;
    public Camera UIIndicatorCamera;

    public Material CameraMat;
    public Canvas Canvas;

    // 开启战争迷雾
    public bool TurnOnBattleFog = true;

    protected override void StartOnlyOneTime()
    {
        var rt = Canvas.GetComponent<RectTransform>();
        var sw = (int)rt.rect.width;
        var sh = (int)rt.rect.height;

        VisionTex = new RenderTexture(sw, sh * 200 / 140, 0, RenderTextureFormat.ARGB32);
        GroundTex = new RenderTexture(sw, sh, 16, RenderTextureFormat.ARGB32);
        UIIndicatorTex = new RenderTexture(sw, sh, 16, RenderTextureFormat.ARGB32);

        // 地图大小 200，满屏 140
        var BuildingTex = new RenderTexture(sw, sh * 200 / 140, 16, RenderTextureFormat.ARGB32);
        BuildingHistoryTex = new RenderTexture(sw, sh * 200 / 140, 16, RenderTextureFormat.ARGB32);

        VisionCamera1.targetTexture = VisionTex;
        VisionCamera2.targetTexture = VisionTex;
        UIIndicatorCamera.targetTexture = UIIndicatorTex;
        BulidingCamera1.targetTexture = BuildingTex;
        BulidingCamera2.targetTexture = BuildingTex;

        var historyCam = BuildingHistoryCam.GetComponent<HistoryCamera>();
        BuildingHistoryCam.targetTexture = BuildingHistoryTex;
        historyCam.VisionTex = VisionTex;
        historyCam.BuildingTex = BuildingTex;

        GroundCamera.targetTexture = GroundTex;
        GroundCamera.Render();

        Room4Client.OnBeforeBattleBegin += OnBeforeBattleBegin;
        Room4Client.OnBattleEnd += OnBattleEnd;
    }

    bool adjusting = false;
    private void OnBeforeBattleBegin(string usr1, string usr2, Vec2 mapSize, bool inReplay)
    {
        AdjustCamera(mapSize);

        adjusting = true;
        var posMe = transform.localPosition.z;
        var posOpposite = posMe == CamPosZMin ? CamPosZMax : CamPosZMin;
        var sl = Mathf.Abs(WorldLen2ScreenLen(posMe - posOpposite));
        MoveCamera(-sl, true);
        StartCoroutine(MoveBackCamera(sl));
    }

    IEnumerator MoveBackCamera(float d)
    {
        yield return new WaitForSeconds(3);
        var timeTotal = 1.0f;
        while(timeTotal > 0)
        {
            var dt = Time.deltaTime;
            timeTotal -= dt;
            MoveCamera(dt * d, true);
            yield return null;
        }

        adjusting = false;
    }

    public void Clear()
    {
        RenderTexture rt = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = GroundTex;
        GL.Clear(true, true, Color.clear);
        UnityEngine.RenderTexture.active = rt;
    }

    private void OnBattleEnd(Room r, string winner, bool inReplay)
    {
        Clear();
    }

    float CamPosZMin = 0;
    float CamPosZMax = 0;
    float fogOffset = 0;
    float pd = 0;
    public bool MoveCamera(float pixelDelta, bool forceMove = false)
    {
        if (pixelDelta == 0 || (!forceMove && adjusting))
            return false;

        var delta = ScreenLen2WorldLen(pixelDelta);
        var rt = Canvas.GetComponent<RectTransform>();
        var sw = (int)rt.rect.width;
        var sh = (int)rt.rect.height;

        var z = transform.localPosition.z;
        pixelDelta /= delta;
        delta = (delta * 2).Clamp(CamPosZMin - z, CamPosZMax - z);
        pixelDelta *= delta;
        z += delta;
        pd += pixelDelta;
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, z);
        fogOffset = pd / VisionTex.height / 2;

        return delta != 0f;
    }

    float sl2wl = 0;
    public float ScreenLen2WorldLen(float sl)
    {
        return sl2wl * sl;
    }

    public float WorldLen2ScreenLen(float wl)
    {
        return wl / sl2wl;
    }

    void AdjustCamera(Vec2 mapSize)
    {
        var me = GameCore.Instance.MePlayer;
        transform.localRotation = Quaternion.Euler(90 - Oblique, me == 2 ? 180 : 0, 0);

        // 一满屏显示 60 x 140 个单位长度
        var dragOffset = (float)mapSize.y - 140;

        var cam = GetComponent<Camera>();
        var rt = Canvas.GetComponent<RectTransform>();
        var sw = (int)rt.rect.width;
        var sh = (int)rt.rect.height;
        var a = Oblique * Mathf.PI / 180;
        var mh = sh / Mathf.Cos(a);
        var offsetX = (float)mapSize.x / 2;
        var offsetY = mh * Mathf.Sin(a) / 2 * Mathf.Cos(a);
        var offsetZ = (float)(mapSize.y / 2 + (me == 2 ? dragOffset / 2 : -dragOffset / 2)) + mh * Mathf.Sin(a) / 2 * Mathf.Sin(a) * (me == 2 ? 1 : -1);
        var camPos = new Vector3(offsetX, offsetY, offsetZ);
        var uiTitleOffset = new Vector3(0, 0, 6);
        transform.localPosition = camPos + uiTitleOffset * (me == 2 ? 1 : -1);
        
        if (me == 2)
        {
            CamPosZMax = (camPos + uiTitleOffset).z;
            CamPosZMin = CamPosZMax - dragOffset;
            sl2wl = cam.orthographicSize / (mh / 2);
        }
        else
        {
            CamPosZMin = (camPos - uiTitleOffset).z;
            CamPosZMax = CamPosZMin + dragOffset;
            sl2wl = -cam.orthographicSize / (mh / 2);
        }
        fogOffset = 0;
        pd = 0;
        BuildingHistoryCam.GetComponent<HistoryCamera>().Clear();

        var pos_y1 = GameCore.Instance.MePlayer == 2 ? CamPosZMin : CamPosZMax;
        var pos_y2 = GameCore.Instance.MePlayer == 2 ? CamPosZMax : CamPosZMin;
        var rectOff = GameCore.Instance.MePlayer == 2 ? dragOffset / sl2wl / 2 : -dragOffset / sl2wl / 2;

        var pos1 = new Vector3(transform.localPosition.x, transform.localPosition.y, pos_y1);

        VisionCamera1.pixelRect = new Rect(0, rectOff, sw, sh);
        VisionCamera1.transform.position = pos1;
        VisionCamera1.transform.rotation = transform.rotation;
        VisionCamera1.transform.localScale = Vector3.zero;

        BulidingCamera1.pixelRect = new Rect(0, rectOff, sw, sh);
        BulidingCamera1.transform.position = pos1;
        BulidingCamera1.transform.rotation = transform.rotation;
        BulidingCamera1.transform.localScale = Vector3.zero;

        var pos2 = new Vector3(transform.localPosition.x, transform.localPosition.y, pos_y2);

        VisionCamera2.pixelRect = new Rect(0, 0, sw, sh);
        VisionCamera2.transform.position = pos2;
        VisionCamera2.transform.rotation = transform.rotation;
        VisionCamera2.transform.localScale = Vector3.zero;

        BulidingCamera2.pixelRect = new Rect(0, 0, sw, sh);
        BulidingCamera2.transform.position = pos2;
        BulidingCamera2.transform.rotation = transform.rotation;
        BulidingCamera2.transform.localScale = Vector3.zero;

        transform.localPosition = pos2;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        CameraMat.SetInt("_TurnOnBattleFog", TurnOnBattleFog ? 1 : 0);
        CameraMat.SetTexture("_VisionTex", VisionTex);
        CameraMat.SetTexture("_GroundTex", GroundTex);
        CameraMat.SetTexture("_BuildingHistoryTex", BuildingHistoryTex);
        CameraMat.SetTexture("_IndicatorTex", UIIndicatorTex);
        CameraMat.SetFloat("_FogOffset", fogOffset);
        Graphics.Blit(source, destination, CameraMat, 0);
    }
}
