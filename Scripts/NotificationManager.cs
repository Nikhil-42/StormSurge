using Godot;
using System.Linq;
using System.Collections.Generic;

public partial class NotificationManager : Control
{
	private Dictionary<string, string> _messages;

	public NotificationManager(Json messagesJson)
	{
		var rawDict = (Godot.Collections.Dictionary<string, string>)messagesJson.Data;
		if (rawDict == null)
		{
			GD.PushError("Failed to parse JSON into a dictionary.");
			return;
		}

		_messages = rawDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

		GD.Print($"[NotificationManager] Loaded {_messages.Count} messages.");
	}

	// Get a message by its key (Generic under "Education 1, Education 2,... 13").
	public string GetMessage(string key)
	{
		return _messages.TryGetValue(key, out var msg) ? msg : null;
	}

	//Returns all messages as a copy.
	public Dictionary<string, string> GetAllMessages()
	{
		return new Dictionary<string, string>(_messages);
	}
}
