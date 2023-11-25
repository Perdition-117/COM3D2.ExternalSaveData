using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HarmonyLib;

namespace CM3D2.ExternalSaveData.Managed;

public static class ExSaveData {
	static SaveDataPluginSettings saveDataPluginSettings = new();

	// 拡張セーブデータの実体
	static SaveDataPluginSettings PluginSettings => saveDataPluginSettings;

	// 通常のプラグイン名よりも前に処理するため、先頭に '.' をつけている。
	// 通常のプラグインは "CM3D2～" のように英数字から始まる名前をつけること
	const string CallbackName = ".CM3D2 ExternalSaveData";

	public static bool TryGetXml(Maid maid, string pluginName, XmlNode xmlNode) {
		if (PluginSettings.saveData.maids.TryGetValue(maid.status.guid, out var m)) {
			if (m.plugins.TryGetValue(pluginName, out var plugin)) {
				plugin.Save(xmlNode);
				return true;
			}
		}
		return false;
	}

	public static void SetXml(Maid maid, string pluginName, XmlNode xmlNode) {
		if (!Contains(maid)) {
			SetMaid(maid);
		}

		if (PluginSettings.saveData.maids.TryGetValue(maid.status.guid, out var m)) {
			if (m.plugins.TryGetValue(pluginName, out var plugin)) {
				plugin.Load(xmlNode);
			} else {
				m.plugins[pluginName] = new SaveDataPluginSettings.Plugin().Load(xmlNode);
			}
		}
	}

	/// <summary>
	/// 拡張セーブデータ内の設定を得る(文字列)
	/// <para>指定した設定が存在しない場合はdefaultValueを返す</para>
	/// <seealso cref="GetBool"/>
	/// <seealso cref="GetInt"/>
	/// <seealso cref="GetFloat"/>
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="defaultValue">プロパティが存在しない場合に返すデフォルト値</param>
	/// <returns>設定文字列</returns>
	public static string Get(Maid maid, string pluginName, string propName, string defaultValue) {
		if (maid == null || pluginName == null || propName == null) {
			return defaultValue;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		return PluginSettings.Get(maid.status.guid, pluginName, propName, defaultValue);
	}

	public static bool GetBool(Maid maid, string pluginName, string propName, bool defaultValue) {
		return Helper.StringToBool(Get(maid, pluginName, propName, null), defaultValue);
	}

	public static int GetInt(Maid maid, string pluginName, string propName, int defaultValue) {
		return Helper.StringToInt(Get(maid, pluginName, propName, null), defaultValue);
	}

	public static float GetFloat(Maid maid, string pluginName, string propName, float defaultValue) {
		return Helper.StringToFloat(Get(maid, pluginName, propName, null), defaultValue);
	}

	/// <summary>
	/// 拡張セーブデータへ設定を書き込む
	/// <seealso cref="SetBool"/>
	/// <seealso cref="SetInt"/>
	/// <seealso cref="SetFloat"/>
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <param name="overwrite">trueなら常に上書き。falseなら設定が存在する場合は書き込みを行わない</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool Set(Maid maid, string pluginName, string propName, string value, bool overwrite) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		if (!overwrite && Contains(maid, pluginName, propName)) {
			return false;
		}
		return PluginSettings.Set(maid.status.guid, pluginName, propName, value);
	}

	public static bool SetBool(Maid maid, string pluginName, string propName, bool value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	public static bool SetInt(Maid maid, string pluginName, string propName, int value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	public static bool SetFloat(Maid maid, string pluginName, string propName, float value, bool overwrite) {
		return Set(maid, pluginName, propName, value.ToString(), overwrite);
	}

	/// <summary>
	/// 拡張セーブデータへ設定を書き込む(常に上書き)
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool Set(Maid maid, string pluginName, string propName, string value) {
		return Set(maid, pluginName, propName, value, true);
	}

	public static bool SetBool(Maid maid, string pluginName, string propName, bool value) {
		return SetBool(maid, pluginName, propName, value, true);
	}

	public static bool SetInt(Maid maid, string pluginName, string propName, int value) {
		return SetInt(maid, pluginName, propName, value, true);
	}

	public static bool SetFloat(Maid maid, string pluginName, string propName, float value) {
		return SetFloat(maid, pluginName, propName, value, true);
	}

	/// <summary>
	/// 拡張セーブデータ内の設定を削除
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:削除に成功(設定が存在し、それを削除した)。false:失敗(設定が存在しないか、何らかのエラー)</returns>
	public static bool Remove(Maid maid, string pluginName, string propName) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		return PluginSettings.Remove(maid.status.guid, pluginName, propName);
	}

	/// <summary>
	/// 拡張セーブデータ内の設定の存在を調査
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:設定が存在。false:存在しない</returns>
	public static bool Contains(Maid maid, string pluginName, string propName) {
		if (maid == null || pluginName == null || propName == null) {
			return false;
		}
		if (!Contains(maid)) {
			SetMaid(maid);
		}
		return PluginSettings.Contains(maid.status.guid, pluginName, propName);
	}

	/// <summary>
	/// 拡張セーブデータ内のメイドの存在を確認
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <returns>true:指定したメイドが存在する。false:存在しない</returns>
	public static bool Contains(Maid maid) {
		if (maid == null) {
			return false;
		}
		return PluginSettings.ContainsMaid(maid.status.guid);
	}

	/// <summary>
	/// 拡張セーブデータへメイドを追加
	/// </summary>
	/// <param name="maid">メイドインスタンス</param>
	/// <returns></returns>
	public static void SetMaid(Maid maid) {
		if (maid == null) {
			return;
		}
		var s = maid.status;
		PluginSettings.SetMaid(s.guid, s.lastName, s.firstName, s.creationTime);
	}

	/// <summary>
	/// 与えたGUIDを持たないメイドを拡張セーブデータから削除
	/// </summary>
	/// <param name="guids">メイドGUIDリスト</param>
	/// <returns></returns>
	public static void CleanupMaids(List<string> guids) {
		PluginSettings.Cleanup(guids);
	}

	public static void CleanupMaids() {
		var guids = new List<string>();
		var cm = GameMain.Instance.CharacterMgr;
		for (int i = 0, n = cm.GetStockMaidCount(); i < n; i++) {
			var maid = cm.GetStockMaid(i);
			guids.Add(maid.status.guid);
		}
		CleanupMaids(guids);
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定を得る(文字列)
	/// <para>指定した設定が存在しない場合はdefaultValueを返す</para>
	/// <seealso cref="GlobalGetBool"/>
	/// <seealso cref="GlobalGetInt"/>
	/// <seealso cref="GlobalGetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="defaultValue">プロパティが存在しない場合に返すデフォルト値</param>
	/// <returns>設定文字列</returns>
	public static string GlobalGet(string pluginName, string propName, string defaultValue) {
		if (pluginName == null || propName == null) {
			return defaultValue;
		}
		return PluginSettings.Get(SaveDataPluginSettings.GlobalMaidGuid, pluginName, propName, defaultValue);
	}

	public static bool GlobalGetBool(string pluginName, string propName, bool defaultValue) {
		return Helper.StringToBool(GlobalGet(pluginName, propName, null), defaultValue);
	}

	public static int GlobalGetInt(string pluginName, string propName, int defaultValue) {
		return Helper.StringToInt(GlobalGet(pluginName, propName, null), defaultValue);
	}

	public static float GlobalGetFloat(string pluginName, string propName, float defaultValue) {
		return Helper.StringToFloat(GlobalGet(pluginName, propName, null), defaultValue);
	}

	/// <summary>
	/// 拡張セーブデータへグローバル設定を書き込む
	/// <seealso cref="GlobalSetBool"/>
	/// <seealso cref="GlobalSetInt"/>
	/// <seealso cref="GlobalSetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <param name="overwrite">trueなら常に上書き。falseなら設定が存在する場合は書き込みを行わない</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool GlobalSet(string pluginName, string propName, string value, bool overwrite) {
		if (pluginName == null || propName == null) {
			return false;
		}
		if (!overwrite && GlobalContains(pluginName, propName)) {
			return false;
		}
		return PluginSettings.Set(SaveDataPluginSettings.GlobalMaidGuid, pluginName, propName, value);
	}

	public static bool GlobalSetBool(string pluginName, string propName, bool value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	public static bool GlobalSetInt(string pluginName, string propName, int value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	public static bool GlobalSetFloat(string pluginName, string propName, float value, bool overwrite) {
		return GlobalSet(pluginName, propName, value.ToString(), overwrite);
	}

	/// <summary>
	/// 拡張セーブデータへグローバル設定を書き込む(常に上書き)
	/// <seealso cref="GlobalSetBool"/>
	/// <seealso cref="GlobalSetInt"/>
	/// <seealso cref="GlobalSetFloat"/>
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <param name="value">書き込む値</param>
	/// <returns>true:書き込み成功。false:失敗</returns>
	public static bool GlobalSet(string pluginName, string propName, string value) {
		return GlobalSet(pluginName, propName, value, true);
	}

	public static bool GlobalSetBool(string pluginName, string propName, bool value) {
		return GlobalSetBool(pluginName, propName, value, true);
	}

	public static bool GlobalSetInt(string pluginName, string propName, int value) {
		return GlobalSetInt(pluginName, propName, value, true);
	}

	public static bool GlobalSetFloat(string pluginName, string propName, float value) {
		return GlobalSetFloat(pluginName, propName, value, true);
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定を削除
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:削除に成功(設定が存在し、それを削除した)。false:失敗(設定が存在しないか、何らかのエラー)</returns>
	public static bool GlobalRemove(string pluginName, string propName) {
		if (pluginName == null || propName == null) {
			return false;
		}
		return PluginSettings.Remove(SaveDataPluginSettings.GlobalMaidGuid, pluginName, propName);
	}

	/// <summary>
	/// 拡張セーブデータ内のグローバル設定の存在を調査
	/// </summary>
	/// <param name="pluginName">プラグイン名("CM3D2.Test.Plugin"など)</param>
	/// <param name="propName">プロパティ名</param>
	/// <returns>true:設定が存在する。false:存在しない</returns>
	public static bool GlobalContains(string pluginName, string propName) {
		if (pluginName == null || propName == null) {
			return false;
		}
		return PluginSettings.Contains(SaveDataPluginSettings.GlobalMaidGuid, pluginName, propName);
	}

	/// <summary>
	/// セーブファイル番号から拡張セーブデータのファイル名を生成
	/// </summary>
	/// <param name="that">GameMainインスタンス</param>
	/// <param name="f_nSaveNo">セーブファイル番号</param>
	/// <returns>拡張セーブデータのファイル名</returns>
	public static string makeXmlFilename(GameMain that, int f_nSaveNo) {
		return GameMainMakeSavePathFileName(that, f_nSaveNo) + ".exsave.xml";
	}

	/// <summary>
	/// セーブファイル番号からセーブデータのファイル名を生成 (GameMain.MakeSavePathFileNameを呼び出す)
	/// </summary>
	/// <param name="that">GameMainインスタンス</param>
	/// <param name="f_nSaveNo">セーブファイル番号</param>
	/// <returns>セーブデータのファイル名</returns>
	public static string GameMainMakeSavePathFileName(GameMain that, int f_nSaveNo) {
		return that.MakeSavePathFileName(f_nSaveNo);
	}

	static bool SetMaidName(Maid maid) {
		if (maid == null) {
			return false;
		}
		var s = maid.status;
		return PluginSettings.SetMaidName(s.guid, s.lastName, s.firstName, s.creationTime);
	}

	[HarmonyPatch(typeof(GameMain), nameof(GameMain.OnInitialize))]
	[HarmonyPrefix]
	public static void DummyInitialize(GameMain __instance) {
		// static class の生成を強要するためのダミーメソッド
		//
		// C# の static class の内容はクラスへの初アクセス時に遅延生成される。
		// このため、ExSaveData対応プラグインが１つも無い場合、
		// ExSaveDataのコンストラクタが呼ばれず、コールバックの設定ができない。
		//
		// このメソッドはこれを回避するためのダミーメソッドで、
		// GameMain.OnInitializeの末尾から呼び出される。
		// 
		// 本当はアトリビュート等でもっと良いやり方があるはずなんだろうけど、
		// 分かっていないのでとりあえずこのまま。
	}

	[HarmonyPatch(typeof(GameMain), nameof(GameMain.Deserialize))]
	[HarmonyPostfix]
	static void deserializeCallback(GameMain __instance, int f_nSaveNo) {
		try {
			var xmlFilePath = makeXmlFilename(__instance, f_nSaveNo);
			saveDataPluginSettings = new();
			if (File.Exists(xmlFilePath)) {
				PluginSettings.Load(xmlFilePath);
			}
		} catch (Exception e) {
			Helper.ShowException(e);
		}
	}

	[HarmonyPatch(typeof(GameMain), nameof(GameMain.Serialize))]
	[HarmonyPostfix]
	static void serializeCallback(GameMain __instance, int f_nSaveNo, string f_strComment) {
		try {
			var cm = GameMain.Instance.CharacterMgr;
			for (int i = 0, n = cm.GetStockMaidCount(); i < n; i++) {
				var maid = cm.GetStockMaid(i);
				SetMaidName(maid);
			}
			CleanupMaids();
			var path = GameMainMakeSavePathFileName(__instance, f_nSaveNo);
			var xmlFilePath = makeXmlFilename(__instance, f_nSaveNo);
			PluginSettings.Save(xmlFilePath, path);
		} catch (Exception e) {
			Helper.ShowException(e);
		}
	}

	[HarmonyPatch(typeof(GameMain), nameof(GameMain.DeleteSerializeData))]
	[HarmonyPostfix]
	static void deleteSerializeDataCallback(GameMain __instance, int f_nSaveNo) {
		try {
			var xmlFilePath = makeXmlFilename(__instance, f_nSaveNo);
			if (File.Exists(xmlFilePath)) {
				File.Delete(xmlFilePath);
			}
		} catch (Exception e) {
			Helper.ShowException(e);
		}
	}
}

internal class SaveDataPluginSettings {
	internal SaveData saveData = new();
	public const string GlobalMaidGuid = "global";

	public SaveDataPluginSettings Load(string xmlFilePath) {
		var xml = Helper.LoadXmlDocument(xmlFilePath);
		saveData = new SaveData().Load(xml.SelectSingleNode("/savedata"));
		return this;
	}

	public void Save(string xmlFilePath, string targetSaveDataFileName) {
		var xml = Helper.LoadXmlDocument(xmlFilePath);
		saveData.target = targetSaveDataFileName;

		var xmlSaveData = SelectOrAppendNode(xml, "savedata", "savedata");
		saveData.Save(xmlSaveData);
		xml.Save(xmlFilePath);
	}

	public bool Contains(string guid, string pluginName, string propName) {
		return saveData.Contains(guid, pluginName, propName);
	}

	public string Get(string guid, string pluginName, string propName, string defaultValue) {
		return saveData.Get(guid, pluginName, propName, defaultValue);
	}

	public bool Set(string guid, string pluginName, string propName, string value) {
		return saveData.Set(guid, pluginName, propName, value);
	}

	public bool Remove(string guid, string pluginName, string propName) {
		return saveData.Remove(guid, pluginName, propName);
	}

	public bool ContainsMaid(string guid) => saveData.ContainsMaid(guid);

	public void SetMaid(string guid, string lastName, string firstName, string createTime) {
		saveData.SetMaid(guid, lastName, firstName, createTime);
	}

	public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
		return saveData.SetMaidName(guid, lastName, firstName, createTime);
	}

	public void Cleanup(List<string> guids) {
		saveData.Cleanup(guids);
	}

	public class SaveData {
		public string target;       // 寄生先のセーブデータ名
		internal Dictionary<string, Maid> maids;

		public SaveData() {
			Clear();
		}

		public void Clear() {
			maids = new();
			SetMaid(GlobalMaidGuid, "", "", "");
		}

		public SaveData Load(XmlNode xmlNode) {
			target = GetAttribute(xmlNode, "target");
			Clear();
			foreach (XmlNode n in xmlNode.SelectNodes("maids/maid")) {
				var a = GetAttribute(n, "guid");
				if (a != null) {
					maids[a] = new Maid().Load(n);
				}
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "target", target);
			var xmlMaids = SelectOrAppendNode(xmlNode, "maids", "maids");

			// 存在しない<maid>を削除
			foreach (XmlNode n in xmlMaids.SelectNodes("maid")) {
				var bRemove = true;
				var guid = GetAttribute(n, "guid");
				if (guid != null && maids.ContainsKey(guid)) {
					bRemove = false;
				}
				if (bRemove) {
					xmlMaids.RemoveChild(n);
				}
			}

			foreach (var kv in maids.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)) {
				var n = SelectOrAppendNode(xmlMaids, string.Format("maid[@guid='{0}']", kv.Key), "maid");
				kv.Value.Save(n);
			}
		}

		public void Cleanup(List<string> guids) {
			maids = maids.Where(kv => guids.Contains(kv.Key) || kv.Key == GlobalMaidGuid).ToDictionary(kv => kv.Key, kv => kv.Value);
		}

		Maid TryGetValue(string guid) {
			if (maids.TryGetValue(guid, out var maid)) {
				return maid;
			}
			return null;
		}

		public void SetMaid(string guid, string lastName, string firstName, string createTime) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				maid = new();
				maids[guid] = maid;
			}
			maid.SetMaid(guid, lastName, firstName, createTime);
		}

		public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				return false;
			}
			return maid.SetMaidName(lastName, firstName, createTime);
		}

		public bool ContainsMaid(string guid) => maids.ContainsKey(guid);

		public bool Contains(string guid, string pluginName, string propName) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				return false;
			}
			return maid.Contains(pluginName, propName);
		}

		public string Get(string guid, string pluginName, string propName, string defaultValue) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				return defaultValue;
			}
			return maid.Get(pluginName, propName, defaultValue);
		}

		public bool Set(string guid, string pluginName, string propName, string value) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				return false;
			}
			return maid.Set(pluginName, propName, value);
		}

		public bool Remove(string guid, string pluginName, string propName) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				return false;
			}
			return maid.Remove(pluginName, propName);
		}
	}

	public class Maid {
		string guid;
		string lastname;
		string firstname;
		string createtime;
		internal Dictionary<string, Plugin> plugins = new();

		public Maid Load(XmlNode xmlNode) {
			guid = GetAttribute(xmlNode, "guid");
			lastname = GetAttribute(xmlNode, "lastname");
			firstname = GetAttribute(xmlNode, "firstname");
			createtime = GetAttribute(xmlNode, "createtime");
			plugins = new();

			foreach (XmlNode n in xmlNode.SelectNodes("plugins/plugin")) {
				var name = GetAttribute(n, "name");
				if (name != null) {
					plugins[name] = new Plugin().Load(n);
				}
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "guid", guid);
			SetAttribute(xmlNode, "lastname", lastname);
			SetAttribute(xmlNode, "firstname", firstname);
			SetAttribute(xmlNode, "createtime", createtime);

			var xmlPlugins = SelectOrAppendNode(xmlNode, "plugins", null);
			foreach (var kv in plugins) {
				var path = string.Format("plugin[@name='{0}']", kv.Key);
				var n = xmlPlugins.SelectSingleNode(path);
				if (n == null) {
					n = SelectOrAppendNode(xmlPlugins, path, "plugin");
				} else {
					n.RemoveAll();
				}
				kv.Value.Save(n);
			}
		}

		public void SetMaid(string guid, string lastName, string firstName, string createTime) {
			this.lastname = lastName;
			this.firstname = firstName;
			this.createtime = createTime;
			this.guid = guid;
			this.plugins = new();
		}

		public bool SetMaidName(string lastName, string firstName, string createTime) {
			this.lastname = lastName;
			this.firstname = firstName;
			this.createtime = createTime;
			return true;
		}

		Plugin TryGetValue(string pluginName) {
			if (plugins.TryGetValue(pluginName, out var plugin)) {
				return plugin;
			}
			return null;
		}

		public bool Contains(string pluginName, string propName) {
			var plugin = TryGetValue(pluginName);
			if (plugin == null) {
				return false;
			}
			return plugin.Contains(propName);
		}

		public string Get(string pluginName, string propName, string defaultValue) {
			var plugin = TryGetValue(pluginName);
			if (plugin == null) {
				return defaultValue;
			}
			return plugin.Get(propName, defaultValue);
		}

		public bool Set(string pluginName, string propName, string value) {
			var plugin = TryGetValue(pluginName);
			if (plugin == null) {
				plugin = new() { name = pluginName };
				plugins[pluginName] = plugin;
			}
			return plugin.Set(propName, value);
		}

		public bool Remove(string pluginName, string propName) {
			var plugin = TryGetValue(pluginName);
			if (plugin == null) {
				return false;
			}
			return plugin.Remove(propName);
		}
	}

	public class Plugin {
		public string name;
		public Dictionary<string, string> props = new();

		public Plugin Load(XmlNode xmlNode) {
			name = GetAttribute(xmlNode, "name");
			props = new();
			foreach (XmlNode e in xmlNode.SelectNodes("prop")) {
				props[GetAttribute(e, "name")] = GetAttribute(e, "value");
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "name", name);
			foreach (var kv in props) {
				var n = SelectOrAppendNode(xmlNode, string.Format("prop[@name='{0}']", kv.Key), "prop");
				SetAttribute(n, "name", kv.Key);
				SetAttribute(n, "value", kv.Value);
			}
		}

		public bool Contains(string propName) => props.ContainsKey(propName);

		public string Get(string propName, string defaultValue) {
			if (!props.TryGetValue(propName, out var value)) {
				value = defaultValue;
			}
			return value;
		}

		public bool Set(string propName, string value) {
			props[propName] = value;
			return true;
		}

		public bool Remove(string propName) {
			var b = props.Remove(propName);
			return b;
		}
	}

	static XmlNode SelectOrAppendNode(XmlNode xmlNode, string path, string prefix) {
		if (xmlNode == null) {
			return null;
		}

		prefix ??= path;

		var n = xmlNode.SelectSingleNode(path);
		if (n == null) {
			var od = xmlNode.OwnerDocument;
			if (xmlNode is XmlDocument document) {
				od = document;
			}
			if (od == null) {
				return null;
			}
			n = xmlNode.AppendChild(od.CreateElement(prefix));
		}
		return n;
	}

	static string GetAttribute(XmlNode xmlNode, string name) {
		if (xmlNode == null) {
			return null;
		}
		var a = xmlNode.Attributes[name];
		return a?.Value;
	}

	static void SetAttribute(XmlNode xmlNode, string name, string value) {
		((XmlElement)xmlNode).SetAttribute(name, value);
	}
}
