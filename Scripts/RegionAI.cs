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

	public RegionStats regionStats;
	public Characteristics chars;

	private ReactionState _state;
	private Progress _progress;

	public TechTree regionTree;

	private double _cultFollowers = 0;  // as % of population

	// Public getters for accessing region data (turn into getters/setters)
	public int Id => regionStats.id;
	public string Name => regionStats.name;
	public double Health => _progress.health;
	public double WindDamage => _progress.windDamage;
	public double FloodDamage => _progress.floodDamage;
	public double SecondaryDamage => _progress.secondaryDamage;
	public double Money => _progress.monies;
	public double GDP => regionStats.gdp;
	public double Population => regionStats.population;

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
		double currentIncome = chars.income/52 * deltaTime * _progress.health;
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
