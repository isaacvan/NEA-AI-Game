using System.Text.RegularExpressions;
using CombatNamespace;
using EnemyClassesNamespace;
using GameClassNamespace;
using GPTControlNamespace;
using GridConfigurationNamespace;
using ItemFunctionsNamespace;
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

        public void DrawMap(Graph graph)
        {
            Console.Clear();

            // Console.Write(GetNodeTxt(graph.GetNode(0), 0, 0));

            List<List<Node>> nodesByDepthThenId = new List<List<Node>>();
            List<int> longestTextForEachDepth = new List<int>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i].NodeDepth >= nodesByDepthThenId.Count)
                {
                    nodesByDepthThenId.Add(new List<Node>());
                    longestTextForEachDepth.Add(0);
                }

                nodesByDepthThenId[graph.Nodes[i].NodeDepth].Add(graph.Nodes[i]);
                if (longestTextForEachDepth[graph.Nodes[i].NodeDepth] < graph.Nodes[i].NodePOI.Length + 2)
                {
                    longestTextForEachDepth[graph.Nodes[i].NodeDepth] = graph.Nodes[i].NodePOI.Length + 2;
                }
            }

            List<List<string>> nodeTxtByDepths = new List<List<string>>();
            for (int i = 0; i < nodesByDepthThenId.Count; i++)
            {
                nodeTxtByDepths.Add(new List<string>());
                for (int j = 0; j < nodesByDepthThenId[i].Count; j++)
                {
                    nodeTxtByDepths[i].Add(GetNodeTxt(nodesByDepthThenId[i][j], 0, 0, graph));
                }
            }

            int maxNodesOnADepth = 0;

            for (int i = 0; i < nodeTxtByDepths.Count; i++)
            {
                if (nodeTxtByDepths[i].Count > maxNodesOnADepth)
                {
                    maxNodesOnADepth = nodeTxtByDepths[i].Count;
                }
            }
            
            if (maxNodesOnADepth % 2 == 0) maxNodesOnADepth++;

            List<List<int>> nodeIndexes = new List<List<int>>();

            for (int i = 0; i < nodeTxtByDepths.Count; i++)
            {
                nodeIndexes.Add(new List<int>());

                int nodeCount = nodeTxtByDepths[i].Count;

                for (int j = 0; j < nodeCount; j++)
                {
                    // Calculate the position to evenly space nodes within the range [0, maxNodesOnADepth - 1]
                    int index = (j * (maxNodesOnADepth - 1)) / Math.Max(1, nodeCount - 1);

                    // Handle single node case (center alignment)
                    if (nodeCount == 1)
                    {
                        index = maxNodesOnADepth / 2;
                    }

                    nodeIndexes[i].Add(index);
                }
            }
            
            List<Dictionary<int, string>> nodeIndexesDict = new List<Dictionary<int, string>>();
            for (int i = 0; i < maxNodesOnADepth; i++)
            {
                nodeIndexesDict.Add(new Dictionary<int, string>());
            }
            for (int i = 0; i < nodeIndexes.Count; i++)
            {
                for (int j = 0; j < nodeIndexes[i].Count; j++)
                {
                    nodeIndexesDict[nodeIndexes[i][j]].Add(i, nodeTxtByDepths[i][j]);
                }
            }
            
            List<List<string>> nodeIndexesDict2 = new List<List<string>>();
            for (int i = 0; i < nodeIndexesDict.Count; i++)
            {
                nodeIndexesDict2.Add(new List<string>());
                for (int j = 0; j < graph.GetHighestDepth() + 1; j++)
                {
                    if (nodeIndexesDict[i].ContainsKey(j))
                    {
                        nodeIndexesDict2[i].Add(nodeIndexesDict[i][j]);
                    }
                    else
                    {
                        nodeIndexesDict2[i].Add("Empty");
                    }
                }
            }

            for (int i = 0; i < nodeIndexesDict2.Count; i++) {
                int buffer = 0;
                for (int j = 0; j < nodeIndexesDict2[i].Count; j++)
                {
                    if (nodeIndexesDict2[i][j] == "Empty")
                    {
                        buffer += longestTextForEachDepth[j] + 1;
                    }
                    else
                    {
                        List<string> strings = Regex.Split(nodeIndexesDict2[i][j], @"\n").ToList();
                        for (int k = 0; k < strings.Count; k++)
                        {
                            Console.SetCursorPosition(buffer, k + 4 * i);
                            Console.Write(strings[k]);
                        }
                        buffer += longestTextForEachDepth[j] + 1;
                    }
                    
                    Console.SetCursorPosition(buffer, longestTextForEachDepth[j]);
                }
                Console.SetCursorPosition(0, Console.CursorTop + (i + 1) * 3);
            }
            
            


            Console.ReadKey(true);


            static string GetNodeTxt(Node node, int height, int width, Graph graph)
            {
                if (width < node.NodePOI.Length + 2) width = node.NodePOI.Length + 2;
                string poi = node.NodePOI;
                string output = "";
                for (int i = 0; i < poi.Length + 2; i++)
                {
                    if (i == 0)
                    {
                        output += "|";
                    }
                    else if (i == poi.Length + 1)
                    {
                        output += "|";
                    }
                    else
                    {
                        output += "-";
                    }
                }

                if (graph.Nodes[graph.CurrentNodePointer].NodeID == node.NodeID)
                {
                    output += $"\n|\x1b[38;2;0;183;235m{poi}\x1b[38;2;255;255;255m|\n";
                }
                else
                {
                    output += $"\n|{poi}|\n";
                }
                

                for (int i = 0; i < poi.Length + 2; i++)
                {
                    if (i == 0)
                    {
                        output += "|";
                    }
                    else if (i == poi.Length + 1)
                    {
                        output += "|";
                    }
                    else
                    {
                        output += "-";
                    }
                }

                return output;
            }
        }

        public async Task<Conversation> IntroduceStoryline(Game game)
        {
            game.chat.AppendUserInput(
                "The game is about to begin. Your next output should read like the start of a short novel: introduce the player to their storyline on a basic level. Tell them what their final goal is, and outline it in a way that reads like the beginning of a story. Ensure you stay consistent with the corresponding locations in the map, enemies, items, and player class.");
            Console.Clear();
            string output = await game.narrator.GetGPTOutput(game.chat, "Narrative intro to player", null);
            UtilityFunctions.TypeText(new TypeText(typingSpeed: 4), output);
            Thread.Sleep(1000);
            UtilityFunctions.TypeText(new TypeText(typingSpeed: 4), "\nPress any key to continue...");
            Console.ReadKey(true);
            return game.chat;
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
                    UtilityFunctions.TypeText(new TypeText(),
                        $"Slot #{attack.Key.ToString().Last()} ---> {attack.Value.Name}");
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(), $"Slot #{attack.Key.ToString().Last()} ---> Empty");
                }
            }
            
            UtilityFunctions.TypeText(new TypeText(), "\nITEMS:");
            int index = 1;
            foreach (var item in game.player.inventory.Items)
            {
                UtilityFunctions.TypeText(new TypeText(), $"Item #{index} {item.Name} ---> {item.ItemType.Name}");
                index++;
            }
            
            UtilityFunctions.TypeText(new TypeText(), "\nEQUIPMENT:");
            foreach (var item in game.player.equipment.ArmourSlots)
            {
                if (item.Value != null)
                {
                    UtilityFunctions.TypeText(new TypeText(), $"{item.Key.ToString()} ---> {item.Value.Name}");
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(), $"{item.Key.ToString()} ---> Empty");
                }
            }
            foreach (var item in game.player.equipment.WeaponSlots)
            {
                if (item.Value != null)
                {
                    UtilityFunctions.TypeText(new TypeText(), $"{item.Key.ToString()} ---> {item.Value.Name}");
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(), $"{item.Key.ToString()} ---> Empty");
                }
            }
            
            
            UtilityFunctions.TypeText(new TypeText(), "\nWould you like to equip / use an item? [y/n]");
            if (Console.ReadLine() == "y")
            {
                UtilityFunctions.TypeText(new TypeText(), "Enter the name or ID of the item you would like to equip");
                string inp = Console.ReadLine();
                bool idSearch = false;
                while (!game.player.inventory.Items.ConvertAll(x => x.Name).Contains(inp) && inp != "n")
                {
                    if (int.TryParse(inp, out int id))
                    {
                        if (id > 0 && id <= index)
                        {
                            idSearch = true;
                            break;
                        }
                    }
                    UtilityFunctions.TypeText(new TypeText(), $"{inp} is not in the inventory. Enter a valid item or 'n' to exit");
                    inp = Console.ReadLine();
                }

                if (inp != "n")
                {
                    Item item;
                    if (idSearch)
                    {
                        item = game.player.inventory.Items[Convert.ToInt32(inp)];
                    }
                    else
                    {
                        item = game.player.inventory.Items.Find(x => x.Name == inp);
                    }
                    
                    // game.player.inventory.RemoveItem(item); - done in equip item
                    if (item.ItemType == typeof(Weapon))
                    {
                        game.player.equipment.EquipItem(EquippableItem.EquipLocation.Weapon, item, game.player.inventory);
                    } else if (item.ItemType == typeof(Armour))
                    {
                        EquippableItem.EquipLocation loc = item.ItemEquipLocation;
                        game.player.equipment.EquipItem(loc, item, game.player.inventory);
                    }
                    
                   // GET ITEM SLOT
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

            UtilityFunctions.TypeText(new TypeText(typingSpeed: 0), UtilityFunctions.universalSeperator);
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

                UtilityFunctions.TypeText(new TypeText(typingSpeed: 0), line);
            }

            UtilityFunctions.TypeText(new TypeText(typingSpeed: 0), UtilityFunctions.universalSeperator);
        }
    }
}