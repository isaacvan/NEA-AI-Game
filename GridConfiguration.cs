using System.Drawing;
using System.Runtime.CompilerServices;
using Emgu.CV.XImgproc;
using GameClassNamespace;
using GPTControlNamespace;
using MainNamespace;
using Newtonsoft.Json;
using OpenAI_API.Chat;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace GridConfigurationNamespace
{
    public class GridFunctions
    {
        public static Dictionary<string, string> CharsToMeanings = new Dictionary<string, string>()
            { { "NodeExit", ("$") }, { "Empty", "." }, { "Player", "P" } };
        // U+220F

        public static List<int> RedGreenBluePlayerVals = new List<int>() { 0, 0, 0 };

        public static async Task<Map> GenerateMap(Game game, GameSetup gameSetup, Conversation chat)
        {
            game.map = new Map();
            return game.map;
        }

        public static List<List<Tile>> PlacePlayer(Point point, List<List<Tile>> tiles)
        {
            tiles[point.X][point.Y].tileChar = CharsToMeanings["Player"][0];
            tiles[point.X][point.Y].playerHere = true;
            return tiles;
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
                    node.tiles[i].Add(new Tile('.', new Point(i, j), "Empty"));
                }
            }

            return node;
        }

        public static void PlaceTile(ref Node node, int x, int y, Tile tile)
        {
            node.tiles[y][x] = tile;
        }

        public static bool CheckIfOutOfBounds(List<List<Tile>> tiles, Point PlayerPos, string input)
        {
            switch (input.ToLower())
            {
                case "w":
                    PlayerPos.Y -= 1;
                    break;
                case "a":
                    PlayerPos.X -= 1;
                    break;
                case "s":
                    PlayerPos.Y += 1;
                    break;
                case "d":
                    PlayerPos.X += 1;
                    break;
                default:
                    Console.WriteLine("Invalid input");
                    break;
            }

            try
            {
                if (PlayerPos.X < 0 || PlayerPos.X > tiles[PlayerPos.Y].Count || PlayerPos.Y < 0 ||
                    PlayerPos.Y > tiles[PlayerPos.X].Count)
                {
                    return false;
                }
                else if (tiles[PlayerPos.X][PlayerPos.Y].tileChar != CharsToMeanings["Empty"][0])
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
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

        public static void UpdateToNewNode(ref Game game, int Id)
        {
            if (game.map.Graphs[game.map.Graphs.Count - 1].Nodes[Id] != null)
            {
                game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer = Id;
            }
            else
            {
                throw new Exception("No node found");
            }
        }

        public static bool MovePlayer(string input, ref Point PlayerPos, ref Game game) // return false if doing something other than moving
        {
            if (Program.GetAllowedInputs("Move").Contains(input))
            {
                game.map.Graphs[game.map.Graphs.Count - 1].Nodes[game.map.GetCurrentNode().NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                    .tileChar = Convert.ToChar(CharsToMeanings["Empty"]);
                game.map.Graphs[game.map.Graphs.Count - 1].Nodes[game.map.GetCurrentNode().NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                    .playerHere = false;
                
                switch (input.ToLower())
                {
                    // assuming input validated already
                    case "w":
                        PlayerPos.Y -= 1;
                        break;
                    case "a":
                        PlayerPos.X -= 1;
                        break;
                    case "s":
                        PlayerPos.Y += 1;
                        break;
                    case "d":
                        PlayerPos.X += 1;
                        break;
                }
                
                game.map.Graphs[game.map.Graphs.Count - 1].Nodes[game.map.GetCurrentNode().NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                    .tileChar = Convert.ToChar(CharsToMeanings["Player"]);

                //game.map.Graphs[game.map.CurrentGraph.Id].Nodes[game.map.CurrentNode.NodeID].tiles[PlayerPos.Y][PlayerPos.X]
                //    .tileChar = CharsToMeanings["Player"][0];
                game.map.Graphs[game.map.Graphs.Count - 1].Nodes[game.map.GetCurrentNode().NodeID].tiles[PlayerPos.X][PlayerPos.Y]
                    .playerHere = true;

                return true;
            }

            return false;
        }

        public static void DrawWholeNode(Game game)
        {
            Node node = game.map.Graphs[game.map.Graphs.Count - 1].Nodes[game.map.GetCurrentNode().NodeID];
            Player player = game.player;
            Point playerPos = player.playerPos;
            int sightRange = player.sightRange;


            Console.Clear();
            UtilityFunctions.clearScreen(player);

            // calculate drawing bounds, keeping player centered if within bounds
            int startX = Math.Max(0, playerPos.X - sightRange);
            int startY = Math.Max(0, playerPos.Y - sightRange);
            int endX = Math.Min(node.NodeWidth, playerPos.X + sightRange + 1);
            int endY = Math.Min(node.NodeHeight, playerPos.Y + sightRange + 1);

            for (int j = startY; j < endY; j++) // for each y-axis tile within range
            {
                for (int i = startX; i < endX; i++) // for each x-axis tile within range
                {
                    int deltaX = j - playerPos.Y;
                    int deltaY = i - playerPos.X;
                    double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY) * 1.1; // get diagonal dist

                    // determine if the tile is within sight range
                    if (distance <= sightRange)
                    {
                        if (node.tiles[i][j].playerHere)
                        {
                            Console.Write(
                                $"\x1b[38;2;{RedGreenBluePlayerVals[0]};{RedGreenBluePlayerVals[1]};{RedGreenBluePlayerVals[2]}m{node.tiles[i][j].tileChar} ");
                        }
                        else if (node.tiles[i][j].tileDesc == "Empty")
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"{node.tiles[i][j].tileChar} ");
                        }
                        else if (node.tiles[i][j].tileDesc == "ExitNode")
                        {
                            Console.Write($"{node.tiles[i][j].tileChar} ");
                        }
                    }
                    // if outside sight range but still within the square frame
                    else if (distance * 1.2 <= sightRange * 2)
                    {
                        int brightness = (int)(255 * (1.0 - (distance - sightRange) / (sightRange * 0.5)));
                        brightness = Math.Clamp(brightness, 0, 255);

                        if (brightness == 0)
                        {
                            Console.Write("  ");
                        }
                        else
                        {
                            Console.Write(
                                $"\x1b[38;2;{brightness};{brightness};{brightness}m{node.tiles[i][j].tileChar} ");
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public class Map
    {
        public List<Graph> Graphs { get; set; }

        public Node GetCurrentNode()
        {
            return Graphs[Graphs.Count - 1].Nodes[Graphs[Graphs.Count - 1].CurrentNodePointer];
        }

        public void SetCurrentNodeTilesContents(List<List<Tile>> tiles)
        {
            Graphs[Graphs.Count - 1].Nodes[Graphs[Graphs.Count - 1].CurrentNodePointer].tiles = tiles;
        }

        public async Task<Graph> AppendGraph(Graph graphToAppend)
        {
            Graphs.Add(graphToAppend);
            return Graphs[Graphs.Count - 1];
        }

        public int GetNextID()
        {
            return (Graphs.Count);
        }
    }

    public class Node
    {
        public int NodeID { get; set; }
        public List<int> ConnectedNodes { get; set; }
        public List<string> ConnectedNodesEdges { get; set; }
        public string NodePOI { get; set; }
        public int NodeDepth { get; set; }
        [Newtonsoft.Json.JsonIgnore] public int NodeWidth { get; set; }
        [Newtonsoft.Json.JsonIgnore] public int NodeHeight { get; set; }
        public bool Milestone { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<List<Tile>> tiles { get; set; }

        public Node(int id, int width, int height, string nodePOI, bool milestone)
        {
            this.NodeID = id;
            this.ConnectedNodes = new List<int>();
            this.ConnectedNodesEdges = new List<string>();
            this.NodeDepth = 0;
            this.NodeWidth = width;
            this.NodeHeight = height;
            this.NodePOI = nodePOI;
            this.Milestone = milestone;
            this.tiles = new List<List<Tile>>();
        }
        
        

        public void AddNeighbour(Node node)
        {
            this.ConnectedNodes.Add(node.NodeID);
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
        public bool playerHere { get; set; }

        public Tile(char tileChar, Point tileXY, string tileDesc)
        {
            this.tileChar = tileChar;
            this.tileXY = tileXY;
            this.tileDesc = tileDesc;
            this.playerHere = false;
        }
    }

    public class Graph
    {
        public List<Node> Nodes { get; set; }
        public int Id { get; set; }
        [Newtonsoft.Json.JsonIgnore] public int CurrentNodePointer { get; set; }

        public Graph(int id, List<Node> nodes)
        {
            Nodes = nodes;
            Id = id;
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