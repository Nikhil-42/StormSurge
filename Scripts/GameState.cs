using System.Collections.Generic;
using Godot;

public class GameState {  // Move node attributes, make not a node, make private
	// ================= GLOBAL TECH TREE VARIABLES ================================
	public TechTree stormTree = null;  // global default
	public TechTree humanTree = null;  // global default

	public weatherVars sharedWeatherVars = null;  // global default
	public geoVars sharedGeoVars = null;  // global default

	private int _religionLevel = 1;  // 1: 1% cult followers = 0.2%/0.5%, 2: 1% = 0.5%/1%, 3: 1% = 2%/2%
	
	private RegionAI[] _regionAIs = null;

	// ================= GLOBAL MONEY & RESEARCH VARIABLES ================================
	private double _solar = 0.0;
	private double _passiveIncome = 1.0;  // Rate multiplier

	private int _globalFunding = 0;  // Funding for global research upgrades, FIXME: need to find reasonable rate
	public List<string> unlockedResearch = [];  // For easier access of which research nodes have been bought
	public List<string> lockedResearch = [];

	// ================= GLOBAL TIME VARIABLES ================================
	private double _timeElapsed = 0.0;  // real-time since game started, except pauses (minutes.fraction of minute)
	private double _gameTime = 0.0;  // in-game time since game started, except pauses (weeks.fraction of week > displayed in days, hours)

	// ================= GLOBAL GETTERS & SETTERS ================================
	public double PassiveIncome { get => _passiveIncome; set => _passiveIncome = value; }
	public int GlobalFunding { get => _globalFunding; set => _globalFunding = value; }
	public double Solar { get => _solar; set => _solar = value; }
	public RegionAI[] RegionAIs { get => _regionAIs; }
	
	// ================= INITIALIZER ================================
	public GameState(RegionStats[] regionStats) {
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
		
		if (GameManager.Instance.PrintDebug) GD.Print("\nCreating Region AI objects...");

		_regionAIs = new RegionAI[regionStats.Length];
		for (int i = 0; i < regionStats.Length; i++) {
			_regionAIs[i] = new RegionAI(regionStats[i]);
			GD.Print(i, "Name: ", regionStats[i].name, ", ID: ", _regionAIs[i].Id);
			if (GameManager.Instance.PrintDebug) _regionAIs[i].regionStats.printRegion();
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