using Godot;
using System.Collections.Generic;
using System.Linq;

public class RegionStats
{
    public int id; // Unique ID for the region, used for identification on the texture
    public string name;
    public string code;
    public string continent;
    public string countries;
    public double population;
    public double coastalPopulation;
    public double developmentIndex;
    public double gdp;
    public int minimumElevation;
    public int maximumElevation;

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
            population = double.Parse(fields[5]),
            coastalPopulation = double.Parse(fields[6]),
            developmentIndex = double.Parse(fields[7]),
            gdp = double.Parse(fields[8]),
            minimumElevation = int.Parse(fields[9]),
            maximumElevation = int.Parse(fields[10])
        };
    }
}

