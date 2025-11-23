using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using NineSolsAPI;
using NineSolsAPI.Utils;

namespace EnlightenedJi;

public class ColorChange {
    public static float lutSize = 32f;
    public static Texture3D lut3D = null!;
    public static float intensity = 1.0f;

    // SpriteRenderer jiSpriteRenderer = null!;
    MaterialPropertyBlock block = null!;

    private static Vector3[] src =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(104f, 24f, 23f),
        new Vector3(228f, 190f, 106f),
        new Vector3(247f, 248f, 241f),
        new Vector3(186f, 240f, 227f),
        new Vector3(117f, 220f, 208f),
        new Vector3(192f, 247f, 237f)
    };

    private static Vector3[] dest =
    {
        new Vector3(0f, 0f, 0f),
        new Vector3(37f, 44f, 31f),
        new Vector3(233f, 238f, 236f),
        new Vector3(237f, 237f, 213f),
        new Vector3(153f, 102f, 204f),
        new Vector3(153f, 102f, 204f),
        new Vector3(153f, 102f, 204f)
    };

    static public SpriteRenderer getJiSprite()
    {
        string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";
        return GameObject.Find($"{spritePath}Jee/JeeSprite").GetComponent<SpriteRenderer>();
    }

    static public Material RecolorSprite(SpriteRenderer jiSpriteRenderer) {
        // string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";
        // jiSpriteRenderer = GameObject.Find($"{spritePath}Jee/JeeSprite").GetComponent<SpriteRenderer>();

        ToastManager.Toast("RecolorSPrite called!");

        string bundlePath = Path.Combine(Application.persistentDataPath, "mymodbundle");
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        var mat = bundle.LoadAsset<Material>("SpriteWithLut3D_Mat");
        var instance = UnityEngine.Object.Instantiate(mat);
        bundle.Unload(false);

        // jiSpriteRenderer.material = instance;
        lut3D = LutUtils.LoadTexture3DFromFile(Path.Combine(Application.persistentDataPath, "lut.png"));
        instance.SetTexture("_Lut3D", lut3D);
        instance.SetFloat("_LutSize", lutSize);
        instance.SetFloat("_LutIntensity", intensity);
        return instance;
        // ToastManager.Toast(jiSpriteRenderer.material);
        // ToastManager.Toast(instance);

        // DebugMaterial(jiSpriteRenderer);

        // block = new MaterialPropertyBlock();

        // jiSpriteRenderer.GetPropertyBlock(block);
        
        // ToastManager.Toast(block);

        // try
        // {
        //     // lut3D = LutUtils.GenerateLutFromRbf(src, dest, (int) lutSize, 2f);
        //     lut3D = LutUtils.LoadTexture3DFromFile(Path.Combine(Application.persistentDataPath, "lut.png"));
        //     LutUtils.SaveTexture3DPreview(lut3D, "lut_file_test.png");

        //     jiSpriteRenderer.sharedMaterial.SetTexture("_Lut3D", lut3D);
        //     jiSpriteRenderer.sharedMaterial.SetFloat("_LutSize", lutSize);
        //     jiSpriteRenderer.sharedMaterial.SetFloat("_LutIntensity", 1f);

        // }
        // catch (Exception ex)
        // {
        //     ToastManager.Toast(ex);
        // }

        // ToastManager.Toast(lut3D);

        // block.SetTexture("_Lut3D", lut3D);
        // ToastManager.Toast("TEST2");
        // block.SetFloat("_LutSize", lutSize);
        // ToastManager.Toast("TEST3");
        // block.SetFloat("_LutIntensity", intensity);
        // ToastManager.Toast("TEST4");
        // jiSpriteRenderer.SetPropertyBlock(block);
        // ToastManager.Toast("TEST5");
    }

    // void DebugMaterial(SpriteRenderer sr)
    // {
    //     if (sr == null) { ToastManager.Toast("No SpriteRenderer"); return; }
    //     Material mat = sr.sharedMaterial; // sharedMaterial shows what actual shader the sprite uses
    //     if (mat == null) { ToastManager.Toast("SpriteRenderer has no material"); return; }
    //     ToastManager.Toast($"SpriteRenderer material: {mat.name}, shader: {mat.shader.name}");
    //     // List properties
    //     int count = ShaderUtil_GetPropertyCount(mat.shader); // helper below
    //     ToastManager.Toast($"Shader property count: {count}");
    //     // Check if shader has _Lut3D
    //     bool hasLutProp = mat.shader.HasProperty("_Lut3D");
    //     ToastManager.Toast($"Shader has _Lut3D property? {hasLutProp}");
    // }

    // // Minimal wrapper to get shader property count to help debugging (editor only).
    // int ShaderUtil_GetPropertyCount(Shader s)
    // {
    // #if UNITY_EDITOR
    //     return UnityEditor.ShaderUtil.GetPropertyCount(s);
    // #else
    //     return -1; // unavailable at runtime
    // #endif
    // }




}