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
using CsvHelper.Configuration.Attributes;

public class RegionStats
{
	[Name("ID")]
	public int ID { get; set; }

	[Name("Name")]
	public string Name { get; set; }

	[Name("Code")]
	public string Code { get; set; }

	[Name("Continent")]
	public string Continent { get; set; }

	[Name("Countries")]
	public string Countries { get; set; }

	[Name("Population")]
	public double Population { get; set; }

	[Name("Coastal Population")]
	public double CoastalPopulation { get; set; }

	[Name("Development Index")]
	public double DevelopmentIndex { get; set; }  // note the new header name here

	[Name("GDP")]
	public double GDP { get; set; }

	[Name("Minimum Elevation")]
	public int MinimumElevation { get; set; }

	[Name("Maximum Elevation")]
	public int MaximumElevation { get; set; }

	public List<string> GetCountries()
	{
		return Countries
			.Split(',')
			.Select(c => c.Trim())
			.ToList();
	}

	// FIXME: make print statement to make sure country geographic data is being parsed correctly
	public void printRegion() {
		List<string> countryList = GetCountries();
		string countryString = "";
		for (int i=0; i<countryList.Count; i++) {
			if (i==0) {
				countryString += countryList[i];
			} else {
				countryString += ", " + countryList[i];
			}
		}
		if (GameManager.Instance.PrintDebug) {
			GD.Print("\t> " + Name + " (" + ID + ", " + Code + ") " + Continent + ", (" + countryString + "): " + Population + ", " + CoastalPopulation + ", " + DevelopmentIndex + ", " + GDP + ", " + MinimumElevation + ", " + MaximumElevation);
		}
	}
}

public partial class RegionAI
// FIXME: list of region AI should be made and handled in the game state,
// not game manager, class
{
	enum ReactionState
	{
		Research,
		Savings,
		Recovery,
		Debauchery,
	}

	public record ActionType
	{
		public record Save() : ActionType;
		public record Research(TechNode node) : ActionType;
		public record Recover() : ActionType;
		public record Debauch() : ActionType;
	}

	public class Progress {	 // Dynamic variables
		public double monies;
		public double cultFollowers;

		// Damage state
		public double health;
		public double windDamage;
		public double floodDamage;
		public double secondaryDamage;

		public Progress() {
			monies = 0.0f; // Starting money
			cultFollowers = 0.0f;  // Starting no followers

			health = 1.0f; // Full health
			windDamage = 0.0f;  // No damage
			floodDamage = 0.0f;
			secondaryDamage = 0.0f;
		}
	}

	/*public class Stats {  // Fixed variables
		public string name;  // region's name
		public string code;  // 3-lettered unique code
		public List<string> countries = new List<string>();  // encompassing countries
		public int pop;  // in millions
		public double coastalPop;  // decimal
		public double devIndex;  // development index
		public double GDP;  // in billions of USD
		public int minElevation;  // in meters
		public int maxElevation;  // in meters

		// All values are (1-5) (least-most), except Transport stats (1-3)
		public int[] values = new int[11];
		public string[] codes = new string[11] {"RES", "CON", "GTP", "ATP", "STP", "INR", "PRE", 
			"GOV", "EDU", "CLI", "SUS"};
		public string[] statistics = new string[11] {"Resources Per Capita", "Connectivity", 
			"Ground Transport", "Air Transport", "Ship Transport", "International Relations", 
			"Disaster Preparation", "Government Function", "Education", "Climate Research", 
			"Storm Susceptibility"};

		var csvPath = "res://Assets/region_geographic_data.csv";

		public Stats(int id) {
			// FIXME: initiator based on id finds and pulls data from csv

		}

		private readCSV(int id) {

		}
	}*/

	// 
	private int _id;
	private ReactionState _state;
	private Progress _progress;
	// private Stats _stats;  // FIXME: if entertree region stats works, edit Stats object
	public RegionStats _regionStats;  // FIXME: make sure this works and rename
	public TechTree regionTree;

	public RegionAI(int id)
	{
		_id = id;
		_state = ReactionState.Savings; // Initial state
		_progress = new Progress();
		// _stats = new Stats(id);

		if (GameManager.Instance.PrintDebug) GD.Print("Creating region AI tech tree...");
		regionTree = new TechTree(false);
		regionTree.setDefaults();
	}

	public void setRegionStats(RegionStats stats, bool debug) {
		_regionStats = stats;
		if (debug) _regionStats.printRegion();
	}

	private ReactionState GetNextState()
	{
		var nextState = _state;
		switch (_state)
		{
			case ReactionState.Research:
				if (_progress.health < 0.5) // Hardcoded decision points, should be members later
				{
					nextState = ReactionState.Recovery; // Switch to recovery if health is low
				}
				else if (_progress.health > 0.9)
				{
					nextState = ReactionState.Savings; // Switch to savings if health is high
				}
				break;
			case ReactionState.Savings:
				if (_progress.health < 0.8)
				{
					nextState = ReactionState.Research; // Switch to research if we get damaged 
				}
				if (_progress.monies > 100.0)
				{
					nextState = ReactionState.Debauchery; // Switch to debauchery if money is high
				}
				break;
			case ReactionState.Recovery:
				if (_progress.health > 0.8 || _progress.monies == 0.0)
				{
					nextState = ReactionState.Savings; // Switch to savings if health is high
				}
				break;
			case ReactionState.Debauchery:
				if (_progress.monies < 50.0 || _progress.health < 0.5)
				{
					nextState = ReactionState.Savings; // Switch back to savings after debauchery 
				}
				break;
			default:
				return _state; // Fallback to current state if unknown
		}
		return nextState;
	}

	public void Process(double deltaTime, GameState gameState)
	{
		ActionType decision = Decide(gameState);
		switch (decision)
		{
			case ActionType.Save:
				// Small additional income from savings
				_progress.monies += 5.0 * 1.2 * deltaTime * _progress.health;
				break;
			case ActionType.Research(TechNode node):
				if (_progress.monies >= node.cost && gameState.humanTree.available.Contains(node))
				{
					_progress.monies -= node.cost; // Deduct cost of research
					gameState.humanTree.buyNode(node); // Perform the research
				}
				_progress.monies += 5.0 * deltaTime * _progress.health; // Passive income based on health
				break;
			case ActionType.Recover:
				var spending = Mathf.Min(5.0 * deltaTime, _progress.monies); // Spend up to 0.1 money per second)
				_progress.health += 0.01 * spending;
				_progress.monies -= spending; // Deduct the money spent on recovery
				break;
			case ActionType.Debauch:
				var debauchSpending = Mathf.Min(5.0 * deltaTime, _progress.monies); // Spend up to 0.1 money per second on luxuries
				_progress.monies -= debauchSpending; // Deduct the money spent on luxuries
				break;
			default:
				GD.PrintErr($"Unknown action type: {decision}");
				break;
		}

		// Apply passive income

		_state = GetNextState(); // Update state based on the current conditions
		if (_id == 1)
		{
			// GD.Print($"Russia - State: {_state}, Health: {_progress.health:F2}, Money: {_progress.monies:F2}");
		}
	}

	public ActionType Decide(GameState gameState)
	{
		switch (_state)
		{
			case ReactionState.Research:
				// Chooses a random available node to research
				var targetPurchase = gameState.humanTree.available[(int)(GD.Randi() % (uint)gameState.humanTree.available.Count)];
				if (targetPurchase.cost < _progress.monies)
				{
					return new ActionType.Research(targetPurchase);
				}
				return new ActionType.Save(); // If no affordable research, save money
			case ReactionState.Savings:
				// Basically no action, slightly increases income
				return new ActionType.Save();
			case ReactionState.Recovery:
				// Humanity spends money on recovering health
				return new ActionType.Recover();
			case ReactionState.Debauchery:
				// Humanity spends money on luxuries, no action taken
				return new ActionType.Debauch();
			default:
				GD.PrintErr($"Unknown state: {_state}");
				return new ActionType.Save(); // Fallback action
		}
	}

	public void ApplyDamage(double damage, DamageType type)
	{
		// TODO: Implement damage handling logic based on resistances
		switch (type)
		{
			case DamageType.Wind:
				_progress.windDamage += damage;
				_progress.health -= 0.1 * damage; // Wind damage reduces health
				break;
			case DamageType.Flood:
				_progress.floodDamage += damage;
				_progress.health -= 0.2 * damage; // Flood damage reduces health more
				break;
			case DamageType.Secondary:
				_progress.secondaryDamage += damage;
				_progress.health -= 0.05 * damage; // Secondary damage reduces health slightly
				break;
			default:
				GD.PrintErr($"Unknown damage type: {type}");
				break;
		}
		if (_progress.health < 0.0f) _progress.health = 0.0f; // Ensure health doesn't go below zero
	}
}
