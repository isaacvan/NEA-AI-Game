using System.Drawing;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using Emgu.CV.Structure;
using Emgu.CV.XImgproc;
using EnemyClassesNamespace;
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
        public static string CurrentNodeName = "";
        public static int LastestGraphDepth = 0;

        public static Dictionary<string, string> CharsToMeanings = new Dictionary<string, string>()
            { { "NodeExit", ("$") }, { "Empty", "." }, { "Player", "P" }, { "Enemy", "E" } };

        public static Dictionary<string, Rgb?> CharsToRGB = new Dictionary<string, Rgb?>()
        {
            { "NodeExit", new Rgb(0, 183, 235) }, { "Empty", new Rgb(255, 255, 255) }, { "Player", null },
            { "Enemy", null }
        };

        public static Dictionary<Nature, Rgb> NatureToRGB = new Dictionary<Nature, Rgb>()
        {
            { Nature.aggressive, new Rgb(255, 87, 51) }, // aggressive red
            { Nature.neutral, new Rgb(255, 250, 160) }, // neutral yelloiw
            { Nature.timid, new Rgb(167, 199, 231) } // timid blue
        };

        // U+220F

        public static List<int> RedGreenBluePlayerVals = new List<int>() { 0, 0, 0 };

        public static async Task<Map> GenerateMap(Game game, GameSetup gameSetup, Conversation chat)
        {
            game.map = new Map();
            return game.map;
        }

        public static List<List<Tile>> PlacePlayer(Point point, List<List<Tile>> tiles, ref Game game)
        {
            tiles[point.X][point.Y].tileChar = CharsToMeanings["Player"][0];
            tiles[point.X][point.Y].playerHere = true;
            game.player.playerPos = point;
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

        public static void UpdateToNewNode(ref Game game, int newId, ref Tile oldTile, int oldId)
        {
            Point oldPos = game.player.playerPos;

            List<List<Tile>> newTiles = cloneTiles(game.map.GetCurrentNode(oldId).tiles);
            newTiles[oldPos.X][oldPos.Y].tileChar = GridFunctions.CharsToMeanings["NodeExit"][0];
            newTiles[oldPos.X][oldPos.Y].tileDesc = "NodeExit";
            newTiles[oldPos.X][oldPos.Y].playerHere = false;
            // newTiles;
            game.map.SetCurrentNodeTilesContents(newTiles, oldId);


            if (game.map.Graphs[game.map.Graphs.Count - 1].Nodes[newId] != null)
            {
                game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer = newId;
            }
            else
            {
                throw new Exception("No node found");
            }

            CurrentNodeId = newId;
            CurrentNodeName = game.map.GetCurrentNode().NodePOI;
            game.map.SetCurrentNodeTilesContents(GridFunctions.PlacePlayer(
                GridFunctions.GetPlayerStartPos(ref game, oldId, newId),
                game.map.GetCurrentNode(newId).tiles, ref game));
        }

        public static void MoveEnemy(Point oldPos, Point newPos, ref Game game)
        {
            List<List<Tile>> oldTiles = cloneTiles(game.map.GetCurrentNode().tiles);
            Tile oldTile = oldTiles[oldPos.X][oldPos.Y];
            Enemy enemyToMove = oldTile.enemyOnTile;
            oldTiles[oldPos.X][oldPos.Y].enemyOnTile = null;
            oldTiles[oldPos.X][oldPos.Y].tileChar = CharsToMeanings[oldTiles[oldPos.X][oldPos.Y].tileDesc][0];
            oldTiles[newPos.X][newPos.Y].enemyOnTile = enemyToMove;
            oldTiles[newPos.X][newPos.Y].tileChar = CharsToMeanings["Enemy"][0];
            game.map.GetCurrentNode().tiles = oldTiles;
        }

        public static bool
            MovePlayer(string input, ref Point PlayerPos, ref Game game,
                ref Tile oldTile) // return false if doing something other than moving
        {
            if (Program.GetAllowedInputs("Move").Contains(input))
            {
                Point oldPos = PlayerPos;

                if (oldTile == null && game.map.GetCurrentNode().tiles[oldPos.X][oldPos.Y].tileDesc == "NodeExit")
                {
                    oldTile = game.map.Graphs[game.map.Graphs.Count - 1]
                        .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                        .tiles[oldPos.X][oldPos.Y];
                    oldTile.tileChar = GridFunctions.CharsToMeanings["NodeExit"][0];
                    oldTile.tileDesc = "NodeExit";
                    oldTile.playerHere = false;
                }
                else if (oldTile == null)
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
                        .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                        .tiles[oldPos.X][oldPos.Y] =
                    oldTile.clone();

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

                // capture old tile if it isnt a node exit
                if (game.map.Graphs[game.map.Graphs.Count - 1]
                        .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                        .tiles[PlayerPos.X][PlayerPos.Y].tileDesc == "NodeExit")
                {
                    // dont pick up the new tile and instead set it to null
                    oldTile = null;

                    // move the player onto the exit point
                    game.map.Graphs[game.map.Graphs.Count - 1]
                        .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                        .tiles[PlayerPos.X][PlayerPos.Y].playerHere = true;
                    game.map.Graphs[game.map.Graphs.Count - 1]
                        .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer]
                        .tiles[PlayerPos.X][PlayerPos.Y].tileChar = CharsToMeanings["Player"][0];
                }
                else
                {
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
                }


                return true;
            }

            return false;
        }

        public static Point GetPlayerStartPos(ref Game game, int oldId = -1, int newId = -1)
        {
            if (oldId != -1 && newId != -1)
            {
                Point p = new Point();
                if (oldId > newId) // going backwards through an entry node
                {
                    List<(Point, int)> exitPointsOfNewNode = game.map.GetCurrentNode(newId).ConnectedExitNodes;
                    foreach (var (exitPoint, exitId) in exitPointsOfNewNode)
                    {
                        if (exitId == oldId)
                        {
                            p = exitPoint;
                        }
                    }
                }
                else if (oldId < newId) // going forwards through an exit node
                {
                    List<(Point, int)> entryPointsOfNewNode = game.map.GetCurrentNode(newId).ConnectedEntryNodes;
                    foreach (var (entryPoint, entryId) in entryPointsOfNewNode)
                    {
                        if (entryId == oldId)
                        {
                            p = entryPoint;
                        }
                    }
                }
                else
                {
                    throw new Exception("oldId = newId??");
                }

                return p;
            }
            else
            {
                int h = game.map.GetCurrentNode().NodeHeight;
                Point p = new Point(0, h / 2);
                game.player.playerPos = p;
                return p;
            }
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
                        WriteTileChar(node, i, j, r, g, b);
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
                            WriteTileChar(node, i, j, Math.Round(brightness * r, 0), Math.Round(brightness * g, 0), Math.Round(brightness * b, 0), brightness);
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteTileChar(Node node, int i, int j, double r, double g, double b, float brightness = 1)
        {
            if (node.tiles[i][j].playerHere)
            {
                Console.Write(
                    $"\x1b[38;2;{RedGreenBluePlayerVals[0]};{RedGreenBluePlayerVals[1]};{RedGreenBluePlayerVals[2]}m{node.tiles[i][j].tileChar} \x1b[0m");
            } else if (node.tiles[i][j].enemyOnTile != null)
            {
                Nature nature = node.tiles[i][j].enemyOnTile.nature;
                Console.Write($"\x1b[38;2;{Math.Round(NatureToRGB[nature].Red*brightness, 0)};{Math.Round(NatureToRGB[nature].Green*brightness, 0)};{Math.Round(NatureToRGB[nature].Blue*brightness, 0)}m{node.tiles[i][j].tileChar} \x1b[0m");
            }
            else
            {
                Console.Write($"\x1b[38;2;{r};{g};{b}m{node.tiles[i][j].tileChar} \x1b[0m");
            }
        }

        public static Node PopulateNodeWithTiles(Node node, Graph graph)
        {
            node.tiles = new List<List<Tile>>();
            if (node.NodeWidth == 0 || node.NodeHeight == 0)
            {
                node.NodeWidth = 20;
                node.NodeHeight = 20;
            }

            for (int i = 0; i < node.NodeWidth; i++)
            {
                node.tiles.Add(new List<Tile>());
                for (int j = 0; j < node.NodeHeight; j++)
                {
                    node.tiles[i].Add(new Tile('.', new Point(i, j), "Empty"));
                }
            }

            return node;
        }


        public static List<List<Tile>> cloneTiles(List<List<Tile>> tiles)
        {
            List<List<Tile>> newTiles = new List<List<Tile>>();
            for (int i = 0; i < tiles.Count; i++)
            {
                newTiles.Add(new List<Tile>());
                for (int j = 0; j < tiles[i].Count; j++)
                {
                    newTiles[i].Add(tiles[i][j].clone());
                }
            }

            return newTiles;
        }
    }

    public class Map
    {
        public List<Graph> Graphs { get; set; }
        public int CurrentGraphPointer { get; set; }

        public Map()
        {
            Graphs = new List<Graph>();
            CurrentGraphPointer = 0;
        }

        public Node GetCurrentNode(int? nodeID = null)
        {
            if (nodeID.HasValue)
            {
                return Graphs[Graphs.Count - 1].Nodes[nodeID.Value];
            }

            return Graphs[Graphs.Count - 1].Nodes[Graphs[Graphs.Count - 1].CurrentNodePointer];
        }

        public void SetCurrentNodeTilesContents(List<List<Tile>> tiles, int id = -1)
        {
            if (id < 0)
            {
                Graphs[Graphs.Count - 1].Nodes[Graphs[Graphs.Count - 1].CurrentNodePointer].tiles = tiles;
            }
            else
            {
                Graphs[Graphs.Count - 1].Nodes[id].tiles = tiles;
            }
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
        [Newtonsoft.Json.JsonIgnore] public List<(Point, int)> ConnectedEntryNodes { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<(Point, int)> ConnectedExitNodes { get; set; }
        public List<string> ConnectedNodesEdges { get; set; }
        public string NodePOI { get; set; }
        public int NodeDepth { get; set; }
        [Newtonsoft.Json.JsonIgnore] public int NodeWidth { get; set; }
        [Newtonsoft.Json.JsonIgnore] public int NodeHeight { get; set; }
        public bool Milestone { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<List<Tile>> tiles { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<EnemySpawn> enemies { get; set; }

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
            this.ConnectedEntryNodes = new List<(Point, int)>();
            this.ConnectedExitNodes = new List<(Point, int)>();
        }


        public void AddNeighbour(Node node)
        {
            this.ConnectedNodes.Add(node.NodeID);
        }

        public void InitialiseEnemies(Game game)
        {
            /*
             public Point spawnPoint { get; set; }
               public Nature nature { get; set; }
               public string name { get; set; }
               public bool boss { get; set; }
             */

            if (enemies != null)
                if (enemies.Count > 0)
                    return;

            Random random = new Random();
            List<EnemySpawn> spawns = new List<EnemySpawn>();
            int enemyCount = random.Next(1, 4);
            if (Milestone) enemyCount = 1;
            for (var i = 0; i < enemyCount; i++)
            {
                spawns.Add(new EnemySpawn());
                Point enemyPoint = new Point();
                bool validPoint = false;
                while (!validPoint)
                {
                    enemyPoint.X = random.Next(1, this.NodeWidth - 2);
                    enemyPoint.Y = random.Next(1, this.NodeHeight - 2);
                    if (tiles[enemyPoint.X][enemyPoint.Y].tileDesc == "Empty" && tiles[enemyPoint.X][enemyPoint.Y].enemyOnTile == null)
                    {
                        validPoint = true;
                    }
                }

                if (Milestone)
                {
                    spawns[i].boss = true;
                    spawns[i].spawnPoint = enemyPoint;
                    spawns[i].name = game.enemyFactory.enemyTypes[random.Next(0, game.enemyFactory.enemyTypes.Count)];
                }
                else
                {
                    spawns[i].boss = false;
                    spawns[i].spawnPoint = enemyPoint;
                    spawns[i].name = game.enemyFactory.enemyTypes[random.Next(0, game.enemyFactory.enemyTypes.Count)];
                }

                spawns[i].id = UtilityFunctions.nextEnemyId;
            }

            enemies = spawns;

            PlaceEnemiesOnNode(game);
        }

        public void PlaceEnemiesOnNode(Game game)
        {
            if (enemies.Count == 0) throw new Exception("No enemies placed");
            foreach (EnemySpawn spawn in enemies)
            {
                EnemyTemplate template = game.enemyFactory.enemyTemplates[spawn.name];

                if (spawn.spawnPoint != Point.Empty && spawn.currentLocation == Point.Empty)
                {
                    tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].tileChar = GridFunctions.CharsToMeanings["Enemy"][0];
                    tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile =
                        game.enemyFactory.CreateEnemy(template, NodeDepth, spawn.spawnPoint, spawn.id);
                    enemies[enemies.IndexOf(spawn)].nature = tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile.nature;
                } else if (spawn.currentLocation != Point.Empty && spawn.spawnPoint == Point.Empty)
                {
                    tiles[spawn.currentLocation.X][spawn.currentLocation.Y].tileChar = GridFunctions.CharsToMeanings["Enemy"][0];
                    tiles[spawn.currentLocation.X][spawn.currentLocation.Y].enemyOnTile =
                        game.enemyFactory.CreateEnemy(template, NodeDepth, spawn.currentLocation, spawn.id);
                    enemies[enemies.IndexOf(spawn)].nature = tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile.nature;
                }
            }
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
        public int? entryNodePointerId { get; set; }
        public Enemy? enemyOnTile { get; set; }

        public Tile(char tileChar, Point tileXY, string tileDesc, int? nodeEntryPointer = null,
            int? nodeExitPointer = null)
        {
            this.tileChar = tileChar;
            this.tileXY = tileXY;
            this.tileDesc = tileDesc;
            this.playerHere = false;
            enemyOnTile = null;
            if (tileDesc != null)
            {
                rgb = GridFunctions.CharsToRGB[tileDesc];
            }

            if (tileDesc == "Empty" || tileDesc == "Enemy")
            {
                walkable = true;
                exitNodePointerId = null;
                entryNodePointerId = null;
            }
            else if (tileDesc == "NodeExit" && nodeEntryPointer == null && nodeExitPointer != null)
            {
                walkable = true;
                exitNodePointerId = nodeExitPointer;
                entryNodePointerId = null;
            }
            else if (tileDesc == "NodeExit" && nodeEntryPointer != null && nodeExitPointer == null)
            {
                walkable = true;
                exitNodePointerId = null;
                entryNodePointerId = nodeEntryPointer;
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
        [Newtonsoft.Json.JsonIgnore] public int GraphDepth { get; set; }

        public Graph(int id, List<Node> nodes, int depth)
        {
            Nodes = nodes;
            Id = id;
            GraphDepth = depth;
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

        public void SetEntryAndExits()
        {
            List<Node> nodes = new List<Node>();

            foreach (Node node in Nodes)
            {
                // entry nodes
                int ycounter = 0;
                List<List<int>> listOfNodeConnections = new List<List<int>>();
                for (int i = 0; i < Nodes.Count; i++)
                {
                    Node tempNode = Nodes[i];
                    List<int> ints = tempNode.ConnectedNodes;
                    listOfNodeConnections.Add(ints);
                }

                List<(Point, int)> entryPoints = new List<(Point, int)>();
                int nodeid = node.NodeID;
                List<int> NodeIdsToPointTo = new List<int>();

                try
                {
                    for (int j = 0; j < listOfNodeConnections[nodeid].Count; j++)
                    {
                        if (Nodes[listOfNodeConnections[nodeid][j]].NodeDepth <
                            Nodes[nodeid]
                                .NodeDepth) // then start node corresponding to listOfNodeConnections[nodeid][j]
                        {
                            entryPoints.Add(new(new Point(0, 0), listOfNodeConnections[nodeid][j]));
                        }
                        else if (Nodes[listOfNodeConnections[nodeid][j]].NodeDepth >
                                 Nodes[nodeid].NodeDepth)
                        {
                            NodeIdsToPointTo.Add(listOfNodeConnections[nodeid][j]);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }


                ycounter = 0;
                int entries = entryPoints.Count;
                if (node.NodeID == 0) entries = 0;
                double h2 = node.NodeHeight - entries;
                int gap2 = (int)Math.Round(h2 / (entries + 1), 0);
                for (int j = 0; j < entryPoints.Count; j++)
                {
                    if (ycounter != 0)
                    {
                        //ycounter++;
                    }

                    ycounter += gap2;
                    Point ExitPoint = new Point(0, ycounter);
                    node.tiles[ExitPoint.X][ExitPoint.Y] =
                        new Tile(Convert.ToChar(GridFunctions.CharsToMeanings["NodeExit"]),
                            new Point(ExitPoint.X, ExitPoint.Y), "NodeExit", entryPoints[j].Item2);
                    node.ConnectedEntryNodes.Add((new Point(ExitPoint.X, ExitPoint.Y), entryPoints[j].Item2));
                }


                // exit nodes
                int exits = node.ConnectedNodes.Count - entries;
                if (node.NodeID == Nodes.Count) exits = 0;
                double h = node.NodeHeight - exits;
                int gap = (int)Math.Round(h / (exits + 1), 0);
                ycounter = 0;
                int index = 0;
                for (int i = 0; i < exits; i++)
                {
                    if (ycounter != 0)
                    {
                        ycounter++;
                    }

                    ycounter += gap;
                    Point ExitPoint = new Point(node.NodeWidth - 1, ycounter);
                    int newNodeId;
                    try
                    {
                        newNodeId = NodeIdsToPointTo[index];
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    index++;
                    node.tiles[ExitPoint.X][ExitPoint.Y] =
                        new Tile(Convert.ToChar(GridFunctions.CharsToMeanings["NodeExit"]),
                            new Point(ExitPoint.X, ExitPoint.Y), "NodeExit", newNodeId);
                    (Point, int) exitTuple = new(new Point(ExitPoint.X, ExitPoint.Y), newNodeId);
                    node.ConnectedExitNodes.Add(exitTuple);
                }

                nodes.Add(node);
            }

            Nodes = nodes;
        }
    }
}