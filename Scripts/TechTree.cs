using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public interface ITechNode {
    string Name { get; }
    string Category { get; }
    float Cost { get; }
    bool Blocked { get; }
    bool Purchased { get; }
    bool Available => !Blocked && !Purchased;
    string[] Parents { get; }
}

public class TechNode<T> : ITechNode where T : IVars<T> {
    public string Name { get; private set; }
    public string Category { get; private set; }
    public virtual float Cost { get; private set; }
    public bool Blocked {
        get => _prerequisites.Any(req => !req.Purchased);
    }
    public bool Purchased { get; protected set; } = false;
    public bool Available => !Blocked && !Purchased;
    public string[] Parents { get; private set; } = [];
    public IVars<T> Vars { get; private set; }

    private ITechNode[] _prerequisites = [];

    internal void AddPrerequisite(ITechNode prerequisite) {
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
    internal virtual bool Buy(ref float balance) {
        if (!Available) {
            return false; // Not available for purchase
        }

        if (balance < Cost) {
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

public class GlobalNode : TechNode<GlobalVars> {
    public override float Cost {
        get => _cost - _downpayment;
    }

    private float _cost = 0.0f;
    private float _downpayment = 0.0f;

    internal override bool Buy(ref float balance) {

        if (!Available) {
            return false; // Not available for purchase
        }

        _downpayment += balance;
        balance -= Cost;
        if (_downpayment >= Cost) {
            Purchased = true;
        }
        return true;
    }
}

public class TechTree<T, V> where T : TechNode<V>, new() where V : IVars<V> {
    public V Vars { get; private set; }
    public List<T> Available => _nodes.Values.Where(node => node.Available).ToList();
    private Dictionary<string, T> _nodes;

    public TechTree(Variant serializedTree) {
        _nodes = [];
        Vars = V.Default;

        if (serializedTree.VariantType is not Variant.Type.Array) {
            GD.PrintErr($"[TechTree] Expected an array, got {serializedTree.VariantType}");
            return;
        }
        var nodes = (Godot.Collections.Array<Variant>)serializedTree;

        foreach (var infoVariant in nodes) {
            if (infoVariant.VariantType is not Variant.Type.Dictionary) {
                GD.PrintErr($"[TechTree] Expected a dictionary, got {infoVariant.VariantType}");
                continue;
            }
            var info = (Godot.Collections.Dictionary<string, Variant>)infoVariant;
            T node = new();
            node.LoadFromJson(info);
            _nodes[node.Name] = node;
        }
    }


    public void UpdatePrerequisites(Dictionary<string, ITechNode> externalNodes) {
        foreach (var node in _nodes.Values) {
            foreach (string prerequisiteName in node.Parents) {
                ITechNode prerequisite = _nodes.GetValueOrDefault(prerequisiteName);
                if (prerequisite == null) externalNodes.GetValueOrDefault(prerequisiteName);
                if (prerequisite == null) {
                    GD.PrintErr($"[TechTree] Prerequisite {prerequisiteName} not found for node {node.Name}");
                    continue;
                }

                node.AddPrerequisite(prerequisite);
            }
        }
    }

    public TechTree(TechTree<T, V> other) {
        _nodes = new Dictionary<string, T>(other._nodes);
        Vars = other.Vars;
    }

    public T GetNode(string search) {  // Returns node by name search, whether available or locked
        if (_nodes.TryGetValue(search, out T value)) {
            return value;
        }
        return null;
    }

    public List<T> GetAllNodes() {
        return _nodes.Values.ToList();
    }

    public bool BuyNode(T node, ref float balance) {
        if (node == null || _nodes == null || !_nodes.TryGetValue(node.Name, out T value) || value != node) {
            GD.PrintErr($"Node {node?.Name} not found in the tech tree.");
            return false; // Node not found
        }
        bool result = node.Buy(ref balance);
        if (node.Purchased) {
            // Update Vars if node is purchased
            Vars = V.Add(Vars, (V)node.Vars);
        }
        return result;
    }

    public bool BuyNode(string name, ref float balance) {
        if (_nodes.TryGetValue(name, out T node)) {
            return BuyNode(node, ref balance);
        }
        return false; // Node not found
    }

    public void PrintNodes() {
        GD.Print("All nodes (" + typeof(T).Name + "):");
        foreach (T node in _nodes.Values) {
            GD.Print("\t> " + node.Name + ": " + node.Cost.ToString());
        }
    }
}
