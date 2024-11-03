using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using Emgu.CV.Structure;
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
        public static int CurrentNodeId = 0;

        public static Dictionary<string, string> CharsToMeanings = new Dictionary<string, string>()
            { { "NodeExit", ("$") }, { "Empty", "." }, { "Player", "P" } };

        public static Dictionary<string, Rgb?> CharsToRGB = new Dictionary<string, Rgb?>()
            { { "NodeExit", new Rgb(0, 183, 235) }, { "Empty", new Rgb(255, 255, 255) }, { "Player", null } };

        public static List<int>
            PointedToNodeIds = new List<int>(); // list of Ids that have been pointed to already; resets every graph
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
            // return false if out of bounds
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
                else if (!tiles[PlayerPos.X][PlayerPos.Y].walkable)
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

            CurrentNodeId = Id;
            game.map.SetCurrentNodeTilesContents(GridFunctions.PlacePlayer(GridFunctions.GetPlayerStartPos(ref game),
                game.map.GetCurrentNode().tiles));
        }

        public static bool
            MovePlayer(string input, ref Point PlayerPos, ref Game game,
                ref Tile oldTile) // return false if doing something other than moving
        {
            if (Program.GetAllowedInputs("Move").Contains(input))
            {
                Point oldPos = PlayerPos;

                if (oldTile == null)
                {
                    oldTile = new Tile(CharsToMeanings["Empty"][0], oldPos, "Empty");
                }

                /*
                game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles[oldPos.X][oldPos.Y]
                    .playerHere = false;
                game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles[oldPos.X][oldPos.Y]
                    .tileChar = oldTile.tileChar;
                    */

                game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles[oldPos.X][oldPos.Y] = oldTile;

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

                // capture old tile
                oldTile = game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                    .tiles[PlayerPos.X][PlayerPos.Y];
                Tile temp = oldTile.clone();

                // set player at new pos
                game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                    .tiles[PlayerPos.X][PlayerPos.Y].playerHere = true;
                game.map.Graphs[game.map.Graphs.Count - 1]
                    .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                    .tiles[PlayerPos.X][PlayerPos.Y].tileChar = CharsToMeanings["Player"][0];

                oldTile = temp.clone();
                
                return true;
            }

            return false;
        }

        public static Point GetPlayerStartPos(ref Game game)
        {
            int h = game.map.GetCurrentNode().NodeHeight;
            Point p = new Point(0, h / 2);
            game.player.playerPos = p;
            return p;
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

                    int r = 0;
                    int g = 0;
                    int b = 0;

                    // rgb
                    if (node.tiles[i][j].rgb != null) // player not here
                    {
                        r = (int)node.tiles[i][j].rgb.Value.Red;
                        g = (int)node.tiles[i][j].rgb.Value.Green;
                        b = (int)node.tiles[i][j].rgb.Value.Blue;
                    }

                    // determine if the tile is within sight range
                    // \x1b[38;2;{r};{g};{b}m
                    // \x1b[38;2;255m
                    if (distance <= sightRange)
                    {
                        if (node.tiles[i][j].playerHere)
                        {
                            Console.Write(
                                $"\x1b[38;2;{RedGreenBluePlayerVals[0]};{RedGreenBluePlayerVals[1]};{RedGreenBluePlayerVals[2]}m{node.tiles[i][j].tileChar} \x1b[0m");
                        }
                        else
                        {
                            Console.Write($"\x1b[38;2;{r};{g};{b}m{node.tiles[i][j].tileChar} \x1b[0m");
                        }
                    }
                    // if outside sight range but still within the square frame
                    else if (distance * 1.2 <= sightRange * 2)
                    {
                        float brightness = (float)(1.0 - (distance - sightRange) / (sightRange * 0.5));
                        brightness = Math.Min(brightness, 1);
                        brightness = Math.Max(brightness, 0);

                        if (brightness == 0)
                        {
                            Console.Write("  ");
                        }
                        else
                        {
                            Console.Write(
                                $"\x1b[38;2;{Math.Round(brightness * r, 0)};{Math.Round(brightness * g, 0)};{Math.Round(brightness * b, 0)}m{node.tiles[i][j].tileChar} \x1b[0m");
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public static int GetNextNodeId()
        {
            PointedToNodeIds.Add(PointedToNodeIds.Count + 1);
            return PointedToNodeIds.Count;
        }
    }

    public class Map
    {
        public List<Graph> Graphs { get; set; }

        public Map()
        {
            Graphs = new List<Graph>();
        }

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
        public Rgb? rgb { get; set; }
        public bool walkable { get; set; }
        public int? exitNodePointerId { get; set; }

        public Tile(char tileChar, Point tileXY, string tileDesc)
        {
            this.tileChar = tileChar;
            this.tileXY = tileXY;
            this.tileDesc = tileDesc;
            this.playerHere = false;
            if (tileDesc != null)
            {
                rgb = GridFunctions.CharsToRGB[tileDesc];
            }
            
            if (tileDesc == "Empty")
            {
                walkable = true;
                exitNodePointerId = null;
            }
            else if (tileDesc == "NodeExit")
            {
                walkable = true;
                exitNodePointerId = GridFunctions.GetNextNodeId();
            }
        }

        public Tile clone()
        {
            Tile newTile = new Tile('.', new Point(), null);
            foreach (PropertyInfo property in typeof(Tile).GetProperties())
            {
                PropertyInfo info = property;
                object value = info.GetValue(this, null);
                typeof(Tile).GetProperty(property.Name).SetValue(newTile, value);
            }
            return newTile;
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