using Godot;
using System;
using System.Collections.Generic;

public partial class NotificationManager : Control
{
	private Dictionary<string, string> _messages = new();

	public override void _Ready()
	{
		LoadMessagesFromJson();
	}

	private void LoadMessagesFromJson()
	{
		var filePath = "res://Library/notifications.json";

		if (!FileAccess.FileExists(filePath))
		{
			GD.PushError($"File not found: {filePath}");
			return;
		}

		using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();

		var jsonParse = Json.ParseString(jsonString);
		var rawDict = jsonParse.As<Godot.Collections.Dictionary>();
		if (rawDict == null)
		{
			GD.PushError("Failed to parse JSON into a dictionary.");
			return;
		}

		_messages.Clear();
		foreach (var keyVariant in rawDict.Keys)
		{
			string strKey = keyVariant.AsString();
			string strValue = rawDict[keyVariant].AsString();
			_messages[strKey] = strValue;
		}

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
