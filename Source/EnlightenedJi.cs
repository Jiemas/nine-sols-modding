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
    BossGeneralState SmallBlackHoleBossGeneralState = null!;
    BossGeneralState ShortFlyingSwordBossGeneralState = null!;
    BossGeneralState QuickHorizontalDoubleSwordBossGeneralState = null!;
    BossGeneralState QuickTeleportSwordBossGeneralState = null!;
    BossGeneralState LaserAltarCircleBossGeneralState = null!;
    // BossGeneralState HealthAltarBossGeneralState = null!;
    // BossGeneralState DivinationJumpKickedBossGeneralState = null!;
    // #endregion

    PostureBreakState JiStunState = null!;

    #region Attack Sequences MonsterStateGroupSequence
    MonsterStateGroupSequence AttackSequence1_FirstAttackGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_GroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_AltarGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_SmallBlackHoleGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_QuickToBlizzardGroupSequence = null!;
    MonsterStateGroupSequence SpecialHealthSequence = null!;

    MonsterStateGroup SneakAttackStateGroup = new MonsterStateGroup();
    #endregion

    #region Attack Groups MonsterStateGroup
    MonsterStateGroup SmallBlackHoleMonsterStateGroup = null!;
    #endregion

    StealthEngaging Engaging = null!;

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

        JiAnimatorSpeed = Config.Bind("General", "JiSpeed", 1.2f, "The speed at which Ji's attacks occur");
        JiHPScale = Config.Bind("General", "JiHPScale", 7000f, "The amount of Ji's HP in Phase 1 (Phase 2 HP is double this value)");
    }

    public void Update() {
        if (SceneManager.GetActiveScene().name == "A10_S5_Boss_Jee") {
            var JiMonster = MonsterManager.Instance.ClosetMonster;

            GetAttackGameObjects();
            AlterAttacks();
            JiHPChange();
            if (JiMonster && (JiMonster.currentMonsterState == SmallBlackHoleBossGeneralState || 
            JiMonster.currentMonsterState == Engaging || 
            (JiMonster.LastClipName == "Attack13" && JiMonster.currentMonsterState != QuickTeleportSwordBossGeneralState))) {
                JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 3;
                // ToastManager.Toast("Increased Ji Animation Speed for Black Hole and Laser Altar Circle Attack");
            } else {
                JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value;
            }
            
        };
    }

    private void JiHPChange() {
        var baseHealthRef = AccessTools.FieldRefAccess<MonsterStat, float>("BaseHealthValue");
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (!HPUpdated && JiMonster) {
            baseHealthRef(JiMonster.monsterStat) = JiHPScale.Value / 1.35f;
            JiMonster.postureSystem.CurrentHealthValue = JiHPScale.Value;
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

        DumpType(MonsterManager.Instance.ClosetMonster);
        MonsterManager.Instance.ClosetMonster.OnShield();
        
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

    private BossGeneralState getBossGeneralState(string name){
        return GameObject.Find($"{jiAttackStatesPath}{name}").GetComponent<BossGeneralState>();
    }

    public void GetAttackGameObjects(){
        // jiAttackStatesPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/States/Attacks/";
        // string foo = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/MonsterCore/Animator(Proxy)/Animator/LogicRoot/Sensors/1_AttackSensor/";

        jiBossPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/";

        jiAttackStatesPath = jiBossPath + "States/Attacks/";
        jiAttackSequencesPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase1/";
        jiAttackGroupsPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateGroupDefinition/";

        AttackSequence1_FirstAttackGroupSequence = getGroupSequence("MonsterStateGroupSequence1_FirstAttack_WithoutDivination");
        AttackSequence1_GroupSequence = getGroupSequence("MonsterStateGroupSequence1");
        AttackSequence1_AltarGroupSequence = getGroupSequence("MonsterStateGroupSequence1_WithAltar");
        AttackSequence1_SmallBlackHoleGroupSequence = getGroupSequence("MonsterStateGroupSequence1_WithSmallBlackHole");
        AttackSequence1_QuickToBlizzardGroupSequence = getGroupSequence("MonsterStateGroupSequence1_QuickToBlizzard");
        SpecialHealthSequence = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/SpecialHealthSequence(Jee_Divination_Logic)").GetComponent<MonsterStateGroupSequence>();
        
        SmallBlackHoleMonsterStateGroup = getGroup("MonsterStateGroup_SmallBlackHole(Attack10)");

        JiStunState = GameObject.Find($"{jiBossPath}States/PostureBreak/").GetComponent<PostureBreakState>();

        SmallBlackHoleBossGeneralState = getBossGeneralState("[10][Altar]SmallBlackHole");
        ShortFlyingSwordBossGeneralState = getBossGeneralState("[11][Short]ShorFlyingSword");
        QuickHorizontalDoubleSwordBossGeneralState = getBossGeneralState("[12][Short]QuickHorizontalDoubleSword");
        LaserAltarCircleBossGeneralState = getBossGeneralState("[14][Altar]Laser Altar Circle");
        QuickTeleportSwordBossGeneralState = getBossGeneralState("[13][Finisher]QuickTeleportSword 危戳");

        Engaging = GameObject.Find($"{jiBossPath}States/1_Engaging").GetComponent<StealthEngaging>();
    }

    private void AddSmallBlackHoleGroupToSequence(MonsterStateGroupSequence sequence){
        sequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
        // List<MonsterStateGroup> attackSequence = sequence.AttackSequence;
        // if (attackSequence[attackSequence.Count - 1] != SmallBlackHoleMonsterStateGroup){
        //     attackSequence.Add(SmallBlackHoleMonsterStateGroup);
        //     ToastManager.Toast($"Inserted Small Black Hole Attack into {sequence.name}");
        // }
    }

    private void AddSneakAttackGroupToSequence(MonsterStateGroupSequence sequence){
        sequence.AttackSequence.Add(SneakAttackStateGroup);
        // if (attackSequence[attackSequence.Count - 1].name != "SneakAttackStateGroup"){
        //     attackSequence.Add(SneakAttackStateGroup);
        //     ToastManager.Toast($"Inserted Sneak Attack into {sequence.name}");
        // }
    }

    private Weight<MonsterState> CreateWeight(MonsterState state){
        return new Weight<MonsterState>{
            weight = 1,
            option = state
        };
    }

    public void AlterAttacks(){
        if (!JiStunState.enabled) return;
        JiStunState.enabled = false;

        Weight<MonsterState> SmallBlackHoleWeight = CreateWeight(SmallBlackHoleBossGeneralState);
        Weight<MonsterState> ShortFlyingSwordWeight = CreateWeight(ShortFlyingSwordBossGeneralState);
        Weight<MonsterState> QuickHorizontalDoubleSwordWeight = CreateWeight(QuickHorizontalDoubleSwordBossGeneralState);
        Weight<MonsterState> LaserAltarCircleWeight = CreateWeight(LaserAltarCircleBossGeneralState);
        Weight<MonsterState> QuickTeleportSwordWeight = CreateWeight(QuickTeleportSwordBossGeneralState);

        MonsterStateWeightSetting SurpriseAttackWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{
                SmallBlackHoleWeight,
                ShortFlyingSwordWeight,
                QuickHorizontalDoubleSwordWeight,
                LaserAltarCircleWeight,
                QuickTeleportSwordWeight
            },
            queue = new List<MonsterState>{
                SmallBlackHoleBossGeneralState, 
                ShortFlyingSwordBossGeneralState, 
                QuickHorizontalDoubleSwordBossGeneralState, 
                LaserAltarCircleBossGeneralState,
                QuickTeleportSwordBossGeneralState
            }
        };

        GameObject go = new GameObject("MonsterStateGroup_SneakAttack(Attack 10/11/12/13/14)");
        SneakAttackStateGroup = go.AddComponent<MonsterStateGroup>();
        SneakAttackStateGroup.setting = SurpriseAttackWeightSetting;


        // SneakAttackStateGroup = new MonsterStateGroup { setting = SurpriseAttackWeightSetting };
        bool isNull = SneakAttackStateGroup == null;
        ToastManager.Toast($"1. isNull? {isNull}");
        if (SneakAttackStateGroup != null) {
            ToastManager.Toast($"1. Type: {SneakAttackStateGroup.GetType().FullName}");
        }

        // SneakAttackStateGroup = GameObject.AddComponent<MonsterStateGroup>();
        // SneakAttackStateGroup.setting = SurpriseAttackWeightSetting;
        // ToastManager.Toast($"11. isNull? {isNull}");
        // if (SneakAttackStateGroup != null) {
        //     ToastManager.Toast($"11. Type: {SneakAttackStateGroup.GetType().FullName}");
        // }

        AddSneakAttackGroupToSequence(AttackSequence1_FirstAttackGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_GroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_AltarGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_SmallBlackHoleGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_QuickToBlizzardGroupSequence);
        AddSneakAttackGroupToSequence(SpecialHealthSequence);

        ToastManager.Toast($"2. {SneakAttackStateGroup}");
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