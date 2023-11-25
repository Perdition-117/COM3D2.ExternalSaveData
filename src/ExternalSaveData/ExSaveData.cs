using System.IO;
using System.Xml;
using HarmonyLib;

namespace CM3D2.ExternalSaveData.Managed;

public static class ExSaveData {
	private static SaveDataPluginSettings saveDataPluginSettings = new();

	// 拡張セーブデータの実体
	private static SaveDataPluginSettings PluginSettings => saveDataPluginSettings;

	// 通常のプラグイン名よりも前に処理するため、先頭に '.' をつけている。
	// 通常のプラグインは "CM3D2～" のように英数字から始まる名前をつけること
	private const string CallbackName = ".CM3D2 ExternalSaveData";

	public static bool TryGetXml(Maid maid, string pluginName, XmlNode xmlNode) {
		if (PluginSettings.saveData.Maids.TryGetValue(maid.status.guid, out var maidSaveData)) {
			if (maidSaveData.Plugins.TryGetValue(pluginName, out var plugin)) {
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

		if (PluginSettings.saveData.Maids.TryGetValue(maid.status.guid, out var maidSaveData)) {
			if (maidSaveData.Plugins.TryGetValue(pluginName, out var plugin)) {
				plugin.Load(xmlNode);
			} else {
				maidSaveData.Plugins[pluginName] = new SaveDataPluginSettings.Plugin().Load(xmlNode);
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
		for (var i = 0; i < cm.GetStockMaidCount(); i++) {
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

	private static bool SetMaidName(Maid maid) {
		if (maid == null) {
			return false;
		}
		var status = maid.status;
		return PluginSettings.SetMaidName(status.guid, status.lastName, status.firstName, status.creationTime);
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
	private static void OnDeserialize(GameMain __instance, int f_nSaveNo) {
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
	private static void OnSerialize(GameMain __instance, int f_nSaveNo, string f_strComment) {
		try {
			var cm = GameMain.Instance.CharacterMgr;
			for (var i = 0; i < cm.GetStockMaidCount(); i++) {
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
	private static void OnDeleteSerializeData(GameMain __instance, int f_nSaveNo) {
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
