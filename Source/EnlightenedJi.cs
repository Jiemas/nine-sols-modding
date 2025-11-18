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
    private static string[] SequenceStrings1 = { "", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard" };
    private static string[] SequenceStrings2 = { "", "_WithAltar", "_WithSmallBlackHole", "_QuickToBlizzard", 
                                            "_QuickToBlackHole", "_Phase2_OpeningBlackHole" };
    private static MonsterStateGroupSequence[] Sequences1 =  new MonsterStateGroupSequence[4];
    private static MonsterStateGroupSequence[] Sequences2 =  new MonsterStateGroupSequence[7];
    private static int Default = 0, WithAltar = 1, WithSmallBlackHole = 2, QuickToBlizzard = 3, 
                        QuickToBlackHole = 4, Opening = 5, Health = 6;
    
    // Group Variables
    private static (int sequence, int index)[] ExistingGroupPairs = new (int sequence, int index)[] 
    {
        (WithSmallBlackHole, 1), (Default, 3), (QuickToBlizzard, 1), (Default, 1), (WithAltar, 1), 
        (WithAltar, 4)
    };
    private static (int[] attacksList, string objectName)[] customGroupPatterns = new (int[] attacksList, string objectName)[]
    {
        ([10, 11, 12, 13, 14], "SneakAttack(Attack 10/11/12/13/14)"),
        ([5, 9], "BackAttack(Attack 5/9)"),
        ([5, 9, 7], "LongerOrBlizardAttack(Attack 5/9/7)"),
        ([11, 12, 14], "SwordOrLaserAttack(Attack 11/12/14)"),
        ([10, 12, 14], "SwordOrAltarAttack(Attack10/12/14)"),
        ([4], "LaserAltarHardAttack(Attack4)"),
        ([2, 5, 9, 12], "DoubleTroubleAttack(Attack2/9/12/5)"),
        ([2, 5, 9, 11, 12, 15], "SneakAttack2(Attack2/5/9/11/12/15)"),
        ([4, 13], "HardAltarOrEasyFinisher(Attack4/13)"),
        ([4, 6, 13, 14], "LaserAltarOrFinisher(Attack 4/6/13/14)")
    };
    private static MonsterStateGroup[] Groups = new MonsterStateGroup[ExistingGroupPairs.Length + customGroupPatterns.Length + 2];
    private static int SmallBlackHole = 0, LongerAttack = 1, Blizzard = 2, QuickAttack = 3, EasyLaserAltar = 4, 
        EasyFinisher = 5, BigBlackHole = 6, EasyOrHardFinisher = 7, SneakAttack = 8, BackAttack = 9,
        LongerOrBlizzard = 10, SwordOrLaser = 11, SwordOrAltar = 12, HardLaserAltar = 13, DoubleTrouble = 14,
        SneakAttack2 = 15, HardAltarOrEasyFinisher = 16, LaserAltarOrFinisher = 17;

    // Miscellaneous Variables
    private static bool HPUpdated = false;
    private static bool phase2 = false;

    private static string temp = "";

    private static int updateCounter = 0;
    private static int randomNum = 0;
    private static int phasesFromBlackHole = 0;
    private static bool firstMessage = true;

    private static System.Random random = new System.Random();

    private static PostureBreakState JiStunState = null!;

    private static StealthEngaging Engaging = null!;
    private static BossPhaseChangeState PhaseChangeState = null!;

    private static AttackSequenceModule attackSequenceModule = null!;

    private static RubyTextMeshProUGUI BossName = null!;
    private static RubyTextMeshPro PhaseTransitionText = null!;

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
      "REMEMBER, ITS OKAY TO TAKE BREAKS!",
      "YOU MAY BE ABLE TO BEAT ME, BUT CAN YOU DO IT HITLESS?"
    ];


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
                OverwriteAttackGroupInSequence(Sequences2[Health], 5, Groups[SneakAttack2]);
                HandlePhaseTransitionText();
            }
            phasesFromBlackHole = JiMonster.currentMonsterState == BossGeneralStates[10] ? 0 : (phasesFromBlackHole + 1);
            ToastManager.Toast(GetCurrentSequence());
            ToastManager.Toast(JiMonster.currentMonsterState);
            ToastManager.Toast("");
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
        JiMonster.currentMonsterState == Engaging || JiMonster.currentMonsterState == BossGeneralStates[Hurt] ||
        JiMonster.currentMonsterState == BossGeneralStates[BigHurt] || JiMonster.currentMonsterState == JiStunState) 
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
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.5f;
        } else if (GetCurrentSequence() == Sequences1[QuickToBlizzard] &&
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
            GetCurrentSequence() != Sequences2[QuickToBlizzard]) 
        {
            JiMonster.monsterCore.AnimationSpeed = JiAnimatorSpeed.Value + 0.65f;

        // Opening Sequence Sword Attack Speed Up
        } else if (GetCurrentSequence() == Sequences2[Opening] &&
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
        Engaging = GameObject.Find($"{jiBossPath}States/1_Engaging").GetComponent<StealthEngaging>();

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
        BossName = GameObject.Find(
            "GameCore(Clone)/RCG LifeCycle/UIManager/GameplayUICamera/MonsterHPRoot/BossHPRoot/UIBossHP(Clone)/Offset(DontKeyAnimationOnThisNode)/AnimationOffset/BossName")
            .GetComponent<RubyTextMeshProUGUI>();
        PhaseTransitionText = GameObject.Find(GeneralBossFightPath + 
            "[CutScene]BossAngry_Cutscene/BubbleRoot/SceneDialogueNPC/BubbleRoot/DialogueBubble/Text")
            .GetComponent<RubyTextMeshPro>();
    }

    private void OverwriteAttackGroupInSequence(MonsterStateGroupSequence sequence, int index, MonsterStateGroup newGroup)
    {
        sequence.AttackSequence[index] = newGroup;
    }

    private void InsertAttackGroupToSequence(MonsterStateGroupSequence sequence, int index, MonsterStateGroup newGroup)
    {
        sequence.AttackSequence.Insert(index, newGroup);
    }

    private void AddAttackGroupToSequence(MonsterStateGroupSequence sequence, MonsterStateGroup newGroup)
    {
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
        GameObject GO = new GameObject($"{objectName}");
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

    public void AlterAttacks(){
        if (Sequences1[Default].AttackSequence.Contains(Groups[SneakAttack])) {
            return;
        }
        phase2 = false;
        // HurtBossGeneralState.enabled = false; TODO CHECK IF THERE IS PROPERTY THAT ACCOUNTS FOR HURT STATES IN NEW STATE GROUPS

        // Custom Attack Groups
        int i = ExistingGroupPairs.Length + 2;
        foreach ((int[] attacksList, string objectName) in customGroupPatterns)
        {
            Groups[i++] = CreateMonsterStateGroup(attacksList, objectName);
        }

        // Phase 1 Sequence Attack Modifications
        ModifySequence(Default, 1, [SneakAttack], 
            new (int group, int index)[] {(SneakAttack, 2), (EasyOrHardFinisher, 4)}, 
            new (int group, int index)[] {(BackAttack, 4)}
        );

        ModifySequence(WithAltar, 1, [SneakAttack],
            new (int group, int index)[] {(Blizzard, 2), (EasyLaserAltar, 3), (EasyOrHardFinisher, 4)},
            new (int group, int index)[] {(BackAttack, 4)}
        );

        ModifySequence(WithSmallBlackHole, 1, [SneakAttack],
            new (int group, int index)[] {(EasyLaserAltar, 2), (LongerAttack, 3), (EasyOrHardFinisher, 5)}, []
        );

        ModifySequence(QuickToBlizzard, 1, [SneakAttack],
            new (int group, int index)[] {(Blizzard, 3), (EasyOrHardFinisher, 4)},
            new (int group, int index)[] {(EasyLaserAltar, 1)}
        );

        ModifySequence(Health, 2, [SneakAttack], [], new (int group, int index)[] {(LongerOrBlizzard, 2)});

        // Phase 2 Sequence Attack Modifications
        ModifySequence(Opening, 2, [SneakAttack2], [], new (int group, int index)[] {
            (HardLaserAltar, 1), (Blizzard, 2), (QuickAttack, 3), (SmallBlackHole, 4), (EasyLaserAltar, 5), 
            (DoubleTrouble, 6), (DoubleTrouble, 7), (DoubleTrouble, 8), (HardAltarOrEasyFinisher, 9)
        });

        ModifySequence(Default, 2, [SneakAttack2], new (int group, int index)[] {(LaserAltarOrFinisher, 3)},
            new (int group, int index)[] {(DoubleTrouble, 2), (DoubleTrouble, 4)}
        );

        ModifySequence(WithAltar, 2, [SneakAttack2], new (int group, int index)[] {(HardLaserAltar, 1)},
            new (int group, int index)[] {(Blizzard, 2)}
        );

        ModifySequence(WithSmallBlackHole, 2, [SneakAttack2], 
            new (int group, int index)[] {(BackAttack, 2), (HardLaserAltar, 3)}, []
        );

        ModifySequence(QuickToBlizzard, 2, [SneakAttack2], new (int group, int index)[] {(LaserAltarOrFinisher, 4)},
            new (int group, int index)[] {(Blizzard, 3)}
        );

        ModifySequence(QuickToBlackHole, 2, [SneakAttack2], [], 
            new (int group, int index)[] {(HardLaserAltar, 2), (BigBlackHole, 3), (LongerAttack, 4)}
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
