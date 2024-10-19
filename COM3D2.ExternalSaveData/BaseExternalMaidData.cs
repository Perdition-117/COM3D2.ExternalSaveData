namespace CM3D2.ExternalSaveData.Managed;

internal abstract class BaseExternalMaidData {
	protected readonly Dictionary<string, ExternalPluginData> _plugins = new();

	protected void LoadPlugins(XmlNode xmlNode) {
		foreach (XmlNode node in xmlNode.SelectNodes("plugins/plugin")) {
			if (node.TryGetAttribute("name", out var name)) {
				LoadPlugin(name, node);
			}
		}
	}

	protected void SavePlugins(XmlNode xmlNode) {
		var xmlPlugins = xmlNode.SelectOrAppendNode("plugins");
		foreach (var kv in _plugins) {
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

	public virtual void Load(XmlNode xmlNode) { }

	public virtual void Save(XmlNode xmlNode) { }

	public void LoadPlugin(string pluginName, XmlNode xmlNode) {
		_plugins[pluginName] = ExternalPluginData.Load(xmlNode);
	}

	public ExternalPluginData GetPlugin(string pluginName) {
		return _plugins.TryGetValue(pluginName, out var plugin) ? plugin : default;
	}

	public bool TryGetValue(string pluginName, out ExternalPluginData plugin) {
		return _plugins.TryGetValue(pluginName, out plugin);
	}

	public bool Contains(string pluginName, string propName) {
		return TryGetValue(pluginName, out var plugin) && plugin.Properties.ContainsKey(propName);
	}

	public string Get(string pluginName, string propName, string defaultValue) {
		return TryGetValue(pluginName, out var plugin) && plugin.Properties.TryGetValue(propName, out var value) ? value : defaultValue;
	}

	public bool Set(string pluginName, string propName, string value) {
		if (!TryGetValue(pluginName, out var plugin)) {
			plugin = new(pluginName);
			_plugins[pluginName] = plugin;
		}
		plugin.Properties[propName] = value;
		return true;
	}

	public bool Remove(string pluginName, string propName) {
		return TryGetValue(pluginName, out var plugin) && plugin.Properties.Remove(propName);
	}
}
