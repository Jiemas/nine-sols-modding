using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using NineSolsAPI;
using NineSolsAPI.Utils;

namespace EnlightenedJi;

//  C:/Users/Jiemas/AppData/LocalLow/RedCandleGames/NineSols
// string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";

public class ColorChange {
    public static int lutSize = 16;
    public static Material material = null!;

    private static ((float x, float y, float z) src, (float x, float y, float z) dst)[] colorPairs = new ((float x, float y, float z) src, (float x, float y, float z) dst)[]
    {
        ((0f, 0f, 0f), (0f, 0f, 0f)),  // Black
        ((104f, 24f, 23f), (37f, 44f, 31f)), // Cape Red -> Grey Black
        ((228f, 190f, 106f), (233f, 238f, 236f)), // Robe Yellow -> Off-White
        ((247f, 248f, 241f), (237f, 237f, 213f)), // Fur White -> Cream White
        ((247f, 249f, 238f), (237f, 237f, 213f)), // Fur White -> Cream White
        ((242f, 241f, 235f), (237f, 237f, 213f)), // Fur White -> Cream White
        ((186f, 240f, 227f), (237f, 47f, 50f)), // Eye Baby Blue -> Purple Amethyst
        // ((117f, 220f, 208f), (237f, 47f, 50f)), // Eye Blue -> Purple Amethyst
        // ((192f, 247f, 237f), (237f, 47f, 50f)), // Eye White Blue -> Purple Amethyst
        ((79f, 193f, 129f), (79f, 193f, 129f)), // Green Claws/Headband
        ((212f, 203f, 167f), (201f, 207f, 203f)) // Tan Robe Highlight -> Bone White
    };

    private static Vector3[] srcColors = new Vector3[colorPairs.Length];
    // {
    //     (0f, 0f, 0f),
    //     (104f, 24f, 23f),
    //     (228f, 190f, 106f),
    //     (247f, 248f, 241f),
    //     (186f, 240f, 227f),
    //     (117f, 220f, 208f),
    //     (192f, 247f, 237f)
    // };

    private static Vector3[] dstColors = new Vector3[colorPairs.Length];
    // {
    //     (0f, 0f, 0f),
    //     (37f, 44f, 31f),
    //     (233f, 238f, 236f),
    //     (237f, 237f, 213f),
    //     (153f, 102f, 204f),
    //     (153f, 102f, 204f),
    //     (153f, 102f, 204f)
    // };

    private static string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";
    private static string[] jiSpritePaths = 
    {
        // "A10S5/Room/Boss And Environment Binder/1_DropPickable 亮亮 FSM Variant/ItemProvider/DropPickable FSM Prototype/FSM Animator/VIEW/DummyJeeAnimator/View/Jee/JeeSprite",
        // "A10S5/Room/Boss And Environment Binder/1_DropPickable 亮亮 FSM Variant/ItemProvider/DropPickable FSM Prototype/FSM Animator/AfterShowView/DummyJeeAnimator/View/Jee/JeeSprite",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Jee/JeeSprite",
        // "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/DummyJeeAnimator/View/Jee/JeeSprite",
        // "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]FirstTimeContact/DummyJeeAnimator/View/Jee/JeeSprite",
        // "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]SecondTimeContact/DummyJeeAnimator/View/Jee/JeeSprite",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]BossAngry_Cutscene/[Timeline]/DummyJeeAnimator/View/Jee/JeeSprite",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]BossDying/[Timeline]/DummyJeeAnimator/View/Jee/JeeSprite",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Jee/Cloak",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Jee/Arm"
    };
    public static SpriteRenderer[] jiSprites = new SpriteRenderer[jiSpritePaths.Length];


    static private void initializeColors()
    {
        int i = 0;
        foreach (((float x, float y, float z) src, (float x, float y, float z) dst) in colorPairs)
        {
            srcColors[i] = new Vector3(src.x, src.y, src.z);
            dstColors[i++] = new Vector3(dst.x, dst.y, dst.z);
        }
    }

    static public void getJiSprite()
    {
        for (int i = 0; i < jiSprites.Length; i++)
        {
            jiSprites[i] = GameObject.Find(jiSpritePaths[i]).GetComponent<SpriteRenderer>();
        }
        // string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";
        // jiSprite = GameObject.Find($"{spritePath}Jee/JeeSprite").GetComponent<SpriteRenderer>();
        // return jiSprite;
    }

    static public void InitializeMat(Material mat) {
        // ToastManager.Toast("RecolorSPrite called!");
        initializeColors();
        Texture3D lut = RBFLUTGenerator.GenerateRBF_LUT(srcColors, dstColors, lutSize);
        mat.SetTexture("_LUT", lut);
        mat.SetFloat("_LUTSize", lutSize);
        material = mat;
        
    }

    static public void updateJiSprite()
    {
        foreach (SpriteRenderer jiSprite in jiSprites)
        {
            jiSprite.material = material;
        }
        // jiSprite.material = material;
    }
}