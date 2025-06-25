using Godot;
using System;
using Godot.Collections;

public partial class GameState : Node {
	public TechTree stormTree;
	public TechTree humanityTree;
	public Variables globalStats;
	
	private int _solar = 1000;
	public double currentSolarDecimal = 0.0;

	private double _passiveIncome = 1.0;  // Rate multiplier
	private double _migrationRate = 1.0;  // Percent
	private double _detectionTime = 4.0;  // Days
	private int _pathPrediction = 0;
	// 0: 50% chance regions correctly predict path, 1 = 75% chance, 2 = 100% chance
	private double _globalWarming = 1.0;  // Decimal multiplier against tech tree climate change
	private double _cultSpreadSpeed = 1.0;
	private double _warCosts = 1.0;
	private double _warSpreadSpeed = 1.0;
	private double _climateResearchCosts = 1.0;

	private int _globalFunding = 0;  // Funding for global research upgrades

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
		stormTree = new TechTree();
		humanityTree = new TechTree();
		stormTree.viewNodes();

		globalStats = new Variables();
		globalStats.setGlobalDefault();
		}

	public void updateGlobalStats() {
		// FIXME: need to call this every time storm/AI node updated
		int[] default_values = {15, 0, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100};
		int temp;
		for (int i=0; i<default_values.Length; i++) {
			temp = default_values[i] + stormTree.stormStats.vars[i];
			// FIXME: add effects of human AI tree
			if (globalStats.vars[i] != temp) {
				globalStats.vars[i] = temp;
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
	private GameState _game;
	private string currentScreen = "start_menu";
	private string currentOption = "";
	private string currentClick = "";
	private RegionAI[] regionAIs = null;
	// FIXME: where, when, and how to set and reset these variables in loop

	public override void _EnterTree() {
		// FIXME: get this to work
		_instance = this;
		_game = new GameState();
	}
	
	public override void _Ready()
	{
		//_instance = this;
		//_game = new GameState();
		var regionNames = (Godot.Collections.Array)((Dictionary) regionsJson.Data)["names"];
		regionAIs = new RegionAI[regionNames.Count-1];
		for (int i = 0; i < regionNames.Count-1; i++)
		{
			regionAIs[i] = new RegionAI(i+1);
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

	public override void _Process(double deltaTime)
	{
		// Update the humanity AIs
		for (int i = 0; i < regionAIs.Length; i++)
		{
			regionAIs[i].Process(deltaTime, _game);
		}
		
		// Passive income generation, rate changes by sea level
		Game.currentSolarDecimal += Game.PassiveIncome * (1 + (0.01 * Game.globalStats.sea_level)) * deltaTime;
		if (Game.currentSolarDecimal > 1.000) {
			Game.Solar += 1;
			Game.currentSolarDecimal = 0.0;
		}
	}

	/*public override void _Process() {
		
		// FIXME: determine current screen
		switch (currentScreen) {
			case "main_menu":
				switch(currentOption) {
					case "start_game":
						// FIXME: start la game
						currentScreen = "game";
						break;
					case "quit_game":
						// FIXME: quit la game
						break;
					default:
						break;
				}
				break;
			case "tech_tree":
				// FIXME: get input for options
				switch (currentOption) {
					case "view_available":
						game.stormTree.viewNodes();
						break;
					case "buy_node":
						// FIXME: get input for node to buy
						string search = "";
						TechNode to_buy = game.stormTree.getNode(search);

						if (to_buy != null) {
							if (game.solar >= to_buy.cost) {
								game.stormTree.buyNode(to_buy);
								solar -= to_buy.cost;
							}
							// FIXME: else - cannot buy node message
						}
						break;
					case "return_to_game":
						currentScreen = "game";
						break;
					default:
						break;
				}
				// FIXME: options to return to main menu/in-game screen
				break;
			case "game":
				// FIXME: get input for options
				switch (currentOption) {
					case "spawn_storm":
						// FIXME: mechanism and UI to spawn storms
						break;
					case "view_stats":
						// FIXME: UI screen to see variable stats of world
						break;
					case "open_tech_tree":
						currentScreen = "tech_tree";
						break;
					case "pause":
						// FIXME: pauses game and opens mini menu with options
						// to return to main menu or quit game
						switch (currentClick) {
							case "return_menu":
								// FIXME: ends current game
								currentScreen = "main_menu";
								
						}
				}

		}
	}*/
}
