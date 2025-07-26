using Godot;
using System;

public partial class GameTime : Control
{
	private int _days;
	private int _hours;
	private int _minutes;

	private string _label;
	public Label timeLabel;
	
	public int Days => _days;
	public int Hours => _hours;
	public int Minutes => _minutes;
	
	public override void _Ready()
	{
		timeLabel = GetNode<Label>("TimeLabel");
		_days = 0;
		_hours = 0;
		UpdateLabel();
	}

	public override void _Process(double delta) {
		double gameTime = (GameManager.Instance.Game.TimeElapsed / 1000.0);
		_days = (int)Math.Truncate(gameTime/24);
		_hours = (int)Math.Truncate(gameTime%24);
		_minutes = (int)Math.Truncate((gameTime%1)*60);

		UpdateLabel();
	}

	public void UpdateLabel() {
		_label = "Day: " + _days.ToString() + " | " + PadZero(_hours) + ":" + PadZero(_minutes);
		timeLabel.Text = _label;
	}

	public string PadZero(int time) {
		if (time < 10) {
			return "0" + time.ToString();
		} else {
			return time.ToString();
		}
	}
}
