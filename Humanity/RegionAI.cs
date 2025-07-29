using Godot;
using System;
using System.Collections.Generic;

public partial class RegionAI
{
	enum ReactionState
	{
		Research,
		Savings,
		Recovery,
		Debauchery,
		Death,
	}

	public record ActionType
	{
		public record Save() : ActionType;
		public record Research(TechNode<RegionVars> node) : ActionType;
		public record Recover() : ActionType;
		public record Debauch() : ActionType;
		public record Death() : ActionType;
	}

	public class Progress {	 // Dynamic variables
		public float monies;
		public float cultFollowers;

		// Damage state
		public float health;
		public float windDamage;
		public float floodDamage;
		public float secondaryDamage;

		public Progress() {
			monies = 0; // Starting money
			cultFollowers = 0.0f;  // Starting no followers

			health = 1.0f; // Full health
			windDamage = 0.0f;  // No damage
			floodDamage = 0.0f;
			secondaryDamage = 0.0f;
		}
	}

	public RegionStats regionStats;
	public Characteristics chars;

	private ReactionState _state;
	private Progress _progress;

	public TechTree<TechNode<RegionVars>, RegionVars> regionTree;

	private float _cultFollowers = 0;  // as % of population

	// Public getters for accessing region data (turn into getters/setters)
	public int Id => regionStats.id;
	public string Name => regionStats.name;
	public float Health => _progress.health;
	public string State => _state.ToString();
	public bool Alive => _progress.health > 0.0 && _state != ReactionState.Death;
	public float WindDamage => _progress.windDamage;
	public float FloodDamage => _progress.floodDamage;
	public float SecondaryDamage => _progress.secondaryDamage;
	public float Money => _progress.monies;
	public float GDP => regionStats.gdp;
	public float Population => regionStats.population;

	public RegionAI(RegionStats regionStats, TechTree<TechNode<RegionVars>, RegionVars> regionTree)
	{
		_state = ReactionState.Savings; // Initial state
		_progress = new Progress();
		this.regionStats = regionStats;
		chars = new Characteristics(regionStats);

		if (GameManager.Instance.PrintDebug) GD.Print("Creating region AI tech tree...");
		this.regionTree = regionTree;
	}

	private ReactionState GetNextState()
	{
		var nextState = _state;
		if (_state == ReactionState.Death || _progress.health <= 0.0) {
			nextState = ReactionState.Death;
		} else {
			switch (_state)
			{
			// FIXME: should have cooldown before switching states again
			// FIXME: when health is not full but above cutoff for poor/mid health,
			// state should depend on how much money region has
				case ReactionState.Research:
					if (_progress.health < chars.poorHealth) // Hardcoded decision points, should be members later
					{
						nextState = ReactionState.Recovery; // Switch to recovery if health is low
					}
					else if (_progress.health > chars.goodHealth)
					{
						nextState = ReactionState.Savings; // Switch to savings if health is high
					}
					break;
				case ReactionState.Savings:
					if (_progress.health < chars.midHealth)
					{
						nextState = ReactionState.Research; // Switch to research if we get damaged 
					}
					if (_progress.monies > chars.goodMoney)
					{
						nextState = ReactionState.Debauchery; // Switch to debauchery if money is high
					}
					break;
				case ReactionState.Recovery:
					if (_progress.health > chars.goodHealth || _progress.monies == 0.0)
					{
						nextState = ReactionState.Savings; // Switch to savings if health is high
					}
					break;
				case ReactionState.Debauchery:
					if (_progress.monies < chars.midMoney || _progress.health < chars.midHealth)
					{
						nextState = ReactionState.Savings; // Switch back to savings after debauchery 
					}
					break;
				default:
					return _state; // Fallback to current state if unknown
			}
		}
		return nextState;
	}

	public void Process(double deltaTime, GameState gameState)
	{
		float currentIncome = (float)(chars.income * deltaTime * _progress.health);

		ActionType decision = Decide(gameState);
		switch (decision)
		{
			case ActionType.Save:
				// Small additional income from savings
				_progress.monies += 1.1f * currentIncome;
				break;
			case ActionType.Research(TechNode<RegionVars> node):
				if (_progress.monies >= node.Cost)
				{
					regionTree.BuyNode(node, ref _progress.monies); // Buy node if affordable
					GD.Print(regionStats.name + " purchased " + node.Name);
					
					if (GD.Randf() <= 0.04f) // 4% chance of notifying
					{
						var message = GameManager.Instance.NotificationManager?.GetMessage(node.Name);
						if (!string.IsNullOrEmpty(message))
						{
							string fullMessage = $"{regionStats.name}: {message}";
							GameManager.Instance.UI?.Notify(fullMessage);
						}
					}
				}
				_progress.monies += currentIncome; // Passive income based on health
				break;
			case ActionType.Recover:
				var spending = Mathf.Min(currentIncome, _progress.monies); // Spend up to 0.1 money per second)
				_progress.health += 0.01f * spending;
				_progress.monies -= spending; // Deduct the money spent on recovery
				break;
			case ActionType.Debauch:
				var debauchSpending = Mathf.Min(currentIncome, _progress.monies); // Spend up to 0.1 money per second on luxuries
				_progress.monies -= debauchSpending; // Deduct the money spent on luxuries
				break;
			case ActionType.Death:  // cannot undie
				_progress.health = 0.0f; // Set health to zero
				_progress.monies = 0.0f; // Reset money
				break;
			default:
				GD.PrintErr($"Unknown action type: {decision}");
				break;
		}

		// Apply passive income

		_state = GetNextState(); // Update state based on the current conditions
	}

	public ActionType Decide(GameState gameState)
	{
		switch (_state)
		{
			case ReactionState.Research:
				// Chooses a random available node to research
				// FIXME: Should have cooldown before buying another node
				GD.Print(regionStats.name + " searching for node to research...");
				string targetNode = TargetNextNode();
				var targetPurchase = regionTree.GetNode(targetNode);
				// var availableNodes = regionTree.Available;
				// var targetPurchase = availableNodes[(int)(GD.Randi() % (uint)availableNodes.Count)];
				if (targetPurchase.Cost <= _progress.monies)
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
			case ReactionState.Death:
				return new ActionType.Death();
			default:
				GD.PrintErr($"Unknown state: {_state}");
				return new ActionType.Save(); // Fallback action
		}
	}

	public void ApplyDamage(float damage, DamageType type)
	{
		// TODO: Implement damage handling logic based on resistances
		switch (type)
		{
			case DamageType.Wind:
				_progress.windDamage += damage * chars.windDamageMultiplier;
				_progress.health -= 0.1f * (damage * chars.windDamageMultiplier); // Wind damage reduces health
				break;
			case DamageType.Flood:
				_progress.floodDamage += damage * chars.floodDamageMultiplier;
				_progress.health -= 0.2f * (damage * chars.floodDamageMultiplier); // Flood damage reduces health more
				break;
			case DamageType.Secondary:
				_progress.secondaryDamage += damage * chars.secondaryDamageMultiplier;
				_progress.health -= 0.05f * (damage * chars.secondaryDamageMultiplier); // Secondary damage reduces health slightly
				break;
			default:
				GD.PrintErr($"Unknown damage type: {type}");
				break;
		}
		if (_progress.health < 0.0f) _progress.health = 0.0f; // Ensure health doesn't go below zero
	}

	public string TargetNextNode() {
		// Random proportional chance choose next node based on value:cost ratio
		List<string> nodeNames = new List<string>();
		List<float> nodeValues = new List<float>();

		float currentValue = 0.0f;
		float totalValue = 0.0f;

		foreach (var node in regionTree.Available) {
			nodeNames.Add(node.Name);
			currentValue = EvaluateNode(node.Name);
			nodeValues.Add(currentValue);
			totalValue += currentValue;
		}

		List<int> probabilities = new List<int>();
		int currentCutoff = 0;
		int currentChance = 0;

		for (int i=0; i<nodeNames.Count; i++) {
			// FIXME: Square(?) to increase chance better nodes will be selected
			// float nodeCost = regionTree.GetNode(nodeNames[i]).Cost;
			// currentChance = (int)Math.Round((nodeValues[i] * 100) / (nodeCost/10));
			currentChance = (int)Math.Round((nodeValues[i]));
			probabilities.Add(currentCutoff + currentChance);
			currentCutoff += currentChance;
		}
		if (currentCutoff <= 0) {
			return null;
		}

		// Random number generate 1-currentCutoff
		GameManager.Instance.Game.RNG.Randomize();
		int randomNumber = GameManager.Instance.Game.RNG.RandiRange(1, currentCutoff+1);

		for (int i=0; i<nodeNames.Count; i++) {
			if (randomNumber <= probabilities[i]) {
				GD.Print("TARGET NODE (" + regionStats.name + "): " + nodeNames[i] + ", Value: " + (int)Mathf.Round(nodeValues[i]));
				// FIXME: add print debug statement for region's next target node
				return nodeNames[i];
			}
		}
		return null;  // FIXME: need to handle error upstream
	}

	public float EvaluateNode(string nodeName) {
		var node = regionTree.GetNode(nodeName);
		RegionVars vars = (RegionVars)node.Vars;

		StormVars stormVars = GameManager.Instance.Game.stormTree.Vars.Storm;
		GeopoliticalVars geoVars = GameManager.Instance.Game.stormTree.Vars.Geopolitical;

		float value = 0.0f;

		// FIXME: Add in effects of region's values, for now it is just global value

		// ================================= STORM VARIABLES ==================================================
		// Wind resistance [0]
		// INCREASE: current wind damage in region, higher wind speeds, higher wind damage
		if (vars.Storm.WindDamage != 0.0f) {
			float windValue = Math.Abs(vars.Storm.WindDamage * 10);
			if (WindDamage > 0) {
				windValue *= (1+WindDamage);
			}
			windValue *= (stormVars.WindSpeed + regionTree.Vars.Storm.WindSpeed)/100.0f;
			windValue *= stormVars.WindDamage + regionTree.Vars.Storm.WindDamage;

			value += windValue;
		}

		// Flood resistance [1]
		// INCREASE: sustained flood damage, higher sea level (TBA), coastal population
		if (vars.Storm.FloodDamage != 0.0f) {
			float floodValue = Math.Abs(vars.Storm.FloodDamage * 10);
			if (FloodDamage > 0) {
				floodValue *= (1+FloodDamage);
			}
			floodValue *= stormVars.FloodDamage + regionTree.Vars.Storm.FloodDamage;
			floodValue *= (1+regionStats.coastalPopulation);
			// floodValue *= stormVars.SeaLevel;  // FIXME: later

			value += floodValue;
		}

		// ================================= GEOPOLITICAL VARIABLES ==================================================
		// Communications [2]
		if (vars.Geopolitical.Communications != 0.0f) {
			float communicationValue = Math.Abs(vars.Geopolitical.Communications * 10);
			communicationValue *= geoVars.Communications + regionTree.Vars.Geopolitical.Communications;

			value += communicationValue;
		}

		// International Cooperation [3]
		// DECREASE: global war presence (TBA), global cult presence (TBA)
		if (vars.Geopolitical.InternationalCooperation != 0.0f) {
			float internationalCooperationValue = Math.Abs(vars.Geopolitical.InternationalCooperation * 10);
			internationalCooperationValue *= Math.Abs(geoVars.InternationalCooperation);

			value += internationalCooperationValue;
		}

		// Transportation [4]
		// INCREASE: high gov't function (TBA)
		if (vars.Geopolitical.Transportation != 0.0f) {
			float transportationValue = Math.Abs(vars.Geopolitical.Transportation * 10);
			transportationValue *= Math.Abs(geoVars.Transportation);

			value += transportationValue;
		}

		// Resources [5]
		// INCREASE: lower gdp
		// DECREASE: higher gdp, further along tech tree (TBA)
		if (vars.Geopolitical.Resources != 0.0f) {
			float resourcesValue = Math.Abs(vars.Geopolitical.Resources * 10);
			resourcesValue *= Math.Abs(geoVars.Resources);

			if (regionStats.gdp > 10000) resourcesValue *= 0.5f;
			else if (regionStats.gdp > 1000) resourcesValue *= 0.9f;
			else if (regionStats.gdp > 500) resourcesValue *= 1.3f;
			else if (regionStats.gdp > 100) resourcesValue *= 1.6f;
			else resourcesValue *= 2.0f;

			value += resourcesValue;
		}

		// Compliance [6]
		// INCREASE: global war presence (TBA), global cult presence (TBA)
		if (vars.Geopolitical.Compliance != 0.0f) {
			float complianceValue = Math.Abs(vars.Geopolitical.Compliance * 10);
			complianceValue *= Math.Abs(geoVars.Compliance);

			value += complianceValue;
		}

		// Preparation [7]
		// INCREASE: number of prior storms in region (TBA), current damage in region
		if (vars.Geopolitical.Preparation != 0.0f) {
			float preparationValue = Math.Abs(vars.Geopolitical.Preparation * 10);
			preparationValue *= Math.Abs(geoVars.Preparation);
			preparationValue *= (1 + ((FloodDamage + WindDamage + SecondaryDamage) / 2));

			value += preparationValue;
		}

		// ================================= HUMAN AI VARIABLES ==================================================
		// Global Migration (N/A), Regional Migration (N/A)

		// Global Warming [2]
		// INCREASE: higher global temperature
		if (vars.GlobalWarming != 0.0f) {
			float globalWarmingValue = Math.Abs(vars.GlobalWarming * 10);
			globalWarmingValue *= Math.Abs(stormVars.Temperature);

			value += globalWarmingValue;
		}

		// Climate Costs (N/A)

		// Cult Spread [4]
		// INCREASE: cult presence (TBA), current cult population in country (TBA)
		if (vars.CultSpread != 0.0f) {
			float cultSpreadValue = Math.Abs(vars.CultSpread * 10);

			value += cultSpreadValue;
		}

		// Recovery [5]
		// INCREASE: current storm damage, number of prior storms in region (TBA)
		if (vars.Recovery != 0.0f) {
			float recoveryValue = Math.Abs(vars.Recovery * 10);
			recoveryValue *= (1 + (FloodDamage + WindDamage + SecondaryDamage));

			value += recoveryValue;
		}

		// Infrastructure Costs (N/A), War Spread (N/A), Detection (N/A), Implementation Costs (N/A)

		// Balance for Cost
		value = (value * 100) / node.Cost;

		GD.Print("\t>NODE EVAL (" + nodeName + "): " + (int)Mathf.Round(value));

		return value;
	}
}
