using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace CM3D2.ExternalSaveData.Managed;

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
