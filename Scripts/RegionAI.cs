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
		public record Research(TechNode node) : ActionType;
		public record Recover() : ActionType;
		public record Debauch() : ActionType;
		public record Death() : ActionType;
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

	public class Characteristics {  // Variables different for each country that are calculated based on stats
		// FIXME: Characteristics should be based on more detailed statistics for countries (storm susceptibility, etc.), but
		// for now everything based on GDP or development pretty much

		// MONIE MATTERS
		public double income;  // Region usable income based on GDP and population
		public double globalResearchFunding;  // % usual contribution of income to global research

		// DAMAGE THRESHOLDS
		public double goodHealth;  // Switch to savings/debauchery/research
		public double midHealth;  // Switch to recovery/research
		public double poorHealth;  // Switch to recovery

		public double goodMoney;  // (% of GDP) Switch from savings to debauchery, full research investment (rarely happens for poorest regions)
		public double midMoney;  // (% of GDP) Savings, partial research investment
		public double poorMoney;  // (% of GDP) Savings, no research investment

		public double lowAlarmThreshold;  // (% of Total Damage) No debauchery, fund research (75% income), upgrades (25% income) instead
		public double highAlarmThreshold;  // (% of Total Damage) Upgrades (75% income), and research (25% income)

		// DAMAGE RATE
		public double windDamageMultiplier;  // Base 1.0, affected by infrastructure and preparedness
		public double floodDamageMultiplier;  // Base 1.0, highly affected by coastal population
		public double secondaryDamageMultiplier;  // Base 1.0, highly affected by development index

		// POLITICAL, INFRASTRUCTURE
		public double governmentEfficiency;  // Base 1.0 multiplier
		public double emissions;  // Greenhouse gas emissions based on GDP
		public double internationalRelations;  // Base 1.0 multiplier, likelihood to join/form alliances
		public double education;  // Base 1.0 multiplier, affects preparation and speed of cult spread
		public double buildingInfrastructure;  // Base 1.0 multiplier, general quality of architecture in region (based on GDP for now)
		public double stormPreparedness;  // Base 1.0 multiplier for countries that regularly experience storms normally (based on GDP + coastal pop for now)

		public Characteristics(RegionStats stats) {
			// income, globalResearchFunding, Money's, emissions
			double PerCapitaGDP = (stats.gdp * 1000) / stats.population;
			if (PerCapitaGDP > 50000) {
				income = stats.gdp * 0.6;
				globalResearchFunding = 0.5;

				goodMoney = 0.9;
				midMoney = 0.75;
				poorMoney = 0.5;
			} else if (PerCapitaGDP > 20000) {
				income = stats.gdp * 0.4;
				globalResearchFunding = 0.2;

				goodMoney = 0.8;
				midMoney = 0.6;
				poorMoney = 0.3;
			} else if (PerCapitaGDP > 10000) {
				income = stats.gdp * 0.3;
				globalResearchFunding = 0.1;

				goodMoney = 0.7;
				midMoney = 0.5;
				poorMoney = 0.2;
			} else {
				income = stats.gdp * 0.2;
				globalResearchFunding = 0.05;

				goodMoney = 0.6;
				midMoney = 0.4;
				poorMoney = 0.2;
			}
			emissions = stats.gdp / 1000;

			// Health's, alarm thresholds, damage multipliers, etc.
			goodHealth = 0.8 + (0.2 * stats.developmentIndex);
			midHealth = 0.6 + (0.4 * stats.developmentIndex);
			poorHealth = 0.4 + (0.6 * stats.developmentIndex);

			lowAlarmThreshold = 0.9 + (0.1 * stats.developmentIndex);
			highAlarmThreshold = 0.75 + (0.25 * stats.developmentIndex);

			windDamageMultiplier = 1 + ((1 - stats.developmentIndex)/2);
			floodDamageMultiplier = 1 + stats.coastalPopulation;
			secondaryDamageMultiplier = 1 + (1 - stats.developmentIndex);

			governmentEfficiency = 0.5 + stats.developmentIndex;
			internationalRelations = 0.5 + stats.developmentIndex;
			education = 0.5 + stats.developmentIndex;
			buildingInfrastructure = 0.5 + stats.developmentIndex;

			stormPreparedness = 0.5 + (stats.developmentIndex * stats.coastalPopulation);
		}
	}

	public Characteristics chars;

	public TechTree regionTree;

	public RegionStats regionStats;

	private ReactionState _state;
	private Progress _progress;

	public RegionAI(RegionStats regionStats)
	{
		_state = ReactionState.Savings; // Initial state
		_progress = new Progress();
		this.regionStats = regionStats;
		chars = new Characteristics(regionStats);

		if (GameManager.Instance.PrintDebug) GD.Print("Creating region AI tech tree...");
		regionTree = new TechTree(false);
		regionTree.setDefaults();
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
		double currentIncome = (chars.income/52) * deltaTime * _progress.health;
		ActionType decision = Decide(gameState);
		switch (decision)
		{
			case ActionType.Save:
				// Small additional income from savings
				_progress.monies += currentIncome;
				break;
			case ActionType.Research(TechNode node):
				if (_progress.monies >= node.cost && gameState.humanTree.available.Contains(node))
				{
					_progress.monies -= node.cost; // Deduct cost of research
					gameState.humanTree.buyNode(node); // Perform the research
				}
				_progress.monies += currentIncome; // Passive income based on health
				break;
			case ActionType.Recover:
				var spending = Mathf.Min(currentIncome, _progress.monies); // Spend up to 0.1 money per second)
				_progress.health += 0.01 * spending;
				_progress.monies -= spending; // Deduct the money spent on recovery
				break;
			case ActionType.Debauch:
				var debauchSpending = Mathf.Min(currentIncome, _progress.monies); // Spend up to 0.1 money per second on luxuries
				_progress.monies -= debauchSpending; // Deduct the money spent on luxuries
				break;
			case ActionType.Death:  // cannot undie
				if (_progress.monies > 0.0) {
					_progress.monies = 0.0;
				}
				if (_progress.health > 0.0) {
					_progress.health = 0.0;
				}
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
			case ReactionState.Death:
				return new ActionType.Death();
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
				_progress.windDamage += damage * chars.windDamageMultiplier;
				_progress.health -= 0.1 * (damage * chars.windDamageMultiplier); // Wind damage reduces health
				break;
			case DamageType.Flood:
				_progress.floodDamage += damage * chars.floodDamageMultiplier;
				_progress.health -= 0.2 * (damage * chars.floodDamageMultiplier); // Flood damage reduces health more
				break;
			case DamageType.Secondary:
				_progress.secondaryDamage += damage * chars.secondaryDamageMultiplier;
				_progress.health -= 0.05 * (damage * chars.secondaryDamageMultiplier); // Secondary damage reduces health slightly
				break;
			default:
				GD.PrintErr($"Unknown damage type: {type}");
				break;
		}
		if (_progress.health < 0.0f) _progress.health = 0.0f; // Ensure health doesn't go below zero
	}
}
