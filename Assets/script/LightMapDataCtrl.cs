using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class LightMapDataCtrl : MonoBehaviour 
{
    [SerializeField]
    public int lightmapIndex;
    [SerializeField]
    private Vector4 lightmapScaleOffset;

    public void resetLightMapData()
    {
        Renderer render = gameObject.GetComponent<Renderer>();
        if (render)
        {
            render.lightmapIndex = lightmapIndex;
            render.lightmapScaleOffset = lightmapScaleOffset;
        }
    }

    public void SaveLightMapData()
    {
        Renderer render = gameObject.GetComponent<Renderer>();
        if (render)
        {
            lightmapIndex = render.lightmapIndex;
            lightmapScaleOffset = render.lightmapScaleOffset;
        }
    }
}

[Serializable]
public class LightMapDataSet : MonoBehaviour
{
    [Serializable]
    public class Set
    {
        public Texture2D color;
        public Texture2D dir;
        public Texture2D mask;

        public Set(LightmapData data)
        {
            color = data.lightmapColor;
            dir = data.lightmapDir;
            mask = data.shadowMask;
        }

        public LightmapData toData()
        {
            LightmapData data = new LightmapData();
            data.lightmapColor = color;
            data.lightmapDir = dir;
            data.shadowMask = mask;
            return data;
        }
    }

    [SerializeField]
    private Set[] sets;
    [SerializeField]
    public LightMapDataCtrl[] ctrls;
    void Start()
    {
        reset();
    }
    public void reset()
    {
        if (sets != null && sets.Length > 0)
        {
            LightmapData[] datas = new LightmapData[sets.Length];
            for (int i = 0; i < sets.Length; i++)
            {
                datas[i] = sets[i].toData();
            }
            LightmapSettings.lightmaps = datas;

            for (int i = 0; i < ctrls.Length; i++)
            {
                ctrls[i].resetLightMapData();
            }
        }
    }

    public void Save(LightMapDataCtrl[] allCtrls)
    {
        ctrls = allCtrls;
        LightmapData[] datas = LightmapSettings.lightmaps;
        if (datas != null) 
        {
            sets = new Set[datas.Length];
            for (int i = 0; i < datas.Length; i++)
            {
                sets[i] = new Set(datas[i]);
            }
        }
    }
}
