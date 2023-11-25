using System.IO;
using System.Xml;
using CM3D2.ExternalSaveData.Managed;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CM3D2.ExternalPreset.Managed;

public static class ExPreset {
	private static readonly HashSet<string> ExternalSaveDataNodes = new();

	private static XmlDocument _xmlMemory = null;

	// プリセット適用時に通知が必要な場合はここに登録
	public static UnityEvent loadNotify = new();

	public static void Load(Maid maid, CharacterMgr.Preset preset) {
		PluginsLoad(maid, preset);
	}

	private static void PluginsLoad(Maid maid, CharacterMgr.Preset preset) {
		XmlDocument xml;

		string fileName;
		if (preset.strFileName == "") {
			xml = _xmlMemory;
			_xmlMemory = null;
		} else {
			fileName = preset.strFileName + ".expreset.xml";
			xml = LoadExFile(preset.strFileName + ".expreset.xml");
		}

		if (xml == null) {
			return;
		}

		foreach (var pluginName in ExternalSaveDataNodes) {
			var node = xml.SelectSingleNode($"//plugin[@name='{pluginName}']");
			if (node == null) continue;

			ExSaveData.SetXml(maid, pluginName, node);
		}

		// エディットシーン以外でもプリセットが使われるようになったが、
		// 通知先がエディットシーン以外で通知されるとエラーが出る場合があるためひとまずエディットシーンの実通知するようにする
		// シーン拡張は要相談
		if (SceneManager.GetActiveScene().name == "SceneEdit") {
			MaidVoicePitch.Plugin.MaidVoicePitch.LogDebug("Notify");
			loadNotify.Invoke();
		}
	}

	private static XmlDocument LoadExFile(string fileName) {
		// Presetフォルダチェック
		var path = FindEXPresetFilePath(fileName);
		// expresetファイルがなければ終了
		if (path == null) {
			return null;
		}

		var xml = new XmlDocument();
		xml.Load(path);

		return xml;
	}

	[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSaveNotWriteFile))]
	[HarmonyPostfix]
	public static void PostCharacterMgrPresetSaveNotWriteFile(CharacterMgr __instance, Maid f_maid, CharacterMgr.PresetType f_type) {
		if (f_type == CharacterMgr.PresetType.Wear) {
			return;
		}

		var xml = new XmlDocument();
		var nodeExist = false;
		var rootNode = xml.AppendChild(xml.CreateElement("plugins"));
		foreach (var pluginName in ExternalSaveDataNodes) {
			var node = xml.CreateElement("plugin");
			if (ExSaveData.TryGetXml(f_maid, pluginName, node)) {
				rootNode.AppendChild(node);
				nodeExist = true;
			}
		}

		if (nodeExist) {
			_xmlMemory = xml;
		}
	}

	public static void Save(Maid maid, string presetFileName, CharacterMgr.PresetType presetType) {
		//MaidVoicePitchSave(maid, presetFileName, presetType);
		PluginsSave(maid, presetFileName, presetType);
	}

	private static void PluginsSave(Maid maid, string presetFileName, CharacterMgr.PresetType presetType) {
		if (presetType == CharacterMgr.PresetType.Wear) {
			return;
		}

		var xml = new XmlDocument();
		var hasNodes = false;
		var rootNode = xml.AppendChild(xml.CreateElement("plugins"));
		foreach (var pluginName in ExternalSaveDataNodes) {
			var node = xml.CreateElement("plugin");
			if (ExSaveData.TryGetXml(maid, pluginName, node)) {
				rootNode.AppendChild(node);
				hasNodes = true;
			}
		}

		if (!hasNodes) {
			return;
		}
		xml.Save($"{Path.GetFullPath(".\\")}Preset\\{presetFileName}.expreset.xml");
	}

	public static void Delete(CharacterMgr.Preset preset) {
		var path = $"{Path.GetFullPath(".\\")}Preset\\{preset.strFileName}.expreset.xml";
		if (File.Exists(path)) {
			File.Delete(path);
		}
	}

	private static string FindEXPresetFilePath(string presetFileName) {
		// Presetフォルダチェック
		var path = $"{Path.GetFullPath(".\\")}Preset\\{presetFileName}";
		if (File.Exists(path)) {
			return path;
		}
		return null;
	}

	// EXSaveDataに保存する情報のうち、EXプリセットにもセーブするノード名を設定
	// 例はMaidVoicePitchなどを参照
	public static void AddExSaveNode(string pluginName) {
		ExternalSaveDataNodes.Add(pluginName);
	}
}
