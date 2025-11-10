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

using System;
using System.Reflection;

namespace EnlightenedJi;

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class EnlightenedJi : BaseUnityPlugin {
    // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html
    private ConfigEntry<bool> enableSomethingConfig = null!;
    private ConfigEntry<KeyboardShortcut> somethingKeyboardShortcut = null!;

    private Harmony harmony = null!;

    private ConfigEntry<float> JiAnimatorSpeed = null!;
    private ConfigEntry<float> JiHPScale = null!;

    private string jiBossPath = "";
    private string jiAttackStatesPath = "";
    private string jiStunStatePath = "";
    private string jiAttackSequencesPath = "";
    private string jiAttackGroupsPath = "";

    private bool HPUpdated = false;
    private float originalHPValue = 0;
    private float originalHPRef = 0;

    // #region Attacks BossGeneralState
    // BossGeneralState DivinationFreeZoneBossGeneralState = null!;
    // BossGeneralState FlyingProjectilesBossGeneralState = null!;
    // BossGeneralState BlackHoleAttackBossGeneralState = null!;
    // BossGeneralState SetLaserAltarsBossGeneralState = null!;
    // BossGeneralState SuckSwordBossGeneralState = null!;
    // BossGeneralState TeleportSwordSmashBossGeneralState = null!;
    // BossGeneralState SwordBlizzardBossGeneralState = null!;
    // BossGeneralState DivinationFreeZoneEndlessBossGeneralState = null!;
    // BossGeneralState GroundSwordBossGeneralState = null!;
    // BossGeneralState SmallBlackHoleBossGeneralState = null!;
    // BossGeneralState ShortFlyingSwordBossGeneralState = null!;
    // BossGeneralState QuickHorizontalDoubleSwordBossGeneralState = null!;
    // BossGeneralState QuickTeleportSwordBossGeneralState = null!;
    // BossGeneralState LaserAltarCircleBossGeneralState = null!;
    // BossGeneralState HealthAltarBossGeneralState = null!;
    // BossGeneralState DivinationJumpKickedBossGeneralState = null!;
    // #endregion

    #region Attack Sequences MonsterStateGroupSequence
    MonsterStateGroupSequence AttackSequence1_FirstAttackGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_GroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_AltarGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_SmallBlackHoleGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_QuickToBlizzardGroupSequence = null!;
    MonsterStateGroupSequence SpecialHealthSequence = null!;
    #endregion

    #region Attack Groups MonsterStateGroup
    MonsterStateGroup SmallBlackHoleMonsterStateGroup = null!;
    #endregion

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(EnlightenedJi).Assembly);

        // enableSomethingConfig = Config.Bind("General.Something", "Enable", true, "Enable the thing");
        // somethingKeyboardShortcut = Config.Bind("General.Something",
        //     "Shortcut",
        //     new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
        //     "Shortcut to execute");

        // Usage of the modding API is entirely optional.
        // It provides utilities like the KeybindManager, utilities for Instantiating objects including the 
        // NineSols lifecycle hooks, displaying toast messages and preloading objects from other scenes.
        // If you do use the API make sure do have it installed when running your mod, and keep the dependency in the
        // thunderstore.toml.

        // KeybindManager.Add(this, TestMethod, () => somethingKeyboardShortcut.Value);
        KeybindManager.Add(this, LoadAssetBundle, KeyCode.T);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        JiAnimatorSpeed = Config.Bind("General", "JiSpeed", 1f, "The speed at which Ji's attacks occur");
        JiHPScale = Config.Bind("General", "JiHPScale", 1f, "The scale for Ji's HP, 1 is modded HP amount");
    }

    public void Update() {
        if (SceneManager.GetActiveScene().name == "A10_S5_Boss_Jee") {
            GetAttackGameObjects();
            AlterAttacks();
            JiHPChange();
            MonsterManager.Instance.ClosetMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value;

            
        };
    }

    private void JiHPChange() {
        var baseHealthRef = AccessTools.FieldRefAccess<MonsterStat, float>("BaseHealthValue");
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        // baseHealthRef(JiMonster.monsterStat) = 5000f;

        if (!HPUpdated && JiMonster) {
            ToastManager.Toast($"HP Value before change: {JiMonster.postureSystem.CurrentHealthValue}");
            baseHealthRef(JiMonster.monsterStat) = 5185.18518519f;
            JiMonster.postureSystem.CurrentHealthValue = 7000f;
            

            HPUpdated = true;
            ToastManager.Toast($"Set Ji Phase 1 HP to {JiMonster.postureSystem.CurrentHealthValue}");
        }
        JiMonster.monsterStat.Phase2HealthRatio = 2;
    }

    private void TestMethod() {
        if (!enableSomethingConfig.Value) return;

        ToastManager.Toast("Shortcut activateds");
        Log.Info("Log messages will only show up in the logging console and LogOutput.txt");

        // Sometimes variables aren't present in the title screen. Make sure to check for null to prevent crashes.
        if (!Player.i) return;

        var hasHat = Player.i.GetFieldValue<bool>("_hasHat"); // gets the field via reflection
        Player.i.SetHasHat(!hasHat);
    }

    private void LoadAssetBundle() {
        // const string path = "/home/jakob/dev/unity/RustyAssetBundleEXtractor/out.test";
        // var allBytes = File.ReadAllBytes(path);
        // var assetBundle = AssetBundle.LoadFromMemory(allBytes);

        // The bundle is defined in the .csproj as <EmbeddedResource />
        // var assetBundle = AssemblyUtils.GetEmbeddedAssetBundle("ExampleMod.preloads.bundle");
        // In a real mod you probably want to load the assetbundle once when you want to use it,
        // and keep the spawned scene in memory if they're not too big.
        // There's a bunch of optimizations you can figure out here.

        // QuickTeleportSwordBossGeneralState = GameObject.Find($"{jiAttackStatesPath}[5][Short]SuckSword 往內/").GetComponent<BossGeneralState>();

        // DumpType(QuickTeleportSwordBossGeneralState);

        ToastManager.Toast($"Current Ji HP {MonsterManager.Instance.ClosetMonster.postureSystem.CurrentHealthValue}");
        // if (assetBundle == null) {
        //     ToastManager.Toast("Failed to load AssetBundle");
        //     return;
        // }

        // StartCoroutine(SpawnEnemies(assetBundle));
    }

    private MonsterStateGroupSequence getGroupSequence(string name){
        return GameObject.Find($"{jiAttackSequencesPath}{name}").GetComponent<MonsterStateGroupSequence>();
    }

    private MonsterStateGroup getGroup(string name){
        return GameObject.Find($"{jiAttackGroupsPath}{name}").GetComponent<MonsterStateGroup>();
    }

    public void GetAttackGameObjects(){
        // jiAttackStatesPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/States/Attacks/";
        // jiStunStatePath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/States/PostureBreak/";
        // string foo = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/LogicRoot/Sensors/1_AttackSensor/";

        jiBossPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/";

        jiAttackSequencesPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase1/";
        jiAttackGroupsPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateGroupDefinition/";

        AttackSequence1_FirstAttackGroupSequence = getGroupSequence("MonsterStateGroupSequence1_FirstAttack_WithoutDivination");
        AttackSequence1_GroupSequence = getGroupSequence("MonsterStateGroupSequence1");
        AttackSequence1_AltarGroupSequence = getGroupSequence("MonsterStateGroupSequence1_WithAltar");
        AttackSequence1_SmallBlackHoleGroupSequence = getGroupSequence("MonsterStateGroupSequence1_WithSmallBlackHole");
        AttackSequence1_QuickToBlizzardGroupSequence = getGroupSequence("MonsterStateGroupSequence1_QuickToBlizzard");
        SpecialHealthSequence = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/SpecialHealthSequence(Jee_Divination_Logic)").GetComponent<MonsterStateGroupSequence>();
        SmallBlackHoleMonsterStateGroup = getGroup("MonsterStateGroup_SmallBlackHole(Attack10)");

    }

    private void AddSmallBlackHoleGroupToSequence(MonsterStateGroupSequence sequence){
        List<MonsterStateGroup> attackSequence = sequence.AttackSequence;
        if (attackSequence[attackSequence.Count - 1] != SmallBlackHoleMonsterStateGroup && MonsterManager.Instance.ClosetMonster){
            attackSequence.Add(SmallBlackHoleMonsterStateGroup);
            ToastManager.Toast($"Inserted Small Black Hole Attack into {sequence.name}");
        }
    }

    public void AlterAttacks(){
        AddSmallBlackHoleGroupToSequence(AttackSequence1_FirstAttackGroupSequence);
        AddSmallBlackHoleGroupToSequence(AttackSequence1_GroupSequence);
        AddSmallBlackHoleGroupToSequence(AttackSequence1_AltarGroupSequence);
        AddSmallBlackHoleGroupToSequence(AttackSequence1_SmallBlackHoleGroupSequence);
        AddSmallBlackHoleGroupToSequence(AttackSequence1_QuickToBlizzardGroupSequence);
        AddSmallBlackHoleGroupToSequence(SpecialHealthSequence);
        // List<MonsterStateGroup> AttackSequence1_FirstAttack = AttackSequence1_FirstAttackGroupSequence.AttackSequence;
        // if (!AttackSequence1_FirstAttack.Contains(SmallBlackHoleMonsterStateGroup)){
        //     AttackSequence1_FirstAttack.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast("Inserted Small Black Hole Attack into First Attack Sequence");
        // }
        // if (!AttackSequence1_GroupSequence.AttackSequence.Contains(SmallBlackHoleMonsterStateGroup)){
        //     AttackSequence1_GroupSequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast("Inserted Small Black Hole Attack into Attack Sequence");
        // }
        // if (!AttackSequence1_AltarGroupSequence.AttackSequence.Contains(SmallBlackHoleMonsterStateGroup)){
        //     AttackSequence1_AltarGroupSequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast("Inserted Small Black Hole Attack into Altar Attack Sequence");
        // }
        // if (!AttackSequence1_SmallBlackHoleGroupSequence.AttackSequence.Contains(SmallBlackHoleMonsterStateGroup)){
        //     AttackSequence1_SmallBlackHoleGroupSequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast("Inserted Small Black Hole Attack into Small Black Hole Attack Sequence");
        // }
        // if (!AttackSequence1_QuickToBlizzardGroupSequence.AttackSequence.Contains(SmallBlackHoleMonsterStateGroup)){
        //     AttackSequence1_QuickToBlizzardGroupSequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast("Inserted Small Black Hole Attack into Quick To Blizzard Attack Sequence");
        // }
    }


    // Loads all the scenes contained in the AssetBundle, and spawn copies of their objects
    private static IEnumerator SpawnEnemies(AssetBundle bundle) {
        var sceneNames = bundle.GetAllScenePaths()
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        var start = Time.time;
        var ops = sceneNames
            .Select(sceneName => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive))
            .ToList();
        foreach (var op in ops) yield return op;

        Log.Info($"Loading took {Time.time - start}s");
        bundle.Unload(false);

        try {
            List<GameObject> bosses = [];
            foreach (var prefab in sceneNames.Select(SceneManager.GetSceneByName)
                         .SelectMany(scene => scene.GetRootGameObjects())) {
                ToastManager.Toast($"Spawning {prefab.name}");
                var instantiation = ObjectUtils.InstantiateInit(prefab);
                instantiation.SetActive(true);
                instantiation.transform.position = Player.i.transform.position;
                bosses.Add(instantiation);
            }

            yield return new WaitForSeconds(20);

            bosses.ForEach(Destroy);
        } finally {
            sceneNames.ForEach(sceneName => SceneManager.UnloadSceneAsync(sceneName));
        }
    }


    private void OnDestroy() {
        // Make sure to clean up resources here to support hot reloading
        HPUpdated = false;
        if (originalHPValue != 0) {
            // MonsterManager.Instance.ClosetMonster.postureSystem.CurrentHealthValue = originalHPValue;
        }
        harmony.UnpatchSelf();
    }




    public static void DumpType(object obj)
    {
        if (obj == null)
        {
            Log.Info("Object is null, cannot inspect");
            return;
        }

        Type type = obj.GetType();
        Log.Info($"Dumping members of type: {type.FullName}");

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Fields
        FieldInfo[] fields = type.GetFields(flags);
        Log.Info($"Fields ({fields.Length}):");
        foreach (var f in fields)
        {
            var value = f.GetValue(obj);
            Log.Info($"  Field: {f.Name} (Type: {f.FieldType.Name}) = {value}");
        }

        // Properties
        PropertyInfo[] props = type.GetProperties(flags);
        Log.Info($"Properties ({props.Length}):");
        foreach (var p in props)
        {
            // Optionally safe-get value
            object val = null;
            try { val = p.GetValue(obj); }
            catch { val = " <unreadable> "; }
            Log.Info($"  Property: {p.Name} (Type: {p.PropertyType.Name}) = {val}");
        }

        // Methods
        MethodInfo[] methods = type.GetMethods(flags);
        Log.Info($"Methods ({methods.Length}):");
        foreach (var m in methods)
        {
            if (m.IsSpecialName) continue;  // skip property accessors, etc
            Log.Info($"  Method: {m.Name} (Return: {m.ReturnType.Name})");
        }
    }
}