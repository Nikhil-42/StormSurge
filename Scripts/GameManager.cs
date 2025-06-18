using Godot;
using System;

public class GameState {
	
	public StormTechTree stormTree;
	public Variables globalStats;
	
	public int solar = 0;
	public float passive_income = 1.0f;
	
	public GameState() {
		GD.Print("Creating game state object...");
		stormTree = new StormTechTree();
		stormTree.viewNodes();
		globalStats = new Variables();
		globalStats.set_default();
		
		solar = 0;
	}
}

public partial class GameManager : Node
{
	GameState game;
	string currentScreen = "start_menu";
	string currentOption = "";

	public override void _Ready() {
		game = new GameState();
	}

	/*public override void _Process() {
		// FIXME: determine current screen
		switch (currentScreen) {
			case "main_menu":
				switch(currentOption) {
					case "start_game":
						// FIXME: start la game
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
								game.stormTree.unlockNode(to_buy);
								solar -= to_buy.cost;
							}
						}
						break;
					default:
						break;
				}
				// FIXME: options to return to main menu/in-game screen
				break;

		}
	}*/
}
