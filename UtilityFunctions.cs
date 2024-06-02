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
        public static int typeSpeed = 1;
        public static string saveSlot = ""; // will be written to in main menu

        public static string mainDirectory =
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\")); // will be written to in main menu

        public static string saveFile = @mainDirectory + saveSlot; // will be written to in main menu
        public static string saveName = "";
        
        public static string itemTemplateDir = @$"{mainDirectory}ItemTemplates\";
        public static string itemTemplateSpecificDirectory = "";
        public static int maxSaves = 5;
        public static bool loadedSave = false;
        public static bool Instant = false;
        public static int colourSchemeIndex = 0;
        public static ColourScheme colourScheme = new ColourScheme(UtilityFunctions.colourSchemeIndex);
        public static string promptPath = @$"{mainDirectory}Prompts\";

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

        public async static Task<string> cleanseXML(string xml)
        {
            // this function will ensure that the output the narrator gives is parseable
            
            string[] lines = xml.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> lineList = lines.ToList();
            List<string> newLineList = new List<string>();

            foreach (string line in lineList)
            {
                if (line.Contains("<") || line.Contains("SEPERATOR", StringComparison.Ordinal))
                {
                    newLineList.Add(line);
                }
            }
            
            string output = string.Join("\n", newLineList);
            return output;
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
        
        public static T DeserializeXmlFromFile<T>(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamReader reader = new StreamReader(filePath))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static void displayStats(Player player, bool menu = false)
        {
            if (player != null)
            {
                DisplayExpBar(player.currentExp, player.maxExp, Console.WindowWidth);
                int currentHealth = player.currentHealth;
                int maxHealth = player.Health;
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

        public static int[] getShadeFromDist(int initialr, int initialg, int initialb, double dist, int scopew,
            int scopeh)
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

        public static Player CreatePlayerInstance()
        {
            return new Player();
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
                    generalAccentCode = setColourScheme(94, 108, 255);
                    break;
                // Add more colour schemes here
                case "isaac":
                    generalTextCode = setColourScheme(255, 255, 255);
                    menuAccentCode = setColourScheme(255, 151, 107);
                    menuMainCode = setColourScheme(255, 222, 255);
                    generalAccentCode = setColourScheme(234, 200, 174);
                    break;
            }
        }

        static string setColourScheme(int r, int g, int b)
        {
            return $"\x1b[38;2;{r};{g};{b}m";
        }
    }
}