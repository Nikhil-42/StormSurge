using Godot;
using System.Collections.Generic;
using System.Linq;


public partial class GameManager : Node
{
	// Singleton instance
	public static GameManager Instance => _instance;
	private static GameManager _instance = null;

	// ================= GLOBAL VARIABLES ================================
	public bool PrintDebug => _printDebug;
	public GameState Game => _game;

	[Export]
	private string regionStatsPath = "res://Library/regionstats.txt";

	[Export]
	private bool _printDebug = true;

	private GameState _game = null;
	private string[] _regionNames = null;
	private Dictionary<string, RegionStats> _regionStats = null;

	private string currentScreen = "start_menu";
	private string currentOption = "";
	private string currentClick = "";
	// FIXME: where, when, and how to set and reset these variables in loop

	private AudioStreamPlayer intro;
	private AudioStreamPlayer loop;
	private AudioStreamPlayer ambience;

	[Signal]
	public delegate void SolarChangedEventHandler(int newSolar);  // FIXME: might be put in game manager instead
	
	public double Solar { get => _game.Solar;
		set {
			if (_game.Solar != value) {
				_game.Solar = value;
				EmitSignal(SignalName.SolarChanged, value);
			}
		}
	}

	public override void _EnterTree() {
		if (_instance != null) {
			GD.PrintErr("WARNING: GameManager instance already exists!");  // debugging
		}
		_instance = this;

		if (Instance.PrintDebug) GD.Print("\nLoading region geographic data...");
		var file = FileAccess.Open(ProjectSettings.GlobalizePath(regionStatsPath), FileAccess.ModeFlags.Read);
		if (file == null) {
			GD.PrintErr("Failed to open region geographic data file: " + regionStatsPath);
			return;
		}

		var regionsStats = new Dictionary<string, RegionStats>();
		var header = file.GetCsvLine();
		while (!file.EofReached()) {
			var line = file.GetCsvLine();
			if (line == null || line.Length == 0) continue;  // Skip empty lines
			var stats = RegionStats.FromCsvLine(line);
			regionsStats.Add(stats.name, stats);
		}

		_regionStats = regionsStats;
		_regionNames = ["Ocean", .. regionsStats.Values.OrderBy(r => r.id).Select(r => r.name)];

		GD.Print("GameManager entering tree");  // debugging
		_game = new GameState(_regionNames, _regionStats);

		intro = GetNode<AudioStreamPlayer>("IntroMusic");
		loop = GetNode<AudioStreamPlayer>("LoopMusic");
		ambience = GetNode<AudioStreamPlayer>("StormAmbience");
	}

	public override void _Ready()
	{
		// Music controls
		var timer = new Timer();
		AddChild(timer);
		timer.OneShot = true;
		timer.WaitTime = (float)intro.Stream.GetLength();

		timer.Connect("timeout", new Callable(this, nameof(OnIntroFinished)));
		intro.Play();
		timer.Start();

		// intro.Connect("finished", new Callable(this, nameof(OnIntroFinished)));
	}
	
	public override void _Process(double deltaTime)
	{
		// Update the humanity AIs		
		for (int i = 0; i < _game.RegionAIs.Length; i++) {
			_game.RegionAIs[i].Process(deltaTime, _game);
		}
		
		// Passive income generation, rate changes by sea level
		Solar += _game.PassiveIncome * (1 + (0.01 * _game.stormTree.treeWeather.sea_level)) * deltaTime;
	}

	public void ApplyDamage(int regionID, double damage, DamageType type)
	{
		if (regionID == 0) {
			// GD.Print("Cannot apply damage to region 0 (Ocean)");
			return;
		} else if (regionID < 0 || regionID > _game.RegionAIs.Length) {
			GD.PrintErr($"Invalid region ID: {regionID}");
			return;
		}
		
		_game.RegionAIs[regionID - 1].ApplyDamage(damage, type);
		// if (PrintDebug) GD.Print($"Applying {damage} damage of type {type} to humanity AI in region {regionID}");
	}

	private void OnIntroFinished() {  // Switch to looping music/sound tracks
		loop.Seek(0);
		ambience.Seek(0);
		loop.Play();
		ambience.Play();
	}
}
