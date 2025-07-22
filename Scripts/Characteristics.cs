using Godot;
using System.Collections.Generic;
using System.Linq;

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