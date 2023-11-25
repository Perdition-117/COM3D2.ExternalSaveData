using System.Linq;
using System.Xml;

namespace CM3D2.ExternalSaveData.Managed;

internal class SaveDataPluginSettings {
	public const string GlobalMaidGuid = "global";

	internal SaveData saveData = new();

	public SaveDataPluginSettings Load(string xmlFilePath) {
		var xml = Helper.LoadXmlDocument(xmlFilePath);
		saveData = new SaveData().Load(xml.SelectSingleNode("/savedata"));
		return this;
	}

	public void Save(string xmlFilePath, string targetSaveDataFileName) {
		var xml = Helper.LoadXmlDocument(xmlFilePath);
		saveData.SaveFileName = targetSaveDataFileName;

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
		public string SaveFileName;       // 寄生先のセーブデータ名

		public SaveData() {
			Clear();
		}

		internal Dictionary<string, Maid> Maids { get; private set; } = new();

		public void Clear() {
			Maids.Clear();
			SetMaid(GlobalMaidGuid, "", "", "");
		}

		public SaveData Load(XmlNode xmlNode) {
			SaveFileName = GetAttribute(xmlNode, "target");
			Clear();
			foreach (XmlNode node in xmlNode.SelectNodes("maids/maid")) {
				var guid = GetAttribute(node, "guid");
				if (guid != null) {
					Maids[guid] = new Maid().Load(node);
				}
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "target", SaveFileName);
			var xmlMaids = SelectOrAppendNode(xmlNode, "maids", "maids");

			// 存在しない<maid>を削除
			foreach (XmlNode node in xmlMaids.SelectNodes("maid")) {
				var bRemove = true;
				var guid = GetAttribute(node, "guid");
				if (guid != null && Maids.ContainsKey(guid)) {
					bRemove = false;
				}
				if (bRemove) {
					xmlMaids.RemoveChild(node);
				}
			}

			foreach (var kv in Maids.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)) {
				var node = SelectOrAppendNode(xmlMaids, string.Format("maid[@guid='{0}']", kv.Key), "maid");
				kv.Value.Save(node);
			}
		}

		public void Cleanup(List<string> guids) {
			Maids = Maids.Where(kv => guids.Contains(kv.Key) || kv.Key == GlobalMaidGuid).ToDictionary(kv => kv.Key, kv => kv.Value);
		}

		private Maid TryGetValue(string guid) {
			if (Maids.TryGetValue(guid, out var maid)) {
				return maid;
			}
			return null;
		}

		public void SetMaid(string guid, string lastName, string firstName, string createTime) {
			var maid = TryGetValue(guid);
			if (maid == null) {
				maid = new();
				Maids[guid] = maid;
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

		public bool ContainsMaid(string guid) => Maids.ContainsKey(guid);

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
		private string _guid;
		private string _lastName;
		private string _firstName;
		private string _createdTime;

		internal Dictionary<string, Plugin> Plugins { get; } = new();

		public Maid Load(XmlNode xmlNode) {
			_guid = GetAttribute(xmlNode, "guid");
			_lastName = GetAttribute(xmlNode, "lastname");
			_firstName = GetAttribute(xmlNode, "firstname");
			_createdTime = GetAttribute(xmlNode, "createtime");
			Plugins.Clear();

			foreach (XmlNode node in xmlNode.SelectNodes("plugins/plugin")) {
				var name = GetAttribute(node, "name");
				if (name != null) {
					Plugins[name] = new Plugin().Load(node);
				}
			}

			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "guid", _guid);
			SetAttribute(xmlNode, "lastname", _lastName);
			SetAttribute(xmlNode, "firstname", _firstName);
			SetAttribute(xmlNode, "createtime", _createdTime);

			var xmlPlugins = SelectOrAppendNode(xmlNode, "plugins", null);
			foreach (var kv in Plugins) {
				var path = $"plugin[@name='{kv.Key}']";
				var node = xmlPlugins.SelectSingleNode(path);
				if (node == null) {
					node = SelectOrAppendNode(xmlPlugins, path, "plugin");
				} else {
					node.RemoveAll();
				}
				kv.Value.Save(node);
			}
		}

		public void SetMaid(string guid, string lastName, string firstName, string createdTime) {
			_guid = guid;
			_lastName = lastName;
			_firstName = firstName;
			_createdTime = createdTime;
			Plugins.Clear();
		}

		public bool SetMaidName(string lastName, string firstName, string createdTime) {
			_lastName = lastName;
			_firstName = firstName;
			_createdTime = createdTime;
			return true;
		}

		private Plugin TryGetValue(string pluginName) {
			if (Plugins.TryGetValue(pluginName, out var plugin)) {
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
				Plugins[pluginName] = plugin;
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
			foreach (XmlNode node in xmlNode.SelectNodes("prop")) {
				props[GetAttribute(node, "name")] = GetAttribute(node, "value");
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "name", name);
			foreach (var kv in props) {
				var node = SelectOrAppendNode(xmlNode, $"prop[@name='{kv.Key}']", "prop");
				SetAttribute(node, "name", kv.Key);
				SetAttribute(node, "value", kv.Value);
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

	private static XmlNode SelectOrAppendNode(XmlNode xmlNode, string path, string prefix) {
		if (xmlNode == null) {
			return null;
		}

		prefix ??= path;

		var node = xmlNode.SelectSingleNode(path);
		if (node == null) {
			var ownerDocument = xmlNode.OwnerDocument;
			if (xmlNode is XmlDocument document) {
				ownerDocument = document;
			}
			if (ownerDocument == null) {
				return null;
			}
			node = xmlNode.AppendChild(ownerDocument.CreateElement(prefix));
		}
		return node;
	}

	private static string GetAttribute(XmlNode xmlNode, string name) {
		return xmlNode?.Attributes[name]?.Value;
	}

	private static void SetAttribute(XmlNode xmlNode, string name, string value) {
		((XmlElement)xmlNode).SetAttribute(name, value);
	}
}
