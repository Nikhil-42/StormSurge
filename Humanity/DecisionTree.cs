using Godot;
using System;
using System.Collections.Generic;

/*public class DNode
{
	public string name;
	public int cost;
	public bool bought;
	public bool blocked;

	public float value;

	public float pathValue;
	public float pathCost;

	public int[] nodeVarValues = new int[9];
	public int[] nodeHumanValues = new int[10];

	public List<string> childNames;
	public List<DNode> children;
	public List<DNode> parents;

	public DNode() {
		name = "Root";
		cost = 0;
		bought = true;
		blocked = false;
	}

	public DNode(TechNode node) {
		name = node.name;
		cost = node.cost;

		bought = node.bought;
		blocked = node.research_locked;

		nodeVarValues[0] = node.weather.vars[3];  // wind damage
		nodeVarValues[1] = node.weather.vars[6];  // flood damage

		for (int i=0; i<node.geo.vars.Length; i++) {
			nodeVarValues[i+2] = node.geo.vars[i];  // all geo vars
		}

		for (int i=0; i<node.human.vars.Length; i++) {
			nodeHumanValues[i] = node.human.vars[i];  // all human vars
		}

		foreach (TechNode child in node.children) {
			childNames.Add(child.name);
		}
	}
}

public class DecisionTree
{
	public float[] defaultVarValues = new float[] {1.0f, 1.0f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f, 0.7f};
	// 1.0 = wind damage, flood damage
	// 0.7 = communication, international co-op, transportation
	// 0.7 = gov't function, resources, compliance, preparation
	public float[] defaultHumanValues = new float[] {0.0f, 0.0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f};
	// 0.0 (variables not yet implemented)= global migration, regional migration, infrastructure research costs, war spread speed, storm detection, implementation costs
	// 0.5 = global warming, climate research costs, cult spread speed, recovery rate

	private DNode root = new DNode();

	Dictionary<string, DNode> nodeDict = new Dictionary<string, DNode>();
	Dictionary<string, List<string>> childDict = new Dictionary<string, List<string>>();

	public DecisionTree(RegionAI region) {
		// Only runs during new game initiation
		foreach(TechNode a in region.regionTree.available) {
			root.childNames.Add(a.name);

			DNode temp = new DNode(a);
			nodeDict.Add(a.name, temp);
			childDict.Add(a.name, temp.childNames);

			temp.parents.Add(root);
			root.children.Add(temp);
		}

		foreach(TechNode l in region.regionTree.locked) {
			DNode temp = new DNode(l);
			nodeDict.Add(l.name, temp);
			childDict.Add(l.name, temp.childNames);
		}

		foreach(var entry in childDict) {
			DNode parent = nodeDict[entry.Key];

			foreach(string childName in entry.Value) {
				DNode child = nodeDict[childName];
				parent.children.Add(child);
				if (!child.parents.Contains(parent)) {
					child.parents.Add(parent);
				}
			}
		}
	}

	public void AssignNodeValues(RegionAI region) {
		
	}

	public void AssignNodeValues(DNode root, RegionAI region) {
        // Runs for each branch of tech tree
		if (root == null) return;

        List<DNode> visited = new List<DNode>();
		Queue<DNode> queue = new Queue<DNode>();
		queue.Enqueue(root);

		float totalValue = 0;
		int totalCost = 0;

		while (queue.Count > 0) {
			DNode current = queue.Dequeue();

            float nodeValue;
			

            visited.Add(current);
            // Calculate value and cost of current node
            for (int i=0; i<defaultVarValues.Length; i++) {
                nodeValue += defaultVarValues[i] * current.nodeVarValues[i];
            }
            for (int i=0; i<defaultHumanValues.Length; i++) {
                nodeValue += defaultHumanValues[i] * current.nodeHumanValues[i];
            }

            // Add values to total value and total cost
            totalValue += nodeValue;
            totalCost += current.cost;

            // Assign value, path value, and path cost to current node
            current.value = nodeValue;
            current.pathValue = totalValue;
            current.pathCost = totalCost;

            // Add children to queue if not already visited
			foreach (DNode child in current.children) {
                if (!visited.Contains(child) && queue.Contains(child))
				queue.Enqueue(child);
			}
		}
	}

	public DNode SearchNode(DNode node, string name) {
		DNode result;
		if (node.name == name) {
			return node;
		}
		foreach (DNode n in node.children) {
			result = SearchNode(n, name);
			if (result != null) {
				return result;
			}
		}
		return null;
	}

	private void EvaluateBranch(DNode root, float totalValue, int totalCost) {
		float value;

	}
}
*/