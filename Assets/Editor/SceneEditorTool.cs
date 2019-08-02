using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;


[InitializeOnLoad]
public class ToggleLightMapSet
{
    static ToggleLightMapSet()
    {
        EditorApplication.playmodeStateChanged += LogPlayModeState;
    }

    private static void LogPlayModeState()
    {
        if (EditorApplication.isPlaying == false && EditorApplication.isCompiling == false)
        {
            LightMapDataSet set = Object.FindObjectOfType<LightMapDataSet>();
            if (set != null)
            {
                //when application stoped, unity will use lightmapasset reset lightmap info ,that will change what we need 
                //so we reset lightmap info by custom set
                set.reset();
            }
        }
    }
}

public class SceneEditorTool
{
    static List<LightmapData> LightMapTextures = new List<LightmapData>();
    static WaitForSeconds _bakeWait = new WaitForSeconds(5);

    [MenuItem("Tool/Cull Bake")]
    static void Bake()
    {
        EditorCoroutine.start(DoMultiBake());

    }
    

   
    private static IEnumerator DoMultiBake()
    {
        LightMapTextures.Clear();

        //part logic from  https://answers.unity.com/questions/61158/beast-lightmap-ignores-light-culling-mask-andor-la.html
        // Get all lights which are active in the scene
        Light[] lights = (from light in (Object.FindObjectsOfType(typeof(Light)) as Light[])
                          where (light.enabled == true && light.gameObject.activeInHierarchy == true)
                          select light).ToArray();

        // Get all the game objects which are active in the scene
        GameObject[] gameObjects = (from go in (Object.FindObjectsOfType(typeof(GameObject)) as GameObject[])
                                    where (go.activeInHierarchy == true &&
                                            (GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.LightmapStatic) > 0 &&
                                            (go.GetComponent<Renderer>() != null))
                                    select go).ToArray();

        ILookup<int, GameObject> gameObjectGroups = gameObjects.ToLookup(go => (1 << go.layer));



        yield return null;

        //disable all light and objs
        SetActive(lights, false);
        SetActive(gameObjects, false);


        yield return _bakeWait;
        
        foreach (IGrouping<int, GameObject> gameObjectGroup in gameObjectGroups)
        {
            int layerForGroup = gameObjectGroup.Key;

            GameObject[] gameObjectsForLayer = gameObjectGroup.ToArray();
            Light[] lightsForLayer = (from light in lights where (light.cullingMask == layerForGroup) select light).ToArray();

            if (lightsForLayer.Length > 0)
            {
                SetActive(lightsForLayer, true);
                SetActive(gameObjectsForLayer, true);

                yield return _bakeWait;
                UnityEditor.Lightmapping.Bake();



                int indexOffset = LightMapTextures.Count;
                for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
                {
                    LightmapData lightData = LightmapSettings.lightmaps[i];

                    //resave lightmap texture
                    lightData.lightmapColor = reSaveLightTexture(lightData.lightmapColor, i + indexOffset);
                    lightData.lightmapDir = reSaveLightTexture(lightData.lightmapDir, i + indexOffset);
                    lightData.shadowMask = reSaveLightTexture(lightData.shadowMask, i + indexOffset);

                    LightMapTextures.Add(lightData);
                }

                LightmapSettings.lightmaps = new LightmapData[0];
                for (int i = 0; i < gameObjectsForLayer.Length; i++)
                {
                    LightMapDataCtrl ctrl = gameObjectsForLayer[i].FetchComponent<LightMapDataCtrl>();
                    ctrl.SaveLightMapData();
                    ctrl.lightmapIndex += indexOffset; // changeIndexInfo
                }

                yield return _bakeWait;
                // Disable the objects

                SetActive(lightsForLayer, false);
                SetActive(gameObjectsForLayer, false);
            }
        }
        yield return _bakeWait;

        // here to bake all light, but with out objs
        //to make light as baked mode
        SetActive(lights, true);
        SetActive(gameObjects, false);
        yield return _bakeWait;
        UnityEditor.Lightmapping.Bake();
        yield return _bakeWait;


        //set lightmap back
        LightmapSettings.lightmaps = LightMapTextures.ToArray();
        
        SetActive(gameObjects, true);


        yield return _bakeWait;
        GameObject mapRoot = GameObject.Find("Scene");//need a mesh root
        if (mapRoot != null)
        {
            //Serialize custorm data to LightMapDataSet
            LightMapDataCtrl[] ctrls = new LightMapDataCtrl[gameObjects.Length];
            for (int i = 0; i < gameObjects.Length; i++)
            {
                ctrls[i] = gameObjects[i].FetchComponent<LightMapDataCtrl>();
            }
            LightMapDataSet set = mapRoot.FetchComponent<LightMapDataSet>();
            set.Save(ctrls);
            set.reset();

            EditorSceneManager.MarkAllScenesDirty();
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Texture2D reSaveLightTexture(Texture2D texture, int index)
    {
        if (texture == null)
        {
            return null;
        }
        string assetName = AssetDatabase.GetAssetPath(texture);
        string newPath = Path.Combine(Path.GetDirectoryName(assetName), Path.GetFileNameWithoutExtension(assetName) +"_"+index+ ".exr");
        AssetDatabase.CopyAsset(assetName, newPath);

        Texture2D result = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
        return result;
    }

    static void SetActive(Light[] lights, bool active)
    {
        foreach (Light light in lights)
        {
            light.gameObject.SetActive(active);
        }
            
    }

    static void SetActive(GameObject[] objs, bool active)
    {
        foreach (GameObject obj in objs)
        {
            obj.SetActive(active);
        }
    }
}
