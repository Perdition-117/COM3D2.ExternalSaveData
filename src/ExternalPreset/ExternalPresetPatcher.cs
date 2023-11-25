using CM3D2.ExternalPreset.Managed;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CM3D2.ExternalPreset.Patcher
{
    public static class ExternalPresetPatch
    { 
		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSet), typeof(Maid), typeof(CharacterMgr.Preset))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> PresetSet(IEnumerable<CodeInstruction> instructions) {
			var codeMatcher = new CodeMatcher(instructions);

			codeMatcher
				.MatchForward(false, new CodeMatch(OpCodes.Ret))
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExPreset), nameof(ExPreset.Load)))
				);

			return codeMatcher.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSave))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> PresetSave(IEnumerable<CodeInstruction> instructions) {
			var codes = new List<CodeInstruction>(instructions);
			var instructionIndex = codes.FindIndex(e => e.opcode == OpCodes.Stfld && (e.operand as FieldInfo).Name == nameof(CharacterMgr.Preset.strFileName));

			var codeMatcher = new CodeMatcher(instructions);

			codeMatcher
				.MatchEndForward(new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(File), nameof(File.WriteAllBytes))))
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloc_S, codes[instructionIndex - 1].operand),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExPreset), nameof(ExPreset.Save)))
				);

			return codeMatcher.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetDelete))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> PresetDelete(IEnumerable<CodeInstruction> instructions) {
			var codeMatcher = new CodeMatcher(instructions);

			codeMatcher
				.MatchEndForward(new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(File), nameof(File.Delete))))
				.Insert(
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExPreset), nameof(ExPreset.Delete)))
				);

			return codeMatcher.InstructionEnumeration();
		}
	}
}
