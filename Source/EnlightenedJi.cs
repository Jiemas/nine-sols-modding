using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using NineSolsAPI;
using NineSolsAPI.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

using System;
using System.Reflection;

namespace EnlightenedJi;

// [BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class EnlightenedJi : BaseUnityPlugin
{
    // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html

    private Harmony harmony = null!;

    private static ConfigEntry<float> JiAnimatorSpeed = null!;
    private static ConfigEntry<float> JiHPScale = null!;
    private static ConfigEntry<bool> JiModifiedAttackSequences = null!;
    private static ConfigEntry<bool> JiModifiedSpeed = null!;
    private static ConfigEntry<bool> JiModifiedSprite = null!;
    private static ConfigEntry<bool> JiModifiedHP = null!;
    private static ConfigEntry<float> JiPhase2HPRatio = null!;

    public static ConfigEntry<string> blackReplace = null!;
    public static ConfigEntry<string> furReplace = null!;
    public static ConfigEntry<string> eyeReplace = null!;
    public static ConfigEntry<string> greenReplace = null!;
    public static ConfigEntry<string> capeReplace = null!;
    public static ConfigEntry<string> robeReplace = null!;
    public static ConfigEntry<string> tanReplace = null!;
    public static ConfigEntry<string> crimsonReplace = null!;

    private static Material mat = null!;
    private static Material crimsonMat = null!;
    private static AssetBundle bundle = null!;

    // Path Variables
    private string jiBossPath = "";
    private string jiAttackStatesPath = "";
    private string jiAttackSequences1Path = "";
    private string jiAttackSequences2Path = "";
    private string jiAttackGroupsPath = "";

    // Attack Variables
    private static string[] Attacks = [
        "",
        "[1]Divination Free Zone",
        "[2][Short]Flying Projectiles",
        "[3][Finisher]BlackHoleAttack",
        "[4][Altar]Set Laser Altar Environment",
        "[5][Short]SuckSword 往內",
        "[6][Finisher]Teleport3Sword Smash 下砸",
        "[7][Finisher]SwordBlizzard",
        "",
        "[9][Short]GroundSword",
        "[10][Altar]SmallBlackHole",
        "[11][Short]ShorFlyingSword",
        "[12][Short]QuickHorizontalDoubleSword",
        "[13][Finisher]QuickTeleportSword 危戳",
        "[14][Altar]Laser Altar Circle",
        "[15][Altar]Health Altar",
        "[16]Divination JumpKicked"
    ];
    private static BossGeneralState[] BossGeneralStates = new BossGeneralState[19];
    private static Weight<MonsterState>[] Weights = new Weight<MonsterState>[17];
    private static int Hurt = 17, BigHurt = 18;

    // Sequence Variables
    private static string[] SequenceStrings1 = ["", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard"];
    private static string[] SequenceStrings2 = [ "", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard", 
                                            "_QuickToBlackHole", "_Phase2_OpeningBlackHole" ];
    private static MonsterStateGroupSequence[] Sequences1 =  new MonsterStateGroupSequence[4];
    private static MonsterStateGroupSequence[] Sequences2 =  new MonsterStateGroupSequence[7];
    private static int Default = 0, WithAltar = 1, WithSmallBlackHole = 2, QuickToBlizzard = 3, 
                        QuickToBlackHole = 4, Opening = 5, Health = 6;
    
    // Group Variables
    private static (int sequence, int index)[] ExistingGroupPairs =
    [
        (WithSmallBlackHole, 1), (Default, 3), (QuickToBlizzard, 1), (Default, 1), (WithAltar, 1), 
        (WithAltar, 4)
    ];
    private static (int[] attacksList, string objectName)[] customGroupPatterns =
    [
        ([11, 12, 13, 14], "SneakAttack(Attack 11/12/13/14)"),
        ([5, 9], "BackAttack(Attack 5/9)"),
        ([5, 9, 7], "LongerOrBlizardAttack(Attack 5/9/7)"),
        ([11, 12, 14], "SwordOrLaserAttack(Attack 11/12/14)"),
        ([10, 12, 14], "SwordOrAltarAttack(Attack10/12/14)"),
        ([4], "LaserAltarHardAttack(Attack4)"),
        ([2, 5, 9, 12], "DoubleTroubleAttack(Attack2/9/12/5)"),
        ([2, 5, 9, 11, 12, 15], "SneakAttack2(Attack2/5/9/11/12/15)"),
        ([4, 13], "HardAltarOrEasyFinisher(Attack4/13)"),
        ([4, 6, 13], "LaserAltarOrFinisher(Attack 4/6/13)")
    ];
    private static MonsterStateGroup[] Groups = new MonsterStateGroup[ExistingGroupPairs.Length + customGroupPatterns.Length + 2];
    private static int SmallBlackHole = 0, LongerAttack = 1, Blizzard = 2,  EasyLaserAltar = 4, BigBlackHole = 6, 
        EasyOrHardFinisher = 7, SneakAttack = 8, BackAttack = 9, HardLaserAltar = 13, DoubleTrouble = 14,
        SneakAttack2 = 15, LaserAltarOrFinisher = 17;

    // private static int QuickAttack = 3, EasyFinisher = 5, LongerOrBlizzard = 10, SwordOrLaser = 11, SwordOrAltar = 12, HardAltarOrEasyFinisher = 16,

    // Miscellaneous Variables
    private static Color crimsonColor;

    private static string BundleName = "EnlightenedJiBundle";

    private static string temp = "";

    private static int randomNum = 0;
    private static bool firstMessage = true;

    private static System.Random random = new();

    private static PostureBreakState JiStunState = null!;

    private static BossPhaseChangeState PhaseChangeState = null!;

    private static AttackSequenceModule attackSequenceModule = null!;

    private static RubyTextMeshPro PhaseTransitionText = null!;

    private static MonsterHurtInterrupt HurtInterrupt = null!;

    // private static StealthEngaging Engaging = null!;

    private static Action JiUpdate = null!;
    private static Action JiSpeedChange = null!;
    private static Action JiStateChange = null!;
    private static Action JiSpriteUpdate = null!;
    private static Action Pass = () => {};

    private static Func<int, float, float> randomAdd = (probability, incr) => randomNum % probability == 0 ? incr : 0f;

    private static Func<string, bool> afterFinisherCheck = lastMove => 
        new List<string>() {"Attack13", "PostureBreak", "Attack6"}.Contains(lastMove);

    private static Dictionary<string, Func<string, float>> CurrSpeedDict = null!;

    private static Dictionary<string, Func<string, float>> SpeedDict1 = new() {
        {"[1]Divination Free Zone (BossGeneralState)",                  _ => 0},
        {"[2][Short]Flying Projectiles (BossGeneralState)",             _ => randomAdd(3, 0.2f)},
        {"[3][Finisher]BlackHoleAttack (BossGeneralState)",             _ => 2f},
        {"[4][Altar]Set Laser Altar Environment (BossGeneralState)",    _ => 1.55f},
        {"[5][Short]SuckSword 往內 (BossGeneralState)",                 _ => Math.Max(randomAdd(2, 0.5f), randomAdd(3, 0.2f))},
        {"[6][Finisher]Teleport3Sword Smash 下砸 (BossGeneralState)",   lastMove => afterFinisherCheck(lastMove) ? 0.5f : randomAdd(3, 0.2f)},
        {"[7][Finisher]SwordBlizzard (BossGeneralState)",               _ => Math.Max(randomAdd(2, 0.5f), randomAdd(3, 0.2f))},
        {"[9][Short]GroundSword (BossGeneralState)",                    _ => randomAdd(3, 0.2f)},
        {"[10][Altar]SmallBlackHole (BossGeneralState)",                _ => 3f},
        {"[11][Short]ShorFlyingSword (BossGeneralState)",               lastMove => afterFinisherCheck(lastMove) ? 2f : Math.Max(randomAdd(2, 0.5f), randomAdd(3, 0.2f))},
        {"[12][Short]QuickHorizontalDoubleSword (BossGeneralState)",    lastMove => afterFinisherCheck(lastMove) ? 2f : randomAdd(3, 0.2f)},
        {"[13][Finisher]QuickTeleportSword 危戳 (BossGeneralState)",    lastMove => afterFinisherCheck(lastMove) ? 0.5f : randomAdd(3, 0.2f)},
        {"[14][Altar]Laser Altar Circle (BossGeneralState)",           lastMove => afterFinisherCheck(lastMove) ? 2f : randomAdd(3, 0.2f)},
        {"[15][Altar]Health Altar (BossGeneralState)",                 _ => 1f},
        {"[16]Divination JumpKicked (BossGeneralState)",               _ => 3f},
        {"PostureBreak (PostureBreakState)",                           _ => 3f},
        {"1_Engaging (StealthEngaging)",                               _ => 3f}
    };

    private static Dictionary<string, Func<string, float>> SpeedDict2 = new() {
        {"[1]Divination Free Zone (BossGeneralState)",                  _ => randomAdd(3, 0.2f)},
        {"[2][Short]Flying Projectiles (BossGeneralState)",             lastMove => afterFinisherCheck(lastMove) ? 0.65f : randomAdd(2, 0.3f)},
        {"[3][Finisher]BlackHoleAttack (BossGeneralState)",             _ => 2f},
        {"[4][Altar]Set Laser Altar Environment (BossGeneralState)",    _ => 1.65f},
        {"[5][Short]SuckSword 往內 (BossGeneralState)",                 lastMove => afterFinisherCheck(lastMove) ? 0.65f : randomAdd(2, 0.3f)},
        {"[6][Finisher]Teleport3Sword Smash 下砸 (BossGeneralState)",   _ => Math.Max(randomAdd(3, 0.35f), randomAdd(2, 0.3f))},
        {"[7][Finisher]SwordBlizzard (BossGeneralState)",               _ => randomNum % 3 == 0 ? 0.7f : (randomNum % 3 == 1 ? 0.5f : 0f)},
        {"[9][Short]GroundSword (BossGeneralState)",                    lastMove => afterFinisherCheck(lastMove) ? 0.65f : randomAdd(2, 0.3f)},
        {"[10][Altar]SmallBlackHole (BossGeneralState)",                _ => 3f},
        {"[11][Short]ShorFlyingSword (BossGeneralState)",               lastMove => afterFinisherCheck(lastMove) ? 2f : randomAdd(2, 0.3f)},
        {"[12][Short]QuickHorizontalDoubleSword (BossGeneralState)",    lastMove => afterFinisherCheck(lastMove) ? 2f : randomAdd(2, 0.3f)},
        {"[13][Finisher]QuickTeleportSword 危戳 (BossGeneralState)",    _ => Math.Max(randomAdd(3, 0.35f), randomAdd(2, 0.3f))},
        {"[14][Altar]Laser Altar Circle (BossGeneralState)",           lastMove => afterFinisherCheck(lastMove) ? 2f : randomAdd(2, 0.3f)},
        {"[15][Altar]Health Altar (BossGeneralState)",                 _ => 1f},
        {"[16]Divination JumpKicked (BossGeneralState)",               _ => 3f},
        {"PostureBreak (PostureBreakState)",                           _ => 3f},
        {"1_Engaging (StealthEngaging)",                               _ => 3f}
    };

    private static string[] lore_quotes = 
    [
      "IT'S TIME TO END THIS!",
      "HEROES ARE FORGED IN AGONY, YI!",
      "DO YOU KNOW WHAT IT IS LIKE TO LEAVE YOUR LOVED ONES SO FAR BEHIND? I SUPPOSE YOU DO...",
      "THANK YOU YI, MY CURTAIN CALL WILL BE FAR GRANDER THAN I COULD HAVE EVER IMAGINED.",
      "SURELY THIS IS NOT ENOUGH TO KILL YOU?",
      "THE END DRAWS NEAR, WILL IT BE MINE OR YOURS?",
      "I WONDER HOW FAR YOU COULD HAVE GOTTEN WITHOUT THAT PIPE OF YOURS?",
      "THE THINGS YOU DO FOR POWER! SURELY APE MAN FLUIDS CANNOT BE THAT GOOD...",
      "ENDURE WHAT OTHERS CANNOT!",
      "LEAR ONCE SPOKE OF YOU, LONG AGO. A MIGHTY FANGSHI ALBEIT OF SHORT STATURE...",
      "WAS NOT LEAR RIGHT AFTER ALL? TIANHUO, A PRODUCT OF SOLARIAN TECHNOLOGY AND ARROGANCE...",
      "I ENVY YOU, YI. TO STILL HAVE LOVED ONES TO MOURN YOUR LOSS...",
      "WHY AM I ATTACKING YOU, YI? HOW ELSE WILL I GET MY CURTAIN CALL?",
      "I THOUGHT MY END WOULD BE GRAND... AND YET YOUR FIREWORK WILL BE THE GRANDEST OF THEM ALL.",
      "WON'T YOU CARE FOR THIS GROTTO FOR ME AFTERWARDS? ...HAHA, I ONLY JEST.",
      "MY LONG AWAITED DEATH IS UPON US. I HOPE YOU ENJOY MY SWAN SONG... OR SURVIVE IT RATHER.",
      "THAT HEART OF YOURS IS JUST AS MUCH A MYSTERY AS MY OWN ABILITIES. FUSANG TRULY CHERISHES YOU...",
      "I DO NOT BLAME EIGONG. DESTINY WOULD HAVE SIMPLY CHOSEN ANOTHER AGENT.",
      "YOU GAVE THE SHEET MUSIC TO AN APE MAN CHILD? DEATH TRULY HAS CHANGED YOU.",
      "YOU ARE REARING AN APE MAN CHILD? SURELY IT DOES NOT KNOW WHAT ELSE YOU HAVE DONE?",
      "TO THINK, OUR FUTURE IS TO BE MERE PETS TO THE APE MEN. DESTINY TRULY HAS A SENSE OF HUMOUR!",
      "COME AND BEHOLD, MY FINAL MOMENTS!",
      "I AM AWARE, THAT ALL OF THIS AROUND US, THIS IS JUST A MEMORY.",
      "IT IS QUITE EMBARRASSING, IS IT NOT? THIS RIDICULOUS TITLE OF MINE...",
      "I AM GLAD I  REREAD THE HEXAGRAMS. OTHERWISE, I WOULD HAVE HELD BACK FAR MORE THAN I NEEDED TO!",
      "YOU MUST BE AWARE, THIS FIGHT? A SIMPLE PASTTIME FOR BEINGS FAR GREATER THAN WE COULD IMAGINE."
    ];
    
    private static string[] meme_quotes = 
    [
      "TIS BUT A SCRATCH!",
      "NAH, I'D WIN.",
      "WHAT'S WRONG YI, NO MAIDENS?",
      "YI, WHAT IS A CAT?",
      "WHICH DO YOU PREFER YI? YOUNG OR ADULT ME?",
      "EIGONG TRIED TO COPY MY IMMORTALITY. DON'T TELL ME YOU ARE THINKING OF TAKING MY GROWTH ABILITY...",
      "PARRYING MY BLACK HOLES? WHAT'S NEXT, A RHIZOMATIC BOMB? RIDICULOUS!",
      "THE RECORD SHOWS. I TOOK THE BLOWS. AND DID IT MY WAY...",
      "...ARE YOU TRYING TO SHAVE ME, YI?!",
      "HOW MANY TIMES DOES THIS MAKE IT NOW?",
      "YOU REQUIRE MY HAIR? OVER MY DEAD BODY!",
      "SURELY YOU HAVE MEMORIZED ALL MY ATTACKS BY NOW?",
      "PROMISED INNER EIGONG PRIME? WHAT A TRULY HORRIFIC COMBINATION OF WORDS!",
      "Meow?",
      "A GUN?! WHERE DID EIGONG GET A GUN FROM?",
      "THIS FIGHT WAS EASIER BEFORE? SURELY THAT IS JUST YOUR MEMORY FAILING YOU!",
      "ARE YOU HAVING A BAD TIME?",
      "REMEMBER, IT'S OKAY TO TAKE BREAKS!",
      "YOU MAY BE ABLE TO BEAT ME, BUT CAN YOU DO IT HITLESS?",
      "PARRY THIS, YOU FILTHY CASUAL!",
      "WHY DOESN'T THAT VIRUS AIL ME LIKE ALL OTHERS? I'M BUILT DIFFERENT!"
    ];

    private void InitConfig() 
    {
        string general = "1. General (Retry boss to make any change have effect)";
        string speed = "2. Speed (Must set JiModifiedSpeed to true to take effect)";
        string hp = "3. HP (Must set JiModifiedHP to true to take effect)";
        string color = "4. Color (Must set JiModifiedSprite to true to take effect)";

        JiModifiedHP = Config.Bind(general, "JiModifiedHP", true, "Modifies Ji's HP");
        JiModifiedAttackSequences = Config.Bind(general, "JiModifiedAttackSequences", true, "Modifies Ji's attack sequences");
        JiModifiedSpeed = Config.Bind(general, "JiModifiedSpeed", true, "Modifies Ji's move speed depending on current attack");
        JiModifiedSprite = Config.Bind(general, "JiModifiedSprite", true, "Modifies the color of Ji's sprite");
        
        JiAnimatorSpeed = Config.Bind(speed, "JiBaseSpeed", 1.2f, "The base speed at which Ji's attacks occur");

        JiHPScale = Config.Bind(hp, "JiHPScale", 6500f, "The amount of Ji's HP in Phase 1");
        JiPhase2HPRatio = Config.Bind(hp, "JiPhase2HPRatio", 1.65f, "The ratio used to determine phase 2 health (Phase 1 health * ratio)");

        blackReplace = Config.Bind(color, "blackReplace", "1,1,1", "Replaces black with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        furReplace = Config.Bind(color, "furReplace", "247,248,241", "Replaces fur color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        eyeReplace = Config.Bind(color, "eyeReplace", "186,240,227", "Replaces the hat eye color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        greenReplace = Config.Bind(color, "greenReplace", "79,193,129", "Replaces the claw and headband color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        capeReplace = Config.Bind(color, "capeReplace", "37,44,31", "Replaces the cape color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        robeReplace = Config.Bind(color, "robeReplace", "128,128,128", "Replaces the robe color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");        
        tanReplace = Config.Bind(color, "tanHighlightReplace", "201,207,203", "Replaces the secondary robe color with specified RGB value on Ji's sprite (Must use \"##,##,##\" format)");
        crimsonReplace = Config.Bind(color, "crimsonReplace", "255,215,0", "Replaces the crimson attack color with specified RGB value (Must use \"##,##,##\" format)");
    }


    public AssetBundle? LoadBundle()
    {
        AssetBundle bundle = null!;

        string persistentPath = Path.Combine(Application.persistentDataPath, BundleName);
        if (File.Exists(persistentPath))
        {
            bundle = AssetBundle.LoadFromFile(persistentPath);
            if (bundle != null) return bundle;
        }

        string pluginDir = Paths.PluginPath;
        string modDir = Path.Combine(pluginDir, "JiSupremacy-EnlightenedJi");
        string installedBundle = Path.Combine(modDir, BundleName);

        if (File.Exists(installedBundle))
        {
            bundle = AssetBundle.LoadFromFile(installedBundle);
            if (bundle != null) return bundle;
        }

        Logger.LogInfo($"[EnlightenedJi] Failed to load bundle from both locations:\n{persistentPath}\n{installedBundle}");
        return null;
    }

    private void Awake() 
    {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        harmony = Harmony.CreateAndPatchAll(typeof(EnlightenedJi).Assembly);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitConfig();

        JiUpdate = Pass;
        JiSpeedChange = Pass;
        JiStateChange = Pass;
        JiSpriteUpdate = Pass;

        bundle = LoadBundle();
        if (bundle is not null) 
        {
            mat = bundle.LoadAsset<Material>("RBFMat");
            if (mat is not null) 
            {
                crimsonMat = new Material(mat);
            }
        }

        var crimsonTuple = ColorChange.parseTuple(crimsonReplace.Value);
        crimsonColor = new Color(crimsonTuple.x / 255, crimsonTuple.y / 255, crimsonTuple.z / 255);

    }

    private static string[] circularObjects = ["CircularDamage_Inverse", "CircularDamage_Inverse(Clone)", "CircularDamage_Inverse_ForBlackHole", "CircularDamage_Inverse_ForBlackHole(Clone)",
                "CircularDamage", "CircularDamage(Clone)"];
    // private static string[] circularObjects = ["TrapMonster_LaserAltar_Circle(Clone)", "TrapMonster_LaserAltar_Circle"];

    private IEnumerator ModifyHiddenObjects() 
    {
        while (SceneManager.GetActiveScene().name != "A10_S5_Boss_Jee") 
        {
            yield return new WaitForSeconds(0.25f);
        }
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all) 
        {
            if (go.name == "MultiSpriteEffect_Prefab 識破提示Variant(Clone)")
            {
                Transform sprite = go.transform.Find("View/Sprite");
                var spriteRenderer = sprite.GetComponent<SpriteRenderer>();
                spriteRenderer.material = mat;

            } 
            else if (go.name == "Effect_TaiDanger(Clone)")
            {
                Transform sprite = go.transform.Find("Sprite");
                var spriteRenderer = sprite.GetComponent<SpriteRenderer>();
                spriteRenderer.material = crimsonMat;
            } else if (circularObjects.Contains(go.name)) {

                DestroyOnDisable foo = go.GetComponent<DestroyOnDisable>();
                if (foo == null) 
                {
                    // GameObject bar = Instantiate(go);
                    // DontDestroyOnLoad(bar);
                    go.AddComponent<DestroyOnDisable>();
                }
            }
        }
    }

    private void ReloadMaterial()
    {
        if (bundle is null || mat is null || SceneManager.GetActiveScene().name != "A10_S5_Boss_Jee")
        {
            return;
        }
        ColorChange.InitializeJiMat(mat, [blackReplace.Value, furReplace.Value, eyeReplace.Value, 
            greenReplace.Value, capeReplace.Value, robeReplace.Value, tanReplace.Value]);
        ColorChange.InitializeCrimsonMat(crimsonMat, [blackReplace.Value, crimsonReplace.Value]);
        // Logger.LogInfo("Reloaded material!");
        StartCoroutine(ModifyHiddenObjects());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "A10_S5_Boss_Jee")
        {
            try 
            {
                ReloadMaterial();
            }
            catch (Exception ex) 
            {
                Logger.LogInfo($"ReloadMaterial Unexpected error: {ex.Message}");
            }
            
            if (JiModifiedSprite.Value && bundle is not null) 
            {
                try 
                {
                    ColorChange.getSprites();
                    JiSpriteUpdate = ActionSpriteUpdate;
                }
                catch (Exception ex) 
                {
                    Logger.LogInfo($"getSprites Unexpected error: {ex.Message}");
                }

            } 
            else 
            {
                JiSpriteUpdate = Pass;
            }

            if (JiModifiedSpeed.Value) 
            {
                CurrSpeedDict = SpeedDict1;
                JiSpeedChange = ActionSpeedChange;
            } 
            else 
            {
                JiSpeedChange = Pass;
            }

            try 
            {
                GetAttackGameObjects();
            }
            catch (Exception ex) 
            {
                Logger.LogInfo($"GetAttackGameObjects Unexpected error: {ex.Message}");
            }

            if (JiModifiedAttackSequences.Value) 
            {
                try 
                {
                    AlterAttacks();    
                }
                catch (Exception ex) 
                {
                    Logger.LogInfo($"AlterAttacks Unexpected error: {ex.Message}");
                }
            }

            JiUpdate = ActionUpdate;

            if (JiModifiedHP.Value) 
            {
                try 
                {
                    StartCoroutine(JiHPChange(JiHPScale.Value, JiPhase2HPRatio.Value));
                }
                catch (Exception ex) 
                {
                    Logger.LogInfo($"JiHPChange custom Unexpected error: {ex.Message}");
                }
            }
            else 
            {
                try 
                {
                    StartCoroutine(JiHPChange(5049f, 2f)); // TODO VERIFY REAL VALUE
                }
                catch (Exception ex) 
                {
                    Logger.LogInfo($"JiHPChange default Unexpected error: {ex.Message}");
                }
            }
            try 
            {
                JiStateChange = Pass;
                StartCoroutine(InitJiStateChange());
            }
            catch (Exception ex) 
            {
                Logger.LogInfo($"InitJiStateChange Unexpected error: {ex.Message}");
            }
        }
        else {
            JiUpdate = Pass;
        }
    }

    private IEnumerator delayTitleChange()
    {
        yield return new WaitForSeconds(1f);

        RubyTextMeshProUGUI BossName = GameObject.Find(
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/MonsterHPRoot/BossHPRoot/UIBossHP(Clone)/Offset(DontKeyAnimationOnThisNode)/AnimationOffset/BossName")
            .GetComponent<RubyTextMeshProUGUI>();
        BossName.text = "The Kunlun Immortal";
    }

    private IEnumerator InitJiStateChange() 
    {
        JiStateChange = Pass;
        while (MonsterManager.Instance.ClosetMonster == null) 
        {
            yield return null;
        }
        JiStateChange = HandleStateChange;
    }

    private static Action ActionSpriteUpdate = () => 
    {
        ColorChange.updateJiSprite(mat);
        ColorChange.updateCrimsonSprites(crimsonMat);
        ColorChange.updateCrimsonParticleRenderers(crimsonMat);
        ColorChange.updateCrimsonParticles(crimsonColor);
    };

    private void ActionUpdate ()
    {
        try 
        {
            JiSpeedChange();
        }
        catch (Exception ex) 
        {
            Logger.LogInfo($"JiSpeedChange Unexpected error: {ex.Message}");
        }
        try 
        {
            JiStateChange();
        }
        catch (Exception ex) 
        {
            Logger.LogInfo($"JiStateChange Unexpected error: {ex.Message}");
        }
        try 
        {
            JiSpriteUpdate();
        }
        catch (Exception ex) 
        {
            Logger.LogInfo($"JiSpriteUpdate Unexpected error: {ex.Message}");
        }
    }

    public void Update() 
    {
        JiUpdate();
    }

    private void HandleStateChange() 
    {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (temp != JiMonster.currentMonsterState.ToString())
        {
            if (JiMonster.currentMonsterState.ToString() != "1_Engaging (StealthEngaging)") 
            {
                temp = JiMonster.currentMonsterState.ToString();
                randomNum = random.Next();
            }

            // Logger.LogInfo($"'{temp}'");
            // Logger.LogInfo(JiMonster.currentMonsterState.ToString());
            // Logger.LogInfo(GetCurrentSequence());
            // Logger.LogInfo("");

            if (JiMonster.currentMonsterState == PhaseChangeState)
            {
                CurrSpeedDict = SpeedDict2;
                HandlePhaseTransitionText();
                StartCoroutine(delayTitleChange());
            } 
            else if (JiMonster.currentMonsterState == BossGeneralStates[1])
            {
                HurtInterrupt.enabled = true;
            } 
            else 
            {
                HurtInterrupt.enabled = false;
            }
        }
    }

    private static void HandlePhaseTransitionText()
    {
        if (firstMessage)
        {
            firstMessage = false;
            return;
        }
        PhaseTransitionText.text = randomNum % 4 == 0 ? meme_quotes[random.Next() % meme_quotes.Length] : lore_quotes[random.Next() % lore_quotes.Length];
    }

    private static Action ActionSpeedChange = () => 
    {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (!JiMonster) return;

        if (CurrSpeedDict.TryGetValue(JiMonster.currentMonsterState.ToString(), out Func<string, float> incrFunc)) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + incrFunc(JiMonster.LastClipName);
        }
        else 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value;
        }
        JiStunState.enabled = JiMonster.currentMonsterState == BossGeneralStates[6];
    };

    private IEnumerator JiHPChange(float newHp, float phase2Ratio) 
    {
        while (!MonsterManager.Instance.ClosetMonster)
        {
            yield return null;
        }
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        var baseHealthRef = AccessTools.FieldRefAccess<MonsterStat, float>("BaseHealthValue");
        if (JiMonster.postureSystem.CurrentHealthValue != newHp || baseHealthRef(JiMonster.monsterStat) != newHp / 1.35f)
        {
            baseHealthRef(JiMonster.monsterStat) = newHp / 1.35f;
            JiMonster.postureSystem.CurrentHealthValue = newHp;
        }
        Logger.LogInfo($"Ji Phase 1 HP at {JiMonster.postureSystem.CurrentHealthValue}");
        JiMonster.monsterStat.Phase2HealthRatio = phase2Ratio;

    }

    private static MonsterStateGroupSequence GetCurrentSequence()
    {
        Type type = attackSequenceModule.GetType();
        FieldInfo fieldInfo = type.GetField("sequence", BindingFlags.Instance | BindingFlags.NonPublic);
        MonsterStateGroupSequence sequenceValue = (MonsterStateGroupSequence)fieldInfo.GetValue(attackSequenceModule);
        return sequenceValue;
    }

    private MonsterStateGroupSequence getGroupSequence1(string name)
    {
        return GameObject.Find($"{jiAttackSequences1Path}{name}").GetComponent<MonsterStateGroupSequence>();
    }

    private MonsterStateGroupSequence getGroupSequence2(string name)
    {
        return GameObject.Find($"{jiAttackSequences2Path}{name}").GetComponent<MonsterStateGroupSequence>();
    }

    private MonsterStateGroup getGroup(string name)
    {
        return GameObject.Find($"{jiAttackGroupsPath}{name}").GetComponent<MonsterStateGroup>();
    }

    private BossGeneralState getBossGeneralState(string name)
    {
        return GameObject.Find($"{jiAttackStatesPath}{name}").GetComponent<BossGeneralState>();
    }

    public void GetAttackGameObjects()
    {
        // Object Paths
        string GeneralBossFightPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/";
        jiBossPath = GeneralBossFightPath + "LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/";
        jiAttackStatesPath = jiBossPath + "States/Attacks/";
        jiAttackSequences1Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase1/";
        jiAttackSequences2Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase2/";
        jiAttackGroupsPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateGroupDefinition/";

        // Gathering BossGeneralStates & Other States
        for (int i = 1; i < Attacks.Length; i++)
        {
            BossGeneralStates[i] = getBossGeneralState(Attacks[i]);
            Weights[i] = CreateWeight(BossGeneralStates[i]);
        }
        BossGeneralStates[Hurt] = GameObject.Find($"{jiBossPath}States/HurtState/").GetComponent<BossGeneralState>();
        BossGeneralStates[BigHurt] = GameObject.Find($"{jiBossPath}States/Hurt_BigState").GetComponent<BossGeneralState>();
        JiStunState = GameObject.Find($"{jiBossPath}States/PostureBreak/").GetComponent<PostureBreakState>();
        PhaseChangeState = GameObject.Find($"{jiBossPath}States/[BossAngry] BossAngry/").GetComponent<BossPhaseChangeState>();
        // Engaging = GameObject.Find($"{jiBossPath}States/1_Engaging").GetComponent<StealthEngaging>();

        // Gathering MonsterGroupStateSequences
        for (int i = 0; i < SequenceStrings1.Length; i++)
        {
            Sequences1[i] = getGroupSequence1($"MonsterStateGroupSequence1{SequenceStrings1[i]}");
        }
        for (int i = 0; i < SequenceStrings2.Length; i++)
        {
            if (SequenceStrings2[i] is null) continue;
            Sequences2[i] = getGroupSequence2($"MonsterStateGroupSequence1{SequenceStrings2[i]}");
        }
        Sequences2[Health] = GameObject.Find(
            $"{jiBossPath}MonsterCore/AttackSequenceModule/SpecialHealthSequence(Jee_Divination_Logic)")
            .GetComponent<MonsterStateGroupSequence>();

        // Gathering existing MonsterGroupState
        int j = 0;
        foreach ((int sequence, int index) in ExistingGroupPairs)
        {
            Groups[j++] = Sequences1[sequence].AttackSequence[index];
        }
        Groups[BigBlackHole] = Sequences2[Opening].AttackSequence[0];
        Groups[EasyOrHardFinisher] = Sequences2[Default].AttackSequence[5];

        // Gathering Miscellaneous Object
        attackSequenceModule = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/").GetComponent<AttackSequenceModule>();
        
        PhaseTransitionText = GameObject.Find(GeneralBossFightPath + 
            "[CutScene]BossAngry_Cutscene/BubbleRoot/SceneDialogueNPC/BubbleRoot/DialogueBubble/Text")
            .GetComponent<RubyTextMeshPro>();

        HurtInterrupt = GameObject.Find(jiBossPath + 
            "MonsterCore/Animator(Proxy)/Animator/LogicRoot/HurtInterrupt").GetComponent<MonsterHurtInterrupt>();
    }

    private Weight<MonsterState> CreateWeight(MonsterState state)
    {
        return new Weight<MonsterState>
        {
            weight = 1,
            option = state
        };
    }

    private MonsterStateGroup CreateMonsterStateGroup(int[] AttacksList, string objectName)
    {
        List<Weight<MonsterState>> newStateWeightList = new();
        GameObject GO = new($"{objectName}");
        MonsterStateGroup newAttackGroup = new();

        foreach (int attackIndex in AttacksList)
        {
            newStateWeightList.Add(Weights[attackIndex]);
        }

        MonsterStateWeightSetting newWeightSetting = new() {
            stateWeightList = newStateWeightList, queue = [], customizedInitQueue = []
        };
        newAttackGroup = GO.AddComponent<MonsterStateGroup>();
        newAttackGroup.setting = newWeightSetting;
        return newAttackGroup;
    }

    private void ModifySequence(int sequenceIndex, int phase, 
        int[] groupsToAdd, 
        (int group, int index)[] groupsToOverwrite,
        (int group, int index)[] groupsToInsert)
    {
        MonsterStateGroupSequence sequence = phase == 1 ? Sequences1[sequenceIndex] : Sequences2[sequenceIndex];

        foreach (int group in groupsToAdd)
        {
            sequence.AttackSequence.Add(Groups[group]);
        }
        foreach ((int group, int index) in groupsToOverwrite)
        {
            sequence.AttackSequence[index] = Groups[group];
        }
        foreach ((int group, int index) in groupsToInsert)
        {
            sequence.AttackSequence.Insert(index, Groups[group]);
        }
    }


    // SOMETHING HERE IN PHASE 2 IS ALLOWING FOR SMALL BLACK HOLE BIG BLACK HOLE BACK TO BACK AND THAT NOT GOOD
    public void AlterAttacks()
    {
        if (Sequences1[Default].AttackSequence.Contains(Groups[SneakAttack])) 
        {
            return;
        }

        // Custom Attack Groups
        int i = ExistingGroupPairs.Length + 2;
        foreach ((int[] attacksList, string objectName) in customGroupPatterns)
        {
            Groups[i++] = CreateMonsterStateGroup(attacksList, objectName);
        }

        // Phase 1 Sequence Attack Modifications
        ModifySequence(Default, 1, [SneakAttack], 
            [(SneakAttack, 2), (EasyOrHardFinisher, 4)], 
            [(BackAttack, 4), (EasyOrHardFinisher, 5)]
        );

        ModifySequence(WithAltar, 1, [SneakAttack],
            [(SmallBlackHole, 2), (EasyLaserAltar, 3), (EasyOrHardFinisher, 4)],
            [(BackAttack, 4), (EasyOrHardFinisher, 5)]
        );

        ModifySequence(WithSmallBlackHole, 1, [SneakAttack],
            [(EasyLaserAltar, 2), (Blizzard, 3), (EasyOrHardFinisher, 5)],
            [(EasyOrHardFinisher, 5)]
        );

        ModifySequence(QuickToBlizzard, 1, [SneakAttack],
            [(Blizzard, 3), (EasyOrHardFinisher, 4)],
            [(EasyLaserAltar, 1), (EasyOrHardFinisher, 5)]
        );

        ModifySequence(Health, 2, [SneakAttack], [], [(LaserAltarOrFinisher, 2), (BigBlackHole, 3)]);
        // ModifySequence(Health, 2, [SneakAttack], [], 
        //     [(EasyLaserAltar, 2), (Blizzard, 2), (EasyLaserAltar, 2), (Blizzard, 2), (EasyLaserAltar, 2), (Blizzard, 2), (EasyLaserAltar, 2), (Blizzard, 2), (EasyLaserAltar, 2), (Blizzard, 2), (EasyLaserAltar, 2), (Blizzard, 2)]
        // );

        // Phase 2 Sequence Attack Modifications
        ModifySequence(Opening, 2, [SneakAttack2], [], [
            (HardLaserAltar, 1), (BigBlackHole, 2), (HardLaserAltar, 3), (Blizzard, 4), (SmallBlackHole, 5), (LongerAttack, 6),
            (HardLaserAltar, 7), (DoubleTrouble, 8), (LongerAttack, 9), (HardLaserAltar, 10)
        ]);

        ModifySequence(Default, 2, [SneakAttack2], 
            [(EasyOrHardFinisher, 3), (EasyOrHardFinisher, 4), (EasyOrHardFinisher, 5)],
            [(DoubleTrouble, 2), (DoubleTrouble, 4), (HardLaserAltar, 5)]
        );

        ModifySequence(WithAltar, 2, [SneakAttack2], 
            [(HardLaserAltar, 1), (DoubleTrouble, 2), (EasyOrHardFinisher, 4), 
                (EasyOrHardFinisher, 5), (EasyOrHardFinisher, 6)],
            [(Blizzard, 2), (HardLaserAltar, 5)]
        );

        ModifySequence(WithSmallBlackHole, 2, [SneakAttack2], 
            [(LongerAttack, 2), (HardLaserAltar, 3), (Blizzard, 4), (EasyOrHardFinisher, 5), 
                (EasyOrHardFinisher, 6), (EasyOrHardFinisher, 7)], 
            [(HardLaserAltar, 5)]
        );

        ModifySequence(QuickToBlizzard, 2, [SneakAttack2], 
            [(EasyOrHardFinisher, 4), (EasyOrHardFinisher, 5), (EasyOrHardFinisher, 6)],
            [(Blizzard, 3), (HardLaserAltar, 5)]
        );

        ModifySequence(QuickToBlackHole, 2, [SneakAttack2], 
            [(EasyOrHardFinisher, 2), (EasyOrHardFinisher, 3), (EasyOrHardFinisher, 4)], 
            [(HardLaserAltar, 2), (BigBlackHole, 3), (LongerAttack, 4), (HardLaserAltar, 5)]
        );
    }

    private void OnDestroy()
    {
        firstMessage = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        bundle.Unload(false);
        harmony.UnpatchSelf();
    }
}
