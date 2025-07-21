using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Linq;

using Godot;
using System.IO;
using System.Globalization;
using CsvHelper;
using System.Collections.Generic;
using System.Linq;

public partial class GameState {  // Move node attributes, make not a node, make private
	// ================= GLOBAL TECH TREE VARIABLES ================================
	public TechTree stormTree = null;  // global default
	public TechTree humanTree = null;  // global default

	public weatherVars sharedWeatherVars = null;  // global default
	public geoVars sharedGeoVars = null;  // global default

	private int _religionLevel = 1;  // 1: 1% cult followers = 0.2%/0.5%, 2: 1% = 0.5%/1%, 3: 1% = 2%/2%
	
	public List<RegionStats> regionStats { get; private set; } = new();
	private string regionStatsPath = "res://Assets/region_geographic_data.csv";

	private RegionAI[] _regionAIs = null;

	// ================= GLOBAL MONEY & RESEARCH VARIABLES ================================
	private int _solar = 1000;
	private double _solarDecimal = 0.0;
	private double _passiveIncome = 1.0;  // Rate multiplier

	private int _globalFunding = 0;  // Funding for global research upgrades, FIXME: need to find reasonable rate
	public List<string> unlockedResearch = new List<string>();  // For easier access of which research nodes have been bought
	public List<string> lockedResearch = new List<string>();

	// ================= GLOBAL TIME VARIABLES ================================
	private double _timeElapsed = 0.0;  // real-time since game started, except pauses (minutes.fraction of minute)
	private double _gameTime = 0.0;  // in-game time since game started, except pauses (weeks.fraction of week > displayed in days, hours)

	// ================= GLOBAL GETTERS & SETTERS ================================
	public double PassiveIncome { get => _passiveIncome; set => _passiveIncome = value; }
	public int GlobalFunding { get => _globalFunding; set => _globalFunding = value; }
	public int Solar { get => _solar; set => _solar = value; }
	public double SolarDecimal { get => _solarDecimal; set => _solarDecimal = value; }
	public RegionAI[] RegionAIs { get => _regionAIs; set => _regionAIs = value; }
	
	// ================= INITIALIZER ================================
	public GameState(Json regionsJson) {
		if (GameManager.Instance.PrintDebug) {
			GD.Print("Creating game state object...");
			GD.Print("Creating game storm and human tech tree...");
		}
		stormTree = new TechTree(true);
		humanTree = new TechTree(false);
		stormTree.viewNodes();  // debug, no UI
		humanTree.viewNodes();  // debug, no UI

		sharedWeatherVars = new weatherVars();
		sharedGeoVars = new geoVars();

		if (GameManager.Instance.PrintDebug) GD.Print("\nLoading region geographic data...");
		using var reader = new StreamReader(ProjectSettings.GlobalizePath(regionStatsPath));
		using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
		regionStats = csv.GetRecords<RegionStats>().ToList();

		if (GameManager.Instance.PrintDebug) GD.Print("\nCreating Region AI objects...");

		// FIXME: from game manager pass in region data from json to this
		var regionNames = (Godot.Collections.Array)((Godot.Collections.Dictionary) regionsJson.Data)["names"];

		_regionAIs = new RegionAI[regionNames.Count-1];
		for (int i = 0; i < regionNames.Count-1; i++) {
			_regionAIs[i] = new RegionAI(i+1);
			_regionAIs[i].setRegionStatsChars(regionStats[i], GameManager.Instance.PrintDebug);
		}
	}

	public void updateSharedVars() {  // Update shared variables to current state of storm + human tech tree
		int total;
		for (int i=0; i<sharedWeatherVars.vars.Length; i++) {
			total = stormTree.treeWeather.vars[i] + humanTree.treeWeather.vars[i];
			sharedWeatherVars.vars[i] = total / 2;
		}
		for (int i=0; i<sharedGeoVars.vars.Length; i++) {
			total = stormTree.treeGeo.vars[i] + humanTree.treeGeo.vars[i];
			sharedGeoVars.vars[i] = total / 2;
		}
	}

	public void spendSolar(int amount) {
		Solar -= amount;
	}

	public void spendGlobalFunding(int amount) {
		GlobalFunding -= amount;
	}
}

public partial class GameManager : Node
{
	public static GameManager Instance => _instance;
	public bool PrintDebug => _printDebug;
	public GameState Game => _game;

	[Export]
	private Json _regionsJson;

	[Export]
	private bool _printDebug = true;

	private static GameManager _instance = null;
	private GameState _game = null;
	private string currentScreen = "start_menu";
	private string currentOption = "";
	private string currentClick = "";
	// FIXME: where, when, and how to set and reset these variables in loop

	[Signal]
	public delegate void SolarChangedEventHandler(int newSolar);  // FIXME: might be put in game manager instead
	
	public int Solar { get => Game.Solar;
		set {
			if (Game.Solar != value) {
				Game.Solar = value;
				EmitSignal(SignalName.SolarChanged, value);
			}
		}
	}

	public override void _EnterTree() {
		if (_instance != null) {
			GD.PrintErr("WARNING: GameManager instance already exists!");  // debugging
		}
		_instance = this;
		GD.Print("GameManager entering tree");  // debugging
		_game = new GameState(_regionsJson);
	}
	
	public override void _Ready()
	{
	}
	
	public override void _Process(double deltaTime)
	{
		// Update the humanity AIs		
		for (int i = 0; i < Game.RegionAIs.Length; i++) {
			Game.RegionAIs[i].Process(deltaTime, _game);
		}
		
		// Passive income generation, rate changes by sea level
		Game.SolarDecimal += Game.PassiveIncome * (1 + (0.01 * Game.stormTree.treeWeather.sea_level)) * deltaTime;
		if (Game.SolarDecimal > 1.000) {
			Solar += 1;
			Game.SolarDecimal = 0.0;
		}
	}

	public void ApplyDamage(int regionID, double damage, DamageType type)
	{
		if (regionID == 0) {
			// GD.Print("Cannot apply damage to region 0 (Ocean)");
			return;
		} else if (regionID < 0 || regionID > Game.RegionAIs.Length) {
			GD.PrintErr($"Invalid region ID: {regionID}");
			return;
		}
		
		Game.RegionAIs[regionID - 1].ApplyDamage(damage, type);
		// if (PrintDebug) GD.Print($"Applying {damage} damage of type {type} to humanity AI in region {regionID}");
	}
}
