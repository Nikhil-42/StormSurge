using Godot;
using System.Collections.Generic;
using System.Linq;

public interface IVars<T> where T : IVars<T>
{
	abstract public static T Default { get; }
	abstract public static T Add(T lhs, T rhs);
	abstract public static T FromJson(Godot.Collections.Dictionary<string, Variant> json);
	abstract public Godot.Collections.Dictionary<string, Variant> ToJson();
}

// 15, 0, 40, 1.0, 1.0, 100, 100
public class GlobalVars : IVars<GlobalVars>
{
	public StormVars Storm { get; init; }
	public GeopoliticalVars Geopolitical { get; init; }

	public static GlobalVars Default => new GlobalVars
	{
		Storm = StormVars.Default,
		Geopolitical = GeopoliticalVars.Default
	};

	public static GlobalVars Add(GlobalVars lhs, GlobalVars rhs)
	{
		return lhs + rhs;
	}

	public static GlobalVars operator +(GlobalVars lhs, GlobalVars rhs)
	{
		return new GlobalVars
		{
			Storm = lhs.Storm + rhs.Storm,
			Geopolitical = lhs.Geopolitical + rhs.Geopolitical
		};
	}

	public static GlobalVars FromJson(Godot.Collections.Dictionary<string, Variant> json)
	{
		var stormVars = StormVars.FromJson(json);
		var geopoliticalVars = GeopoliticalVars.FromJson(json);
		return new GlobalVars
		{
			Storm = stormVars,
			Geopolitical = geopoliticalVars
		};
	}

	public Godot.Collections.Dictionary<string, Variant> ToJson()
	{
		var json = new Godot.Collections.Dictionary<string, Variant>();
		foreach ((string key, var item) in Storm.ToJson()) {
			json[key] = item;
		}

		foreach ((string key, var item) in Geopolitical.ToJson())
		{
			json[key] = item;
		}

		return json;
	}
}

public class StormVars : IVars<StormVars>
{
	// Zeros defaults for node contributions
	public float Temperature { get; init; }
	public float SeaLevel { get; init; }
	public float Range { get; init; }
	public float FloodDamage { get; init; }
	public float WindDamage { get; init; }
	public float WindSpeed { get; init; }
	public float Radius { get; init; }

	public static StormVars Default => new StormVars
	{
		Temperature = 288.0f,  // Kelvin
		SeaLevel = 0.0f,  // km
		Range = 5000.0f,  // km
		WindDamage = 1.0f,  // multiplier
		WindSpeed = 100.0f,  // m/s
		Radius = 50.0f,  // km
		FloodDamage = 1.0f,  // multiplier
	};

	public static StormVars Add(StormVars lhs, StormVars rhs)
	{
		return lhs + rhs;
	}

	public static StormVars operator +(StormVars lhs, StormVars rhs)
	{
		return new StormVars
		{
			Temperature = lhs.Temperature + rhs.Temperature,
			SeaLevel = lhs.SeaLevel + rhs.SeaLevel,
			Range = lhs.Range + rhs.Range,
			FloodDamage = lhs.FloodDamage * rhs.FloodDamage,
			WindDamage = lhs.WindDamage * rhs.WindDamage,
			WindSpeed = lhs.WindSpeed + rhs.WindSpeed,
			Radius = lhs.Radius + rhs.Radius
		};
	}

	public static StormVars FromJson(Godot.Collections.Dictionary<string, Variant> json)
	{
		Variant value;
		return new StormVars
		{
			Temperature = json.TryGetValue("temperature", out value) ? (float)value : 0.0f,
			SeaLevel = json.TryGetValue("sea_level", out value) ? (float)value : 0.0f,
			Range = json.TryGetValue("storm_range", out value) ? (float)value : 0.0f,
			WindDamage = json.TryGetValue("wind_damage", out value) ? (float)value : 1.0f,
			WindSpeed = json.TryGetValue("wind_speed", out value) ? (float)value : 0.0f,
			Radius = json.TryGetValue("storm_radius", out value) ? (float)value : 0.0f,
			FloodDamage = json.TryGetValue("flood_damage", out value) ? (float)value : 1.0f,
		};
	}

	public Godot.Collections.Dictionary<string, Variant> ToJson()
	{
		var json = new Godot.Collections.Dictionary<string, Variant>();
		if (Temperature != 0.0f) json["temperature"] = Temperature;
		if (SeaLevel != 0.0f) json["sea_level"] = SeaLevel;
		if (Range != 0.0f) json["range"] = Range;
		if (FloodDamage != 1.0f) json["flood_damage"] = FloodDamage;
		if (WindDamage != 1.0f) json["wind_damage"] = WindDamage;
		if (WindSpeed != 0.0f) json["wind_speed"] = WindSpeed;
		if (Radius != 0.0f) json["storm_radius"] = Radius;
		return json;
	}
}

public class GeopoliticalVars : IVars<GeopoliticalVars>
{
	public float Communications { get; init; }
	public float InternationalCooperation { get; init; }
	public float Transportation { get; init; }
	public float GovernmentFunction { get; init; }
	public float Resources { get; init; }
	public float Compliance { get; init; }
	public float Preparation { get; init; }

	public static GeopoliticalVars Default => new GeopoliticalVars
	{
		Communications = 1.0f,
		InternationalCooperation = 1.0f,
		Transportation = 1.0f,
		GovernmentFunction = 1.0f,
		Resources = 1.0f,
		Compliance = 1.0f,
		Preparation = 1.0f
	};

	public static GeopoliticalVars Add(GeopoliticalVars lhs, GeopoliticalVars rhs)
	{
		return lhs + rhs;
	}

	public static GeopoliticalVars operator +(GeopoliticalVars lhs, GeopoliticalVars rhs)
	{
		return new GeopoliticalVars
		{
			Communications = lhs.Communications * rhs.Communications,
			InternationalCooperation = lhs.InternationalCooperation * rhs.InternationalCooperation,
			Transportation = lhs.Transportation * rhs.Transportation,
			GovernmentFunction = lhs.GovernmentFunction * rhs.GovernmentFunction,
			Resources = lhs.Resources * rhs.Resources,
			Compliance = lhs.Compliance * rhs.Compliance,
			Preparation = lhs.Preparation * rhs.Preparation
		};
	}

	public static GeopoliticalVars FromJson(Godot.Collections.Dictionary<string, Variant> json)
	{
		Variant value;
		return new GeopoliticalVars
		{
			Communications = json.TryGetValue("communications", out value) ? (float)value : 1.0f,
			InternationalCooperation = json.TryGetValue("international_cooperation", out value) ? (float)value : 1.0f,
			Transportation = json.TryGetValue("transportation", out value) ? (float)value : 1.0f,
			GovernmentFunction = json.TryGetValue("government_function", out value) ? (float)value : 1.0f,
			Resources = json.TryGetValue("resources", out value) ? (float)value : 1.0f,
			Compliance = json.TryGetValue("compliance", out value) ? (float)value : 1.0f,
			Preparation = json.TryGetValue("preparation", out value) ? (float)value : 1.0f
		};
	}

	public Godot.Collections.Dictionary<string, Variant> ToJson()
	{
		var json = new Godot.Collections.Dictionary<string, Variant>();
		if (Communications != 1.0f) json["communications"] = Communications;
		if (InternationalCooperation != 1.0f) json["international_cooperation"] = InternationalCooperation;
		if (Transportation != 1.0f) json["transportation"] = Transportation;
		if (GovernmentFunction != 1.0f) json["government_function"] = GovernmentFunction;
		if (Resources != 1.0f) json["resources"] = Resources;
		if (Compliance != 1.0f) json["compliance"] = Compliance;
		if (Preparation != 1.0f) json["preparation"] = Preparation;
		return json;
	}
}

public class RegionVars : IVars<RegionVars>
{
	public float GlobalMigration { get; init; }
	public float RegionMigration { get; init; }
	public float GlobalWarming { get; init; }
	public float ClimateCosts { get; init; }
	public float CultSpread { get; init; }
	public float Recovery { get; init; }
	public float InfrastructureCosts { get; init; }
	public float WarSpread { get; init; }
	public float Detection { get; init; }
	public float ImplementCosts { get; init; }

	public StormVars Storm { get; init; }
	public GeopoliticalVars Geopolitical { get; init; }

	public static RegionVars Default => new RegionVars
	{
		GlobalMigration = 1.0f,
		RegionMigration = 1.0f,
		GlobalWarming = 1.0f,
		ClimateCosts = 1.0f,
		CultSpread = 1.0f,
		Recovery = 1.0f,
		InfrastructureCosts = 1.0f,
		WarSpread = 1.0f,
		Detection = 1.0f,
		ImplementCosts = 1.0f,
		Storm = new StormVars
		{
			Temperature = 0.0f,  // Kelvin
			SeaLevel = 0.0f,  // km
			Range = 0.0f,  // km
			FloodDamage = 1.0f,  // multiplier
			WindDamage = 1.0f,  // multiplier
			WindSpeed = 0.0f,  // m/s
			Radius = 0.0f,  // km
		},
		Geopolitical = GeopoliticalVars.Default,
	};

	public static RegionVars Add(RegionVars lhs, RegionVars rhs)
	{
		return lhs + rhs;
	}

	public static RegionVars operator +(RegionVars lhs, RegionVars rhs)
	{
		return new RegionVars
		{
			GlobalMigration = lhs.GlobalMigration * rhs.GlobalMigration,
			RegionMigration = lhs.RegionMigration * rhs.RegionMigration,
			GlobalWarming = lhs.GlobalWarming * rhs.GlobalWarming,
			ClimateCosts = lhs.ClimateCosts * rhs.ClimateCosts,
			CultSpread = lhs.CultSpread * rhs.CultSpread,
			Recovery = lhs.Recovery * rhs.Recovery,
			InfrastructureCosts = lhs.InfrastructureCosts * rhs.InfrastructureCosts,
			WarSpread = lhs.WarSpread * rhs.WarSpread,
			Detection = lhs.Detection * rhs.Detection,
			ImplementCosts = lhs.ImplementCosts * rhs.ImplementCosts
		};
	}

	public static RegionVars FromJson(Godot.Collections.Dictionary<string, Variant> json)
	{
		Variant value;
		return new RegionVars
		{
			GlobalMigration = json.TryGetValue("global_migration", out value) ? (float)value : 1.0f,
			RegionMigration = json.TryGetValue("region_migration", out value) ? (float)value : 1.0f,
			GlobalWarming = json.TryGetValue("global_warming", out value) ? (float)value : 1.0f,
			ClimateCosts = json.TryGetValue("climate_costs", out value) ? (float)value : 1.0f,
			CultSpread = json.TryGetValue("cult_spread", out value) ? (float)value : 1.0f,
			Recovery = json.TryGetValue("recovery", out value) ? (float)value : 1.0f,
			InfrastructureCosts = json.TryGetValue("infrastructure_costs", out value) ? (float)value : 1.0f,
			WarSpread = json.TryGetValue("war_spread", out value) ? (float)value : 1.0f,
			Detection = json.TryGetValue("detection", out value) ? (float)value : 1.0f,
			ImplementCosts = json.TryGetValue("implement_costs", out value) ? (float)value : 1.0f
		};
	}

	public Godot.Collections.Dictionary<string, Variant> ToJson()
	{
		var json = new Godot.Collections.Dictionary<string, Variant>();
		if (GlobalMigration != 1.0f) json["global_migration"] = GlobalMigration;
		if (RegionMigration != 1.0f) json["region_migration"] = RegionMigration;
		if (GlobalWarming != 1.0f) json["global_warming"] = GlobalWarming;
		if (ClimateCosts != 1.0f) json["climate_costs"] = ClimateCosts;
		if (CultSpread != 1.0f) json["cult_spread"] = CultSpread;
		if (Recovery != 1.0f) json["recovery"] = Recovery;
		if (InfrastructureCosts != 1.0f) json["infrastructure_costs"] = InfrastructureCosts;
		if (WarSpread != 1.0f) json["war_spread"] = WarSpread;
		if (Detection != 1.0f) json["detection"] = Detection;
		if (ImplementCosts != 1.0f) json["implement_costs"] = ImplementCosts;
		foreach (var (key, value) in Storm.ToJson())
		{
			json[key] = value;
		}
		foreach (var (key, value) in Geopolitical.ToJson())
		{
			json[key] = value;
		}
		return json;
	}
}


public interface ITechNode
{
	string Name { get; }
	string Category { get; }
	float Cost { get; }
	bool Blocked { get; }
	bool Purchased { get; }
	bool Available => !Blocked && !Purchased;
	string[] Parents { get; }
}

public class TechNode<T> : ITechNode where T : IVars<T>
{
	public string Name { get; private set; }
	public string Category { get; private set; }
	public virtual float Cost { get; private set; }
	public bool Blocked
	{
		get => _prerequisites.Any(req => !req.Purchased);
	}
	public bool Purchased { get; protected set; } = false;
	public bool Available => !Blocked && !Purchased;
	public string[] Parents { get; private set; } = [];
	public IVars<T> Vars { get; private set; }

	private ITechNode[] _prerequisites = [];

	internal void AddPrerequisite(ITechNode prerequisite)
	{
		_prerequisites = _prerequisites.Append(prerequisite).ToArray();
	}

	/// <summary>
	/// Attempts to buy the node with a downpayment.
	/// Returns `false` if the downpayment bounced.
	/// Note, the downpayment need not be the full cost of the node.
	/// If the payment is sufficient, the node will be marked as purchased.
	/// </summary>
	/// <param name="downpayment"></param>
	/// <returns>`true` if the downpayment should be deducted from the caller's currency, `false` otherwise.</returns>
	internal virtual bool Buy(ref float balance)
	{
		if (!Available)
		{
			return false; // Not available for purchase
		}

		if (balance < Cost)
		{
			return false; // Not enough downpayment
		}

		Purchased = true;
		balance -= Cost;
		return true;
	}

	public void LoadFromJson(Godot.Collections.Dictionary<string, Variant> json) {
		Name = (string)json["name"];
		Category = (string)json["category"];
		Cost = (int)json["cost"];
		Parents = ((Godot.Collections.Array<string>)json["prereqs"]).ToArray();
		Vars = T.FromJson(json);
	}
}

public class GlobalNode : TechNode<GlobalVars>
{
	public override float Cost
	{
		get => _cost - _downpayment;
	}

	private float _cost = 0.0f;
	private float _downpayment = 0.0f;

	internal override bool Buy(ref float balance)
	{

		if (!Available)
		{
			return false; // Not available for purchase
		}

		_downpayment += balance;
		balance -= Cost;
		if (_downpayment >= Cost)
		{
			Purchased = true;
		}
		return true;
	}
}

public class TechTree<T, V> where T : TechNode<V>, new() where V : IVars<V> {
	public V Vars { get; private set; }
	public List<T> Available => _nodes.Values.Where(node => node.Available).ToList();
	private Dictionary<string, T> _nodes;

	public TechTree(Variant serializedTree)
	{
		_nodes = [];
		Vars = V.Default;

		if (serializedTree.VariantType is not Variant.Type.Array)
		{
			GD.PrintErr($"[TechTree] Expected an array, got {serializedTree.VariantType}");
			return;
		}
		var nodes = (Godot.Collections.Array<Variant>)serializedTree;

		foreach (var infoVariant in nodes)
		{
			if (infoVariant.VariantType is not Variant.Type.Dictionary)
			{
				GD.PrintErr($"[TechTree] Expected a dictionary, got {infoVariant.VariantType}");
				continue;
			}
			var info = (Godot.Collections.Dictionary<string, Variant>)infoVariant;
			T node = new();
			node.LoadFromJson(info);
			_nodes[node.Name] = node;
		}
	}


	public void UpdatePrerequisites(Dictionary<string, ITechNode> externalNodes)
	{
		foreach (var node in _nodes.Values)
		{
			foreach (string prerequisiteName in node.Parents)
			{
				ITechNode prerequisite = _nodes.GetValueOrDefault(prerequisiteName);
				if (prerequisite == null ) externalNodes.GetValueOrDefault(prerequisiteName);
				if (prerequisite == null)
				{
					GD.PrintErr($"[TechTree] Prerequisite {prerequisiteName} not found for node {node.Name}");
					continue;
				}

				node.AddPrerequisite(prerequisite);
			}
		}
	}

	public TechTree(TechTree<T, V> other)
	{
		_nodes = new Dictionary<string, T>(other._nodes);
		Vars = other.Vars;
	}

	public T GetNode(string search)
	{  // Returns node by name search, whether available or locked
		if (_nodes.TryGetValue(search, out T value))
		{
			return value;
		}
		return null;
	}

	public List<T> GetAllNodes() {
		return _nodes.Values.ToList();
	}

	public bool BuyNode(T node, ref float balance)
	{
		if (node == null || _nodes == null || !_nodes.TryGetValue(node.Name, out T value) || value != node)
		{
			GD.PrintErr($"Node {node?.Name} not found in the tech tree.");
			return false; // Node not found
		}
		bool result = node.Buy(ref balance);
		if (node.Purchased)
		{
			// Update Vars if node is purchased
			Vars = V.Add(Vars, (V)node.Vars);
		}
		return result;
	}

	public bool BuyNode(string name, ref float balance)
	{
		if (_nodes.TryGetValue(name, out T node))
		{
            return BuyNode(node, ref balance);
		}
		return false; // Node not found
	}

	public void PrintNodes()
	{
		GD.Print("All nodes (" + typeof(T).Name + "):");
		foreach (T node in _nodes.Values)
		{
			GD.Print("\t> " + node.Name + ": " + node.Cost.ToString());
		}
	}
}
