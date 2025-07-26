import json


storm_variables = [
    "temperature",
    "sea_level",
    "storm_range",
    "wind_damage",
    "storm_speed",
    "storm_radius",
    "flood_damage",
    "communications",
    "international_coop",
    "transportation",
    "government_function",
    "resources",
    "compliance",
    "preparation"
]

human_variables = [
    "wind_damage",
    "flood_damage",
    "communicaions",
    "international_cooperation",
    "transportation",
    "government_function",
    "resources",
    "compliance",
    "preparation",
    "global_migration",
    "region_migration",
    "global_warming",
    "climate_costs",
    "cult_spread",
    "recovery",
    "infrastructure_costs",
    "war_spread",
    "detection",
    "implement_costs",
]


category = ''

class Node:
    def __init__(self, name, category, cost, stats):
        self.name = name
        self.category = category
        self.cost = cost
        self.prereqs = []
        self.stats = stats
    
    def to_dict(self):
        return {
            "name": self.name,
            "category": self.category,
            "cost": self.cost,
            **{k: v for k, v in self.stats.items() if v != 0},  # Only include non-zero stats
            "prereqs": self.prereqs
        }

def parse_file(file, variables) -> list[Node] | None:
    nodes = []
    category = 'default'
    prereqs = {}
    for line_num, line in enumerate(file.readlines()):
        try:
            if line.strip() == '':
                continue
            
            if line[0] == '=':
                category = line[1:].strip()
            elif line[0] == '~':
                # Parse as a child of the current node:
                child_name = line[1:-2].strip()
                if child_name not in prereqs:
                    prereqs[child_name] = []
                prereqs[child_name].append(nodes[-1].name)
            else:
                if line[0] == '>':
                    line = line[2:]  # Remove leading '>' if present

                # Parse as a node
                parts = [part.strip() for part in line.split('.')]
                name = parts[0]
                cost = int(parts[1]) if len(parts) > 1 else 0

                modified_stats = [variables[i] for i, v in enumerate(parts[2].replace('-', '')) if v == '1']
                stats = {var: int(v) for var, v in zip(modified_stats, parts[3].split(' '))}
                
                node = Node(name, category, cost, stats)
                nodes.append(node)
        except Exception as e:
            print(f"Error processing line {line_num+1}: {line}")
            print(f"Exception: {e}")
            break

    else:
        # Assign prereqs to nodes
        for node in nodes:
            if node.name in prereqs:
                node.prereqs = prereqs[node.name]
        return nodes
    return None

with open("data/stormtree.txt", 'r') as file:
    nodes = parse_file(file, storm_variables)
    if nodes is None:
        print("Failed to parse storm tree data.")
    else:
        with open("data/stormtree.json", 'w') as file:
            json.dump([node.to_dict() for node in nodes], file, indent=4)

with open("data/regiontree.txt", "r") as file:
    nodes = parse_file(file, human_variables)
    if nodes is None:
        print("Failed to parse region tree data.")
    else:
        with open("data/regiontree.json", 'w') as file:
            json.dump([node.to_dict() for node in nodes], file, indent=4)
    
with open("data/globaltree.txt", "r") as file:
    nodes = parse_file(file, human_variables)
    if nodes is None:
        print("Failed to parse global tree data.")
    else:
        with open("data/globaltree.json", 'w') as file:
            json.dump([node.to_dict() for node in nodes], file, indent=4)
            