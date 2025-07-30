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
	
	[Export] public TextureProgressBar CompletionBar;
	
	private NotificationManager _notificationManager; 

	// Tutorial mode
	public bool NotificationHistoryEnabled = true;
	public bool TechTreeEnabled = true;

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
		NotificationButton.Pressed += () => GameManager.Instance?.PlayUIClickSound();
		
		
		// Tech Tree button
		TechTreeButton.Pressed += OpenTechTree;
		TechTreeButton.Pressed += () => GameManager.Instance?.PlayUIClickSound();
		
		// Start Generic Notifications Function
		_ = RunNotificationTestLoop();
		
		// Update the bar 
		_ = UpdateCompletionBarLoop();
	}

	private void ToggleHistory()
	{
		if (!NotificationHistoryEnabled)
			return;

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
			await Task.Delay(60000); // Every 1 minute

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
	
	private async Task UpdateCompletionBarLoop()
	{
		while (true)
		{
			await Task.Delay(10000); // every 10 seconds

			var percent = GameManager.Instance.Game.PercentCompletion;
			CompletionBar.Value = percent * 100f;
		}
	}
	
	private void OpenTechTree()
	{
		if (!TechTreeEnabled)
			return;
		
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

		// Connect UI click sound signal
		if (treeUI is TechTreeUI techTreeUIInstance)
		{
			techTreeUIInstance.Connect(TechTreeUI.SignalName.UIClick, new Callable(GameManager.Instance, nameof(GameManager.PlayUIClickSound)));
		}

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
		
		// Check if NotificationPopup is still valid before adding child
		if (NotificationPopup == null || IsInstanceValid(NotificationPopup) == false)
		{
			card.QueueFree();
			return;
		}
		
		NotificationPopup.AddChild(card);

		// Fade-in
		var tween = CreateTween();
		tween.TweenProperty(card, "modulate:a", 1f, 0.4f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

		// Display for set duration
		await ToSignal(GetTree().CreateTimer(duration), "timeout");

		// Check if objects are still valid after the delay
		if (NotificationPopup == null || IsInstanceValid(NotificationPopup) == false || 
			card == null || IsInstanceValid(card) == false)
		{
			return;
		}

		// Fade out
		var fadeOutTween = CreateTween();
		fadeOutTween.TweenProperty(card, "modulate:a", 0f, 0.6f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);

		await ToSignal(fadeOutTween, "finished");
		
		// Final check before cleanup
		if (card != null && IsInstanceValid(card))
		{
			card.QueueFree();
		}
	}

	// Notify function
	public void Notify(string message, float popupDuration = 2.5f)
	{
		ShowPopupNotification(message, popupDuration);
		AddNotificationToHistory(message);
	}
	
	public void SetNotificationManager(NotificationManager manager)
	{
		_notificationManager = manager;
	}
}
