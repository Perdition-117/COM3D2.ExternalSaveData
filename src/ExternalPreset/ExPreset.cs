using CM3D2.ExternalSaveData.Managed;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CM3D2.ExternalPreset.Managed {
    public static class ExPreset {
        static HashSet<string> exsaveNodeNameMap = new HashSet<string>();

        static XmlDocument xmlMemory = null;

        // プリセット適用時に通知が必要な場合はここに登録
        public static UnityEvent loadNotify = new UnityEvent();

        public static void Load(Maid maid, CharacterMgr.Preset f_prest) {
            PluginsLoad(maid, f_prest);
        }
        
        static void PluginsLoad(Maid maid, CharacterMgr.Preset f_prest) {
            XmlDocument xml;

            string fileName;
            if (f_prest.strFileName == "") {
                xml = xmlMemory;
                xmlMemory = null;
            } else {
                fileName = f_prest.strFileName + ".expreset.xml";
                xml = LoadExFile(f_prest.strFileName + ".expreset.xml");
            }
            if (xml == null) return;
            foreach (string pluginName in exsaveNodeNameMap) {
                var node = xml.SelectSingleNode(@"//plugin[@name='" + pluginName + "']");
                if (node == null) continue;

                ExSaveData.SetXml(maid, pluginName, node);
            }

            // エディットシーン以外でもプリセットが使われるようになったが、
            // 通知先がエディットシーン以外で通知されるとエラーが出る場合があるためひとまずエディットシーンの実通知するようにする
            // シーン拡張は要相談
            if (SceneManager.GetActiveScene().name == "SceneEdit") {
                Debug.Log("Notify");
                loadNotify.Invoke();
            }
        }

        static XmlDocument LoadExFile(string fileName) {
            // Presetフォルダチェック
            var path = FindEXPresetFilePath(fileName);
            // expresetファイルがなければ終了
            if (path == null) return null;

            var xml = new XmlDocument();
            xml.Load(path);

            return xml;
        }

		[HarmonyPatch(typeof(CharacterMgr), nameof(CharacterMgr.PresetSaveNotWriteFile))]
		[HarmonyPostfix]
		public static void PostCharacterMgrPresetSaveNotWriteFile(CharacterMgr __instance, Maid f_maid, CharacterMgr.PresetType f_type) {
            if (f_type == CharacterMgr.PresetType.Wear) return;
            var xml = new XmlDocument();
            bool nodeExist = false;
            var rootNode = xml.AppendChild(xml.CreateElement("plugins"));
            foreach (string pluginName in exsaveNodeNameMap) {
                var node = xml.CreateElement("plugin");
                if (ExSaveData.TryGetXml(f_maid, pluginName, node)) {
                    rootNode.AppendChild(node);
                    nodeExist = true;
                }
            }

            if (nodeExist) {
                xmlMemory = xml;
            }
        }

        public static void Save(Maid maid, string f_strFileName, CharacterMgr.PresetType f_type) {
            //MaidVoicePitchSave(maid, f_strFileName, f_type);
            PluginsSave(maid, f_strFileName, f_type);
        }

        static void PluginsSave(Maid maid, string f_strFileName, CharacterMgr.PresetType f_type) {
            if (f_type == CharacterMgr.PresetType.Wear) return;
            var xml = new XmlDocument();
            bool nodeExist = false;
            var rootNode = xml.AppendChild(xml.CreateElement("plugins"));
            foreach (string pluginName in exsaveNodeNameMap) {
                var node = xml.CreateElement("plugin");
                if (ExSaveData.TryGetXml(maid, pluginName, node)) {
                    rootNode.AppendChild(node);
                    nodeExist = true;
                }
            }



            if (!nodeExist) {
                return;
            }
            xml.Save(Path.GetFullPath(".\\") + "Preset\\" + f_strFileName + ".expreset.xml");
        }

        public static void Delete(CharacterMgr.Preset f_prest) {
            var path = Path.GetFullPath(".\\") + "Preset\\" + f_prest.strFileName + ".expreset.xml";
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }

        static string FindEXPresetFilePath(string fileName) {
            // Presetフォルダチェック
            var path = Path.GetFullPath(".\\") + "Preset\\" + fileName;
            if (File.Exists(path)) return path;

            return null;
        }

        // EXSaveDataに保存する情報のうち、EXプリセットにもセーブするノード名を設定
        // 例はMaidVoicePitchなどを参照
        public static void AddExSaveNode(string pluginName) {
            exsaveNodeNameMap.Add(pluginName);
        }
    }
}
