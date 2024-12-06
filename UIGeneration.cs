using System.Text.RegularExpressions;
using CombatNamespace;
using EnemyClassesNamespace;
using GameClassNamespace;
using GPTControlNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using OpenAI_API.Chat;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace UIGenerationNamespace
{
    public class UIConstructer
    {
        public Player player { get; set; }
        public List<Enemy> nearbyEnemies { get; set; }
        public List<List<Tile>> map { get; set; }
        public string currentNarration { get; set; }
        public bool narrationPending { get; set; }

        public UIConstructer(Player plyr)
        {
            player = plyr;
        }

        public Conversation InitialiseNarration(Game game)
        {
            string prompt2 = File.ReadAllText(UtilityFunctions.promptPath + "Prompt2.txt");
            game.chat.AppendUserInput(prompt2);
            // game.narrator.GetGPTOutput(game.chat, "Narration initialisation");
            return game.chat;
        }

        public async Task TypeNarration(string narrative)
        {
            currentNarration = narrative;
            narrationPending = true;
        }

        public void drawCharacterMenu(Game game)
        {
            UtilityFunctions.clearScreen(game.player);
            UtilityFunctions.TypeText(new TypeText(), "ATTACKS:");
            foreach (var attack in game.player.PlayerAttacks)
            {
                if (attack.Value != null)
                {
                    UtilityFunctions.TypeText(new TypeText(), $"Slot #{attack.Key.ToString().Last()} ---> {attack.Value.Name}");
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(), $"Slot #{attack.Key.ToString().Last()} ---> Empty");
                }
            }
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

        public static void Func()
        {
            
        }

        public async Task drawUI()
        {
            // main UI fnction
        }
        
        public void displayCombatUI(Dictionary<int, Enemy> enemiesDict)
        {
            // combat ui
            // UtilityFunctions.TypeText(new TypeText(), )
            
            UtilityFunctions.TypeText(new TypeText(), UtilityFunctions.universalSeperator);
            List<Enemy> enemies = enemiesDict.Values.ToList();
            
            int columnWidth = 12; // Adjust as necessary for layout
            string separator = "     |      ";
            string ResetANSI = $"\x1b[38;2;255;255;255m";

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
                FormatString($"Level: {player.Level}{ResetANSI}", columnWidth),
                FormatString($"{UtilityFunctions.DrawHealthBar(player)}{ResetANSI}", columnWidth),
                FormatString($"{UtilityFunctions.DrawManaBar(player)}{ResetANSI}", columnWidth),
                FormatString(DrawStatus(player.statusMap.Values.ToList()), columnWidth)
            };

            List<List<string>> enemyLines = enemies.Select(e =>
            {
                string colour = GetNatureColor(e);
                return new List<string>
                {
                    FormatString($"[{colour}{e.Name}{ResetANSI}]", columnWidth),
                    FormatString($"Level: {e.Level}{ResetANSI}", columnWidth),
                    FormatString($"{UtilityFunctions.DrawHealthBar(e)}{ResetANSI}", columnWidth),
                    FormatString($"{UtilityFunctions.DrawManaBar(e)}{ResetANSI}", columnWidth),
                    FormatString(DrawStatus(e.statusMap.Values.ToList()), columnWidth)
                };
            }).ToList();

            // Print all lines side by side
            for (int i = 0; i < playerLines.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                string line = playerLines[i];
                foreach (var enemy in enemyLines)
                {
                    line += separator + enemy[i];
                }
                UtilityFunctions.TypeText(new TypeText(), line);
            }
            UtilityFunctions.TypeText(new TypeText(), UtilityFunctions.universalSeperator);
        }
    }
}