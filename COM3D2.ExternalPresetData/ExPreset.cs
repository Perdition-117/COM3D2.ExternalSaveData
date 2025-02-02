using System.Collections.Generic;
using System.IO;
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

		Harmony.CreateAndPatchAll(typeof(ExternalPresetPatch));
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
}
