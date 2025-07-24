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
	
	private NotificationManager _notificationManager; 

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
		
		_notificationManager = GetNode<NotificationManager>("../NotificationManager");

		// Start Generic Notifications Function
		_ = RunNotificationTestLoop();
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
	
	// General Notification  function (Change wait to 1min for real game)
	private async Task RunNotificationTestLoop()
	{
		int educationIndex = 1;

		while (educationIndex <= 13)
		{
			await Task.Delay(10000); // 10 seconds for now

			string key = $"Education {educationIndex}";
			string msg = _notificationManager.GetMessage(key);

			if (!string.IsNullOrEmpty(msg))
			{
				Notify(msg);
			}
			else
			{
				GD.PrintErr($"Missing message for {key}");
			}

			educationIndex++;
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
