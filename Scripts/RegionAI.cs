using Godot;

public partial class RegionAI
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

	private int _id;
	private ReactionState _state;
	private double _health;
	private double _windDamage;
	private double _floodDamage;
	private double _secondaryDamage;

	private double _monies;
	private double _GDP;  // GDP

	// Region statistics (1 = least, 3/5 = most)
	private int _RES;  // Resources per capita (1-5)
	private int _CON;  // Connectivity (1-5)
	private int _GTP;  // Ground transport (1-3)
	private int _ATP;  // Air transport (1-3)
	private int _STP;  // Ship transport (1-3)
	private int _INR;  // International relations (1-5)
	private int _PRE;  // Storm/disaster prep (1-5)
	private int _GOV;  // Government function (1-5)
	private int _EDU;  // Education (1-5)
	private int _CLI;  // Climate research (1-5)
	private int _SUS;  // Storm susceptibility (1-5)

	// Variables affected by tech trees
	private double _govtFunction;
	private double _resources;
	private double _compliance;
	private double _preparation;

	private double _cultFollowers = 0;
	private int _religionLevel = 1;
	// 1: 1% followers = 0.2%/0.5%, 2: 1% = 0.5%/1%, 3: 1% = 2%/2%
	private double _recoverySpeed;
	private double _allianceDiscount = 0;

	public RegionAI(int id)
	{
		_id = id;
		_state = ReactionState.Savings; // Initial state
		_health = 1.0f; // Full health
		_windDamage = 0.0f;
		_floodDamage = 0.0f;
		_secondaryDamage = 0.0f;

		_monies = 0.0f; // Starting money

		_govtFunction = 1.00f;
		_resources = 1.00f;
		_compliance = 1.00f;
		_preparation = 1.00f;
	}

	private ReactionState GetNextState()
	{
		var nextState = _state;
		switch (_state)
		{
			case ReactionState.Research:
				if (_health < 0.5) // Hardcoded decision points, should be members later
				{
					nextState = ReactionState.Recovery; // Switch to recovery if health is low
				}
				else if (_health > 0.9)
				{
					nextState = ReactionState.Savings; // Switch to savings if health is high
				}
				break;
			case ReactionState.Savings:
				if (_health < 0.8)
				{
					nextState = ReactionState.Research; // Switch to research if we get damaged 
				}
				if (_monies > 100.0)
				{
					nextState = ReactionState.Debauchery; // Switch to debauchery if money is high
				}
				break;
			case ReactionState.Recovery:
				if (_health > 0.8 || _monies == 0.0)
				{
					nextState = ReactionState.Savings; // Switch to savings if health is high
				}
				break;
			case ReactionState.Debauchery:
				if (_monies < 50.0 || _health < 0.5)
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
				_monies += 5.0 * 1.2 * deltaTime * _health;
				break;
			case ActionType.Research(TechNode node):
				if (_monies >= node.cost && gameState.humanityTree.available.Contains(node))
				{
					_monies -= node.cost; // Deduct cost of research
					gameState.humanityTree.buyNode(node); // Perform the research
				}
				_monies += 5.0 * deltaTime * _health; // Passive income based on health
				break;
			case ActionType.Recover:
				var spending = Mathf.Min(5.0 * deltaTime, _monies); // Spend up to 0.1 money per second)
				_health += 0.01 * spending;
				_monies -= spending; // Deduct the money spent on recovery
				break;
			case ActionType.Debauch:
				var debauchSpending = Mathf.Min(5.0 * deltaTime, _monies); // Spend up to 0.1 money per second on luxuries
				_monies -= debauchSpending; // Deduct the money spent on luxuries
				break;
			default:
				GD.PrintErr($"Unknown action type: {decision}");
				break;
		}

		// Apply passive income

		_state = GetNextState(); // Update state based on the current conditions
		if (_id == 1)
		{
			GD.Print($"Russia - State: {_state}, Health: {_health:F2}, Money: {_monies:F2}");
		}
	}

	public ActionType Decide(GameState gameState)
	{
		switch (_state)
		{
			case ReactionState.Research:
				// Chooses a random available node to research
				var targetPurchase = gameState.humanityTree.available[(int)(GD.Randi() % (uint)gameState.humanityTree.available.Count)];
				if (targetPurchase.cost < _monies)
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
				_windDamage += damage;
				_health -= 0.1 * damage; // Wind damage reduces health
				break;
			case DamageType.Flood:
				_floodDamage += damage;
				_health -= 0.2 * damage; // Flood damage reduces health more
				break;
			case DamageType.Secondary:
				_secondaryDamage += damage;
				_health -= 0.05 * damage; // Secondary damage reduces health slightly
				break;
			default:
				GD.PrintErr($"Unknown damage type: {type}");
				break;
		}
		if (_health < 0.0f) _health = 0.0f; // Ensure health doesn't go below zero
	}
}
