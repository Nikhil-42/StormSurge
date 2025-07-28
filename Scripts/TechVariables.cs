using Godot;

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
	/// <summary>
	/// The global temperature in Kelvin.
	/// </summary>
	public float Temperature { get; init; }
	/// <summary>
	/// The sea level in meters.
	/// </summary>
	public float SeaLevel { get; init; }
	/// <summary>
	/// The distance in kilometers until the storm dissipates.
	/// </summary>
	public float Range { get; init; }
	/// <summary>
	/// The multiplier for flood damage.
	/// </summary>
	public float FloodDamage { get; init; }
	/// <summary>
	/// The multiplier for wind damage.
	/// </summary>
	public float WindDamage { get; init; }
	/// <summary>
	/// The speed of the wind in meters per second.
	/// </summary>
	public float WindSpeed { get; init; }
	/// <summary>
	/// The radius of the storm in kilometers.
	/// </summary>
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
			FloodDamage = lhs.FloodDamage + rhs.FloodDamage,
			WindDamage = lhs.WindDamage + rhs.WindDamage,
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
		if (Range != 0.0f) json["storm_range"] = Range;
		if (FloodDamage != 1.0f) json["flood_damage"] = FloodDamage;
		if (WindDamage != 1.0f) json["wind_damage"] = WindDamage;
		if (WindSpeed != 0.0f) json["wind_speed"] = WindSpeed;
		if (Radius != 0.0f) json["storm_radius"] = Radius;
		return json;
	}
}

public class GeopoliticalVars : IVars<GeopoliticalVars>
{
	public float Communications { get; init; }  // TODO: reevaluate this later
	/// <summary>
	/// The multiplier for the likelihood of countries contributing to international efforts (global tree, alliance, etc).
	/// </summary>
	public float InternationalCooperation { get; init; }
	public float Transportation { get; init; }  // TODO: reevaluate this later
	public float GovernmentFunction { get; init; }  // TODO: reevaluate this later
	/// <summary>
	/// The multiplier for the amount of income a country can utilize
	/// </summary>
	public float Resources { get; init; }
	/// <summary>
	/// The multiplier for the effectiveness of technology upgrades
	/// </summary>
	public float Compliance { get; init; }
	/// <summary>
	/// The multiplier for all types of damage a storm a storm causes
	/// </summary>
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
			Communications = lhs.Communications + rhs.Communications,
			InternationalCooperation = lhs.InternationalCooperation + rhs.InternationalCooperation,
			Transportation = lhs.Transportation + rhs.Transportation,
			GovernmentFunction = lhs.GovernmentFunction + rhs.GovernmentFunction,
			Resources = lhs.Resources + rhs.Resources,
			Compliance = lhs.Compliance + rhs.Compliance,
			Preparation = lhs.Preparation + rhs.Preparation
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
			FloodDamage = 0.0f,  // multiplier
			WindDamage = 0.0f,  // multiplier
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
			GlobalMigration = lhs.GlobalMigration + rhs.GlobalMigration,
			RegionMigration = lhs.RegionMigration + rhs.RegionMigration,
			GlobalWarming = lhs.GlobalWarming + rhs.GlobalWarming,
			ClimateCosts = lhs.ClimateCosts + rhs.ClimateCosts,
			CultSpread = lhs.CultSpread + rhs.CultSpread,
			Recovery = lhs.Recovery + rhs.Recovery,
			InfrastructureCosts = lhs.InfrastructureCosts + rhs.InfrastructureCosts,
			WarSpread = lhs.WarSpread + rhs.WarSpread,
			Detection = lhs.Detection + rhs.Detection,
			ImplementCosts = lhs.ImplementCosts + rhs.ImplementCosts,
			Storm = lhs.Storm + rhs.Storm,
			Geopolitical = lhs.Geopolitical + rhs.Geopolitical,
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
			ImplementCosts = json.TryGetValue("implement_costs", out value) ? (float)value : 1.0f,
			Storm = StormVars.FromJson(json),
			Geopolitical = GeopoliticalVars.FromJson(json),
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
