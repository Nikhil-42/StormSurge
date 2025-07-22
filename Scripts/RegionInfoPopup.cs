using Godot;
using System;

public partial class RegionInfoPopup : Panel
{
	private Label _healthLabel;
	private Label _populationLabel;
	private Label _gdpLabel;
	private Label _idLabel;

	public override void _Ready()
	{
		_idLabel = GetNode<Label>("VBoxContainer/ID");
		_healthLabel = GetNode<Label>("VBoxContainer/Health");
		_populationLabel = GetNode<Label>("VBoxContainer/Population");
		_gdpLabel = GetNode<Label>("VBoxContainer/GDP");
		
		var closeButton = GetNode<TextureButton>("CloseButton");
		closeButton.Pressed += OnClosePressed;

		Hide(); // Start hidden
	}

	public void ShowInfo(int regionID)
	{
		var region = GameManager.Instance.GetRegionAI(regionID);
		if (region == null)
		{
			GD.PrintErr($"Invalid region ID: {regionID}");
			return;
		}
	
		_idLabel.Text = $"Region: {region.ID}";
		_healthLabel.Text = $"Health: {region.Health:F2}";
		_populationLabel.Text = $"Population: {region.Population:N0}";
		_gdpLabel.Text = $"GDP: ${region.GDP:N0}";

		Show();
	}
	
	private void OnClosePressed()
	{
		Hide(); 
	}
}
