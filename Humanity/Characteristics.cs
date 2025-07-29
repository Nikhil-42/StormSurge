using Godot;
using System.Collections.Generic;
using System.Linq;

public class Characteristics {  // Variables different for each country that are calculated based on stats
	// FIXME: Characteristics should be based on more detailed statistics for countries (storm susceptibility, etc.), but
	// for now everything based on GDP or development pretty much

	// MONIE MATTERS
	public float income;  // Region usable income based on GDP and population
	public float globalResearchFunding;  // % usual contribution of income to global research

	// DAMAGE THRESHOLDS
	public float goodHealth;  // Switch to savings/debauchery/research
	public float midHealth;  // Switch to recovery/research
	public float poorHealth;  // Switch to recovery

	public float goodMoney;  // (% of GDP) Switch from savings to debauchery, full research investment (rarely happens for poorest regions)
	public float midMoney;  // (% of GDP) Savings, partial research investment
	public float poorMoney;  // (% of GDP) Savings, no research investment

	public float lowAlarmThreshold;  // (% of Total Damage) No debauchery, fund research (75% income), upgrades (25% income) instead
	public float highAlarmThreshold;  // (% of Total Damage) Upgrades (75% income), and research (25% income)

	// DAMAGE RATE
	public float windDamageMultiplier;  // Base 1.0, affected by infrastructure and preparedness
	public float floodDamageMultiplier;  // Base 1.0, highly affected by coastal population
	public float secondaryDamageMultiplier;  // Base 1.0, highly affected by development index

	// POLITICAL, INFRASTRUCTURE
	public float governmentEfficiency;  // Base 1.0 multiplier
	public float emissions;  // Greenhouse gas emissions based on GDP
	public float internationalRelations;  // Base 1.0 multiplier, likelihood to join/form alliances
	public float education;  // Base 1.0 multiplier, affects preparation and speed of cult spread
	public float buildingInfrastructure;  // Base 1.0 multiplier, general quality of architecture in region (based on GDP for now)
	public float stormPreparedness;  // Base 1.0 multiplier for countries that regularly experience storms normally (based on GDP + coastal pop for now)

	public Characteristics(RegionStats stats) {
		// income, globalResearchFunding, Money's, emissions
		float perCapitaGDP = stats.gdp * 1000 / stats.population;

		float normalizedGDP = 700 * Mathf.Log((stats.gdp + 200) / 4000) + 1000;

		if (perCapitaGDP > 50000) {
			income = 0.5f;
			globalResearchFunding = 0.5f;

			goodMoney = 0.9f;
			midMoney = 0.75f;
			poorMoney = 0.5f;
		} else if (perCapitaGDP > 20000) {
			income = 0.4f;
			globalResearchFunding = 0.2f;

			goodMoney = 0.8f;
			midMoney = 0.6f;
			poorMoney = 0.3f;
		} else if (perCapitaGDP > 10000) {
			income = 0.3f;
			globalResearchFunding = 0.1f;

			goodMoney = 0.7f;
			midMoney = 0.5f;
			poorMoney = 0.2f;
		} else {
			income = 0.2f;
			globalResearchFunding = 0.05f;

			goodMoney = 0.6f;
			midMoney = 0.4f;
			poorMoney = 0.2f;
		}
		// Normalized GDP
		income *= normalizedGDP;
		globalResearchFunding *= normalizedGDP;
		goodMoney *= normalizedGDP;
		midMoney *= normalizedGDP;
		poorMoney *= normalizedGDP;

		// Convert yearly income to hourly income 
		income /= 8760.0f;  // 365 days * 24 hours

		emissions = normalizedGDP / 1000f;

		// Health's, alarm thresholds, damage multipliers, etc.
		goodHealth = 0.8f + (0.2f * stats.developmentIndex);
		midHealth = 0.6f + (0.4f * stats.developmentIndex);
		poorHealth = 0.4f + (0.6f * stats.developmentIndex);

		lowAlarmThreshold = 0.9f + (0.1f * stats.developmentIndex);
		highAlarmThreshold = 0.75f + (0.25f * stats.developmentIndex);

		windDamageMultiplier = 1f + ((1f - stats.developmentIndex)/2f) + (stats.coastalPopulation/2f);
		floodDamageMultiplier = 1f + (2f * stats.coastalPopulation);
		secondaryDamageMultiplier = 1f + ((1f - stats.developmentIndex) * 2f);

		governmentEfficiency = 0.5f + stats.developmentIndex;
		internationalRelations = 0.5f + stats.developmentIndex;
		education = 0.5f + stats.developmentIndex;
		buildingInfrastructure = 0.5f + stats.developmentIndex;

		stormPreparedness = 0.5f + (stats.developmentIndex * stats.coastalPopulation);
	}
}
