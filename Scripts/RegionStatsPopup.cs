using Godot;


// Note/TODO: Popup currently does not close if the techtree button/display is opened. 
//            Should also probably remove the hightlight from a region if the popup is closed.
public partial class RegionStatsPopup : Control
{
	[Export] private Label _title;

	[Export] private Label _healthLabel;
	[Export] private Label _stateLabel;
	[Export] private Label _moneyLabel;
	[Export] private Label _populationLabel;

	[Export] private Label _windDamageLabel;
	[Export] private Label _floodDamageLabel;
	[Export] private Label _secondaryDamageLabel;

	[Export] private Button _closeButton;

	private RegionAI _currentRegion;
	private float _updateInterval = 0.1f; 
	private float _timeSinceLastUpdate = 0f;

	public override void _Ready()
	{
		// Get references to UI elements
		_title = GetNode<Label>("Control/VBoxContainer/Title");
		_healthLabel = GetNode<Label>("Control/VBoxContainer/BasicStats/HealthLabel");
		_stateLabel = GetNode<Label>("Control/VBoxContainer/BasicStats/StateLabel");
		_moneyLabel = GetNode<Label>("Control/VBoxContainer/BasicStats/MoneyLabel");
		_populationLabel = GetNode<Label>("Control/VBoxContainer/BasicStats/PopulationLabel");
		_windDamageLabel = GetNode<Label>("Control/VBoxContainer/DamageStats/WindDamageLabel");
		_floodDamageLabel = GetNode<Label>("Control/VBoxContainer/DamageStats/FloodDamageLabel");
		_secondaryDamageLabel = GetNode<Label>("Control/VBoxContainer/DamageStats/SecondaryDamageLabel");
		_closeButton = GetNode<Button>("Control/VBoxContainer/CloseButton");

		// Connect close button
		_closeButton.Pressed += () => {
			_currentRegion = null;
			Hide();
		};

		// Start hidden
		Hide();
	}

	public void ShowRegionStats(RegionAI region)
	{
		if (region == null)
		{
			Hide();
			return;
		}

		_currentRegion = region;
		_timeSinceLastUpdate = 0f;

		// Update stats immediately
		UpdateRegionStats();

		// Position the popup (top right, left of the info/techtree buttons)
		var viewport = GetViewport().GetVisibleRect();
		Position = new Vector2(viewport.Size.X - Size.X - 80, 20);

		Show();
	}

	public override void _Process(double delta)
	{
		// Only update if popup is visible and we have a region
		if (!Visible || _currentRegion == null)
			return;

		_timeSinceLastUpdate += (float)delta;

		if (_timeSinceLastUpdate >= _updateInterval)
		{
			UpdateRegionStats();
			_timeSinceLastUpdate = 0f;
		}
	}

	private void UpdateRegionStats()
	{
		if (_currentRegion == null)
			return;

		// Update Current Information
		// Note: If performance becomes an issue, we may want to update values at different intervals ie: (health every 0.1s, money every 0.5s)
		//       or only update specific values that have changed.
		_title.Text = $"Region {_currentRegion.Name}";

		_healthLabel.Text = $"Health: {_currentRegion.Health:P1}";
		_moneyLabel.Text = $"Money: ${_currentRegion.Money:F0}";
		_populationLabel.Text = $"Population: {_currentRegion.Population:F0}";

		_windDamageLabel.Text = $"Wind Damage: {_currentRegion.WindDamage:F1}";
		_floodDamageLabel.Text = $"Flood Damage: {_currentRegion.FloodDamage:F1}";
		_secondaryDamageLabel.Text = $"Secondary Damage: {_currentRegion.SecondaryDamage:F1}";
	}

	private string FormatPopulation(int population)
	{
		if (population >= 1000000)
			return $"{population / 1000000.0:F1}M";
		else if (population >= 1000)
			return $"{population / 1000.0:F1}K";
		else
			return population.ToString();
	}

	public override void _Input(InputEvent @event)
	{
		// Close pop when pressing escape (should probably be tied to an action in project settings)
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
		{
			_currentRegion = null;
			Hide();
		}
	}

	public override void _Notification(int what)
	{
		// Clear current region when popup becomes hidden
		if (what == NotificationVisibilityChanged && !Visible)
		{
			_currentRegion = null;
		}
	}
}
