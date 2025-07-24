using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class UI : Control
{
	[Export] public TextureButton NotificationButton;
	[Export] public PanelContainer NotificationHistoryPanel;
	[Export] public VBoxContainer HistoryList;
	[Export] public BaseButton TechTreeButton;
	[Export] public PackedScene TechTreeUIScene;
	[Export] public Control NotificationPopup; 

	[Export] private PackedScene _notificationCardScene;

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
		if (_notificationCardScene == null)
		{
			GD.PrintErr("ERROR: Could not load NotificationCard.tscn");
			return;
		}

		// Setup button signal
		NotificationButton.Pressed += ToggleHistory;
		
		
		// Tech Tree button
		TechTreeButton.Pressed += OpenTechTree;

		// Start test loop
		//_ = RunNotificationTestLoop();
	}

	private void ToggleHistory()
	{
		NotificationHistoryPanel.Visible = !NotificationHistoryPanel.Visible;
		
		// Hide Popups when history is open
		NotificationPopup.Visible = !NotificationHistoryPanel.Visible;
	}
	
	public void AddNotificationToHistory(string message)
	{
		if (_notificationCardScene == null)
		{
			GD.PrintErr("ERROR: NotificationCard scene not loaded");
			return;
		}

		var instance = _notificationCardScene.Instantiate();
		if (instance is NotificationCard card)
		{
			card.SetText(message);
			HistoryList.AddChild(card);
			HistoryList.MoveChild(card, 0); // Newest on top
		}
		else
		{
			GD.PrintErr("ERROR: Failed to instantiate NotificationCard.");
		}
	}
	
	// Testing function (no longer used)
	private async Task RunNotificationTestLoop()
	{
		var rng = new Random();

		while (true)
		{
			await Task.Delay(rng.Next(4000, 8000)); // Wait 4-8 seconds before next
			string msg = _testMessages[rng.Next(_testMessages.Count)];
			Notify(msg);
		}
	}
	
	private void OpenTechTree()
	{
		if (GameManager.Instance == null)
		{
			GD.PushError("Tried to open tech tree but GameManager is not ready.");
			return;
		}

		if (TechTreeUIScene == null)
		{
			GD.PushError("TechTreeUI.tscn not loaded.");
			return;
		}

		var treeUI = TechTreeUIScene.Instantiate();

		// Add to UI or main scene
		AddChild(treeUI);
	}
	
	public async void ShowPopupNotification(string message, float duration = 2.5f)
	{
		if (_notificationCardScene == null)
		{
			GD.PrintErr("ERROR: NotificationCard scene null.");
			return;
		}

		var instance = _notificationCardScene.Instantiate();
		if (instance is not NotificationCard card)
		{
			GD.PrintErr("ERROR: NotificationCard could not instantiate.");
			return;
		}
		
		card.SetText(message);

		card.Modulate = new Color(1, 1, 1, 0); // Fully transparent
		NotificationPopup.AddChild(card);

		// Fade-in
		var tween = CreateTween();
		tween.TweenProperty(card, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

		// Display for set duration
		await ToSignal(GetTree().CreateTimer(duration), "timeout");

		// Fade out
		var fadeOutTween = CreateTween();
		fadeOutTween.TweenProperty(card, "modulate:a", 0f, 0.6f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);

		await ToSignal(fadeOutTween, "finished");
		card.QueueFree();
	}

	// Notify function
	public void Notify(string message, float popupDuration = 2.5f)
	{
		ShowPopupNotification(message, popupDuration);
		AddNotificationToHistory(message);
	}

}
