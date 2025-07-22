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
		_closeButton.Pressed += Hide;

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

		// Note: Should probably change this to poll the data instead of grabbing it once. ~ Justin

		// Get Current Information
		_title.Text = $"Region {region.Name}";

		_healthLabel.Text = $"Health: {region.Health:P1}";
		_moneyLabel.Text = $"Money: ${region.Money:F0}";
		_populationLabel.Text = $"Population: {region.Population:F0}";

		_windDamageLabel.Text = $"Wind Damage: {region.WindDamage:F1}";
		_floodDamageLabel.Text = $"Flood Damage: {region.FloodDamage:F1}";
		_secondaryDamageLabel.Text = $"Secondary Damage: {region.SecondaryDamage:F1}";

		// Position the popup (top right, left of the info/techtree buttons)
		var viewport = GetViewport().GetVisibleRect();
		Position = new Vector2(viewport.Size.X - Size.X - 80, 20);

		Show();
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
			Hide();
		}
	}
}
