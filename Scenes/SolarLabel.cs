using Godot;
using System;

public partial class SolarLabel : Label {
	public override void _Ready() {
		// Connect to the money changed signal
		
		GameManager.Instance.Game.Connect(GameState.SignalName.SolarChanged, new Callable(this, nameof(OnSolarChanged)));

		// Set initial value
		Text = $"{GameManager.Instance.Game.Solar}";
	}

	private void OnSolarChanged(int newSolar) {
		Text = $"{newSolar}";
	}
}
