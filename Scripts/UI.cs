using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class UI : Control
{
	[Export] public TextureButton NotificationButton;
	[Export] public PanelContainer NotificationHistoryPanel;
	[Export] public VBoxContainer HistoryList;

	private PackedScene _notificationCardScene;

	private List<string> _testMessages = new()
	{
		"Research Unlocked!.",
		"You’ve destroyed a city!",
		"Alert: humans have unlocked storm walls.",
		"Storms are now 10% stronger.",
		"Humanity has come to an agreement. Resources have been shared and global storm resistance has increased.",
		"You received 500 solar for destroying Moscow.",
		"Weather warning systems now in affect."
	};

	public override void _Ready()
	{
		// Load the notification card scene
		_notificationCardScene = GD.Load<PackedScene>("res://Scenes/NotificationCard.tscn");

		if (_notificationCardScene == null)
		{
			GD.PrintErr("ERROR: Could not load NotificationCard.tscn");
			return;
		}

		// Setup button signal
		NotificationButton.Pressed += ToggleHistory;

		// Start test loop
		_ = RunNotificationTestLoop();
	}

	private void ToggleHistory()
	{
		NotificationHistoryPanel.Visible = !NotificationHistoryPanel.Visible;
	}

	public void AddNotificationToHistory(string message)
	{
		if (_notificationCardScene == null)
		{
			GD.PrintErr("ERROR: NotificationCard.tscn not loaded!");
			return;
		}

		// Instantiation
		var instance = _notificationCardScene.Instantiate();
		if (instance is NotificationCard card)
		{
			card.SetText(message);
			HistoryList.AddChild(card);
		}
		else
		{
			GD.PrintErr("ERROR: Failed to instantiate NotificationCard.");
		}
	}

	private async Task RunNotificationTestLoop()
	{
		var rng = new Random();

		while (true)
		{
			await Task.Delay(rng.Next(1500, 4000)); // Wait 1.5–4 seconds before next
			string msg = _testMessages[rng.Next(_testMessages.Count)];
			AddNotificationToHistory(msg);
		}
	}
}
