using System.Collections.Generic;
using System.Linq;
using Godot;

public class GameState {  // Move node attributes, make not a node, make private
	// ================= GLOBAL TECH TREE VARIABLES ================================
	public TechTree<TechNode<GlobalVars>, GlobalVars> stormTree = null;  // global default
	public TechTree<GlobalNode, GlobalVars> humanityTree = null;  // global default
	
	public float PercentCompletion => (float)humanityTree.GetAllNodes().Count(node => node.Purchased) / humanityTree.GetAllNodes().Count();
	
	public GlobalVars GlobalVars => stormTree.Vars + humanityTree.Vars;

	private int _religionLevel = 1;  // 1: 1% cult followers = 0.2%/0.5%, 2: 1% = 0.5%/1%, 3: 1% = 2%/2%
	private RegionAI[] _regionAIs = null;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	// ================= GLOBAL MONEY & RESEARCH VARIABLES ================================
	private float _solar = 2000.0f;
	private float _passiveIncome = 1.0f;  // Rate multiplier

	private float _globalFunding = 0.0f;  // Funding for global research upgrades, FIXME: need to find reasonable rate
	public List<string> unlockedResearch = [];  // For easier access of which research nodes have been bought
	public List<string> lockedResearch = [];

	// ================= GLOBAL TIME VARIABLES ================================
	private float _startTime;
	private float _timeElapsed = 0.0f;  // milliseconds

	// ================= GLOBAL GETTERS & SETTERS ================================
	public float PassiveIncome { get => _passiveIncome; set => _passiveIncome = value; }
	public float GlobalFunding { get => _globalFunding; set => _globalFunding = value; }
	public float Solar { get => _solar; set => _solar = value; }
	public RegionAI[] RegionAIs { get => _regionAIs; }
	public double TimeElapsed { get => _timeElapsed; }
	public RandomNumberGenerator RNG { get => _rng; }
	
	// ================= INITIALIZER ================================
	public GameState(RegionStats[] regionStats, Json stormJson, Json humanityJson, Json regionJson) {
		if (GameManager.Instance.PrintDebug) {
			GD.Print("Creating game state object...");
			GD.Print("Creating game storm and human tech tree...");
		}

		stormTree = new(stormJson.Data);
		humanityTree = new(humanityJson.Data);

		if (GameManager.Instance.PrintDebug) {
			GD.Print("Loading storm and human tech tree from JSON...");
		}

		if (GameManager.Instance.PrintDebug)
		{
			stormTree.PrintNodes();  // debug, no UI
			humanityTree.PrintNodes();  // debug, no UI
		}

		stormTree.UpdatePrerequisites(null);
		humanityTree.UpdatePrerequisites(null);

		Dictionary<string, ITechNode> externalNodes = new();
		foreach (var node in stormTree.GetAllNodes())
		{
			externalNodes[node.Name] = node;
		}
		foreach (var node in humanityTree.GetAllNodes())
		{
			externalNodes[node.Name] = node;
		}

		if (GameManager.Instance.PrintDebug) GD.Print("\nCreating Region AI objects...");

		_regionAIs = new RegionAI[regionStats.Length];
		for (int i = 0; i < regionStats.Length; i++) {
			var regionTree = new TechTree<TechNode<RegionVars>, RegionVars>(regionJson.Data);
			regionTree.UpdatePrerequisites(externalNodes);
			_regionAIs[i] = new RegionAI(regionStats[i], regionTree);
			GD.Print(i, "Name: ", regionStats[i].name, ", ID: ", _regionAIs[i].Id);
			if (GameManager.Instance.PrintDebug) _regionAIs[i].regionStats.printRegion();
		}

		_startTime = Time.GetTicksMsec();
	}

	public void spendSolar(int amount) {
		Solar -= amount;
	}

	public void spendGlobalFunding(int amount) {
		GlobalFunding -= amount;
	}

	public void updateTime() {
		_timeElapsed = Time.GetTicksMsec() - _startTime;
	}
}
