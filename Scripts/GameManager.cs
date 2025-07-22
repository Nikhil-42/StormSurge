using Godot;
using System;
using System.Collections.Generic;

public partial class GameState : Node {
	// Global gamestate variable states
	public TechTree stormTree = null;  // global default
	public TechTree humanTree = null;  // global default
	public geoVars sharedVars = null;  // global default
	
	private int _solar = 1000;
	public double currentSolarDecimal = 0.0;
	private double _passiveIncome = 1.0;  // Rate multiplier

	private int _globalFunding = 0;  // Funding for global research upgrades
	public List<string> unlockedResearch = new List<string>();  // For easier access of which research nodes have been bought
	public List<string> lockedResearch = new List<string>();

	private double _timeElapsed = 0.0;  // real-time since game started, except pauses (minutes.fraction of minute)
	private double _gameTime = 0.0;  // in-game time since game started, except pauses (weeks.fraction of week > displayed in days, hours)
	private int _religionLevel = 1;  // 1: 1% cult followers = 0.2%/0.5%, 2: 1% = 0.5%/1%, 3: 1% = 2%/2%

	public double PassiveIncome {
		get => _passiveIncome;
		set {
			if (_passiveIncome != value) {
				_passiveIncome = value;
			}
		}
	}
	
	[Signal]
	public delegate void SolarChangedEventHandler(int newSolar);
	
	public int Solar { get => _solar;
		set {
			if (_solar != value) {
				_solar = value;
				EmitSignal(SignalName.SolarChanged, _solar);
			}
		}
	}
	
	public GameState() {
		if (GameManager.Instance.PrintDebug) GD.Print("Creating game state object...");
		if (GameManager.Instance.PrintDebug) GD.Print("Creating game storm tech tree...");
		stormTree = new TechTree(true);
		if (GameManager.Instance.PrintDebug) GD.Print("Creating game human AI tech tree...");
		humanTree = new TechTree(false);
		stormTree.viewNodes();
		humanTree.viewNodes();  

		sharedVars = new geoVars();
	}

	public void updateSharedVars() {
		int total;
		for (int i=0; i<sharedVars.vars.Length; i++) {
			total = stormTree.treeGeo.vars[i] + humanTree.treeGeo.vars[i];
			if (total != 200 && total != sharedVars.vars[i]) {
				sharedVars.vars[i] = 100 + (total - 200);
			}
		}

	}
}

public partial class GameManager : Node
{
	public static GameManager Instance => _instance;
	public bool PrintDebug => _printDebug;
	public GameState Game => _game;

	[Export]
	private bool _printDebug = false;

	[Export]
	private Json regionsJson;

	private static GameManager _instance = null;
	private GameState _game = null;
	private string currentScreen = "start_menu";
	private string currentOption = "";
	private string currentClick = "";
	private RegionAI[] regionAIs = null;
	// FIXME: where, when, and how to set and reset these variables in loop

	public override void _EnterTree() {
		if (_instance != null) {
			GD.PrintErr("WARNING: GameManager instance already exists!");  // debugging
		}
		_instance = this;
		GD.Print("GameManager entering tree");  // debugging
		_game = new GameState();
	}
	
	public override void _Ready()
	{
		var regionNames = (Godot.Collections.Array)((Godot.Collections.Dictionary) regionsJson.Data)["names"];
		regionAIs = new RegionAI[regionNames.Count-1];
		for (int i = 0; i < regionNames.Count-1; i++)
		{
			regionAIs[i] = new RegionAI(i+1);
		}
	}
	
	public override void _Process(double deltaTime)
	{
		// Update the humanity AIs
		for (int i = 0; i < regionAIs.Length; i++)
		{
			regionAIs[i].Process(deltaTime, _game);
		}
		
		// Passive income generation, rate changes by sea level
		Game.currentSolarDecimal += Game.PassiveIncome * (1 + (0.01 * Game.stormTree.treeWeather.sea_level)) * deltaTime;
		if (Game.currentSolarDecimal > 1.000) {
			Game.Solar += 1;
			Game.currentSolarDecimal = 0.0;
		}
	}

	public void ApplyDamage(int regionID, double damage, DamageType type)
	{
		if (regionID == 0)
		{
			// GD.Print("Cannot apply damage to region 0 (Ocean)");
			return;
		} else if (regionID < 0 || regionID > regionAIs.Length)
		{
			GD.PrintErr($"Invalid region ID: {regionID}");
			return;
		}
		
		regionAIs[regionID - 1].ApplyDamage(damage, type);
		if (PrintDebug) GD.Print($"Applying {damage} damage of type {type} to humanity AI in region {regionID}");
	}
	
	// Public region access function  
	public RegionAI GetRegionAI(int regionID)
	{
		if (regionID <= 0 || regionID > regionAIs.Length)
			return null;

		return regionAIs[regionID - 1];
	}

}
