using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using Emgu.CV.Structure;
using Emgu.CV.XImgproc;
using EnemyClassesNamespace;
using GameClassNamespace;
using GPTControlNamespace;
using ItemFunctionsNamespace;
using MainNamespace;
using Microsoft.VisualBasic;
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
        {
            { "NodeExit", ("$") }, { "Empty", "." }, { "Player", "@" }, { "Enemy", "E" }, { "Objective", "?" },
            { "Structure", " " }
        };

        public static Dictionary<string, Rgb?> CharsToRGB = new Dictionary<string, Rgb?>()
        {
            { "NodeExit", new Rgb(0, 183, 235) }, { "Empty", new Rgb(255, 255, 255) }, { "Player", null },
            { "Enemy", null }, { "Objective", new Rgb(251, 198, 207) }
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
                    return true;
                    break;
            }

            try
            {
                if (PlayerPos.X < 0 || PlayerPos.X > tiles.Count + 1 || PlayerPos.Y < 0 ||
                    PlayerPos.Y > tiles[PlayerPos.X].Count + 1)
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

            game.gameState.currentNodeId = newId;

            if (Program.game.map.GetCurrentNode().Obj == null)
            {
                Task.Run(() => { Program.game.map.GetCurrentNode().AddObjectiveToNode(false); }).GetAwaiter()
                    .GetResult();
            }
        }

        public static void MoveEnemy(Point oldPos, Point newPos, ref Game game)
        {
            try
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
            catch
            {
                return;
            }
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

                    // check for dead enemies
                    if (node.tiles[i][j].enemyOnTile == null && node.tiles[i][j].playerHere == false)
                    {
                        if (node.tiles[i][j].tileChar == CharsToMeanings["Enemy"][0])
                        {
                            string str = "" + node.tiles[i][j].tileDesc;
                            node.tiles[i][j].tileChar = CharsToMeanings[$"{str}"][0];
                        }
                    }

                    // double check for player tile
                    if (node.tiles[i][j].playerHere)
                    {
                        if (node.tiles[i][j].tileChar != CharsToMeanings["Player"][0])
                        {
                            node.tiles[i][j].tileChar = CharsToMeanings[$"Player"][0];
                        }
                    }

                    // check for complete objective
                    if (node.tiles[i][j].objective != null)
                    {
                        if (node.tiles[i][j].objective.IsCompleted)
                        {
                            if (node.tiles[i][j].tileChar != CharsToMeanings["Empty"][0])
                            {
                                node.tiles[i][j].tileChar = CharsToMeanings[$"Empty"][0];
                                node.tiles[i][j].rgb = CharsToRGB[$"Empty"];
                            }
                        }
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
                            WriteTileChar(node, i, j, Math.Round(brightness * r, 0), Math.Round(brightness * g, 0),
                                Math.Round(brightness * b, 0), brightness);
                        }
                    }
                }

                

                Console.WriteLine();
            }
            
            DrawSideStats(sightRange, game, Console.GetCursorPosition());

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n\n");

            if (game.uiConstructer.narrationPending)
            {
                UtilityFunctions.TypeText(new TypeText(), game.uiConstructer.currentNarration);
                game.uiConstructer.narrationPending = false;
            }
            else
            {
                Console.WriteLine(game.uiConstructer.currentNarration);
            }
        }

        public static void DrawSideStats(int sightRange, Game game, (int, int) initialCursor)
        {
            Point p = game.player.playerPos;
            var tiles = game.map.GetCurrentNode().tiles;
            int indent = 0;
            int constIndent = 2;
            if (p.X < sightRange + 1)
            {
                indent += p.X + sightRange + constIndent;
            }
            else
            {
                indent += 2 * sightRange + constIndent;
            }

            if (p.X > tiles.Count - sightRange - 1)
            {
                indent = indent - sightRange + (tiles.Count - p.X) - 1;
            }

            indent *= 2;

            int height = 0;
            int yconst = 8;
            if (p.Y < sightRange + 1)
            {
                height += p.Y + sightRange;
            }
            else
            {
                height += 2 * sightRange;
            }

            if (p.Y > tiles[0].Count - sightRange - 1)
            {
                height = height - sightRange + (tiles[0].Count - p.Y) - 1;
            }

            List<string> rowsToPrint = game.uiConstructer.drawCharacterMenu(game, true, true);

            /*
            List<string> rowsToPrint = new List<string>()
            {
                "Player Stats",
                $"Strength: {game.player.Strength}",
                $"Dexterity: {game.player.Dexterity}",
                $"Intelligence: {game.player.Intelligence}",
                $"Constitution: {game.player.Constitution}",
                $"Charisma: {game.player.Charisma}"
            };
            */


            int newConstIndentForSecondColumn = 0;
            for (int i = 0; i <= height; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(indent, yconst + i);
                if (rowsToPrint.Count >= i)
                    rowsToPrint.Add("");
                Console.Write($"| {rowsToPrint[i]}");
                if (rowsToPrint[i].Count() > newConstIndentForSecondColumn)
                {
                    newConstIndentForSecondColumn = rowsToPrint[i].Count();
                }
            }
            
            List<string> statsToPrint = new List<string>()
            {
                "\x1b[38;2;255;165;0mPLAYER STATS\x1b[38;2;255;255;255m",
                $"Strength ---> {game.player.Strength}",
                $"Dexterity ---> {game.player.Dexterity}",
                $"Intelligence ---> {game.player.Intelligence}",
                $"Constitution ---> {game.player.Constitution}",
                $"Charisma ---> {game.player.Charisma}"
            };
            
            for (int i = 0; i <= height; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.SetCursorPosition(indent + newConstIndentForSecondColumn - 10, yconst + i);
                if (statsToPrint.Count >= i) 
                    statsToPrint.Add("");
                Console.Write($"| {statsToPrint[i]}");
            }

            Console.SetCursorPosition(initialCursor.Item1, initialCursor.Item2);
        }

        public static void WriteTileChar(Node node, int i, int j, double r, double g, double b, float brightness = 1)
        {
            if (node.tiles[i][j].playerHere)
            {
                Console.Write(
                    $"\x1b[38;2;{RedGreenBluePlayerVals[0]};{RedGreenBluePlayerVals[1]};{RedGreenBluePlayerVals[2]}m{node.tiles[i][j].tileChar} \x1b[0m");
            }
            else if (node.tiles[i][j].enemyOnTile != null)
            {
                Nature nature = node.tiles[i][j].enemyOnTile.nature;
                Console.Write(
                    $"\x1b[38;2;{Math.Round(NatureToRGB[nature].Red * brightness, 0)};{Math.Round(NatureToRGB[nature].Green * brightness, 0)};{Math.Round(NatureToRGB[nature].Blue * brightness, 0)}m{node.tiles[i][j].tileChar} \x1b[0m");
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

        public static Node AddStructures(Node node)
        {
            List<string> structureNames = new List<string>()
            {
                "Bush3x2", "Bush3x2", "Bush3x2", "Bush3x2", "Tree3x3", "Bush3x3", "House4x4"
            };
            Random rnd = new Random();
            int structureCount = rnd.Next(structureNames.Count);
            for (int i = 0; i < structureCount; i++)
            {
                Structure s = new Structure(structureNames[i]);
                bool valid = false;
                Point rndPoint = Point.Empty;
                while (!valid)
                {
                    rndPoint = new Point(rnd.Next(0, node.NodeWidth), rnd.Next(0, node.NodeHeight));
                    if (node.tiles[rndPoint.X][rndPoint.Y].tileDesc == "Empty" &&
                        rndPoint.X + s.Width < node.NodeWidth && rndPoint.Y + s.Height < node.NodeHeight)
                    {
                        valid = true;
                        node.tiles = ImplementStructure(rndPoint, node.tiles, s, ref valid);
                    }
                }
            }

            return node;
        }

        public static List<List<Tile>> ImplementStructure(Point p, List<List<Tile>> tiles, Structure s, ref bool valid)
        {
            List<List<Tile>> clonedTiles = new List<List<Tile>>();
            for (int i = 0; i < tiles.Count; i++)
            {
                clonedTiles.Add(new List<Tile>());
                for (int j = 0; j < tiles[i].Count; j++)
                {
                    clonedTiles[i].Add(tiles[i][j].clone());
                }
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                List<Tile> tileRow = tiles[i];
                for (int j = 0; j < tileRow.Count; j++)
                {
                    Tile tile = tileRow[j];
                    if (tile.tileXY.X == p.X && tile.tileXY.Y == p.Y)
                    {
                        for (int structurei = 0; structurei < s.Width; structurei++)
                        {
                            for (int structurej = 0; structurej < s.Height; structurej++)
                            {
                                if (tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileDesc == "Empty" &&
                                    tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].enemyOnTile == null)
                                {
                                    tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileChar =
                                        s.ASCII[structurej][structurei];
                                    tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileDesc =
                                        "Structure";
                                    if (s.ASCII[structurej][structurei] == '.')
                                        tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileDesc =
                                            "Empty";

                                    if ((tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileChar ==
                                         '|' ||
                                         tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileChar ==
                                         '/' ||
                                         tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileChar ==
                                         '\\') && s.Name == "House4x4")
                                    {
                                        tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].walkable = false;
                                    }

                                    tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].rgb =
                                        s.RGBDict[
                                            tiles[tile.tileXY.X + structurei][tile.tileXY.Y + structurej].tileChar];
                                }
                                else
                                {
                                    valid = false;
                                    return clonedTiles;
                                }
                            }
                        }
                    }
                }
            }

            return tiles;
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

        public void fixMapStructure()
        {
            // NEXT JOB
            // goes through eahc of the connected nodes and finds if there is any discrepancies then fixes it
            Graph g = Graphs[CurrentGraphPointer];
            bool neededFix = false;

            /*
            for (int i = 0; i < g.Nodes.Count; i++)
            {
                for (int j = 0; j < g.Nodes[i].ConnectedNodes.Count; j++)
                {
                    // if node with the id of g.Nodes[i].ConnectedNodes[j] doesnt have the id g.Nodes[i].Id in its connected nodes, add it
                    if (g.Nodes.Find(n => n.NodeID == g.Nodes[i].ConnectedNodes[j]).ConnectedNodes
                        .Contains(g.Nodes[i].NodeID))
                    {
                        g.Nodes[i].ConnectedNodes.Add(g.Nodes[i].NodeID);
                        neededFix = true;
                    }
                }
            }
            */

            for (int i = 0; i < g.Nodes.Count; i++)
            {
                for (int j = 0; j < g.Nodes[i].ConnectedNodes.Count; j++)
                {
                    // Get the connected node
                    var connectedNode = g.Nodes.Find(n => n.NodeID == g.Nodes[i].ConnectedNodes[j]);

                    // Ensure the connected node exists
                    if (connectedNode != null)
                    {
                        // Check if the connection is mutual; if not, add it
                        if (!connectedNode.ConnectedNodes.Contains(g.Nodes[i].NodeID))
                        {
                            connectedNode.ConnectedNodes.Add(g.Nodes[i].NodeID);
                            neededFix = true;
                        }
                    }
                }
            }

            if (neededFix)
            {
                g.SetEntryAndExits();
            }
        }

        public void saveMapStructure()
        {
            string path = $"{UtilityFunctions.mapsSpecificDirectory}";
            File.WriteAllText(path,
                JsonConvert.SerializeObject(this, new JsonSerializerSettings() { Formatting = Formatting.Indented }));
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

    public class Objective
    {
        public string ObjectiveName { get; set; }
        public string Description { get; set; }
        public List<string> NarrativePrompts { get; set; }
        [JsonIgnore] public Func<Player, Game, string, bool> OnInteraction { get; set; }
        public bool IsCompleted { get; set; }
        public Point Location { get; set; }

        public Objective(string name, string description, List<string> prompts,
            Func<Player, Game, string, bool> interaction)
        {
            ObjectiveName = name;
            Description = description;
            NarrativePrompts = prompts;
            OnInteraction = interaction;
            IsCompleted = false;
        }

        public void BeginObjective(ref Game game) // MAIN OBJECTIVEV FUNCTION
        {
            Console.Clear();
            Console.CursorVisible = true;
            Console.WriteLine(Description + "\n");
            List<string> completedPrompts = new List<string>();
            List<string> items = game.itemFactory.weaponTemplates.Select(t => t.Name).Concat(game.itemFactory
                    .armourTemplates.Select(t => t.Name)
                    .Concat(game.itemFactory.consumableTemplates.Select(t => t.Name)))
                .ToList();
            while (!IsCompleted)
            {
                foreach (var prompt in NarrativePrompts)
                {
                    if (completedPrompts.Contains(prompt))
                        continue;
                    completedPrompts.Add(prompt);
                    UtilityFunctions.TypeText(new TypeText(typingSpeed: 2), prompt);
                }

                Console.Write("> ");
                string input = Console.ReadLine();
                (bool, List<string>) output = GetContinuation(game, input).GetAwaiter().GetResult();

                // send input to narrator, get a response that involves 2 thingss. One can indicate the fact it is doing anohter narrative line, and the other is an outcome.
                // This can be giving the player an item, damaging them, starting a combat, etc.
                // New prompt needed?!!

                if (output.Item1)
                {
                    NarrativePrompts = NarrativePrompts.Concat(output.Item2).ToList();
                    UtilityFunctions.TypeText(new TypeText(typingSpeed: 2), NarrativePrompts.Last());
                    Thread.Sleep(1000);
                    UtilityFunctions.TypeText(new TypeText(typingSpeed: 2), "This objective is completed.");
                    foreach (string item in items)
                    {
                        if (output.Item2[0].Contains(item))
                        {
                            UtilityFunctions.TypeText(new TypeText(typingSpeed: 2),
                                $"\nYou received the item: '{item}'.");
                            var bluntTemplate = game.itemFactory.GetAllTemplates().Find(t => t.Name == item);
                            Type itemType = bluntTemplate.GetType();
                            ItemTemplate itemTemplate;
                            if (itemType == typeof(ArmourTemplate))
                            {
                                itemTemplate =
                                    (ArmourTemplate)game.itemFactory.armourTemplates.Find(t => t.Name == item);
                            }
                            else if (itemType == typeof(ConsumableTemplate))
                            {
                                itemTemplate =
                                    (ConsumableTemplate)game.itemFactory.consumableTemplates.Find(t => t.Name == item);
                            }
                            else
                            {
                                itemTemplate =
                                    (WeaponTemplate)game.itemFactory.weaponTemplates.Find(t => t.Name == item);
                            }

                            game.player.AddItem(game.itemFactory.createItem(itemTemplate));
                            game.player.initialiseInventory();
                        }
                    }

                    CompleteObjective();
                    Thread.Sleep(1000);
                    UtilityFunctions.TypeText(new TypeText(typingSpeed: 2), "\n\nPress any button to leave.");
                }
                else
                {
                    NarrativePrompts = NarrativePrompts.Concat(output.Item2).ToList();
                }
            }

            Console.ReadKey(true);

            Console.CursorVisible = false;

            GridFunctions.DrawWholeNode(game);
        }

        public async Task<(bool, List<string>)> GetContinuation(Game game, string input)
        {
            string prompt = "";
            if (NarrativePrompts.Count == 0)
            {
                prompt += $"\n\nAs a reminder, description of this objective is {Description}";
                prompt += $"This is the first narrative line for this objective.";
            }
            else
            {
                prompt = $"The player inputted this in response to your narrative: {input}";
                prompt += $"\n\nAs a reminder, your last narrative lines were {string.Join("", NarrativePrompts)}";
            }

            prompt +=
                $"\n\nPlease now output the narrators response to this input to continue the objective. You can introduce NPC dialogues to your narrative. It is critical that your response provides a scenario where the user can easily respond with what they will do next.";
            prompt +=
                "\nIt is important that you act as a narrator: Feel free to use dice rolls and random events, where the outcome changes how positive the next narrative is.";
            prompt +=
                "You may start this response with the word in full capitals CONTINUE or END. If you use continue, then all the following text will be outputted to the user";
            prompt +=
                $"However if you start with END, the objective will end and you can decide give the player an item (as a reward if they made positive choices) or just end (the player will lose hp).";
            prompt +=
                $"The items you can give to the player are: {string.Join(", ", game.itemFactory.weaponTemplates.Select(t => t.Name).Concat(game.itemFactory.armourTemplates.Select(t => t.Name).Concat(game.itemFactory.consumableTemplates.Select(t => t.Name))))}";
            prompt += "For example, to end the objective and give the user an item write END (item name)";
            prompt += "Or, write CONTINUE (next narrative lines)";

            game.chat.AppendUserInput(prompt);
            string outp = await game.narrator.GetGPTOutput(game.chat,
                $"GetContinuation{ObjectiveName}-{NarrativePrompts.Count}");
            List<string> output = outp.Split('\n').ToList();
            if (output[0].Substring(0, Math.Min(6, output[0].Length)).ToLower().Contains("end"))
            {
                output[0] = output[0].Remove(0, 3);
                return (true, output);
            }
            else if (output[0].Substring(0, Math.Min(12, output[0].Length)).ToLower().Contains("continue"))
            {
                output[0] = output[0].Remove(0, 8);
                return (false, output);
            }
            else
            {
                throw new Exception("Output was in incorrect format.");
            }
        }

        public void CompleteObjective()
        {
            IsCompleted = true;
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
        public int NodeWidth { get; set; }
        public int NodeHeight { get; set; }
        public bool Milestone { get; set; }
        [Newtonsoft.Json.JsonIgnore] public List<List<Tile>> tiles { get; set; }
        public List<EnemySpawn> enemies { get; set; }
        public Objective? Obj { get; set; }

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

        public async Task AddObjectiveToNode(bool loaded)
        {
            bool valid = false;
            while (!valid)
            {
                Random rnd = new Random();
                Tile tile = tiles[rnd.Next(0, tiles.Count)][rnd.Next(0, tiles[rnd.Next(0, tiles.Count)].Count)];
                if (tile.enemyOnTile == null && tile.tileDesc == "Empty" && tile.walkable && !tile.playerHere)
                {
                    await tile.AddObjectiveToTile(this, Program.game, loaded);
                    valid = true;
                }
            }
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
            {
                if (enemies.Count > 0)
                {
                    PlaceEnemiesOnNode(game);
                    return;
                }
            }

            Random random = new Random();
            List<EnemySpawn> spawns = new List<EnemySpawn>();
            int enemyCount = random.Next(1, 4);
            if (Milestone) enemyCount = 1;
            for (var i = 0; i < enemyCount; i++)
            {
                spawns.Add(new EnemySpawn());
                Point enemyPoint = new Point();
                bool validPoint = false;
                if (NodeHeight == 0 || NodeWidth == 0)
                {
                    NodeHeight = 20;
                    NodeWidth = 20;
                }

                while (!validPoint)
                {
                    enemyPoint.X = random.Next(1, this.NodeWidth - 2);
                    enemyPoint.Y = random.Next(1, this.NodeHeight - 2);
                    if (tiles[enemyPoint.X][enemyPoint.Y].tileDesc == "Empty" &&
                        tiles[enemyPoint.X][enemyPoint.Y].enemyOnTile == null)
                    {
                        validPoint = true;
                    }
                }

                // checks
                if (game.enemyFactory == null)
                {
                    game.itemFactory.initialiseItemFactoryFromNarrator(game.api, game.chat, false).ConfigureAwait(true);
                }

                if (Milestone)
                {
                    spawns[i].boss = true;
                    spawns[i].spawnPoint = enemyPoint;
                    spawns[i].name = game.enemyFactory.enemyTypes[random.Next(0, game.enemyFactory.enemyTypes.Count)];
                    spawns[i].alive = true;
                }
                else
                {
                    spawns[i].boss = false;
                    spawns[i].spawnPoint = enemyPoint;
                    spawns[i].name = game.enemyFactory.enemyTypes[random.Next(0, game.enemyFactory.enemyTypes.Count)];
                    spawns[i].alive = true;
                }

                spawns[i].id = UtilityFunctions.GiveNewEnemyId();
            }

            enemies = spawns;

            PlaceEnemiesOnNode(game);
        }

        public void PlaceEnemiesOnNode(Game game)
        {
            if (enemies.Count == 0) throw new Exception("No enemies placed");
            foreach (EnemySpawn spawn in enemies)
            {
                if (!spawn.alive)
                    continue;
                EnemyTemplate template = game.enemyFactory.enemyTemplates[spawn.name];

                if (spawn.spawnPoint != Point.Empty && spawn.currentLocation == Point.Empty)
                {
                    tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].tileChar = GridFunctions.CharsToMeanings["Enemy"][0];
                    tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile =
                        game.enemyFactory.CreateEnemy(template, NodeDepth, spawn.spawnPoint, spawn.id);
                    enemies[enemies.IndexOf(spawn)].nature =
                        tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile.nature;
                }
                else if (spawn.currentLocation != Point.Empty && spawn.spawnPoint == Point.Empty)
                {
                    tiles[spawn.currentLocation.X][spawn.currentLocation.Y].tileChar =
                        GridFunctions.CharsToMeanings["Enemy"][0];
                    tiles[spawn.currentLocation.X][spawn.currentLocation.Y].enemyOnTile =
                        game.enemyFactory.CreateEnemy(template, NodeDepth, spawn.currentLocation, spawn.id);
                    // enemies[enemies.IndexOf(spawn)].nature = tiles[spawn.spawnPoint.X][spawn.spawnPoint.Y].enemyOnTile.nature;
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
        public Objective? objective { get; set; }

        public Tile(char tileChar, Point tileXY, string tileDesc, int? nodeEntryPointer = null,
            int? nodeExitPointer = null)
        {
            this.tileChar = tileChar;
            this.tileXY = tileXY;
            this.tileDesc = tileDesc;
            this.playerHere = false;
            enemyOnTile = null;
            objective = null;

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

        public async Task<Tile> AddObjectiveToTile(Node node, Game game, bool loaded)
        {
            tileDesc = "Objective";
            tileChar = GridFunctions.CharsToMeanings[tileDesc][0];
            rgb = GridFunctions.CharsToRGB[tileDesc];
            if (loaded)
            {
                objective = node.Obj;
            }
            else
            {
                objective = await game.narrator.GenerateInitialObjective(game, node);
            }

            return this;
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


    public class Structure
    {
        public string Name { get; set; }
        public List<List<char>> ASCII { get; set; }
        public Dictionary<char, Rgb> RGBDict { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Structure(string type)
        {
            Name = type;
            // many structures generated by AI
            RGBDict = new Dictionary<char, Rgb>() { { '.', (Rgb)GridFunctions.CharsToRGB["Empty"] } };
            switch (type)
            {
                case "Tree3x2":
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '.', '*', '.' },
                        new List<char>() { '*', '*', '*' }
                    };
                    RGBDict.Add('*', new Rgb(34, 139, 34)); // forest green
                    Width = 3;
                    Height = 2;
                    break;

                case "Tree3x3":
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '.', '*', '.' },
                        new List<char>() { '*', '*', '*' },
                        new List<char>() { '.', '|', '.' }
                    };
                    RGBDict.Add('*', new Rgb(34, 139, 34)); // green
                    RGBDict.Add('|', new Rgb(76, 34, 10)); // brown
                    Width = 3;
                    Height = 3;
                    break;

                case "Bush3x2":
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '*', '*', '*' },
                        new List<char>() { '*', '*', '*' }
                    };
                    RGBDict.Add('*', new Rgb(34, 139, 34)); // green
                    Width = 3;
                    Height = 2;
                    break;

                case "Bush3x3":
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '.', '*', '.' },
                        new List<char>() { '*', '*', '*' },
                        new List<char>() { '*', '*', '*' }
                    };
                    RGBDict.Add('*', new Rgb(34, 139, 34)); // green
                    Width = 3;
                    Height = 3;
                    break;

                case "House4x4":
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '.', '/', '\\', '.' },
                        new List<char>() { '/', '_', '_', '\\' },
                        new List<char>() { '|', '.', '.', '|' },
                        new List<char>() { '|', '_', '_', '|' }
                    };
                    RGBDict.Add('/', new Rgb(101, 67, 33)); // Medium brown (roof edges)
                    RGBDict.Add('\\', new Rgb(101, 67, 33)); // Medium brown (roof edges)
                    RGBDict.Add('_', new Rgb(169, 169, 169)); // Gray (roof base and foundation)
                    RGBDict.Add('|', new Rgb(139, 69, 19)); // Dark brown (walls and vertical supports)
                    Width = 4;
                    Height = 4;
                    break;

                default:
                    ASCII = new List<List<char>>()
                    {
                        new List<char>() { '.' }
                    };
                    Width = 1;
                    Height = 1;
                    break;
            }
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

        public async Task AddObjectivesToNodes(bool loaded)
        {
            string prompt12 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt12.txt");
            prompt12 += $"\n{string.Join(", ", Nodes.FindAll(n => n.Obj == null).Select(n => n.NodePOI))}";
            if (Nodes.FindAll(n => n.Obj == null).Count == 0)
            {
                PlaceExistingObjectivesToNodes();
                return;
            }

            Program.game.chat.AppendUserInput(prompt12);
            string output = await Program.game.narrator.GetGPTOutput(Program.game.chat, "Dictionary of Objectives");
            try
            {
                Dictionary<string, Objective> objectives =
                    JsonConvert.DeserializeObject<Dictionary<string, Objective>>(output);
                for (int i = 0; i < objectives.Count; i++)
                {
                    Nodes.Find(n => n.NodePOI == objectives.ElementAt(i).Key).Obj = objectives.ElementAt(i).Value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse Objectives: {ex.Message}");
            }
        }

        public async Task PlaceExistingObjectivesToNodes()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                Point point = new Point();
                if (Nodes[i].Obj == null)
                {
                    throw new Exception("Objective is null.");
                }
                else if (Nodes[i].Obj.Location == Point.Empty)
                {
                    bool valid = false;
                    Random rnd = new Random();
                    while (!valid)
                    {
                        Point newPoint = new Point(rnd.Next(0, Nodes[i].tiles.Count - 1),
                            rnd.Next(0, Nodes[i].tiles[rnd.Next(0, Nodes[i].tiles.Count - 1)].Count - 1));
                        Tile tile = Nodes[i].tiles[newPoint.X][newPoint.Y];
                        if (tile.enemyOnTile == null && tile.playerHere == false && tile.tileDesc == "Empty")
                        {
                            point = newPoint;
                            valid = true;
                        }
                    }
                }
                else
                {
                    point = Nodes[i].Obj.Location;
                }

                Nodes[i].tiles[point.X][point.Y].tileChar = GridFunctions.CharsToMeanings["Objective"][0];
                Nodes[i].tiles[point.X][point.Y].tileDesc = "Objective";
                Nodes[i].tiles[point.X][point.Y].objective = Nodes[i].Obj;
                Nodes[i].tiles[point.X][point.Y].rgb = GridFunctions.CharsToRGB["Objective"];
            }
        }

        public int GetHighestDepth()
        {
            int depth = 0;
            foreach (Node n in Nodes)
            {
                if (n.NodeDepth > depth) depth = n.NodeDepth;
            }

            return depth;
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