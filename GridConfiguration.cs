using System.Drawing;
using GameClassNamespace;
using GPTControlNamespace;
using MainNamespace;
using Newtonsoft.Json;
using OpenAI_API.Chat;

namespace GridConfigurationNamespace
{
    public class GridFunctions
    {
        public Dictionary<string, string> CharsToMeanings = new Dictionary<string, string>()
            { { "NodeExit", "U+220F" }, { "Empty", "." } };

        public async Task<Map> GenerateMap(Game game, GameSetup gameSetup, Conversation chat)
        {
            Game newGame = await gameSetup.GenerateGraphStructure(chat, game);
            game.map = newGame.map;
            return game.map;
        }

        public static Node FillNode(Node node)
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

            return node;
        }

        public static bool CheckIfOutOfBounds(List<List<Tile>> tiles, Point PlayerPos, string input)
        {
            switch (input.ToLower())
            {
                case "w":
                    PlayerPos.Y += 1;
                    break;
                case "a":
                    PlayerPos.X -= 1;
                    break;
                case "s":
                    PlayerPos.Y -= 1;
                    break;
                case "d":
                    PlayerPos.X += 1;
                    break;
                default:
                    Console.WriteLine("Invalid input");
                    break;
            }

            if (PlayerPos.X < 0 || PlayerPos.X >= tiles.Count || PlayerPos.Y < 0 ||
                PlayerPos.Y >= tiles[PlayerPos.X].Count)
            {
                return false;
            }
            else if (tiles[PlayerPos.X][PlayerPos.Y].tileChar != '.')
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool CheckIfNewNode(List<List<Tile>> tiles, Point PlayerPos)
        {
            if (tiles[PlayerPos.X][PlayerPos.Y].tileDesc == "NodeExit")
            {
                return true;
            }

            return false;
        }

        public static void UpdateToNewNode(ref Game game)
        {
            if (game.map.Graphs[game.map.GetNextID()].Nodes[0] != null)
            {
                game.map.CurrentNode = game.map.Graphs[game.map.GetNextID()].Nodes[0];
            }
            else
            {
                throw new Exception("No node found");
            }
        }

        public static void MovePlayer(string input, ref Point PlayerPos, ref Game game)
        {
            game.map.Graphs[game.map.CurrentGraph.Id].Nodes[game.map.CurrentNode.NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                .tileChar = '.';
            switch (input.ToLower())
            {
                // assuming input validated already
                case "w":
                    PlayerPos.Y += 1;
                    break;
                case "a":
                    PlayerPos.X -= 1;
                    break;
                case "s":
                    PlayerPos.Y -= 1;
                    break;
                case "d":
                    PlayerPos.X += 1;
                    break;
                default:
                    Console.WriteLine("Invalid input");
                    break;
            }

            game.map.Graphs[game.map.CurrentGraph.Id].Nodes[game.map.CurrentNode.NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                .tileChar = 'P';
        }

        public static void DrawWholeNode(Node node, Point PlayerPos)
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
    }

    public class Map
    {
        public List<Graph> Graphs { get; set; }
        public Node CurrentNode { get; set; }
        public Graph CurrentGraph { get; set; }

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
        public string tileDesc { get; set; }
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
                Program.logger.Info(
                    $"Node with ID {nodeStart.NodeID} is already connected to Node with ID {nodeEnd.NodeID}.");
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