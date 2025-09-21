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

	public ExternalPluginData GetPluginData(string pluginName) {
		return _plugins.TryGetValue(pluginName, out var plugin) ? plugin : null;
	}

	public bool SetPropertyValue(string pluginName, string propertyName, string value) {
		if (!_plugins.TryGetValue(pluginName, out var plugin)) {
			plugin = new(pluginName);
			_plugins[pluginName] = plugin;
		}
		return plugin.SetPropertyValue(propertyName, value);
	}
}
