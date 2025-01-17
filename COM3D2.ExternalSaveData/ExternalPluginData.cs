namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalPluginData {
	private readonly string _pluginName;

	public ExternalPluginData(string pluginName) {
		_pluginName = pluginName;
	}

	public Dictionary<string, string> Properties { get; } = new();

	public static ExternalPluginData Load(XmlNode xmlNode) {
		var pluginName = xmlNode.GetAttribute("name");
		var pluginData = new ExternalPluginData(pluginName);
		foreach (XmlNode node in xmlNode.SelectNodes("prop")) {
			pluginData.Properties[node.GetAttribute("name")] = node.GetAttribute("value");
		}
		return pluginData;
	}

	public void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("name", _pluginName);
		foreach (var kv in Properties) {
			var node = xmlNode.SelectOrAppendNode($"prop[@name='{kv.Key}']", "prop");
			node.SetAttribute("name", kv.Key);
			node.SetAttribute("value", kv.Value);
		}
	}

	public bool HasProperty(string propertyName) {
		return Properties.ContainsKey(propertyName);
	}

	public string GetPropertyValue(string propertyName) {
		return Properties.TryGetValue(propertyName, out var value) ? value : null;
	}

	public bool SetPropertyValue(string propertyName, string value) {
		Properties[propertyName] = value;
		return true;
	}

	public bool RemoveProperty(string propertyName) {
		return Properties.Remove(propertyName);
	}
}
