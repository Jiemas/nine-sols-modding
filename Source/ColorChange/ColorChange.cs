using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using NineSolsAPI;
using NineSolsAPI.Utils;

namespace EnlightenedJi;

//  C:/Users/Jiemas/AppData/LocalLow/RedCandleGames/NineSols
// string spritePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/";

// light in staff tip during attack 6 and light in orb during attack 14 charging
// A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Weapon/JeeStaffTip/Effect_BEAM/STPBALL/Light/

// Might be the red laser circles
// CircularDamage(Clone)/Animator/Effect_BEAM/P_ScretTreePowerCIRCLE/

// Red stars are called Tai danger

public class ColorChange {
    public static int lutSize = 16;
    public static Material material = null!;
    private static int stayRange = 1;

private static (float x, float y, float z)[] stayColors = new (float x, float y, float z)[]
    {
        // (79f, 193f, 129f), // Green Claws/Headband
        // (186f, 240f, 227f), // Eye Blue 1
        // (117f, 220f, 208f), // Eye Blue 2
        // (192f, 247f, 237f), // Eye Blue 3
    };

    private static ((float x, float y, float z) src, (float x, float y, float z) dst)[] colorPairs = new ((float x, float y, float z) src, (float x, float y, float z) dst)[]
    {
        ((1f, 1f, 1f), (1f, 1f, 1f)),  // Black
        ((247f, 248f, 241f), (247f, 248f, 241f)), // Fur White 
        ((186f, 240f, 227f), (186f, 240f, 227f)), // Eye Baby Blue
        ((79f, 193f, 129f), (79f, 193f, 129f)), // Green Claws/Headband
        ((104f, 24f, 23f), (37f, 44f, 31f)), // Cape Red -> Grey Black
        ((228f, 190f, 106f), (128f, 128f, 128f)), // Robe Yellow -> Gray
        ((212f, 203f, 167f), (201f, 207f, 203f)) // Tan Robe Highlight -> Bone White
    };

    private static Vector3[] srcColors = new Vector3[colorPairs.Length * 27];


    private static Vector3[] dstColors = new Vector3[colorPairs.Length * 27];


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
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]BossAngry_Cutscene/[Timeline]/DummyJeeAnimator/View/Jee/Cloak",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]BossAngry_Cutscene/[Timeline]/DummyJeeAnimator/View/Jee/Arm",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/[CutScene]BossDying/[Timeline]/DummyJeeAnimator/View/Jee/JeeSprite",

        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Jee/Cloak",
        "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Jee/Arm",
        // "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Effect/Attack_TeleportSword/Sprite"
    };
    public static SpriteRenderer[] jiSprites = new SpriteRenderer[jiSpritePaths.Length];

    static private void initializeColors()
    {
        int index = 0;
        foreach (((float x, float y, float z) src, (float x, float y, float z) dst) in colorPairs)
        {
            for (float i = -stayRange; i <= stayRange; i++)
            {
                for (float j = -stayRange; j <= stayRange; j++)
                {
                    for (float k = -stayRange; k <= stayRange; k++)
                    {
                        srcColors[index] = new Vector3(src.x + i, src.y + j, src.z + k);
                        dstColors[index++] = new Vector3(dst.x + i, dst.y + j, dst.z + k);
                    }
                }
            }
        }
    }

    static public void getJiSprite()
    {
        for (int i = 0; i < jiSprites.Length; i++)
        {
            jiSprites[i] = GameObject.Find(jiSpritePaths[i]).GetComponent<SpriteRenderer>();
        }

        var laserCircle = GameObject.Find("A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Weapon/Jee_Sitck/Effect/EffectSprite");
        var laserCircle2 = GameObject.Find("A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/View/Effect/Attack_TeleportSword/Sprite");
        ToastManager.Toast(laserCircle);
        laserCircle.AddComponent<_2dxFX_ColorChange>();
        laserCircle2.AddComponent<_2dxFX_ColorChange>();
        _2dxFX_ColorChange laserCircleHueValue = laserCircle.GetComponent<_2dxFX_ColorChange>();
        _2dxFX_ColorChange laserCircle2HueValue = laserCircle2.GetComponent<_2dxFX_ColorChange>();
        laserCircleHueValue._HueShift = 230;
        laserCircle2HueValue._HueShift = 230;
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
            if (jiSprite.material != material)
            {  
                jiSprite.material = material;
            }
            
        }
    }
}