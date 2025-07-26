using Godot;
using System.Collections.Generic;
using System.Linq;

public class RegionStats
{
    public int id; // Unique ID for the region, used for identification on the texture
    public string name;
    public string code;  // Unique 3-letter region code
    public string continent;
    public string countries;
    public float population;  // In millions of people
    public float coastalPopulation;  // Decimal 0-1
    public float developmentIndex;  // Scale of 0-1
    public float gdp;  // In billions of USD/year
    public int minimumElevation;  // In meters
    public int maximumElevation;  // In meters

    public List<string> GetCountries()
    {
        return [.. countries
            .Split(',')
            .Select(c => c.Trim())];
    }

    public void printRegion()
    {
        List<string> countryList = GetCountries();
        string countryString = "";
        for (int i = 0; i < countryList.Count; i++)
        {
            if (i == 0)
            {
                countryString += countryList[i];
            }
            else
            {
                countryString += ", " + countryList[i];
            }
        }
        if (GameManager.Instance.PrintDebug)
        {
            GD.Print("\t> " + name + " (" + code + ") " + continent + ", (" + countryString + "): " + population + ", " + coastalPopulation + ", " + developmentIndex + ", " + gdp + ", " + minimumElevation + ", " + maximumElevation);
        }
    }

    public static RegionStats FromCsvLine(string[] fields)
    {
        return new RegionStats
        {
            id = int.Parse(fields[0]), // Assuming the first field is the ID
            name = fields[1],
            code = fields[2],
            continent = fields[3],
            countries = fields[4],
            population = float.Parse(fields[5]),
            coastalPopulation = float.Parse(fields[6]),
            developmentIndex = float.Parse(fields[7]),
            gdp = float.Parse(fields[8]),
            minimumElevation = int.Parse(fields[9]),
            maximumElevation = int.Parse(fields[10])
        };
    }
}

