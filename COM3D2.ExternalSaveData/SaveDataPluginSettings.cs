using System.Linq;

namespace CM3D2.ExternalSaveData.Managed;

internal class SaveDataPluginSettings {
	public const string GlobalMaidGuid = "global";

	internal SaveData saveData = new();

	public SaveDataPluginSettings Load(string xmlFilePath) {
		var xml = LoadXmlDocument(xmlFilePath);
		saveData = new SaveData().Load(xml.SelectSingleNode("/savedata"));
		return this;
	}

	public void Save(string xmlFilePath, string targetSaveDataFileName) {
		var xml = LoadXmlDocument(xmlFilePath);
		saveData.SaveFileName = targetSaveDataFileName;

		var xmlSaveData = xml.SelectOrAppendNode("savedata", "savedata");
		saveData.Save(xmlSaveData);
		xml.Save(xmlFilePath);
	}

	private static XmlDocument LoadXmlDocument(string xmlFilePath) {
		var xml = new XmlDocument();
		if (File.Exists(xmlFilePath)) {
			xml.Load(xmlFilePath);
		}
		return xml;
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
				if (node.TryGetAttribute("guid", out var guid)) {
					Maids[guid] = new Maid().Load(node);
				}
			}
			return this;
		}

		public void Save(XmlNode xmlNode) {
			SetAttribute(xmlNode, "target", SaveFileName);
			var xmlMaids = xmlNode.SelectOrAppendNode("maids", "maids");

			// 存在しない<maid>を削除
			foreach (XmlNode node in xmlMaids.SelectNodes("maid")) {
				if (!(node.TryGetAttribute("guid", out var guid) && Maids.ContainsKey(guid))) {
					xmlMaids.RemoveChild(node);
				}
			}

			foreach (var kv in Maids.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value)) {
				var node = xmlMaids.SelectOrAppendNode($"maid[@guid='{kv.Key}']", "maid");
				kv.Value.Save(node);
			}
		}

		public void Cleanup(List<string> guids) {
			Maids = Maids.Where(kv => guids.Contains(kv.Key) || kv.Key == GlobalMaidGuid).ToDictionary(kv => kv.Key, kv => kv.Value);
		}

		private bool TryGetValue(string guid, out Maid maid) {
			return Maids.TryGetValue(guid, out maid);
		}

		public void SetMaid(string guid, string lastName, string firstName, string createTime) {
			if (!TryGetValue(guid, out var maid)) {
				maid = new();
				Maids[guid] = maid;
			}
			maid.SetMaid(guid, lastName, firstName, createTime);
		}

		public bool SetMaidName(string guid, string lastName, string firstName, string createTime) {
			return TryGetValue(guid, out var maid) && maid.SetMaidName(lastName, firstName, createTime);
		}

		public bool ContainsMaid(string guid) => Maids.ContainsKey(guid);

		public bool Contains(string guid, string pluginName, string propName) {
			return TryGetValue(guid, out var maid) && maid.Contains(pluginName, propName);
		}

		public string Get(string guid, string pluginName, string propName, string defaultValue) {
			return TryGetValue(guid, out var maid) ? maid.Get(pluginName, propName, defaultValue) : defaultValue;
		}

		public bool Set(string guid, string pluginName, string propName, string value) {
			return TryGetValue(guid, out var maid) && maid.Set(pluginName, propName, value);
		}

		public bool Remove(string guid, string pluginName, string propName) {
			return TryGetValue(guid, out var maid) && maid.Remove(pluginName, propName);
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
				if (node.TryGetAttribute("name", out var name)) {
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

			var xmlPlugins = xmlNode.SelectOrAppendNode("plugins", null);
			foreach (var kv in Plugins) {
				var path = $"plugin[@name='{kv.Key}']";
				var node = xmlPlugins.SelectSingleNode(path);
				if (node == null) {
					node = xmlPlugins.SelectOrAppendNode(path, "plugin");
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

		private bool TryGetValue(string pluginName, out Plugin plugin) {
			return Plugins.TryGetValue(pluginName, out plugin);
		}

		public bool Contains(string pluginName, string propName) {
			return TryGetValue(pluginName, out var plugin) && plugin.Contains(propName);
		}

		public string Get(string pluginName, string propName, string defaultValue) {
			return TryGetValue(pluginName, out var plugin) ? plugin.Get(propName, defaultValue) : defaultValue;
		}

		public bool Set(string pluginName, string propName, string value) {
			if (!TryGetValue(pluginName, out var plugin)) {
				plugin = new() { name = pluginName };
				Plugins[pluginName] = plugin;
			}
			return plugin.Set(propName, value);
		}

		public bool Remove(string pluginName, string propName) {
			return TryGetValue(pluginName, out var plugin) && plugin.Remove(propName);
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
				var node = xmlNode.SelectOrAppendNode($"prop[@name='{kv.Key}']", "prop");
				SetAttribute(node, "name", kv.Key);
				SetAttribute(node, "value", kv.Value);
			}
		}

		public bool Contains(string propName) => props.ContainsKey(propName);

		public string Get(string propName, string defaultValue) {
			return props.TryGetValue(propName, out var value) ? value : defaultValue;
		}

		public bool Set(string propName, string value) {
			props[propName] = value;
			return true;
		}

		public bool Remove(string propName) {
			return props.Remove(propName);
		}
	}

	private static string GetAttribute(XmlNode xmlNode, string name) {
		return xmlNode?.Attributes[name]?.Value;
	}

	private static void SetAttribute(XmlNode xmlNode, string name, string value) {
		((XmlElement)xmlNode).SetAttribute(name, value);
	}
}

static class XmlNodeExtensions {
	public static bool TryGetAttribute(this XmlNode xmlNode, string name, out string value) {
		var xmlElement = (XmlElement)xmlNode;
		value = null;
		if (xmlElement.HasAttribute(name)) {
			value = xmlElement.GetAttribute(name);
			return true;
		}
		return false;
	}

	public static XmlNode SelectOrAppendNode(this XmlNode xmlNode, string path, string prefix) {
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
}
