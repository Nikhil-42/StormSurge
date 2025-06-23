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

    public RegionAI(int id)
    {
        _id = id;
        _state = ReactionState.Savings; // Initial state
        _health = 1.0f; // Full health
        _windDamage = 0.0f;
        _floodDamage = 0.0f;
        _secondaryDamage = 0.0f;
        _monies = 0.0f; // Starting money
    }

    private ReactionState GetNextState()
    {
        var nextState = _state;
        switch (_state)
        {
            case ReactionState.Research:
                if (_health < 0.5f) // Hardcoded decision points, should be members later
                {
                    nextState = ReactionState.Recovery; // Switch to recovery if health is low
                }
                else if (_health > 0.9f)
                {
                    nextState = ReactionState.Savings; // Switch to savings if health is high
                }
                break;
            case ReactionState.Savings:
                if (_health < 0.8f)
                {
                    nextState = ReactionState.Research; // Switch to research if we get damaged 
                }
                if (_monies > 100.0f)
                {
                    nextState = ReactionState.Debauchery; // Switch to debauchery if money is high
                }
                break;
            case ReactionState.Recovery:
                if (_health > 0.8f)
                {
                    nextState = ReactionState.Savings; // Switch to savings if health is high
                }
                break;
            case ReactionState.Debauchery:
                if (_monies < 0.0f)
                {
                    _state = ReactionState.Savings;
                }

                if (_health < 0.7)
                {
                    _state = ReactionState.Recovery; // Switch to recovery if health is low
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
                _monies += 1.0 * deltaTime * _health;
                break;
            case ActionType.Research(TechNode node):
                if (_monies >= node.cost && gameState.humanityTree.available.Contains(node))
                {
                    _monies -= node.cost; // Deduct cost of research
                    gameState.humanityTree.buyNode(node); // Perform the research
                }
                break;
            case ActionType.Recover:
                var spending = Mathf.Min(1.0 * deltaTime, _monies); // Spend up to 0.1 money per second)
                _health += 0.01 * spending;
                _monies -= spending; // Deduct the money spent on recovery
                break;
            case ActionType.Debauch:
                var debauchSpending = Mathf.Min(0.1 * deltaTime, _monies); // Spend up to 0.1 money per second on luxuries
                _monies -= debauchSpending; // Deduct the money spent on luxuries
                break;
            default:
                GD.PrintErr($"Unknown action type: {decision}");
                break;
        }

        // Apply passive income
        _monies += 5.0 * deltaTime * _health; // Passive income based on health

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
