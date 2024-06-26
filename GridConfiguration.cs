using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using System.Xml.Linq;
using System.Text.Json;
using System.Reflection.Metadata.Ecma335;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace GridConfigurationNamespace
{
    public class GridFunctions
    {
        private const string wallChar = $"\x1b[38;2;0;0;0m\u25a0";
        private const string expChar = $"\x1b[38;2;100;100;255me";
        private const string floorChar = $"\x1b[38;2;200;200;200m.\x1b[0m";
        
        public static List<List<Tile>> CreateGrid(int width, int height)
        {
            List<List<Tile>> grid = new List<List<Tile>>();
            string t;

            for (int i = 0; i < width; i++)
            {
                List<Tile> row = new List<Tile>();
                for (int j = 0; j < height; j++)
                {
                    
                    Event @event;

                    if (i == 5 && j == 5)
                    {
                        t = expChar;
                        List<string> desc = new List<string> { "plrExp" };
                        List<string> consq = new List<string> { "5" };
                        @event = new Event("exp", desc, consq);

                    } else {
                        t = floorChar;
                        List<string> desc = new List<string> { "none" };
                        List<string> consq = new List<string> { "none" };
                        @event = new Event("none", desc, consq);
                    }
                    
                    row.Add(new Tile(i, j, t, false, 1, false, @event));

                }
                grid.Add(row);
            }

            return grid;
        }

        public static List<List<Tile>> LoadGrid(string fileName)
        {
            string json = File.ReadAllText(fileName);
            List<List<Tile>> grid = JsonSerializer.Deserialize<List<List<Tile>>>(json);
            return grid;
        }




        public static bool PrintGrid(List<List<Tile>> grid, Point oldplayerLocation, Point newplayerLocation, int scopeWidth, int scopeHeight, Player player)
        {
            bool moveDone = true;
            UtilityFunctions.clearScreen(player);
            int playerX = newplayerLocation.X;
            int playerY = newplayerLocation.Y;
            grid[oldplayerLocation.X][oldplayerLocation.Y].playerHere = false;
            grid[oldplayerLocation.X][oldplayerLocation.Y].t = floorChar;
            
            if (playerX >= 0 && playerX < grid.Count && playerY >= 0 && playerY < grid[playerX].Count) // if in bounds
            {
                grid[playerX][playerY].playerHere = true;
                grid[playerX][playerY].t = "@";
            }
            else
            {
                if (player != null) {
                    player.playerPos = oldplayerLocation;
                }
                playerX = oldplayerLocation.X;
                playerY = oldplayerLocation.Y;
                grid[playerX][playerY].playerHere = true;
                moveDone = false;
                //Environment.FailFast($"Player out of bounds. X: {playerX} Y: {playerY} Width: {grid.Count} Height: {grid[playerX].Count}");
            }
            
            //grid[playerX][playerY].playerHere = true;
            //Console.CursorTop = 3 + scopeHeight;
            //Console.CursorLeft = scopeWidth;
            
            if (grid[playerX][playerY].eventHere.name == "exp")
            {
                for (var i = 0; i < grid[playerX][playerY].eventHere.description.Count; i++)
                {
                    if (grid[playerX][playerY].eventHere.description[i] == "plrExp")
                    {
                        //player.changePlayerStats("currentExp", player.currentExp + Convert.ToInt32(grid[playerX][playerY].eventHere.consequences[i]));

                    }
                }
            }
            
            UtilityFunctions.clearScreen(player);
            List<string> arr = new List<string>();

            for (int i = playerY - scopeHeight; i <= playerY + scopeHeight; i++)
            {
                for (int j = playerX - scopeWidth; j <= playerX + scopeWidth; j++)
                {
                    if (i >= 0 && i < grid.Count && j >= 0 && j < grid[i].Count) // if in bounds
                    {
                        if (grid[j][i].playerHere)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($" {grid[j][i].t} ");
                            //Console.Write($" @ ");
                            //arr.Add($"\x1b[38;2;{255};{0};{0}m" + grid[j][i].t + " \x1b[0m");
                            arr.Add($" @ ");
                            Console.ResetColor();
                        } else if (grid[i][j].eventHere.name == "exp") {
                            //Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($" {expChar} ");
                            arr.Add($" {expChar} ");
                            //Console.ResetColor();
                        
                        }
                        else
                        {
                            Console.ResetColor();
                            double distance = Math.Sqrt(Math.Pow(i - playerX, 2) + Math.Pow(j - playerY, 2));
                            int[] colours = UtilityFunctions.getShadeFromDist(255, 255, 255, distance, scopeWidth, scopeHeight);
                            int r = colours[0];
                            int g = colours[1];
                            int b = colours[2];
                            Console.ForegroundColor = ConsoleColor.White;
                            //Console.Write($"\x1b[38;2;{r};{g};{b}m" + grid[j][i].t + " \x1b[0m");
                            Console.Write($" {grid[j][i].t} ");
                            //arr.Add($"\x1b[38;2;{r};{g};{b}m" + grid[j][i].t + " \x1b[0m");
                            arr.Add($" {grid[j][i].t} ");
                        }
                        Console.ResetColor();
                    }
                }
                if (i >= 0)
                {
                    arr.Add("\n");
                    Console.Write("\n");
                }
            }
            
            if (player != null)
            {
                //UtilityFunctions.clearScreen(player);
            }
            //Console.WriteLine($"X: {playerX} Y: {playerY}\n");
            //printArr(arr);
            return moveDone;
        }

        static void printArr(List<string> arr)
        {
            // this function prints the array as one whole string with \n as new indexes
            string result = "";
            for (int i = 0; i < arr.Count; i++)
            {
                result += arr[i];
            }
            Console.WriteLine(result);
        }

        public static List<List<Tile>> SaveGrid(List<List<Tile>> grid, string fileName)
        {
            string json = JsonSerializer.Serialize(grid);
            File.WriteAllText(fileName, json);
            return grid;
        }
    }

    public class Tile
    {
        public int x { get; set; }
        public int y { get; set; }
        public string t { get; set; }
        public bool playerHere { get; set; }
        public float darkness { get; set; }
        public bool wallV { get; set;}
        public Event eventHere { get; set; }


        public Tile(int x, int y, string t, bool playerHere, float darkness, bool wallV, Event eventHere)
        {
            this.x = x;
            this.y = y;
            this.t = t;
            this.playerHere = false;
            this.darkness = darkness;
            this.wallV = wallV;
            this.eventHere = eventHere;
        }
    }

    public class Event {
        public string name { get; set; }
        public List<string> description { get; set; }
        public List<string> consequences { get; set; }
        
        public Event(string name, List<string> description, List<string> consequences) {
            this.name = name;
            this.description = description;
            this.consequences = consequences;
        }
    }


}