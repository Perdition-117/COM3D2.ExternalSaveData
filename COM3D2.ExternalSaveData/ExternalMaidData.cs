namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalMaidData {
	public const string GlobalMaidGuid = "global";

	private string _guid;
	private string _lastName;
	private string _firstName;
	private string _createdTime;

	internal Dictionary<string, ExternalPluginData> Plugins { get; } = new();

	public ExternalMaidData Load(XmlNode xmlNode) {
		_guid = xmlNode.GetAttribute("guid");
		_lastName = xmlNode.GetAttribute("lastname");
		_firstName = xmlNode.GetAttribute("firstname");
		_createdTime = xmlNode.GetAttribute("createtime");
		Plugins.Clear();

		foreach (XmlNode node in xmlNode.SelectNodes("plugins/plugin")) {
			if (node.TryGetAttribute("name", out var name)) {
				Plugins[name] = new ExternalPluginData().Load(node);
			}
		}

		return this;
	}

	public void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("guid", _guid);
		xmlNode.SetAttribute("lastname", _lastName);
		xmlNode.SetAttribute("firstname", _firstName);
		xmlNode.SetAttribute("createtime", _createdTime);

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

	private bool TryGetValue(string pluginName, out ExternalPluginData plugin) {
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
