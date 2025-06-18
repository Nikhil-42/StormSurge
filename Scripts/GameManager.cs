using Godot;
using System;

public class GameState {
	
	public StormTechTree stormTree;
	
	public GameState() {
		GD.Print("Creating game state object...");
		stormTree = new StormTechTree();
		stormTree.viewNodes();
	}
}

public partial class GameManager : Node
{
	public override void _Ready() {
		GameState game = new GameState();
	}
}
