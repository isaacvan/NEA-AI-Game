using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PlayerClassesNamespace;
using EnemyClassesNamespace;
using System.Drawing;
using System.Xml.Serialization;

namespace UtilityFunctionsNamespace
{
    public class UtilityFunctions
    {





        public static string[] enemies = { "boar", "orc", "snake" };
        public static int typeSpeed = 1;
        public static bool quickEnd = false;
        public static string saveSlot = ""; // will be written to in main menu
        public static string mainDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")); // will be written to in main menu
        public static string saveFile = @mainDirectory + saveSlot; // will be written to in main menu
        public static int maxSaves = 3;
        public static bool loadedSave = false;
        public static bool Instant = false;
        public static int colourSchemeIndex = 0;
        public static ColourScheme colourScheme = new ColourScheme(UtilityFunctions.colourSchemeIndex);
        






        public static void lobby(Player player)
        {
            bool actionTyped = false;
            while (!actionTyped)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nWhat action would you like to take next?", UtilityFunctions.typeSpeed);
                string input = Console.ReadLine();
                UtilityFunctions.clearScreen(player);
                if (input == "help" || input == "h")
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "help - displays all possible commands\ni - displays your inventory\ns - displays your stats\nc - clear the console\ng - back to the game\n", UtilityFunctions.typeSpeed);
                }
                else if (input == "i" || input == "inv" || input == "inventory")
                {
                    UtilityFunctions.clearScreen(player);

                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Would you like to use a consumable?\n(y/n)", UtilityFunctions.typeSpeed);
                    string inp = Console.ReadLine();
                    if (inp == "y" || inp == "yes")
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Here is your inventory. Press b to exit.", UtilityFunctions.typeSpeed);
                        bool consumed = UtilityFunctions.displayInventory(player, true);
                    }
                    else
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Here is your inventory. Press b to exit.", UtilityFunctions.typeSpeed);
                        UtilityFunctions.displayInventory(player, false);
                    }


                }
                else if (input == "s" || input == "stat" || input == "stats")
                {
                    UtilityFunctions.DisplayAllStats(player);
                }

                else if (input == "c" || input == "clear")
                {
                    UtilityFunctions.clearScreen(player);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "The console has been cleared", UtilityFunctions.typeSpeed);
                }
                else if (input == "g" || input == "game")
                {
                    UtilityFunctions.clearScreen(player);
                    actionTyped = true;
                }
                else
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Please enter a valid command. Type help for assistance.", UtilityFunctions.typeSpeed);
                }
            }

        }





        public static void editSave(string label, int value)
        {
            // i will call this function whenever i want to save something to the selected savefile. for example when i get a new item, when my exp changes, etc. 
            // i will search through the save and match each line to the label in the parameters, then once found assign that value.
            // i will also need to split the string array of each line into each array being a small array, due to the layout of X:0:equipment/consumable
            // i will substring the lines using the splitter ":"

            File.ReadAllLines(saveFile);
            string[] saveLayout = File.ReadAllLines(saveFile);
            string splitter = ":";
        }





        public static void overrideSave(string slot)
        {
            // override the current save with a blank

            try
            {
                // Open the file in write mode and truncate its content
                using (FileStream fileStream = new FileStream(slot, FileMode.Truncate))
                {
                    // The file will now be empty, and you can start writing new data to it
                }
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Save file cleared successfully.", typeSpeed);
            }
            catch (Exception ex)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"An error occurred: {ex.Message}", typeSpeed);
            }
        }





        public static void loadSave(string slot)
        {
            // load the save from the save slot
            UtilityFunctions.loadedSave = true;
        }

        // load the class from the first line of the corresponding save. think of lines needed to add to the save. base stats can be generated from the class. will need exp, level. items.



        public static void clearScreen(Player player)
        {
            Console.Clear();

            displayStats(player);

            Console.Write("\n\n");
        }





        public static void displayStats(Player player, bool menu = false)
        {
            if (player != null)
            {
                DisplayExpBar(player.currentExp, player.maxExp, Console.WindowWidth);
                int currentHealth = player.currentHealth;
                int maxHealth = player.maxHealth;
                double healthPercentage = (double)currentHealth / maxHealth;

                int redValue, greenValue;
                if (healthPercentage > 0.5)
                {
                    redValue = (int)(255 * (1 - healthPercentage) * 2);
                    greenValue = 255;
                }
                else
                {
                    redValue = 255;
                    greenValue = (int)(255 * healthPercentage * 2);
                }

                string healthColor = string.Format("{0:X2}{1:X2}00", redValue, greenValue);
                Console.WriteLine($"Health: \x1b[38;2;{redValue};{greenValue};0m{currentHealth}/{maxHealth}\x1b[0m");
                //DisplayHealthBar(player.currentHealth, player.maxHealth, Console.WindowWidth);
                Console.WriteLine($"X: {player.playerPos.X} Y: {player.playerPos.Y}");
            }
            else if (menu)
            {
                Console.WriteLine("\x1b[38;2;200;50;50mTesting Environment\x1b[0m");
                Console.WriteLine(mainDirectory);
                /*
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Testing Environment");
                Console.ForegroundColor = ConsoleColor.White;
                */
                //Console.WriteLine("X: 0 Y: 0");
            }
        }




        public static void DisplayHealthBar()
        {

        }




        public static Player loadPlayerFromFile()
        {
            // this function will initialise a new player, but instead of giving them the base stats and everything, it will take a save file and put those stats into the game.
            // it will then return the player.
            string[] lines = File.ReadAllLines(saveFile);
            List<string> statLabels = new List<string>() { "strength=", "dexterity=", "intelligence=", "currentHealth=", "maxHealth=", "currentExp=", "maxExp=", "level=", "defense=", "dodge=" };
            string chosenCharacter = lines[1].Substring(12, 1); // gets first letter of class to identify
            for (var i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(':');
                if (statLabels.Contains(parts[0]))
                {
                    switch (parts[0])
                    {
                        case "strength":
                            statLabels[0] += parts[1];
                            break;
                        case "dexterity":
                            statLabels[1] += parts[1];
                            break;
                        case "intelligence":
                            statLabels[2] += parts[1];
                            break;
                        case "currentHealth":
                            statLabels[3] += parts[1];
                            break;
                        case "maxHealth":
                            statLabels[4] += parts[1];
                            break;
                        case "currentExp":
                            statLabels[5] += parts[1];
                            break;
                        case "maxExp":
                            statLabels[6] += parts[1];
                            break;
                        case "level":
                            statLabels[7] += parts[1];
                            break;
                        case "defense":
                            statLabels[8] += parts[1];
                            break;
                        case "dodge":
                            statLabels[9] += parts[1];
                            break;
                        default:
                            throw new Exception("Invalid stat label");
                    }
                }
            }
            string[] uncheckedStats = statLabels.ToArray();
            List<int> stats = new List<int>();
            foreach (string stat in uncheckedStats)
            {
                if (stat.Contains("="))
                {
                    stats.Add(Convert.ToInt32(stat.Substring(stat.IndexOf("=") + 1)));
                }
            }

            Player player;
            if (chosenCharacter.ToLower() == "w")
            {
                player = new Warrior() { strength = stats[0], dexterity = stats[1], intelligence = stats[2], currentHealth = stats[3], maxHealth = stats[4], currentExp = stats[5], maxExp = stats[6], level = stats[7], defense = stats[8], dodge = stats[9] };
                return player;
            }
            else if (chosenCharacter.ToLower() == "r")
            {
                player = new Rogue() { strength = stats[0], dexterity = stats[1], intelligence = stats[2], currentHealth = stats[3], maxHealth = stats[4], currentExp = stats[5], maxExp = stats[6], level = stats[7], defense = stats[8], dodge = stats[9] };
                return player;
            }
            else
            {
                player = new Mage() { strength = stats[0], dexterity = stats[1], intelligence = stats[2], currentHealth = stats[3], maxHealth = stats[4], currentExp = stats[5], maxExp = stats[6], level = stats[7], defense = stats[8], dodge = stats[9] };
            }
            return player;
        }




        public static string chooseClass()
        {
            Console.Clear();
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nSelect a class from the following:\n\n[W] Warrior\n[R] Rogue\n[M] Mage", typeSpeed);
            bool chosen = false;
            bool valid = false;
            bool valid2 = false;
            string input = Console.ReadLine();
            string[] classes = { "Warrior", "Rogue", "Mage" };
            string[] classAbbreviations = { "W", "R", "M" };
            input = input.Substring(0, 1);
            int index = 0;
            while (!chosen)
            {
                foreach (string i in classAbbreviations)
                {
                    if (input.ToLower() == i.ToLower())
                    {
                        valid = true;
                        valid2 = true;
                        chosen = true;
                    }
                    else if (!chosen)
                    {
                        index++;
                    }
                }

                while (!valid2)
                {
                    if (!valid)
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Please enter a valid class", typeSpeed);
                        Thread.Sleep(1000);
                        Console.Clear();
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nSelect a class from the following:\n\n[W] Warrior\n[R] Rogue\n[M] Mage", typeSpeed);
                        input = Console.ReadLine();
                        input = input.Substring(0, 1);
                        index = 0;

                        foreach (string i in classAbbreviations)
                        {
                            if (input.ToLower() == i.ToLower())
                            {
                                valid = true;
                                chosen = true;
                            }
                            else if (!chosen)
                            {
                                index++;
                            }
                        }
                        if (valid)
                        {
                            valid2 = true;

                        }
                    }
                }
            }

            return classes[index];
        }





        public static void TypeText(bool inst, string text, int typingSpeed, bool newLine = true)
        {
            if (inst)
            {
                Console.WriteLine($"{colourScheme.generalTextCode}{text}{colourScheme.generalTextCode}");
            }
            else if (!newLine)
            {
                Thread.Sleep(20);
                Console.Write($"{colourScheme.generalTextCode}{text}{colourScheme.generalTextCode}");
            }
            else
            {
                Thread.Sleep(20);
                Console.Write($"{colourScheme.generalTextCode}{text}{colourScheme.generalTextCode}");
                // foreach (char c in text)
                // {
                //     Console.Write(c);
                //     Thread.Sleep(typingSpeed);
                // }
                Console.Write("\n");
            }
        }







        public static void DisplayExpBar(int currentExp, int maxExp, int barLength)
        {
            double progress = (double)currentExp / maxExp;
            barLength -= 15;
            int filledLength = (int)(barLength * progress);

            string filledPart = new string('█', filledLength);
            string emptyPart = new string('-', barLength - filledLength);

            Console.Write("EXP: [");
            Console.Write("\x1b[94m" + filledPart + "\x1b[0m");
            Console.WriteLine(emptyPart + "] " + currentExp + "/" + maxExp);
        }




        public static int[] getShadeFromDist(int initialr, int initialg, int initialb, double dist, int scopew, int scopeh)
        {
            // this function will return a reduced fraction of each rgb value to reduce its shade if its further away.
            double baseSight = 0.1;
            double scopeavg = (scopew + scopeh) / 2;
            double fraction = (scopeavg / dist) + baseSight;
            if (fraction > 1)
            {
                fraction = 1;
            }
            else if (fraction < 0)
            {
                fraction = 0;
            }

            int red = (int)(initialr * fraction);
            int green = (int)(initialg * fraction);
            int blue = (int)(initialb * fraction);
            return new int[] { red, green, blue };
        }


        // public static void DisplayAllStats(string classChosen)
        // {
        //     string filePath = $"Classes/{classChosen}.txt";
        //     string[] lines = File.ReadAllLines(filePath);

        //     Console.WriteLine($"\n{classChosen} Stats:\n");
        //     foreach (string line in lines)
        //     {
        //         string[] parts = line.Split(':');
        //         string statName = parts[0].Trim();
        //         int statValue = int.Parse(parts[1].Trim());
        //         Console.WriteLine($"{0}. {statName} : {statValue}");
        //     }
        // }
        public static void DisplayAllStats(Player chosenCharacter)
        {
            Console.WriteLine($"\n{chosenCharacter.GetType().Name} Stats:\n");
            Console.WriteLine($"Strength: {chosenCharacter.strength}");
            Console.WriteLine($"Dexterity: {chosenCharacter.dexterity}");
            Console.WriteLine($"Intelligence: {chosenCharacter.intelligence}");
            Console.WriteLine($"Max Health: {chosenCharacter.maxHealth}");
            Console.WriteLine($"Current Health: {chosenCharacter.currentHealth}");
            Console.WriteLine($"Defense: {chosenCharacter.defense}");
            Console.WriteLine($"Dodge: {chosenCharacter.dodge}");
            // Add any other stats you want to display
        }





        public static bool alive(string type, Enemy enemy, Player player)
        {
            if (type == "enemy")
            {
                if (enemy.currentHealth > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                    // YOU WIN FIGHT
                }
            }
            else
            {
                if (player.currentHealth > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                    // YOU DIE
                }
            }
        }





        public static Enemy CreateEnemyInstance(string enemyName)
        {
            if (enemyName == "orc")
            {
                return new Orc();
            }
            else if (enemyName == "snake")
            {
                return new Snake();
            }
            else if (enemyName == "boar")
            {
                return new Boar();
            }
            else
            {
                throw new ArgumentException("Invalid enemy name");
            }
        }

        public static Player CreatePlayerInstance(string chosenClass)
        {
            if (chosenClass == "Mage")
            {
                return new Mage();
            }
            else if (chosenClass == "Rogue")
            {
                return new Rogue();
            }
            else
            {
                return new Warrior();
            }
        }





        public static void useItem(Player player, string item, int index)
        {
            for (var i = 0; i < player.inventory.Length; i++)
            { // displays inventory - only consumables
                if (int.Parse(player.inventory[i][1]) > 0)
                {
                    if (player.inventory[i][2] == "consumable")
                    {
                        if (player.inventory[i][0] == item)
                        { // use item
                            int newVal = int.Parse(player.inventory[i][1]);
                            newVal -= 1;
                            player.inventory[i][1] = newVal.ToString();
                            // add item effects
                            int oldHealth = player.currentHealth;
                            switch (item)
                            {
                                case "small-health-potion":

                                    player.currentHealth += 25;
                                    if (player.currentHealth > player.maxHealth)
                                    {
                                        player.currentHealth = player.maxHealth;
                                    }
                                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "\n\x1b[31mHealth: " + oldHealth + " => " + player.currentHealth + "\x1b[0m", typeSpeed);
                                    break;
                                case "large-health-potion":

                                    player.currentHealth += 50;
                                    if (player.currentHealth > player.maxHealth)
                                    {
                                        player.currentHealth = player.maxHealth;
                                    }
                                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "\n\x1b[31mHealth: " + oldHealth + " => " + player.currentHealth + "\x1b[0m", typeSpeed);
                                    break;
                                default:
                                    Console.WriteLine("Unknown item: error");
                                    break;
                            }
                        }
                    }

                }
            }
        }





        public static bool displayInventory(Player player, bool more)
        {

            if (more)
            {
                UtilityFunctions.clearScreen(player);
                for (var i = 0; i < player.inventory.Length; i++)
                { // displays inventory - only consumables
                    if (int.Parse(player.inventory[i][1]) > 0)
                    {
                        if (player.inventory[i][2] == "consumable")
                        {
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, i + ". " + player.inventory[i][0] + " : " + player.inventory[i][1], typeSpeed);
                        }

                    }
                }
                bool typed = false;
                while (!typed)
                {
                    string input = Console.ReadLine();
                    if (input == "b" || input == "back")
                    { // takes input
                        typed = true;
                        return false;
                    }
                    else if (int.Parse(input) > player.inventory.Length || int.Parse(input) < 0)
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Enter a valid input", typeSpeed);
                    }
                    else
                    {
                        for (var v = 0; v < player.inventory.Length; v++)
                        {

                            if (int.Parse(player.inventory[v][1]) > 0)
                            {
                                if (player.inventory[v][2] == "consumable")
                                {
                                    if (int.Parse(input) == v)
                                    {
                                        useItem(player, player.inventory[v][0], v);
                                        typed = true;
                                        return true;
                                    }
                                }

                            }
                        }

                    }
                }
            }
            else
            {
                for (var i = 0; i < player.inventory.Length; i++)
                {
                    if (int.Parse(player.inventory[i][1]) > 0)
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, i + ". " + player.inventory[i][0] + " : " + player.inventory[i]
                    [1], typeSpeed);
                    }
                }
                string input = Console.ReadLine();
                if (input == "b" || input == "back")
                { // takes input
                    return false;
                }
            }
            return false;
        }





        public static string RandomWord(string[] words)
        {
            Random random = new Random();
            int randomIndex = random.Next(words.Length);
            return words[randomIndex];
        }





        public static void givePlayerItem(string item, Player player)
        {
            for (var i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i][0] == item)
                {
                    int newVal = int.Parse(player.inventory[i][1]);
                    newVal += 1;
                    player.inventory[i][1] = newVal.ToString();
                }
            }
        }
    }




    public class ColourScheme
    {
        public string[] schemes = { "default", "isaac" };
        public string generalTextCode = "";
        public string menuMainCode = "";
        public string menuAccentCode = "";
        public string generalAccentCode = "";
        

        public ColourScheme(int colourSchemeIndex)
        {

            switch (schemes[colourSchemeIndex])
            {
                case "default":
                    generalTextCode = setColourScheme(255, 255, 255);
                    menuAccentCode = setColourScheme(137, 239, 245);
                    menuMainCode = setColourScheme(210, 226, 252);
<<<<<<< HEAD
                    generalAccentCode = setColourScheme(94, 108, 255);
                    break;
                // Add more colour schemes here
                case "isaac":
                    generalTextCode = setColourScheme(255, 255, 255);
                    menuAccentCode = setColourScheme(255, 151, 107);
                    menuMainCode = setColourScheme(255, 222, 255);
                    generalAccentCode = setColourScheme(255, 255, 255);
=======
                    break;
                // Add more colour schemes here
                case "isaac":
                    generalTextCode = setColourScheme(145, 255, 215);
                    menuAccentCode = setColourScheme(255, 151, 107);
                    menuMainCode = setColourScheme(255, 222, 255);
>>>>>>> 1063f3ab3a4c7adf9b4173b09bdb61bff3bca8a8
                    break;
            }

        }

        static string setColourScheme(int r, int g, int b)
        {
            return $"\x1b[38;2;{r};{g};{b}m";
        }
    }


}