using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public class Variables {
	// Zeros default for node contributions
	public int[] vars = new int[13];
	
	public int temp {  // 1. degrees Celsius
		get => vars[0];
		set => vars[0] = 0;
	}
	public int sea_level {  // 2. meters
		get => vars[0];
		set => vars[0] = 0;
	}
	public int range {  // 3. percent
		get => vars[0];
		set => vars[0] = 0;
	}

	public int govt_function {  // 4. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int resources {  // 5. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int compliance {  // 6. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int prep {  // 7. percent
		get => vars[0];
		set => vars[0] = 0;
	}

	public int comms {  // 8. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int intern_coop {  // 9. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int transport {  // 10. percent
		get => vars[0];
		set => vars[0] = 0;
	}

	public int damage {  // 11. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int speed {  // 12. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	public int radius {  // 13. percent
		get => vars[0];
		set => vars[0] = 0;
	}
	
	public void setGlobalDefault() {
		// Start of game, fresh map
		temp = 15;
		sea_level = 0;
		range = 100;
		
		govt_function = 100;
		resources = 100;
		compliance = 100;
		prep = 100;
		
		comms = 100;
		intern_coop = 100;
		transport = 100;
		
		damage = 100;
		speed = 100;
		radius = 100;
	}
}

public class TechNode {
	public int cost;
	public string name;
	public string category;
	public bool available;
	public bool bought;
	
	public List<TechNode> children = new List<TechNode>();
	
	public Variables stats;
	
	public TechNode(int c, string n, string cat, bool a, bool b, List<int> positions, List<int> effects) {  // FIX THIS FUNCTION RAHHHHHHHH
		cost = c;
		name = n;
		category = cat;
		available = a;
		bought = b;
		
		stats = new Variables{};
		for (int i=0; i<positions.Count; i++) {
			stats.vars[positions[i]-1] += effects[i];
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

public class StormTechTree {
	public string dataPath = "res://Library/stormtreedata.txt";
	public Variables stormStats;

	public List<TechNode> bought = new List<TechNode>();
	public List<TechNode> available = new List<TechNode>();
	public List<TechNode> locked = new List<TechNode>();

	public StormTechTree() {
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

	public void updateStats(TechNode node) {
		// FIXME: need to test
			for (int i=0; i<node.stats.vars.Length; i++) {
				if (node.stats.vars[i] != 0) {
					stormStats.vars[i] += node.stats.vars[i];
				}
			}
	}
}
