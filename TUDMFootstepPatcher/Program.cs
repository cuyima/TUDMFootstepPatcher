using System;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;

namespace TUDMFootstepPatcher
{
    public class Program
    {

        static ModKey ultimateDodgeMod = ModKey.FromNameAndExtension("Ultimate Dodge Mod.esp");
        static FormKey[] playableRaces = new FormKey[]{
                Skyrim.Race.DefaultRace,
                Skyrim.Race.NordRace,
                Skyrim.Race.BretonRace,
                Skyrim.Race.RedguardRace,
                Skyrim.Race.ImperialRace,
                Skyrim.Race.DarkElfRace,
                Skyrim.Race.HighElfRace,
                Skyrim.Race.WoodElfRace,
                Skyrim.Race.OrcRace,
                Skyrim.Race.KhajiitRace,
                Skyrim.Race.ArgonianRace,
            };
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
            args,
            RunPatch,
            new UserPreferences
            {
                ActionsForEmptyArgs = new RunDefaultPatcher
                {
                    IdentifyingModKey = "TUDMFootstepPatch.esp",
                    TargetRelease = GameRelease.SkyrimSE
                }
            });

        }

        public static bool HasBodyFlag(ArmorAddon armorAddon)
        {
            if (armorAddon == null || armorAddon.BodyTemplate == null) return false;
            var v = armorAddon.BodyTemplate.FirstPersonFlags.AsEnumerable();
            foreach (var flag in v)
            {
                if (flag.ToString().Contains("Body")) return true;
            }
            return false;
        }
        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!state.LoadOrder.ContainsKey(ultimateDodgeMod))
                throw new Exception("ERROR: The Ultimate Dodge Mod hasn't been detected in your load order. You need to install TUDM prior to running this patcher!");

            FormKey UDRollNakedLandingSetKey = ultimateDodgeMod.MakeFormKey(0x011591);
            FormKey UDRollLightLandingSetKey = ultimateDodgeMod.MakeFormKey(0x011590);
            FormKey UDRollHeavyLandingSetKey = ultimateDodgeMod.MakeFormKey(0x01158F);
            if (!state.LinkCache.TryResolve<IFootstepSetGetter>(UDRollNakedLandingSetKey, out var UDRollNakedLandingSet) || UDRollNakedLandingSet == null)
                throw new Exception("ERROR: UDRollNakedLandingSet FormKey not found! Are you on the latest TUDM x64 version?");
            if (!state.LinkCache.TryResolve<IFootstepSetGetter>(UDRollLightLandingSetKey, out var UDRollLightLandingSet) || UDRollLightLandingSet == null)
                throw new Exception("ERROR: UDRollLightLandingSet FormKey not found! Are you on the latest TUDM x64 version?");
            if (!state.LinkCache.TryResolve<IFootstepSetGetter>(UDRollHeavyLandingSetKey, out var UDRollHeavyLandingSet) || UDRollHeavyLandingSet == null)
                throw new Exception("ERROR: UDRollHeavyLandingSet FormKey not found! Are you on the latest TUDM x64 version?");

            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
            {
                Console.WriteLine("CHECKING ARMOR " + armor.EditorID);
                if (armor.Keywords == null) continue;
                if (!armor.Keywords.Contains(Skyrim.Keyword.ArmorCuirass) && !armor.Keywords.Contains(Skyrim.Keyword.ClothingBody))
                    continue;
                foreach (var armature in armor.Armature)
                {
                    armature.TryResolve(state.LinkCache, out var armorAddonVar);
                    if (armorAddonVar == null) continue;
                    var race = armorAddonVar.Race;
                    race.TryResolve(state.LinkCache, out var raceVar);
                    if (raceVar == null || !playableRaces.Contains<FormKey>(raceVar.FormKey)) continue;

                    ArmorAddon armorAddon = armorAddonVar.DeepCopy();
                    Console.WriteLine("CHECKING ARMATURE " + armorAddon.EditorID);
                    if (!HasBodyFlag(armorAddon)) continue;
                    Console.WriteLine("ARMATURE HAS BODYFLAG");
                    switch (armor.DeepCopy().BodyTemplate!.ArmorType)
                    {
                        case ArmorType.Clothing:
                            armorAddon.FootstepSound = UDRollNakedLandingSet.DeepCopy();
                            break;
                        case ArmorType.LightArmor:
                            armorAddon.FootstepSound = UDRollLightLandingSet.DeepCopy();
                            break;
                        case ArmorType.HeavyArmor:
                            armorAddon.FootstepSound = UDRollHeavyLandingSet.DeepCopy();
                            break;
                    }
                    state.PatchMod.ArmorAddons.Set(armorAddon);
                }

            }
        }
    }
}
