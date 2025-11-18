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

[BepInDependency(NineSolsAPICore.PluginGUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class EnlightenedJi : BaseUnityPlugin {
    // https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/4_configuration.html

    private Harmony harmony = null!;

    private ConfigEntry<float> JiAnimatorSpeed = null!;
    private ConfigEntry<float> JiHPScale = null!;

    private string jiBossPath = "";
    private string jiAttackStatesPath = "";
    private string jiAttackSequences1Path = "";
    private string jiAttackSequences2Path = "";
    private string jiAttackGroupsPath = "";

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

    private static BossGeneralState[] BossGeneralStates = new BossGeneralState[17];

    private static Weight<MonsterState>[] Weights = new Weight<MonsterState>[17];

    private static string[] SequenceStrings1 = { "", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard" };
    private static MonsterStateGroupSequence[] Sequences1 =  new MonsterStateGroupSequence[4];

    private static string[] SequenceStrings2 = { "", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard", 
                                            "_QuickToBlackHole", "_Phase2_OpeningBlackHole" };
    private static MonsterStateGroupSequence[] Sequences2 =  new MonsterStateGroupSequence[7];

    private static int Default = 0, WithAltar = 1, WithSmallBlackHole = 2, QuickToBlizzard = 3, QuickToBLackHole = 4, Opening = 5, Health = 6;

    private static string[] lore_quotes = [
      "IT'S TIME TO END THIS!",
      "HEROES ARE FORGED IN AGONY, YI!",
      "DO YOU KNOW WHAT IT IS LIKE TO LEAVE YOUR LOVED ONES SO FAR BEHIND? I SUPPOSE YOU DO...",
      "I THANK YOU YI, MY CURTAIN CALL WILL BE MUCH GRANDER THAN I COULD HAVE EVER IMAGINED.",
      "SURELY THIS IS NOT ENOUGH TO KILL YOU?",
      "THE END DRAWS NEAR, WILL IT BE MINE OR YOURS?",
      "THAT PIPE OF YOURS, I WONDER HOW FAR YOU COULD HAVE GOTTEN WITHOUT IT?",
      "THE THINGS YOU DO FOR POWER, YI! SURELY APE MAN FLUIDS CANNOT BE THAT GOOD...",
      "ENDURE WHAT OTHERS CANNOT!",
      "ONLY NOW DO I REALIZE, LEAR ONCE SPOKE OF YOU. A MIGHTY FIANGSHI ALBEIT OF SHORT STATURE.",
      "LEAR WAS RIGHT AFTERALL, WAS HE NOT? TIANHUO, A PRODUCT OF SOLARIAN TECHNOLOGY AND ARROGANCE...",
      "I ENVY YOU, YI. TO STILL HAVE LOVED ONES TO MOURN YOUR LOSS...",
      "WHY AM I ATTACKING YOU, YI? HOW ELSE WILL I GET MY CURTAIN CALL?",
      "I THOUGHT MY END WOULD BE GRAND... AND YET YOUR FIREWORK WILL BE THE GRANDEST OF THEM ALL",
      "THIS GROTTO, YOU WILL TAKE CARE OF IT FOR ME, WILL YOU NOT? ...HAHA, I ONLY JEST.",
      "MY LONG AWAITED DEATH IS UPON US. I HOPE YOU ENJOY MY SWAN SONG... OR SURVIVE IT RATHER.",
      "THAT HEART OF YOURS IS JUST AS MUCH A MYSTERY AS MY OWN ABILITIES. FUSANG TRULY CHERISHES YOU...",
      "I DO NOT BLAME EIGONG. DESTINY WOULD HAVE SIMPLY CHOSEN ANOTHER AGENT.",
      "YOU GAVE THE SHEET MUSIC TO AN APE MAN CHILD? DEATH TRULY HAS CHANGED YOU.",
      "YOU ARE REARING AN APE MAN CHILD? SURELY IT DOES NOT KNOW WHAT ELSE YOU HAVE DONE?",
      "TO THINK, THE FUTURE OF THE SOLARIANS IS TO BE MERE PETS TO THE APE MEN. DESTINY TRULY HAS A SENSE OF HUMOUR!",
      "COME AND BEHOLD, MY FINAL MOMENTS!",
      "I AM AWARE, THAT ALL OF THIS AROUND US, THIS IS JUST A MEMORY.",
      "IT IS QUITE EMBARRASSING, IS IT NOT? THIS RIDICULOUS TITLE OF MINE...",
      "I AM QUITE GLAD I DECIDED TO REREAD THE HEXAGRAMS. OTHERWISE, I WOULD HAVE HELD BACK FAR MORE THAN I NEEDED TO!",
      "SURELY, YOU MUST BE AWARE, THIS FIGHT? A SIMPLE PASTTIME FOR BEINGS FAR GREATER THAN WE COULD IMAGINE."
    ];
    
    private static string[] meme_quotes = [
      "TIS BUT A SCRATCH!",
      "NAH, I'D WIN",
      "WHAT'S WRONG YI, NO MAIDENS?",
      "YI, WHAT IS A CAT?",
      "WHICH DO YOU PREFER YI? TALL OR SHORT ME?",
      "EIGONG TRIED TO REPLICATE MY IMMORTALITY. DON'T TELL ME YOU ARE THINKING OF TAKING MY GROWTH ABILITY...",
      "PARRY MY BLACK HOLES? WHAT'S NEXT, A RHIZOMATIC BOMB? RIDICULOUS!",
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
      "REMEMBER, ITS OKAY TO TAKE BREAKS!",
      "SURE, YOU CAN BEAT ME. BUT CAN YOU DO IT HITLESS?"
    ];

    private bool HPUpdated = false;
    public static bool phase2 = false;

    string temp = "";

    int updateCounter = 0;
    private int randomNum = 0;
    int phasesFromBlackHole = 0;
    bool firstMessage = true;

    System.Random random = new System.Random();

    // #region Attacks BossGeneralState
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
    MonsterStateGroupSequence AttackSequence2_QuickToBlizzardGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence2 = null!;
    MonsterStateGroupSequence AttackSequence2_AltarGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence2_SmallBlackHoleGroupSequence = null!;
    MonsterStateGroupSequence AttackSequence2_QuickToBlackHoleGroupSequence = null!;
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
    MonsterStateGroup HardAltarOrEasyFinisherAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup HardAltarOrHardFinisherAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup LaserAltarOrFinisherAttackStateGroup = new MonsterStateGroup();
    MonsterStateGroup SmallBlackHoleMonsterStateGroup = null!;
    MonsterStateGroup LongerAttackStateGroup = null!;
    MonsterStateGroup BlizzardAttackStateGroup = null!;
    MonsterStateGroup QuickAttackStateGroup = null!;
    MonsterStateGroup LaserAltarEasyAttackStateGroup = null!;
    MonsterStateGroup FinisherEasierAttackStateGroup = null!;
    MonsterStateGroup BigBlackHoleAttackStateGroup = null!;
    MonsterStateGroup FinisherHardOrEasierAttackStateGroup = null!;
    #endregion

    StealthEngaging Engaging = null!;
    BossPhaseChangeState PhaseChangeState = null!;

    AttackSequenceModule attackSequenceModule = null!;

    RubyTextMeshProUGUI BossName = null!;
    RubyTextMeshPro PhaseTransitionText = null!;

    private void Awake() {
        Log.Init(Logger);
        RCGLifeCycle.DontDestroyForever(gameObject);

        // Load patches from any class annotated with @HarmonyPatch
        harmony = Harmony.CreateAndPatchAll(typeof(EnlightenedJi).Assembly);

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        JiAnimatorSpeed = Config.Bind("General", "JiSpeed", 1.2f, "The speed at which Ji's attacks occur");
        JiHPScale = Config.Bind("General", "JiHPScale", 6000f, "The amount of Ji's HP in Phase 1 (Phase 2 HP is double this value)");
    }

    public void Update() {
        if (SceneManager.GetActiveScene().name == "A10_S5_Boss_Jee") {
            GetAttackGameObjects();
            AlterAttacks();
            JiHPChange();
            JiSpeedChange();
            HandleStateChange();
        } 
    }

    private void HandleStateChange() 
    {
        var JiMonster = MonsterManager.Instance.ClosetMonster;

        if (JiMonster.currentMonsterState == PhaseChangeState) {
            if (updateCounter < 100)
            {
                updateCounter++;
            }
            else {
                updateCounter = 0;
                BossName.text = "The Kunlun Immortal";
            }
        } 

        if (JiMonster && temp != JiMonster.currentMonsterState.ToString())
        {
            temp = JiMonster.currentMonsterState.ToString();
            randomNum = random.Next();
            if (JiMonster.currentMonsterState == PhaseChangeState)
            {
                phase2 = true;
                OverwriteAttackGroupInSequence(SpecialHealthSequence, 5, SneakAttack2StateGroup);
                HandlePhaseTransitionText();
            }
            phasesFromBlackHole = JiMonster.currentMonsterState == BossGeneralStates[10] ? 0 : (phasesFromBlackHole + 1);
            // ToastManager.Toast(JiMonster.currentMonsterState);
            // ToastManager.Toast(GetCurrentSequence());
            // ToastManager.Toast("");
        }
    }

    private void HandlePhaseTransitionText()
    {
        if (firstMessage)
        {
            firstMessage = false;
            return;
        }
        PhaseTransitionText.text = randomNum % 99 < 5 ? meme_quotes[randomNum % (meme_quotes.Length)] : lore_quotes[randomNum % (lore_quotes.Length)];
    }

    private List<BossGeneralState> GetIndices(int[] indices)
    {
        List<BossGeneralState> pickedStates = new List<BossGeneralState>();
        foreach (int i in indices)
        {
            pickedStates.Add(BossGeneralStates[i]);
        }
        return pickedStates;
    }

    private void JiSpeedChange() {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (!JiMonster) return;

        if (GetIndices([10, 16]).Contains(JiMonster.currentMonsterState) || 
        JiMonster.currentMonsterState == Engaging || JiMonster.currentMonsterState == HurtBossGeneralState ||
        JiMonster.currentMonsterState == BigHurtBossGeneralState || JiMonster.currentMonsterState == JiStunState) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 3;
        } else if (JiMonster.currentMonsterState == BossGeneralStates[15]) 
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
        var JiMonster = MonsterManager.Instance.ClosetMonster;

        // Sneak Attack Speed Up
        if ((JiMonster.LastClipName == "Attack13" || JiMonster.LastClipName == "PostureBreak") && 
            GetIndices([11, 12, 14]).Contains(JiMonster.currentMonsterState))
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2;
        
        // 2nd Finisher & Long Attack Speed Up
        } else if ((JiMonster.currentMonsterState == BossGeneralStates[13] && 
            (JiMonster.LastClipName == "Attack13" || JiMonster.LastClipName == "PostureBreak")) || 
            (randomNum % 2 == 0 && GetIndices([11, 5]).Contains(JiMonster.currentMonsterState)))
        {
            ToastManager.Toast(JiMonster.currentMonsterState);
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.5f;
        } else if (GetCurrentSequence() == AttackSequence1_QuickToBlizzardGroupSequence &&
            JiMonster.currentMonsterState == BossGeneralStates[7])
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.5f;

        // Random Attack Speed Up
        } else if (randomNum % 3 == 0) {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.2f;
        } else {
            return false;
        }
        return true;
    }

    private bool JiSpeedChangePhase2() {
        var JiMonster = MonsterManager.Instance.ClosetMonster;
        if (JiMonster.currentMonsterState == BossGeneralStates[3]) {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2;

        // Sneak Attack Speed Up
        } else if ((JiMonster.LastClipName == "Attack13" || JiMonster.LastClipName == "PostureBreak" || 
            JiMonster.LastClipName == "Attack6") && !(GetIndices([6, 13]).Contains(JiMonster.currentMonsterState)))
        {
            if (JiMonster.currentMonsterState == BossGeneralStates[14]) {
                JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 2f;
            } else {
               JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.65f;
            }

        // Hard Altar Attack Speed Up
        } else if (JiMonster.currentMonsterState == BossGeneralStates[4] && phasesFromBlackHole > 6) 
        {
            if (phasesFromBlackHole > 6)
            {
                updateCounter++;
                if (updateCounter < 500) {
                    JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 1.65f;
                } else {
                    updateCounter = 0;
                }    
            } else {
                JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value - 0.1f;
            }
        
        // Blizzard Attack Speed up
        } else if (JiMonster.currentMonsterState == BossGeneralStates[7] &&
            GetCurrentSequence() != AttackSequence2_QuickToBlizzardGroupSequence) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.65f;

        // Opening Sequence Sword Attack Speed Up
        } else if (GetCurrentSequence() == AttackSequence2_Opening &&
            GetIndices([11, 12, 2, 9]).Contains(JiMonster.currentMonsterState))
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.25f;
        
        // Red/Green Attack Speed Up
        } else if (randomNum % 3 == 0 && GetIndices([6, 13]).Contains(JiMonster.currentMonsterState))
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.35f;
        } else if (randomNum % 2 == 0) {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.3f;
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

    private MonsterStateGroupSequence GetCurrentSequence(){
        Type type = attackSequenceModule.GetType();
        FieldInfo fieldInfo = type.GetField("sequence", BindingFlags.Instance | BindingFlags.NonPublic);
        MonsterStateGroupSequence sequenceValue = (MonsterStateGroupSequence)fieldInfo.GetValue(attackSequenceModule);
        return sequenceValue;
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
        string GeneralBossFightPath = "A10S5/Room/Boss And Environment Binder/General Boss Fight FSM Object 姬 Variant/FSM Animator/";

        jiBossPath = GeneralBossFightPath + "LogicRoot/---Boss---/BossShowHealthArea/StealthGameMonster_Boss_Jee/";

        jiAttackStatesPath = jiBossPath + "States/Attacks/";
        jiAttackSequences1Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase1/";
        jiAttackSequences2Path = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateSequence_Phase2/";
        jiAttackGroupsPath = jiBossPath + "MonsterCore/AttackSequenceModule/MonsterStateGroupDefinition/";

        for (int i = 1; i < Attacks.Length; i++)
        {
            BossGeneralStates[i] = getBossGeneralState(Attacks[i]);
            Weights[i] = CreateWeight(BossGeneralStates[i]);
        }

        AttackSequence1_FirstAttackGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_FirstAttack_WithoutDivination");
        AttackSequence1_GroupSequence = getGroupSequence1("MonsterStateGroupSequence1");
        AttackSequence1_AltarGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_WithAltar");
        AttackSequence1_SmallBlackHoleGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_WithSmallBlackHole");
        AttackSequence1_QuickToBlizzardGroupSequence = getGroupSequence1("MonsterStateGroupSequence1_QuickToBlizzard");
        SpecialHealthSequence = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/SpecialHealthSequence(Jee_Divination_Logic)").GetComponent<MonsterStateGroupSequence>();
        
        AttackSequence2_Opening = getGroupSequence2("MonsterStateGroupSequence1_Phase2_OpeningBlackHole");
        AttackSequence2_QuickToBlizzardGroupSequence = getGroupSequence2("MonsterStateGroupSequence1_QuickToBlizzard");
        AttackSequence2 = getGroupSequence2("MonsterStateGroupSequence1");
        AttackSequence2_AltarGroupSequence = getGroupSequence2("MonsterStateGroupSequence1_WithAltar");
        AttackSequence2_SmallBlackHoleGroupSequence = getGroupSequence2("MonsterStateGroupSequence1_WithSmallBlackHole");
        AttackSequence2_QuickToBlackHoleGroupSequence = getGroupSequence2("MonsterStateGroupSequence1_QuickToBlackHole");
    
        SmallBlackHoleMonsterStateGroup = getGroup("MonsterStateGroup_SmallBlackHole(Attack10)");
        LongerAttackStateGroup = AttackSequence1_GroupSequence.AttackSequence[3];
        BlizzardAttackStateGroup = AttackSequence1_QuickToBlizzardGroupSequence.AttackSequence[1];
        QuickAttackStateGroup = AttackSequence1_FirstAttackGroupSequence.AttackSequence[0];
        LaserAltarEasyAttackStateGroup = AttackSequence1_AltarGroupSequence.AttackSequence[1];
        FinisherEasierAttackStateGroup = AttackSequence1_AltarGroupSequence.AttackSequence[4];
        BigBlackHoleAttackStateGroup = AttackSequence2_Opening.AttackSequence[0];
        FinisherHardOrEasierAttackStateGroup = AttackSequence2.AttackSequence[5];
        JiStunState = GameObject.Find($"{jiBossPath}States/PostureBreak/").GetComponent<PostureBreakState>();
        PhaseChangeState = GameObject.Find($"{jiBossPath}States/[BossAngry] BossAngry/").GetComponent<BossPhaseChangeState>();
        
        HurtBossGeneralState = GameObject.Find($"{jiBossPath}States/HurtState/").GetComponent<BossGeneralState>();
        BigHurtBossGeneralState = GameObject.Find($"{jiBossPath}States/Hurt_BigState").GetComponent<BossGeneralState>();
        
        Engaging = GameObject.Find($"{jiBossPath}States/1_Engaging").GetComponent<StealthEngaging>();

        attackSequenceModule = GameObject.Find($"{jiBossPath}MonsterCore/AttackSequenceModule/").GetComponent<AttackSequenceModule>();

        BossName = GameObject.Find("GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/MonsterHPRoot/BossHPRoot/UIBossHP(Clone)/Offset(DontKeyAnimationOnThisNode)/AnimationOffset/BossName").GetComponent<RubyTextMeshProUGUI>();

        PhaseTransitionText = GameObject.Find(GeneralBossFightPath + "[CutScene]BossAngry_Cutscene/BubbleRoot/SceneDialogueNPC/BubbleRoot/DialogueBubble/Text").GetComponent<RubyTextMeshPro>();

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

    private MonsterStateGroup CreateMonsterStateGroup(int[] AttacksList, string objectName)
    {
        List<Weight<MonsterState>> newStateWeightList = new List<Weight<MonsterState>>();
        List<MonsterState> newQueue = new List<MonsterState>();
        GameObject GO = new GameObject(objectName);
        MonsterStateGroup newAttackGroup = new MonsterStateGroup();

        foreach (int attackIndex in AttacksList)
        {
            newStateWeightList.Add(Weights[attackIndex]);
            newQueue.Add(BossGeneralStates[attackIndex]);
        }

        MonsterStateWeightSetting newWeightSetting = new MonsterStateWeightSetting {
            stateWeightList = newStateWeightList,
            queue = newQueue
        };
        newAttackGroup = GO.AddComponent<MonsterStateGroup>();
        newAttackGroup.setting = newWeightSetting;
        return newAttackGroup;
    }

    private void ModifySequence(MonsterStateGroupSequence sequence, 
        MonsterStateGroup[] groupsToAdd, 
        (MonsterStateGroup group, int index)[] groupsToOverwrite,
        (MonsterStateGroup group, int index)[] groupsToInsert)
    {
        foreach (MonsterStateGroup group in groupsToAdd)
        {
            sequence.AttackSequence.Add(group);
        }
        foreach ((MonsterStateGroup group, int index) in groupsToOverwrite)
        {
            sequence.AttackSequence[index] = group;
        }
        foreach ((MonsterStateGroup group, int index) in groupsToInsert)
        {
            sequence.AttackSequence.Insert(index, group);
        }
    }

    public void AlterAttacks(){
        if (AttackSequence1_GroupSequence.AttackSequence.Contains(SneakAttackStateGroup)) {
            return;
        }
        phase2 = false;
        HurtBossGeneralState.enabled = false;
        // Custom Attack Groups
        SneakAttackStateGroup = CreateMonsterStateGroup([10, 11, 12, 13, 14], "MonsterStateGroup_SneakAttack(Attack 10/11/12/13/14)");
        BackAttackStateGroup = CreateMonsterStateGroup([5, 9], "MonsterStateGroup_BackAttack(Attack 5/9)");
        LongerOrBlizardAttackStateGroup = CreateMonsterStateGroup([5, 9, 7], "MonsterStateGroup_LongerOrBlizardAttack(Attack 5/9/7)");
        SwordOrLaserAttackStateGroup = CreateMonsterStateGroup([11, 12, 14], "MonsterStateGroup_SwordOrLaserAttack(Attack 11/12/14)");
        SwordOrAltarAttackStateGroup = CreateMonsterStateGroup([10, 12, 14], "MonsterStateGroup_SwordOrAltarAttack(Attack10/12/14)");
        LaserAltarHardAttackStateGroup = CreateMonsterStateGroup([4], "MonsterStateGroup_LaserAltarHardAttack(Attack4)");
        DoubleTroubleAttackStateGroup = CreateMonsterStateGroup([2, 5, 9, 12], "MonsterStateGroup_DoubleTroubleAttack(Attack2/9/12/5)");
        SneakAttack2StateGroup = CreateMonsterStateGroup([2, 5, 9, 11, 12, 15], "MonsterStateGroup_SneakAttack2(Attack2/5/9/11/12/15)");
        HardAltarOrEasyFinisherAttackStateGroup = CreateMonsterStateGroup([4, 13], "MonsterStateGroup_HardAltarOrEasyFinisher(Attack4/13)");
        HardAltarOrHardFinisherAttackStateGroup = CreateMonsterStateGroup([4, 6], "MonsterStateGroup_HardAltarOrHardFinisher(Attack4/6)");
        LaserAltarOrFinisherAttackStateGroup = CreateMonsterStateGroup([4, 6, 13, 14], "MonsterStateGroup_LaserAltarOrFinisher(Attack 4/6/13/14)");

        // Phase 1 Sequence Attack Modifications
        ModifySequence(AttackSequence1_GroupSequence, [SneakAttackStateGroup], 
            new (MonsterStateGroup group, int index)[] {(SneakAttackStateGroup, 2), (FinisherHardOrEasierAttackStateGroup, 4)}, 
            new (MonsterStateGroup group, int index)[] {(BackAttackStateGroup, 4)}
        );

        ModifySequence(AttackSequence1_AltarGroupSequence, [SneakAttackStateGroup],
            new (MonsterStateGroup group, int index)[] {(BlizzardAttackStateGroup, 2), (LaserAltarEasyAttackStateGroup, 3), 
                (FinisherHardOrEasierAttackStateGroup, 5)},
            new (MonsterStateGroup group, int index)[] {(BackAttackStateGroup, 4)}
        );

        ModifySequence(AttackSequence1_SmallBlackHoleGroupSequence, [SneakAttackStateGroup],
            new (MonsterStateGroup group, int index)[] {(LaserAltarEasyAttackStateGroup, 2), 
                (LongerAttackStateGroup, 3), (FinisherHardOrEasierAttackStateGroup, 5)}, 
            []
        );

        ModifySequence(AttackSequence1_QuickToBlizzardGroupSequence, [SneakAttackStateGroup],
            new (MonsterStateGroup group, int index)[] {(BlizzardAttackStateGroup, 3), (FinisherHardOrEasierAttackStateGroup, 4)},
            new (MonsterStateGroup group, int index)[] {(LaserAltarEasyAttackStateGroup, 1)}
        );

        ModifySequence(AttackSequence1_FirstAttackGroupSequence, [SneakAttackStateGroup],
            new (MonsterStateGroup group, int index)[] {(SneakAttackStateGroup, 1), (FinisherHardOrEasierAttackStateGroup, 3)},
            new (MonsterStateGroup group, int index)[] {(LongerOrBlizardAttackStateGroup, 3)}
        );

        ModifySequence(SpecialHealthSequence, [SneakAttackStateGroup],
            [], new (MonsterStateGroup group, int index)[] {(LongerOrBlizardAttackStateGroup, 2)}
        );

        // Phase 2 Sequence Attack Modifications
        ModifySequence(AttackSequence2_Opening, [SneakAttack2StateGroup], [], new (MonsterStateGroup group, int index)[] {
            (LaserAltarHardAttackStateGroup, 1), (BlizzardAttackStateGroup, 2), (QuickAttackStateGroup, 3),
            (SmallBlackHoleMonsterStateGroup, 4), (LaserAltarEasyAttackStateGroup, 5), (DoubleTroubleAttackStateGroup, 6),
            (DoubleTroubleAttackStateGroup, 7), (DoubleTroubleAttackStateGroup, 8), (HardAltarOrEasyFinisherAttackStateGroup, 9)
        });

        ModifySequence(AttackSequence2, [SneakAttack2StateGroup], 
            new (MonsterStateGroup group, int index)[] {(LaserAltarOrFinisherAttackStateGroup, 3)},
            new (MonsterStateGroup group, int index)[] {(DoubleTroubleAttackStateGroup, 2), (DoubleTroubleAttackStateGroup, 4)}
        );

        ModifySequence(AttackSequence2_AltarGroupSequence, [SneakAttack2StateGroup],
            new (MonsterStateGroup group, int index)[] {(LaserAltarHardAttackStateGroup, 1)},
            new (MonsterStateGroup group, int index)[] {(BlizzardAttackStateGroup, 2)}
        );

        ModifySequence(AttackSequence2_SmallBlackHoleGroupSequence, [SneakAttack2StateGroup],
            new (MonsterStateGroup group, int index)[] {(BackAttackStateGroup, 2), (LaserAltarHardAttackStateGroup, 3)},
            []
        );

        ModifySequence(AttackSequence2_QuickToBlizzardGroupSequence, [SneakAttack2StateGroup],
            new (MonsterStateGroup group, int index)[] {(LaserAltarOrFinisherAttackStateGroup, 4)},
            new (MonsterStateGroup group, int index)[] {(BlizzardAttackStateGroup, 3)}
        );;

        ModifySequence(AttackSequence2_QuickToBlackHoleGroupSequence, [SneakAttack2StateGroup], [],
            new (MonsterStateGroup group, int index)[] {(LaserAltarHardAttackStateGroup, 2), 
                (BigBlackHoleAttackStateGroup, 3), (LongerAttackStateGroup, 4)}
        );
    }

    private void OnDestroy()
    {
        // Make sure to clean up resources here to support hot reloading
        HPUpdated = false;
        phase2 = false;
        updateCounter = 0;
        firstMessage = true;
        harmony.UnpatchSelf();
    }
}
