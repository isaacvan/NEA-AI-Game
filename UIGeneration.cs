using System.Text.RegularExpressions;
using CombatNamespace;
using EnemyClassesNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace UIGenerationNamespace
{
    public class UIConstructer
    {
        public Player player { get; set; }
        public List<Enemy> nearbyEnemies { get; set; }
        public List<List<Tile>> map { get; set; }

        public UIConstructer(Player plyr)
        {
            player = plyr;
        }

        public async Task fillNearbyEnemies() // needs filling
        {
            foreach (List<Tile> row in map)
            {
                foreach (Tile tile in row)
                {
                    /* if (tile.enemyHere)
                    {
                     nearbyEnemies.Add(tile.getEnemy())
                    }*/
                }
            }
        }

        public async Task drawUI()
        {
            // main UI fnction
        }
        
        public void displayCombatUI(Dictionary<int, Enemy> enemiesDict)
        {
            // combat ui
            
            Console.WriteLine(UtilityFunctions.universalSeperator);
            List<Enemy> enemies = enemiesDict.Values.ToList();
            
            int columnWidth = 12; // Adjust as necessary for layout
            string separator = " | ";
            string ResetANSI = "\x1b[0m";

            string FormatString(string str, int width)
            {
                string ansiStripped = Regex.Replace(str, @"\x1B\[.*?m", ""); // Remove ANSI codes
                int paddingNeeded = width - ansiStripped.Length;
                if (paddingNeeded > 0)
                {
                    return str + new string(' ', paddingNeeded); // Pad with spaces
                }
                return str;
            }

            string DrawStatus(List<Status> statuses)
            {
                return statuses.Count == 0 ? "No Statuses" : $"Statuses: {String.Join(", ", statuses)}";
            }
            
            string GetNatureColor(Enemy enemy)
            {
                switch (enemy.nature)
                {
                    case Nature.aggressive:
                        return UtilityFunctions.aggressiveANSIUI;
                    case Nature.neutral:
                        return UtilityFunctions.neutralANSIUI;
                    case Nature.timid:
                        return UtilityFunctions.timidANSIUI;
                    default:
                        return ResetANSI;
                }
            }

            // Collect all lines to be printed
            List<string> playerLines = new List<string>
            {
                FormatString($"[{UtilityFunctions.playerANSIUI}Player{ResetANSI}]", columnWidth),
                FormatString($"Level: {player.Level}", columnWidth),
                FormatString($"{UtilityFunctions.DrawHealthBar(player)}", columnWidth),
                FormatString($"{UtilityFunctions.DrawManaBar(player)}", columnWidth),
                FormatString(DrawStatus(player.statusMap.Values.ToList()), columnWidth)
            };

            List<List<string>> enemyLines = enemies.Select(e =>
            {
                string colour = GetNatureColor(e);
                return new List<string>
                {
                    FormatString($"[{colour}{e.Name}{ResetANSI}]", columnWidth),
                    FormatString($"Level: {e.Level}", columnWidth),
                    FormatString($"{UtilityFunctions.DrawHealthBar(e)}", columnWidth),
                    FormatString($"{UtilityFunctions.DrawManaBar(e)}", columnWidth),
                    FormatString(DrawStatus(e.statusMap.Values.ToList()), columnWidth)
                };
            }).ToList();

            // Print all lines side by side
            for (int i = 0; i < playerLines.Count; i++)
            {
                string line = playerLines[i];
                foreach (var enemy in enemyLines)
                {
                    line += separator + enemy[i];
                }
                Console.WriteLine(line);
            }
            Console.WriteLine(UtilityFunctions.universalSeperator);
        }
    }
}