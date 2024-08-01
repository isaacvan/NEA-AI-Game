

using MainNamespace;

namespace GridConfigurationNamespace
{
    public class GridFunctions
    {
        public static Map GenerateMap(Map mapToBeFilled)
        {
            mapToBeFilled = new Map();
            mapToBeFilled.Graphs = new List<Graph>();
            mapToBeFilled.Graphs.Add(GenerateGraph(mapToBeFilled));
            return new Map();
        }
        
        public static Graph GenerateGraph(Map currentMap)
        {
            int IDofGraph = currentMap.GetNextID();
            Graph graph = new Graph(IDofGraph);
            // graph = GENERATE NEW GRAPH FROM NARRATOR FUNCTION
            return null;
        }
    }

    public class Map
    {
        public List<Graph> Graphs { get; set; }

        public int GetNextID()
        {
            return (Graphs.Count);
        }
    }

    public class Node
    {
        public int NodeID { get; set; }
        public List<Node> ConnectedNodes { get; set; }
        // public List<>
        public int NodeDepth { get; set; }

        public Node(int id)
        {
            this.NodeID = id;
            this.ConnectedNodes = new List<Node>();
        }

        public void AddNeighbour(Node node)
        {
            this.ConnectedNodes.Add(node);
        }
    }

    public class Graph
    {
        public List<Node> Nodes { get; set; }
        public int Id { get; set; }

        public Graph(int id)
        {
            Nodes = new List<Node>();
            Id = id;
        }

        public Node AddNode(int id)
        {
            Node node = new Node(id);
            this.Nodes.Add(node);
            return node;
        }

        public bool ConnectNodes(Node nodeStart, Node nodeEnd) // true if successful
        {
            if (nodeStart.ConnectedNodes.Contains(nodeEnd))
            {
                // already connected
                Program.logger.Info($"Node with ID {nodeStart.NodeID} is already connected to Node with ID {nodeEnd.NodeID}.");
                return false;
            }
            else
            {
                nodeStart.ConnectedNodes.Add(nodeEnd);
                return true;
            }
        }

        public Node? GetNode(int id)
        {
            foreach (Node node in this.Nodes)
            {
                if (node.NodeID == id)
                {
                    return node;
                }
            }
            return null;
        }
    }

    public class Edge
    {
        public Node start { get; set; }
        public Node end { get; set; }
        public string name { get; set; } // e.g crumbling bridge, road, path etc   
    }
}