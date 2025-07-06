using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public class weatherVars {
	// Zeros default for node contributions
	public int[] vars = new int[7];
	
	// CLIMATE VARS
	public int temp {  // 1. degrees Celsius above normal
		get => vars[0];
		set => vars[0] = 0;
	}
	public int sea_level {  // 2. meters above normal
		get => vars[0];
		set => vars[0] = 0;
	}
	public int range {  // 3. percent (base 40, up to 100)
		get => vars[0];
		set => vars[0] = 0;
	}

	// STORM VARS
	public int wind_damage {  // 4. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int speed {  // 5. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int radius {  // 6. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int flood_damage {  // 7. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	
	public void setGlobalDefault() {
		// Start of game, fresh map
		temp = 15;
		sea_level = 0;
		range = 40;

		wind_damage = 100;
		speed = 100;
		radius = 100;
		flood_damage = 100;
	}
}

public class geoVars {
	// Zeros default for node contributions
	public int[] vars = new int[7];

	// GLOBAL VARS
	public int comms {  // 1. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int intern_coop {  // 2. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int transport {  // 3. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}

	// INTRAREGIONAL VARS
	public int govt_function {  // 4. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int resources {  // 5. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int compliance {  // 6. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int prep {  // 7. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	
	public void setGlobalDefault() {
		// Start of game, fresh map
		comms = 100;
		intern_coop = 100;
		transport = 100;

		govt_function = 100;
		resources = 100;
		compliance = 100;
		prep = 100;
	}
}

public class humanVars {
	// Zeros default for node contributions
	public int[] vars = new int[8];

	// HUMAN BEHAVIOR VARS
	public int global_migration {  // 1. percent (base 100, must be enabled)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int region_migration {  // 2. percent (base 100, must be enabled)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int global_warming {  // 3. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int climate_costs {  // 4. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int cult_spread {  // 5. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int recovery {  // 6. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int infrastructure_costs {  // 7. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int war_spread {  // 8. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int detection {  // 9. time in hours (base 96)
		get => vars[0];
		set => vars[0] = 0;
	}
	public int implement_costs {  // 10. percent (base 100)
		get => vars[0];
		set => vars[0] = 0;
	}
	
	public void setGlobalDefault() {
		// Start of game, fresh map
		global_migration = 100;
		region_migration = 100;
		global_warming = 100;
		climate_costs = 100;
		cult_spread = 100;

		recovery = 100;
		infrastructure_costs = 100;
		war_spread = 100;
		detection = 96;
		implement_costs = 100;
	}
}

public class TechNode {
	public int cost;
	public bool storm;  // false = human AI tech node
	public string name;
	public string category;
	public bool available;
	public bool bought;

	public bool research_node;  // true if research node only
	public string research_locked = "";  // human tree only, ex: "climate_1", "weather_pattern", "materials", etc.
	
	public List<TechNode> children = new List<TechNode>();
	
	public weatherVars weather;
	public geoVars geo;
	public humanVars human;
	
	public TechNode(int c, string n, string cat, bool a, bool b, List<int> positions, List<int> effects) {  // FIX THIS FUNCTION RAHHHHHHHH
		cost = c;
		name = n;
		category = cat;
		available = a;
		bought = b;
		
		// FIXME: input for all 3 categories of variables (separate lists?? or altogether separate by index?)
		weather = new weatherVars();
		for (int i=0; i<positions.Count; i++) {
			weather.vars[positions[i]-1] += effects[i];
		}

		geo = new geoVars();
		for (int i=0; i<positions.Count; i++) {
			geo.vars[positions[i]-1] += effects[i];
		}

		human = new humanVars();
		for (int i=0; i<positions.Count; i++) {
			human.vars[positions[i]-1] += effects[i];
		}

	}

	public void addChildNode(TechNode child) {
		children.Add(child);
	}
	
	public void unlock() {
		available = true;
	}

	public void buy() {
		available = false;
		bought = true;
	}
}

public class TechTree {
	public string stormDataPath = "res://Library/stormtreedata.txt";
	public string humanDataPath = "";  // FIXME: make this (or make someone else make it)
	
	public weatherVars treeWeather;
	public geoVars treeGeo;
	public humanVars treeHuman;
	private bool _storm;

	public List<TechNode> bought = new List<TechNode>();
	public List<TechNode> available = new List<TechNode>();
	public List<TechNode> locked = new List<TechNode>();

	public TechTree(bool storm) {
		_storm = storm;
		if (_storm) {
			treeWeather = new weatherVars();
			treeWeather.setGlobalDefault();

			treeGeo = new geoVars();
			treeGeo.setGlobalDefault();

			// FIXME: Json file format for input data file
			// FIXME: Change to new variable structures
			/*
			stormStats = new Variables{};
			
			if (!Godot.FileAccess.FileExists(dataPath)) {
				if (GameManager.Instance.PrintDebug) GD.PrintErr($"File not found: {dataPath}");
				return;
			}
			
			using Godot.FileAccess stormFile = Godot.FileAccess.Open(dataPath, Godot.FileAccess.ModeFlags.Read);

			int cost = 0;
			string name = "";
			string child_name = "";
			List<int> positions = new List<int>();
			List<int> effects = new List<int>();

			List<TechNode> parents = new List<TechNode>();
			List<string> children = new List<string>();
			TechNode currentNode = null;
			
			string currentCat = "";

			while (!stormFile.EofReached()) {
				var line = stormFile.GetLine().Trim();

				if (string.IsNullOrWhiteSpace(line)) 
					continue;
				string[] parts = line.Split(' ');

				string currentType = "";
				string currentToken = "";

				for (int i = 0; i < parts.Length; i++) {
					if (i == 0) {
						switch(parts[i]) {
							case "=":
								currentType = "category";
								break;
							case ">":
								currentType = "start_node";
								currentToken = "name";
								break;
							case "~":
								currentType = "child_name";
								break;
							default:
								currentType = "node";
								currentToken = "name";
								name += parts[i];
								break;
						}
						continue;  // Go to next part
					} else {
						if (currentType == "category") {
							currentCat = parts[i];
							if (GameManager.Instance.PrintDebug) GD.Print("Set Category: " + currentCat);
							continue;
						}
						if (parts[i] == ".") {
							switch (currentToken) {
								case "name":
									currentToken = "cost";
									break;
								case "cost":
									currentToken = "positions";
									break;
								case "positions":
									currentToken = "effects";
									break;
								case "effects":
									if (currentType == "start_node") {
										currentNode = new TechNode(cost, name, currentCat, true, false, positions, effects);
										available.Add(currentNode);
									} else {
										currentNode = new TechNode(cost, name, currentCat, false, false, positions, effects);
										locked.Add(currentNode);
									}
									if (GameManager.Instance.PrintDebug) {
										GD.Print("Creating node " + currentNode.name);
										GD.Print("\t>Cost: " + currentNode.cost.ToString() + ", Cat: " + currentNode.category + ", Type: " + currentType);
										if (positions.Count != effects.Count) {
											GD.PrintErr("Parsing error (storm data): positions != effects");
										} else {
											for (int k=0; k<positions.Count; k++) {
												GD.Print("\t\t>>Variable " + positions[k].ToString() + ": " + effects[k].ToString());
											}
										}
									}
									cost = 0;
									name = "";
									positions.Clear();
									effects.Clear();
									currentType = "";
									currentToken = "";
									break;
							}
							continue;  // Go to next part
						}
						else if (currentType == "start_node" || currentType == "node") {
							switch (currentToken) {
								case "name":
									if (!string.IsNullOrWhiteSpace(name)) {
										name += " ";
									}
									name += parts[i];
									break;
								case "cost":
									cost = int.Parse(parts[i]);
									break;
								case "positions":
									string temp = parts[i].Replace("-", "");
									for (int j = 0; j < temp.Length; j++) {
										if (temp[j] != '0') {
											positions.Add(j+1);
										}
									}
									break;
								case "effects":
									effects.Add(int.Parse(parts[i]));
									break;
							}
						} else if (currentType == "child_name") {
							if (parts[i] == "<") {
								parents.Add(currentNode);
								children.Add(child_name);

								child_name = "";
								currentType = "";
								currentToken = "";
								continue;  // Move to next line
							}
							if (!string.IsNullOrWhiteSpace(child_name)) {
								child_name += " ";
							}
							child_name += parts[i];
						}
					}
				}
			}
			for (int i=0; i<parents.Count; i++) {
				foreach (TechNode node in locked) {
					if (node.name == children[i]) {
						parents[i].addChildNode(node);
						if (GameManager.Instance.PrintDebug) GD.Print(">" + node.name + " added as child of " + parents[i].name);
					}
				}
			}
			
			if (GameManager.Instance.PrintDebug) GD.Print("Finished parsing storm data.");
			stormFile.Close();
			*/
		}
		else {
			// FIXME: Make human AI tech tree

			treeGeo = new geoVars();
			treeGeo.setGlobalDefault();

			treeHuman = new humanVars();
			treeHuman.setGlobalDefault();
		}
	}

	public void viewNodes() {
		// Return available nodes
		if (GameManager.Instance.PrintDebug) GD.Print("\nAvailable nodes:");
		foreach (TechNode node in available) {
			if (GameManager.Instance.PrintDebug) GD.Print("\t> " + node.name + ": " + node.cost.ToString());
		}
	}

	public TechNode getNode(string search) {
		foreach (TechNode node in available) {
			if (node.name == search) {
				return node;
			}
		}
		return null;
	}

	public void buyNode(TechNode node) {
		available.Remove(node);
		node.buy();
		bought.Add(node);
		foreach (TechNode child in node.children) {
			if (!available.Contains(child)) {
				available.Add(child);
				child.unlock();
			}
		}
		updateStats(node);
	}

	public void setDefaults() {
		if (_storm) {
			treeWeather.setGlobalDefault();
			treeGeo.setGlobalDefault();
		} else {
			treeGeo.setGlobalDefault();
			treeHuman.setGlobalDefault();
		}
	}

	public void updateStats(TechNode node) {
		if (_storm) {
			for (int i=0; i<node.weather.vars.Length; i++) {
				if (node.weather.vars[i] != 0) {
					treeWeather.vars[i] += node.weather.vars[i];
				}
			}
			for (int i=0; i<node.geo.vars.Length; i++) {
				if (node.geo.vars[i] != 0) {
					treeGeo.vars[i] += node.geo.vars[i];
				}
			}
		} else {
			for (int i=0; i<node.geo.vars.Length; i++) {
				if (node.geo.vars[i] != 0) {
					treeGeo.vars[i] += node.geo.vars[i];
				}
			}
			for (int i=0; i<node.human.vars.Length; i++) {
				if (node.human.vars[i] != 0) {
					treeHuman.vars[i] += node.human.vars[i];
				}
			}
		}
	}
}
