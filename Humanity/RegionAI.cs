using Godot;

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
				var availableNodes = regionTree.Available;
				var targetPurchase = availableNodes[(int)(GD.Randi() % (uint)availableNodes.Count)];
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
		// FIXME: Create structure to track values of nodes
		// FIXME: Random proportional chance choose next node based on value:cost ratio
		// FIXME: Top 2 nodes have additional chance as well

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
		float floatTotalCutoff = 0.0f;
		int currentCutoff = 0;
		int currentChance = 0;

		for (int i=0; i<nodeNames.Count; i++) {
			probabilities 
		}



		return currentTarget;
	}

	public float EvaluateNode(string nodeName) {
		var node = regionTree.GetNode(nodeName);
		float value;
		float totalValue;

		// ================================= STORM VARIABLES ==================================================
		// Wind resistance [0]
		// INCREASE: current wind damage in region, higher wind speeds, higher wind damage
		if (node.Storm.WindDamage != 0.0f) {
			float windValue = Math.Abs(node.Storm.WindDamage);
			if (WindDamage > 0) {
				windValue *= (1+WindDamage);
			}
			windValue *= GameManager.Instance.Game.stormTree.WindSpeed/100.0f;
			windValue *= GameManager.Instance.Game.stormTree.WindDamage;

			value += windValue;
		}

		// Flood resistance [1]
		// INCREASE: sustained flood damage, higher sea level (TBA), coastal population
		if (node.Storm.FloodDamage != 0.0f) {
			float floodValue = Math.Abs(node.Storm.FloodDamage);
			if (FloodDamage > 0) {
				floodValue *= (1+FloodDamage);
			}
			floodValue *= GameManager.Instance.Game.stormTree.FloodDamage;
			floodValue *= (1+regionStats.coastalPopulation);
			// floodValue *= GameManager.Instance.Game.stormTree.SeaLevel;  // FIXME: later

			value += floodValue;
		}

		// ================================= GEOPOLITICAL VARIABLES ==================================================
		// Communications [2]
		if (node.Geopolitical.Communications != 0.0f) {
			float communicationValue = Math.Abs(node.Geopolitical.Communications);
			communicationValue *= Math.Abs(GameManager.Instance.Game.stormTree.Communications);

			value += communicationValue;
		}

		// International Cooperation [3]
		// DECREASE: global war presence (TBA), global cult presence (TBA)
		if (node.Geopolitical.InternationalCooperation != 0.0f) {
			float internationalCooperationValue = Math.Abs(node.Geopolitical.InternationalCooperation);
			internationalCooperationValue *= Math.Abs(GameManager.Instance.Game.stormTree.InternationalCooperation);

			value += internationalCooperationValue;
		}

		// Transportation [4]
		// INCREASE: high gov't function (TBA)
		if (node.Geopolitical.Transportation != 0.0f) {
			float transportationValue = Math.Abs(node.Geopolitical.Transportation);
			transportationValue *= Math.Abs(GameManager.Instance.Game.stormTree.Transportation);

			value += transportationValue;
		}

		// Resources [5]
		// INCREASE: lower gdp
		// DECREASE: higher gdp, further along tech tree (TBA)
		if (node.Geopolitical.Resources != 0.0f) {
			float resourcesValue = Math.Abs(node.Geopolitical.Resources);
			resourcesValue *= Math.Abs(GameManager.Instance.Game.stormTree.Resources);

			if (regionStats.gdp > 10000) resourcesValue *= 0.5f;
			else if (regionStats.gdp > 1000) resourcesValue *= 0.9f;
			else if (regionStats.gdp > 500) resourcesValue *= 1.3f;
			else if (regionStats.gdp > 100) resourcesValue *= 1.6f;
			else resourcesValue *= 2.0f;

			value += resourcesValue;
		}

		// Compliance [6]
		// INCREASE: global war presence (TBA), global cult presence (TBA)
		if (node.Geopolitical.Compliance != 0.0f) {
			float complianceValue = Math.Abs(node.Geopolitical.Compliance);
			complianceValue *= Math.Abs(GameManager.Instance.Game.stormTree.Compliance);

			value += complianceValue;
		}

		// Preparation [7]
		// INCREASE: number of prior storms in region (TBA), current damage in region
		if (node.Geopolitical.Preparation != 0.0f) {
			float preparationValue = Math.Abs(node.Geopolitical.Preparation);
			preparationValue *= Math.Abs(GameManager.Instance.Game.stormTree.Preparation);
			preparationValue *= (1+((FloodDamage+WindDamage+SecondaryDamage)/2));

			value += preparationValue;
		}

		// ================================= HUMAN AI VARIABLES ==================================================
		// Global Migration (N/A)
		// Regional Migration (N/A)

		// Global Warming [2]
		// INCREASE: higher global temperature
		if (node.RegionVars.GlobalWarming != 0.0f) {
			float globalWarmingValue = Math.Abs(node.RegionVars.GlobalWarming);
			globalWarmingValue *= Math.Abs(GameManager.Instance.Game.stormTree.Temperature);

			value += globalWarmingValue;
		}

		// Climate Costs (N/A)

		// Cult Spread [4]
		// INCREASE: cult presence (TBA), current cult population in country (TBA)
		if (node.RegionVars.CultSpread != 0.0f) {
			float cultSpreadValue = Math.Abs(node.RegionVars.CultSpread);

			value += cultSpreadValue;
		}

		// Recovery [5]
		// INCREASE: current storm damage, number of prior storms in region (TBA)
		if (node.RegionVars.Recovery != 0.0f) {
			float recoveryValue = Math.Abs(node.RegionVars.Recovery);
			recoveryValue *= (1+(FloodDamage+WindDamage+SecondaryDamage));

			value += recoveryValue;
		}

		// Infrastructure Costs (N/A)
		// War Spread (N/A)
		// Detection (N/A)
		// Implementation Costs (N/A)

		return value;
	}
}
