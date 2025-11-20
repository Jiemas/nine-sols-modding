using HarmonyLib;
using I2.Loc;

namespace EnlightenedJi;

[HarmonyPatch]
public class Patches {

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
    private static void EnlightenedJiNameChange(string Term, ref string __result) {
        if (Term != "Characters/NameTag_Jee") return;
        __result =$"Enlightened {__result}";
    }
}