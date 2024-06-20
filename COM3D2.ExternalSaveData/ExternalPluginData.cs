namespace CM3D2.ExternalSaveData.Managed;

internal class ExternalPluginData {
	public string name;
	public Dictionary<string, string> props = new();

	public ExternalPluginData Load(XmlNode xmlNode) {
		name = xmlNode.GetAttribute("name");
		props = new();
		foreach (XmlNode node in xmlNode.SelectNodes("prop")) {
			props[node.GetAttribute("name")] = node.GetAttribute("value");
		}
		return this;
	}

	public void Save(XmlNode xmlNode) {
		xmlNode.SetAttribute("name", name);
		foreach (var kv in props) {
			var node = xmlNode.SelectOrAppendNode($"prop[@name='{kv.Key}']", "prop");
			node.SetAttribute("name", kv.Key);
			node.SetAttribute("value", kv.Value);
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
