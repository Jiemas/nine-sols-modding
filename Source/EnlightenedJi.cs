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
    private string jiAttackSequences1Path = "";
    private string jiAttackSequences2Path = "";
    private string jiAttackGroupsPath = "";

    private bool HPUpdated = false;
    private bool random = true;
    private bool phase2 = false;

    string temp = "";

    // #region Attacks BossGeneralState
    BossGeneralState DivinationFreeZoneBossGeneralState = null!;
    BossGeneralState FlyingProjectilesBossGeneralState = null!;
    BossGeneralState BlackHoleAttackBossGeneralState = null!;
    BossGeneralState SetLaserAltarsBossGeneralState = null!;
    BossGeneralState SuckSwordBossGeneralState = null!;
    BossGeneralState TeleportSwordSmashBossGeneralState = null!;
    BossGeneralState SwordBlizzardBossGeneralState = null!;
    // BossGeneralState DivinationFreeZoneEndlessBossGeneralState = null!;
    BossGeneralState GroundSwordBossGeneralState = null!;
    BossGeneralState SmallBlackHoleBossGeneralState = null!;
    BossGeneralState ShortFlyingSwordBossGeneralState = null!;
    BossGeneralState QuickHorizontalDoubleSwordBossGeneralState = null!;
    BossGeneralState QuickTeleportSwordBossGeneralState = null!;
    BossGeneralState LaserAltarCircleBossGeneralState = null!;
    BossGeneralState HealthAltarBossGeneralState = null!;
    BossGeneralState DivinationJumpKickedBossGeneralState = null!;

    BossGeneralState HurtBossGeneralState = null!;
    BossGeneralState BigHurtBossGeneralState = null!;
    // #endregion

    PostureBreakState JiStunState = null!;

    #region Attack Sequences MonsterStateGroupSequence
    MonsterStateGroupSequence AttackSequence1_FirstAttackGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_GroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_AltarGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_SmallBlackHoleGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence1_QuickToBlizzardGroupSequence = null!;
    MonsterStateGroupSequence SpecialHealthSequence = null!;
    MonsterStateGroupSequence AttackSequence2_Opening = null!;
    #endregion

    #region Attack Groups MonsterStateGroup
    MonsterStateGroup SneakAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup BackAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup LongerOrBlizardAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup SwordOrLaserAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup SwordOrAltarAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup LaserAltarHardAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup DoubleTroubleAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup SneakAttack2StateGroup = new MonsterStateGroup();
    MonsterStateGroup SmallBlackHoleMonsterStateGroup = null!;
    MonsterStateGroup LongerAttackStateGroup = null!;
    MonsterStateGroup BlizzardAttackStateGroup = null!;
    MonsterStateGroup QuickAttackStateGroup = null!;
    MonsterStateGroup LaserAltarEasyAttackStateGroup = null!;
    MonsterStateGroup FinisherEasierAttackStateGroup = null!;
    #endregion

    StealthEngaging Engaging = null!;
    BossPhaseChangeState PhaseChangeState = null!;

    AttackSequenceModule attackSequenceModule = null!;

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
        JiHPScale = Config.Bind("General", "JiHPScale", 6000f, "The amount of Ji's HP in Phase 1 (Phase 2 HP is double this value)");
    }

    public void Update() {
        if (SceneManager.GetActiveScene().name == "A10_S5_Boss_Jee") {
            var JiMonster = MonsterManager.Instance.ClosetMonster;
            GetAttackGameObjects();
            AlterAttacks();
            JiHPChange();
            JiSpeedChange();

            if (JiMonster && temp != JiMonster.currentMonsterState.ToString()) {
                temp = JiMonster.currentMonsterState.ToString();
                random = !random;
                if (JiMonster.currentMonsterState == PhaseChangeState) {
                    phase2 = true;
                }
                ToastManager.Toast($"{JiMonster.currentMonsterState} | Random: {random} | Phase 2: {phase2}");
            }
        } 
    }

    private void JiSpeedChange() {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (!JiMonster) return;

        if (JiMonster.currentMonsterState == SmallBlackHoleBossGeneralState || 
        JiMonster.currentMonsterState == Engaging || 
        JiMonster.currentMonsterState == DivinationJumpKickedBossGeneralState ||
        JiMonster.currentMonsterState == HurtBossGeneralState ||
        JiMonster.currentMonsterState == BigHurtBossGeneralState ||
        JiMonster.currentMonsterState == JiStunState) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 3;
        } else if (JiMonster.currentMonsterState == HealthAltarBossGeneralState) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 1;
        } else if (!phase2 && JiSpeedChangePhase1()) {
            return;
        } else if (phase2 && JiSpeedChangePhase2()) {
            return;
        } else {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value;
        }

    }

    private bool JiSpeedChangePhase1() {
        // ToastManager.Toast("Phase 1 Speed Change Active");
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if ((JiMonster.LastClipName == "Attack13" || JiMonster.LastClipName == "PostureBreak") && (
            JiMonster.currentMonsterState == ShortFlyingSwordBossGeneralState ||
            JiMonster.currentMonsterState == QuickHorizontalDoubleSwordBossGeneralState ||
            JiMonster.currentMonsterState == LaserAltarCircleBossGeneralState)) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2;
        } else if (JiMonster.currentMonsterState == QuickTeleportSwordBossGeneralState && 
            (JiMonster.LastClipName == "Attack13" || JiMonster.LastClipName == "PostureBreak") || 
            (random && (JiMonster.currentMonsterState == ShortFlyingSwordBossGeneralState 
                || JiMonster.currentMonsterState == SuckSwordBossGeneralState))){
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.5f;
        } else {
            return false;
        }
        return true;
    }

    private bool JiSpeedChangePhase2() {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (JiMonster.currentMonsterState == BlackHoleAttackBossGeneralState) {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2;

        // Sneak Attack Speed Up
        } else if ((JiMonster.LastClipName == "Attack13" || 
            JiMonster.LastClipName == "PostureBreak" || 
            JiMonster.LastClipName == "Attack6") &&
            JiMonster.currentMonsterState != QuickTeleportSwordBossGeneralState && 
            JiMonster.currentMonsterState != TeleportSwordSmashBossGeneralState)
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2; // Might be too fast??
        // } else if (attackSequenceModule.sequence == AttackSequence2_Opening &&
        //     JiMonster.currentMonsterState == LaserAltarHardAttackStateGroup)
        // {
        //     JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 1;
        } else {
            return false;
        }
        return true;
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
        Type type = attackSequenceModule.GetType();
        FieldInfo fieldInfo = type.GetField("sequence", BindingFlags.Instance | BindingFlags.NonPublic);
        MonsterStateGroupSequence sequenceValue = (MonsterStateGroupSequence)fieldInfo.GetValue(attackSequenceModule);
        ToastManager.Toast($"Attack Sequence Module Sequence: {sequenceValue}");

        // DumpType(attackSequenceModule);

        // DumpType(MonsterManager.Instance.ClosetMonster);
        // MonsterManager.Instance.ClosetMonster.currentMonsterState = HealthAltarBossGeneralState;
        
        // if (assetBundle == null) {
        //     ToastManager.Toast("Failed to load AssetBundle");
        //     return;
        // }

        // StartCoroutine(SpawnEnemies(assetBundle));
    }

    private MonsterStateGroupSequence getGroupSequence1(string name){
        return GameObject.Find($"{jiAttackSequences1Path}{name}").GetComponent<MonsterStateGroupSequence>();
    }

    private MonsterStateGroupSequence getGroupSequence2(string name){
        return GameObject.Find($"{jiAttackSequences2Path}{name}").GetComponent<MonsterStateGroupSequence>();
    }

    private MonsterStateGroup getGroup(string name){
        return GameObject.Find($"{jiAttackGroupsPath}{name}").GetComponent<MonsterStateGroup>();
    }

    private BossGeneralState getBossGeneralState(string name){
        return GameObject.Find($"{jiAttackStatesPath}{name}").GetComponent<BossGeneralState>();
    }

    public void GetAttackGameObjects(){

        jiBossPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/";

        jiAttackStatesPath = jiBossPath + "States/Attacks/";
        jiAttackSequences1Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase1/";
        jiAttackSequences2Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase2/";
        jiAttackGroupsPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateGroupDefinition/";

        AttackSequence1_FirstAttackGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_FirstAttack_WithoutDivination");
        AttackSequence1_GroupSequence = getGroupSequence1("MonsterStateGroupSequence1");
        AttackSequence1_AltarGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_WithAltar");
        AttackSequence1_SmallBlackHoleGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_WithSmallBlackHole");
        AttackSequence1_QuickToBlizzardGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_QuickToBlizzard");
        SpecialHealthSequence = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/SpecialHealthSequence(Jee_Divination_Logic)").GetComponent<MonsterStateGroupSequence>();
        
        AttackSequence2_Opening = getGroupSequence2("MonsterStateGroupSequence1_Phase2_OpeningBlackHole");

        SmallBlackHoleMonsterStateGroup = getGroup("MonsterStateGroup_SmallBlackHole(Attack10)");
        LongerAttackStateGroup = AttackSequence1_GroupSequence.AttackSequence[3];
        BlizzardAttackStateGroup = AttackSequence1_QuickToBlizzardGroupSequence.AttackSequence[1];
        QuickAttackStateGroup = AttackSequence1_FirstAttackGroupSequence.AttackSequence[0];
        LaserAltarEasyAttackStateGroup = AttackSequence1_AltarGroupSequence.AttackSequence[1];
        FinisherEasierAttackStateGroup = AttackSequence1_AltarGroupSequence.AttackSequence[4];

        JiStunState = GameObject.Find($"{jiBossPath}States/PostureBreak/").GetComponent<PostureBreakState>();
        PhaseChangeState = GameObject.Find($"{jiBossPath}States/[BossAngry] BossAngry/").GetComponent<BossPhaseChangeState>();

        DivinationFreeZoneBossGeneralState = getBossGeneralState("[1]Divination Free Zone");
        FlyingProjectilesBossGeneralState = getBossGeneralState("[2][Short]Flying Projectiles");
        BlackHoleAttackBossGeneralState = getBossGeneralState("[3][Finisher]BlackHoleAttack");
        SetLaserAltarsBossGeneralState = getBossGeneralState("[4][Altar]Set Laser Altar Environment");
        SuckSwordBossGeneralState = getBossGeneralState("[5][Short]SuckSword 往內");
        TeleportSwordSmashBossGeneralState = getBossGeneralState("[6][Finisher]Teleport3Sword Smash 下砸");
        SwordBlizzardBossGeneralState = getBossGeneralState("[7][Finisher]SwordBlizzard");
        GroundSwordBossGeneralState = getBossGeneralState("[9][Short]GroundSword");
        SmallBlackHoleBossGeneralState = getBossGeneralState("[10][Altar]SmallBlackHole");
        ShortFlyingSwordBossGeneralState = getBossGeneralState("[11][Short]ShorFlyingSword");
        QuickHorizontalDoubleSwordBossGeneralState = getBossGeneralState("[12][Short]QuickHorizontalDoubleSword");
        QuickTeleportSwordBossGeneralState = getBossGeneralState("[13][Finisher]QuickTeleportSword 危戳");
        LaserAltarCircleBossGeneralState = getBossGeneralState("[14][Altar]Laser Altar Circle");
        DivinationJumpKickedBossGeneralState = getBossGeneralState("[16]Divination JumpKicked");
        HealthAltarBossGeneralState = getBossGeneralState("[15][Altar]Health Altar");
        
        HurtBossGeneralState = GameObject.Find($"{jiBossPath}States/HurtState/").GetComponent<BossGeneralState>();
        BigHurtBossGeneralState = GameObject.Find($"{jiBossPath}States/Hurt_BigState").GetComponent<BossGeneralState>();
        
        Engaging = GameObject.Find($"{jiBossPath}States/1_Engaging").GetComponent<StealthEngaging>();

        attackSequenceModule = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/").GetComponent<AttackSequenceModule>();

    }

    private void AddSmallBlackHoleGroupToSequence(MonsterStateGroupSequence sequence){
        sequence.AttackSequence.Add(SmallBlackHoleMonsterStateGroup);
    }

    private void AddSneakAttackGroupToSequence(MonsterStateGroupSequence sequence){
        sequence.AttackSequence.Add(SneakAttackStateGroup);
    }

    private void InsertBackAttackGroupToSequence(MonsterStateGroupSequence sequence, int index){
        sequence.AttackSequence.Insert(index, BackAttackStateGroup);
    }

    private void OverwriteAttackGroupInSequence(MonsterStateGroupSequence sequence, int index, MonsterStateGroup newGroup){
        sequence.AttackSequence[index] = newGroup;
    }

    private void InsertAttackGroupToSequence(MonsterStateGroupSequence sequence, int index, MonsterStateGroup newGroup){
        sequence.AttackSequence.Insert(index, newGroup);
    }

    private void AddAttackGroupToSequence(MonsterStateGroupSequence sequence, MonsterStateGroup newGroup){
        sequence.AttackSequence.Add(newGroup);
    }

    private Weight<MonsterState> CreateWeight(MonsterState state){
        return new Weight<MonsterState>{
            weight = 1,
            option = state
        };
    }

    public void AlterAttacks(){
        if (AttackSequence1_FirstAttackGroupSequence.AttackSequence.Contains(SneakAttackStateGroup)) {
            return;
        }
        phase2 = false;

        Weight<MonsterState> SmallBlackHoleWeight = CreateWeight(SmallBlackHoleBossGeneralState);
        Weight<MonsterState> ShortFlyingSwordWeight = CreateWeight(ShortFlyingSwordBossGeneralState);
        Weight<MonsterState> QuickHorizontalDoubleSwordWeight = CreateWeight(QuickHorizontalDoubleSwordBossGeneralState);
        Weight<MonsterState> LaserAltarCircleWeight = CreateWeight(LaserAltarCircleBossGeneralState);
        Weight<MonsterState> QuickTeleportSwordWeight = CreateWeight(QuickTeleportSwordBossGeneralState);
        Weight<MonsterState> SuckSwordWeight = CreateWeight(SuckSwordBossGeneralState);
        Weight<MonsterState> GroundSwordWeight = CreateWeight(GroundSwordBossGeneralState);
        Weight<MonsterState> SwordBlizzardWeight = CreateWeight(SwordBlizzardBossGeneralState);
        Weight<MonsterState> FlyingProjectilesWeight = CreateWeight(FlyingProjectilesBossGeneralState);
        Weight<MonsterState> LaserAltarHardWeight = CreateWeight(SetLaserAltarsBossGeneralState);

        MonsterStateWeightSetting SurpriseAttackWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{
            CreateWeight(SmallBlackHoleBossGeneralState),
            CreateWeight(ShortFlyingSwordBossGeneralState),
            CreateWeight(QuickHorizontalDoubleSwordBossGeneralState),
            CreateWeight(LaserAltarCircleBossGeneralState),
            CreateWeight(QuickTeleportSwordBossGeneralState)
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

        AddSneakAttackGroupToSequence(AttackSequence1_FirstAttackGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_GroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_AltarGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_SmallBlackHoleGroupSequence);
        AddSneakAttackGroupToSequence(AttackSequence1_QuickToBlizzardGroupSequence);
        AddSneakAttackGroupToSequence(SpecialHealthSequence);

        OverwriteAttackGroupInSequence(AttackSequence1_GroupSequence, 2, SneakAttackStateGroup);
        OverwriteAttackGroupInSequence(AttackSequence1_FirstAttackGroupSequence, 1, SneakAttackStateGroup);

        MonsterStateWeightSetting BackAttackWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{SuckSwordWeight, GroundSwordWeight},
            queue = new List<MonsterState>{SuckSwordBossGeneralState, GroundSwordBossGeneralState}
        };
        GameObject go2 = new GameObject("MonsterStateGroup_BackAttack(Attack 5/9)");
        BackAttackStateGroup = go2.AddComponent<MonsterStateGroup>();
        BackAttackStateGroup.setting = BackAttackWeightSetting;

        InsertBackAttackGroupToSequence(AttackSequence1_GroupSequence, 4);
        InsertBackAttackGroupToSequence(AttackSequence1_AltarGroupSequence, 4);

        MonsterStateWeightSetting LongerOrBlizardWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{
                SuckSwordWeight, GroundSwordWeight, SwordBlizzardWeight
            },
            queue = new List<MonsterState>{
                SuckSwordBossGeneralState, GroundSwordBossGeneralState, SwordBlizzardBossGeneralState
            }
        };
        GameObject go3 = new GameObject("MonsterStateGroup_LongerOrBlizardAttack(Attack 5/9/7)");;
        LongerOrBlizardAttackStateGroup = go3.AddComponent<MonsterStateGroup>();
        LongerOrBlizardAttackStateGroup.setting = LongerOrBlizardWeightSetting;

        OverwriteAttackGroupInSequence(AttackSequence1_AltarGroupSequence, 2, LongerOrBlizardAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence1_FirstAttackGroupSequence, 3, LongerOrBlizardAttackStateGroup);
        InsertAttackGroupToSequence(SpecialHealthSequence, 2, LongerOrBlizardAttackStateGroup);

        MonsterStateWeightSetting SwordOrLaserWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{ShortFlyingSwordWeight, QuickHorizontalDoubleSwordWeight, LaserAltarCircleWeight},
            queue = new List<MonsterState>{ShortFlyingSwordBossGeneralState, QuickHorizontalDoubleSwordBossGeneralState, LaserAltarCircleBossGeneralState}
        };
        GameObject go4 = new GameObject("MonsterStateGroup_SwordOrLaserAttack(Attack 11/12/14)");
        SwordOrLaserAttackStateGroup = go4.AddComponent<MonsterStateGroup>();
        SwordOrLaserAttackStateGroup.setting = SwordOrLaserWeightSetting;
        OverwriteAttackGroupInSequence(AttackSequence1_SmallBlackHoleGroupSequence, 2, SwordOrLaserAttackStateGroup);
        OverwriteAttackGroupInSequence(AttackSequence1_SmallBlackHoleGroupSequence, 3, LongerAttackStateGroup);

        MonsterStateWeightSetting SwordOrAltarWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{SmallBlackHoleWeight, QuickHorizontalDoubleSwordWeight, LaserAltarCircleWeight},
            queue = new List<MonsterState>{SmallBlackHoleBossGeneralState, QuickHorizontalDoubleSwordBossGeneralState, LaserAltarCircleBossGeneralState}
        };
        GameObject go6 = new GameObject("MonsterStateGroup_SwordOrAltarAttack(Attack10/12/14)");
        SwordOrAltarAttackStateGroup = go6.AddComponent<MonsterStateGroup>();
        SwordOrAltarAttackStateGroup.setting = SwordOrAltarWeightSetting;
        InsertAttackGroupToSequence(AttackSequence1_QuickToBlizzardGroupSequence, 1, SwordOrAltarAttackStateGroup);

        MonsterStateWeightSetting LaserAltarHardWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{LaserAltarHardWeight},
            queue = new List<MonsterState>{SetLaserAltarsBossGeneralState}
        };
        GameObject go7 = new GameObject("MonsterStateGroup_LaserAltarHardAttack(Attack4)");
        LaserAltarHardAttackStateGroup = go7.AddComponent<MonsterStateGroup>();
        LaserAltarHardAttackStateGroup.setting = LaserAltarHardWeightSetting;

        InsertAttackGroupToSequence(AttackSequence2_Opening, 1, LaserAltarHardAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 2, BlizzardAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 3, QuickAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 4, SmallBlackHoleMonsterStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 5, LaserAltarEasyAttackStateGroup);

        MonsterStateWeightSetting DoubleTroubleWeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{
                FlyingProjectilesWeight, GroundSwordWeight, QuickHorizontalDoubleSwordWeight, SuckSwordWeight
            },
            queue = new List<MonsterState>{
                FlyingProjectilesBossGeneralState, GroundSwordBossGeneralState, QuickHorizontalDoubleSwordBossGeneralState, SuckSwordBossGeneralState
            }
        };
        GameObject go5 = new GameObject("MonsterStateGroup_DoubleTroubleAttack(Attack2/9/12/5)");
        DoubleTroubleAttackStateGroup = go5.AddComponent<MonsterStateGroup>();
        DoubleTroubleAttackStateGroup.setting = DoubleTroubleWeightSetting;
        InsertAttackGroupToSequence(AttackSequence2_Opening, 6, DoubleTroubleAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 7, DoubleTroubleAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 8, DoubleTroubleAttackStateGroup);
        InsertAttackGroupToSequence(AttackSequence2_Opening, 9, FinisherEasierAttackStateGroup);

        MonsterStateWeightSetting SneakAttack2WeightSetting = new MonsterStateWeightSetting{
            stateWeightList = new List<Weight<MonsterState>>{
                FlyingProjectilesWeight, LaserAltarHardWeight, SuckSwordWeight, GroundSwordWeight, SmallBlackHoleWeight, ShortFlyingSwordWeight, QuickHorizontalDoubleSwordWeight, LaserAltarCircleWeight
            },
            queue = new List<MonsterState>{
                FlyingProjectilesBossGeneralState, SetLaserAltarsBossGeneralState, SuckSwordBossGeneralState, GroundSwordBossGeneralState, SmallBlackHoleBossGeneralState, ShortFlyingSwordBossGeneralState, QuickHorizontalDoubleSwordBossGeneralState, LaserAltarCircleBossGeneralState
            }
        };
        GameObject go8 = new GameObject("MonsterStateGroup_SneakAttack2(Attack2/4/5/9/10/11/12/14)");
        SneakAttack2StateGroup = go8.AddComponent<MonsterStateGroup>();
        SneakAttack2StateGroup.setting = SneakAttack2WeightSetting;
        AddAttackGroupToSequence(AttackSequence2_Opening, SneakAttack2StateGroup);
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
        phase2 = false;
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

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

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