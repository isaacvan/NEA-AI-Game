

using System.Drawing;
using MainNamespace;
using Newtonsoft.Json;

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

        public static void FillNode(Node node)
        {
            // for now im going to fill it with an empty 2d array of tiles with the '.' char.
            node.tiles = new List<List<Tile>>();
            for (int i = 0; i < node.NodeWidth; i++) // for each x axis tile
            {
                node.tiles.Add(new List<Tile>());
                for (int j = 0; j < node.NodeHeight; j++) // for each y axis tile
                {
                    node.tiles[i].Add(new Tile() { tileChar = '.', tileXY = new Point(i, j) });
                }
            }
        }

        public static void DrawWholeNode(Node node)
        {
            for (int i = 0; i < node.NodeWidth; i++) // for each x axis tile
            {
                for (int j = 0; j < node.NodeHeight; j++) // for each y axis tile
                {
                    Console.Write(node.tiles[i][j].tileChar);
                }
                Console.WriteLine();
            }
        }

        public static void PlacePlayer(Node node, Point point)
        {
            node.tiles[point.X][point.Y].tileChar = 'P';
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
        public int NodeWidth { get; set; }
        public int NodeHeight { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<List<Tile>> tiles { get; set; }

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

    public class Tile
    {
        // the Tile class will represent the basic block in maps. A Tile represents a singular space that the player can move into and out of.
        // Therefore, Tiles will need all the basic attributes such as a character that displays what it is, x-y locations on its nodde, etc.
        // Tiles will also have a list of connected nodes, which will be used to determine the player's movement options.
        // And to clarify, NODES represent a small map. Eventually nodes will contain a 2d list of Tiles each. Nodes are like areas in a map, containing tiles.
        public char tileChar { get; set; }
        public Point tileXY { get; set; }
        
        
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