using System.Data.Common;
using Godot;
using Godot.Collections;

public class GameState {
	public TechTree stormTree;
	public TechTree humanityTree;
	public Variables globalStats;
	
	public int solar = 0;
	public float passiveIncome = 1.0f;
	
	public GameState() {
		if (GameManager.Instance.PrintDebug) GD.Print("Creating game state object...");
		stormTree = new TechTree();
		humanityTree = new TechTree();
		stormTree.viewNodes();

		globalStats = new Variables();
		globalStats.setGlobalDefault();
		
		solar = 0;
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

	public override void _Ready()
	{
		_instance = this;
		_game = new GameState();
		var regionNames = (Array)((Dictionary) regionsJson.Data)["names"];
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
