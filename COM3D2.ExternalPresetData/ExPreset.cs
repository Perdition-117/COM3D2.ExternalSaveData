using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using BepInEx;
using BepInEx.Logging;
using CM3D2.ExternalSaveData.Managed;
using COM3D2.ExternalPresetData;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CM3D2.ExternalPreset.Managed;

[BepInPlugin("COM3D2.ExternalPresetData", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("COM3D2.ExternalSaveData")]
public class ExPreset : BaseUnityPlugin {
	private static readonly HashSet<string> ExternalSaveDataNodes = new();

	// backwards compatibility with AutoConverter
	private static readonly HashSet<string> exsaveNodeNameMap = ExternalSaveDataNodes;

	private static XmlDocument _xmlMemory = null;

	// プリセット適用時に通知が必要な場合はここに登録
	public static UnityEvent loadNotify = new();

	private static ManualLogSource _logger;

	private void Awake() {
		_logger = Logger;

		var version = new Version(GameUty.GetBuildVersionText());
		Harmony.CreateAndPatchAll(typeof(ExPreset));
		Harmony.CreateAndPatchAll(version.Major >= 3 ? typeof(ExPreset30) : typeof(ExPreset20));
	}

	public static void Load(Maid maid, CharacterMgr.Preset preset) {
		PluginsLoad(maid, preset);
	}

	private static void PluginsLoad(Maid maid, CharacterMgr.Preset preset) {
		XmlDocument xml = null;

		if (preset.strFileName == "") {
			xml = _xmlMemory;
			_xmlMemory = null;
		} else {
			var exPresetPath = GetExPresetPath(preset.strFileName);

			if (File.Exists(exPresetPath)) {
				xml = new XmlDocument();
				xml.Load(exPresetPath);
			}
		}

		if (xml == null) {
			return;
		}

		foreach (var pluginName in ExternalSaveDataNodes) {
			var node = xml.SelectSingleNode($"//plugin[@name='{pluginName}']");
			if (node == null) continue;

			ExSaveData.LoadPluginData(maid, pluginName, node);
		}

		// エディットシーン以外でもプリセットが使われるようになったが、
		// 通知先がエディットシーン以外で通知されるとエラーが出る場合があるためひとまずエディットシーンの実通知するようにする
		// シーン拡張は要相談
		if (SceneManager.GetActiveScene().name == "SceneEdit") {
			_logger.LogDebug("Notify");
			loadNotify.Invoke();
		}
	}

	private static bool TryGetExternalSaveData(Maid maid, CharacterMgr.PresetType presetType, out XmlDocument xmlDocument) {
		xmlDocument = null;

		if (presetType == CharacterMgr.PresetType.Wear) {
			return false;
		}

		xmlDocument = new XmlDocument();
		var rootNode = xmlDocument.AppendChild(xmlDocument.CreateElement("plugins"));
		var hasNodes = false;

		foreach (var pluginName in ExternalSaveDataNodes) {
			var node = xmlDocument.CreateElement("plugin");
			if (ExSaveData.TrySavePluginData(maid, pluginName, node)) {
				rootNode.AppendChild(node);
				hasNodes = true;
			}
		}

		return hasNodes;
	}

	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSaveNotWriteFile))]
	[HarmonyPostfix]
	public static void PostCharacterMgrPresetSaveNotWriteFile(CharacterMgr __instance, Maid f_maid, CharacterMgr.PresetType f_type) {
		if (TryGetExternalSaveData(f_maid, f_type, out var xml)) {
			_xmlMemory = xml;
		}
	}

	public static void Save(Maid maid, string presetFileName, CharacterMgr.PresetType presetType) {
		// file name may be null when the save method is intercepted and cancelled
		if (presetFileName != null) {
			//MaidVoicePitchSave(maid, presetFileName, presetType);
			PluginsSave(maid, presetFileName, presetType);
		}
	}

	private static void PluginsSave(Maid maid, string presetFileName, CharacterMgr.PresetType presetType) {
		if (TryGetExternalSaveData(maid, presetType, out var xml)) {
			xml.Save(GetExPresetPath(presetFileName));
		}
	}

	public static void Delete(CharacterMgr.Preset preset) {
		var exPresetPath = GetExPresetPath(preset.strFileName);
		if (File.Exists(exPresetPath)) {
			File.Delete(exPresetPath);
		}
	}

	private static string GetExPresetPath(string presetFileName) {
		return Path.Combine(GameMain.Instance.CharacterMgr.PresetDirectory, $"{presetFileName}.expreset.xml");
	}

	// EXSaveDataに保存する情報のうち、EXプリセットにもセーブするノード名を設定
	// 例はMaidVoicePitchなどを参照
	public static void AddExSaveNode(string pluginName) {
		ExternalSaveDataNodes.Add(pluginName);
	}

	class ExPreset30 {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSet), typeof(Maid), typeof(CharacterMgr.Preset), typeof(bool))]
		private static void OnPresetSet(Maid f_maid, CharacterMgr.Preset f_prest) => Load(f_maid, f_prest);
	}

	class ExPreset20 {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSet), typeof(Maid), typeof(CharacterMgr.Preset))]
		private static void OnPresetSet(Maid f_maid, CharacterMgr.Preset f_prest) => Load(f_maid, f_prest);
	}

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSave))]
	private static IEnumerable<CodeInstruction> PresetSave(IEnumerable<CodeInstruction> instructions) {
		var codes = new List<CodeInstruction>(instructions);
		var instructionIndex = codes.FindIndex(e => e.opcode == OpCodes.Stfld && (e.operand as FieldInfo) == AccessTools.Field(typeof(CharacterMgr.Preset), nameof(CharacterMgr.Preset.strFileName)));

		var codeMatcher = new CodeMatcher(instructions);

		codeMatcher
			.MatchEndForward(new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(File), nameof(File.WriteAllBytes))))
			.Insert(
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldloc_S, codes[instructionIndex - 1].operand),
				new CodeInstruction(OpCodes.Ldarg_2),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExPreset), nameof(Save)))
			);

		return codeMatcher.InstructionEnumeration();
	}

	[HarmonyTranspiler]
	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetDelete))]
	private static IEnumerable<CodeInstruction> PresetDelete(IEnumerable<CodeInstruction> instructions) {
		var codeMatcher = new CodeMatcher(instructions);

		codeMatcher
			.MatchEndForward(new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(File), nameof(File.Delete))))
			.Insert(
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExPreset), nameof(Delete)))
			);

		return codeMatcher.InstructionEnumeration();
	}
}
