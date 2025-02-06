using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PlayerClassesNamespace;
using EnemyClassesNamespace;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CombatNamespace;
using ItemFunctionsNamespace;
using Newtonsoft.Json;
using DynamicExpresso;
using GameClassNamespace;
using GPTControlNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using OpenAI_API.Chat;

namespace UtilityFunctionsNamespace
{
    public class TypeText
    {
        public bool _newLine;
        public int _typingSpeed;
        public bool _inst;

        public TypeText(bool inst = false, int typingSpeed = 2, bool newLine = true)
        {
            _inst = inst;
            _typingSpeed = typingSpeed;
            _newLine = newLine;
        }
    }

    public class UtilityFunctions
    {
        // public static Interpreter interpreter = new Interpreter()
        //    .Reference(typeof(Player));

        public static string playerContextInput = "";
        public static int maxNodeDepth;
        public static int maxGraphDepth;
        public static int stdNodeDepth = 5; // PRESET
        public static int stdGraphDepth = 10; // PRESET

        public static int nextEnemyId = 0;

        public static string universalSeperator = "------------------------------------------";
        public static string playerANSIUI = $"\x1b[38;2;80;200;120m";
        public static string aggressiveANSIUI = $"\x1b[38;2;210;43;43m";
        public static string neutralANSIUI = $"\x1b[38;2;204;85;0m";
        public static string timidANSIUI = $"\x1b[38;2;255;191;0m";

        public static Interpreter
            interpreter = new Interpreter().Reference(typeof(Player)).Reference(typeof(Enemy)); // gets set up 

        public static int typeSpeed = 1;
        public static string saveSlot = ""; // will be written to in main menu. NAME + EXT OF SAVE

        public static string mainDirectory =
            @"/Users/18vanenckevorti/RiderProjects/NEA-AI-Game/"; // will be written to in main menu

        public static string
            saveFile = @mainDirectory + saveSlot; // will be written to in main menu. WHOLE PATH TO FILE

        public static string saveName = ""; // ONLY NAME OF SAVE

        public static string itemTemplateDir = @$"{mainDirectory}ItemTemplates{Path.DirectorySeparatorChar}";
        public static string itemTemplateSpecificDirectory = "";

        public static string enemyTemplateDir = @$"{mainDirectory}EnemyTemplates{Path.DirectorySeparatorChar}";
        public static string enemyTemplateSpecificDirectory = "";

        public static string attackBehaviourTemplateDir =
            @$"{mainDirectory}AttackBehaviours{Path.DirectorySeparatorChar}";

        public static string attackBehaviourTemplateSpecificDirectory = "";

        public static string statusesDir = $@"{mainDirectory}Statuses{Path.DirectorySeparatorChar}";
        public static string statusesSpecificDirectory = "";

        public static string logsDir = $@"{mainDirectory}Logs{Path.DirectorySeparatorChar}";
        public static string logsSpecificDirectory = "";

        public static string playerAttacksDir = @$"{mainDirectory}CharacterAttacks{Path.DirectorySeparatorChar}";
        public static string playerAttacksSpecificDirectory = "";

        public static string mapsDir = @$"{mainDirectory}MapStructures{Path.DirectorySeparatorChar}";
        public static string mapsSpecificDirectory = ""; // TO BE DONE

        public static bool showExampleInSaves = true; // testing purposes

        public static int maxSaves = 20;
        public static bool loadedSave = false;
        public static bool Instant = false;
        public static int colourSchemeIndex = 0;
        public static ColourScheme colourScheme = new ColourScheme(UtilityFunctions.colourSchemeIndex);
        public static string promptPath = @$"{mainDirectory}Prompts{Path.DirectorySeparatorChar}";

        

        public static Point ClonePoint(Point point)
        {
            Point newPoint = new Point();
            newPoint.X = point.X;
            newPoint.Y = point.Y;
            return newPoint;
        }

        public static int GiveNewEnemyId()
        {
            nextEnemyId++;
            return nextEnemyId - 1;
        }

        public static string getBaseDir()
        {
            string fullDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory)); // bin debug net
            List<string> directories = fullDir.Split(Path.DirectorySeparatorChar).ToList();
            string finalPath = "";
            foreach (string dir in directories)
            {
                if (dir == "bin" && directories.IndexOf(dir) + 4 == directories.Count)
                {
                    finalPath.Remove(finalPath.LastIndexOf(Path.DirectorySeparatorChar));
                    return finalPath;
                    // /Users/18vanenckevorti/RiderProjects/NEA-AI-Game
                }
                else if (dir != "")
                {
                    finalPath += dir + Path.DirectorySeparatorChar;
                }
            }

            // error
            throw new DirectoryNotFoundException($"Could not find the directory {finalPath}.");
        }

        public static void initialiseGPTLogging()
        {
            if (Directory.Exists(@$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}") == false &&
                UtilityFunctions.saveName != "saveExample")
            {
                Directory.CreateDirectory(UtilityFunctions.logsSpecificDirectory);
            }
            else if (Directory.Exists(@$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}") == false &&
                     UtilityFunctions.saveName == "saveExample")
            {
                Directory.CreateDirectory(UtilityFunctions.logsSpecificDirectory);
            }
            else if (Directory.Exists(@$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}") == true &&
                     UtilityFunctions.saveName == "saveExample")
            {
                // check to see what user wants, for now just replace
                Directory.Delete(@$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}", true);
                Directory.CreateDirectory(UtilityFunctions.logsSpecificDirectory);
            }
            else if (Directory.Exists(@$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}") == true &&
                     UtilityFunctions.saveName != "saveExample")
            {
                // the directory should be archived down the line; for now just leaving
            }
        }

        public static async Task writeToJSONFile<T>(string path, T objectToWrite) where T : class
        {
            string json = JsonConvert.SerializeObject(objectToWrite, Formatting.Indented);

            // writes an object to a json file at the path path
            using (StreamWriter file = File.CreateText(path))
            {
                await file.WriteAsync(json);
            }
        }

        public static void writeToJSONFileSync<T>(string path, T objectToWrite) where T : class
        {
            string json = JsonConvert.SerializeObject(objectToWrite, Formatting.Indented);

            // writes an object to a json file at the path path
            using (StreamWriter file = File.CreateText(path))
            {
                file.Write(json);
            }
        }

        public static async Task<T> readFromJSONFile<T>(string path) where T : class
        {
            // reads an object from a json file at the path path
            using (StreamReader file = File.OpenText(path))
            {
                string json = await file.ReadToEndAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        public static async Task writeToXMLFile<T>(string path, T objectToWrite) where T : class
        {
            // writes an object to an xml file at the path path
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StreamWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, objectToWrite);
            }


            await Task.CompletedTask;
        }

        public static async Task<T> readFromXMLFile<T>(string path, T objectToRead) where T : class
        {
            // reads an object from an xml file at the path path
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StreamReader reader = new StreamReader(path))
            {
                objectToRead = serializer.Deserialize(reader) as T;
            }

            await Task.CompletedTask;
            return (T)objectToRead;
        }

        public static void CopyProperties(object source, object destination)
        {
            if (source.GetType() != destination.GetType())
                throw new InvalidOperationException("Mismatched types");

            var properties = source.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = property.GetValue(source);
                    property.SetValue(destination, value);
                }
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

        public static void DisplayAllEnemyTemplatesWithDetails()
        {
            Console.ForegroundColor = ConsoleColor.Black;

            foreach (KeyValuePair<string, EnemyTemplate> enemyTemplate in Program.game.enemyFactory.enemyTemplates)
            {
                Console.WriteLine($"Enemy Template: {enemyTemplate.Value.Name}");
                foreach (PropertyInfo property in typeof(EnemyTemplate).GetProperties())
                {
                    if (property.Name == "AttackBehaviours")
                    {
                        foreach (AttackSlot slot in Enum.GetValues(typeof(AttackSlot)))
                        {
                            if (enemyTemplate.Value.AttackBehaviours[slot] != null)
                            {
                                Console.WriteLine($"     {slot}: {enemyTemplate.Value.AttackBehaviours[slot].Name}");
                                if (enemyTemplate.Value.AttackBehaviours[slot].Statuses.Count == 0)
                                {
                                    Console.WriteLine(
                                        $"               This attack applies no statuses.");
                                }

                                foreach (string statusName in enemyTemplate.Value.AttackBehaviours[slot].Statuses)
                                {
                                    Console.WriteLine($"          Status: {statusName}");
                                    List<string> statusNamesList = new List<string>();
                                    foreach (Status status1 in Program.game.statusFactory.statusList)
                                    {
                                        statusNamesList.Add(status1.Name);
                                    }

                                    Status status = new Status();

                                    try
                                    {
                                        status =
                                            Program.game.statusFactory.statusList[
                                                Array.IndexOf(statusNamesList.ToArray(), statusName)];
                                        foreach (PropertyInfo statusProperty in typeof(Status).GetProperties())
                                        {
                                            Console.WriteLine(
                                                $"          {statusProperty.Name}: {statusProperty.GetValue(status)}");
                                        }
                                    }
                                    catch
                                    {
                                        Console.WriteLine(
                                            $"               Status {statusName} not found in status list.");
                                    }
                                }
                            }
                        }
                    }
                    else if (property.Name == "AttackBehaviourKeys")
                    {
                    }
                    else
                    {
                        Console.WriteLine($"{property.Name}: {property.GetValue(enemyTemplate)}");
                    }
                }
            }
        }

        public static async Task<string> FixJson(string json)
        {
            try
            {
                JsonConvert.DeserializeObject(json);
                return json;
            }
            catch (JsonReaderException)
            {
                Program.logger.Info($"Before FixJson: {json}");
                
                json = json.Substring(json.IndexOf('{'), json.Length - json.IndexOf('}') - 1);

                // Remove markdown code block indicators
                json = Regex.Replace(json, @"```json|```", "");

                // Normalize whitespace carefully, avoiding content inside quotes
                json = Regex.Replace(json, @"(?<!:\s*""[^""]*)\s+|\s+(?![^""]*""\s*:)", " ");

                // Attempt to fix common JSON errors
                json = Regex.Replace(json, @"([{,])(\s*)([^""{}\s:]+?)\s*:", "$1\"$3\":");
                //json = Regex.Replace(json, @":\s*(?!(?:null|true|false)(?=[,\]}])|(\[\])|(\{\}))(?:([""'])(?:(?!\3).)*\3|([^""'\s:][^,]*?))([,\]})", ":\"$4\"$5");

                //json = Regex.Replace(json, @":\s*(?!(?:null|true|false|\[\]|\{\})\b)([^""{}\s:]+?)([},])", ":\"$1\"$2");
                //json = Regex.Replace(json, @":\s*(?!(?:null|true|false)\b)([^""{}\s:]+?)([},])", ":\"$1\"$2");

                // Ensure curly braces
                if (!json.Trim().StartsWith("{"))
                    json = "{" + json;
                if (!json.Trim().EndsWith("}"))
                    json += "}";

                // Remove empty lines
                json = Regex.Replace(json, @"^\s*$\n|\r", "", RegexOptions.Multiline);

                var jsonObj = JsonConvert.DeserializeObject(json);
                string prettyJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);

                Program.logger.Info($"After FixJson: {prettyJson}");

                try
                {
                    return prettyJson;
                }
                catch (JsonReaderException)
                {
                    return "JSON is still not valid after attempted fixes.";
                }
            }
        }

        public static void CorrectXmlTags(string filePath)
        {
            Stack<string> tagStack = new Stack<string>();
            StringBuilder correctedXml = new StringBuilder();
            string line;

            using (StreamReader reader = new StreamReader(filePath))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    // This regex matches opening tags and closing tags
                    MatchCollection matches = Regex.Matches(line, @"<(/?)(\w+)[^>]*>");

                    foreach (Match match in matches)
                    {
                        string tag = match.Groups[2].Value;
                        bool isClosing = match.Groups[1].Value == "/";

                        if (!isClosing) // Opening tag
                        {
                            tagStack.Push(tag);
                        }
                        else if (tagStack.Count > 0 && tagStack.Peek() == tag) // Correct closing tag
                        {
                            tagStack.Pop();
                        }
                        else // Mismatch found
                        {
                            if (tagStack.Count > 0)
                            {
                                string correctTag = tagStack.Pop();
                                line = Regex.Replace(line, $"</{tag}>", $"</{correctTag}>");
                            }
                        }
                    }

                    correctedXml.AppendLine(line);
                }
            }

            // Output the corrected XML to a new file or overwrite the old one
            File.WriteAllText(filePath, correctedXml.ToString());
            //Console.WriteLine("XML has been corrected and written to " + filePath);
        }

        // load the class from the first line of the corresponding save. think of lines needed to add to the save. base stats can be generated from the class. will need exp, level. items.

        public static void clearScreen(Player player, bool sidePrint = false)
        {
            Console.Clear();
            if (Program.testing)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("MODE: Testing");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("MODE: Game");
                Console.ResetColor();
            }
            
            if (sidePrint)
            {
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            if (player != null) Console.Write($"CLASS: ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            if (player != null) Console.Write($"{player.Class}\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            if (Program.gameStarted)
                Console.Write($"NODE: {GridFunctions.CurrentNodeName}, id - {GridFunctions.CurrentNodeId}\n");

            displayStats(player);

            Console.Write("\n");
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
            Console.ForegroundColor = ConsoleColor.White;
            if (player != null)
            {
                Console.WriteLine($"LEVEL: {player.Level}");
                DisplayExpBar(player.currentExp, player.maxExp, 80);
                Console.WriteLine("\n" + DrawHealthBar(player));
                //DisplayHealthBar(player.currentHealth, player.maxHealth, Console.WindowWidth);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"X: {player.playerPos.X} Y: {player.playerPos.Y}");
            }
            else if (menu)
            {
                Console.WriteLine("\x1b[38;2;200;50;50mTesting Environment\x1b[0m");
                Console.WriteLine(mainDirectory);
                /*
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Testing Environment");
                Console.ForegroundColor = ConsoleColor.Black;
                */
                //Console.WriteLine("X: 0 Y: 0");
            }
        }

        public static string DrawHealthBar(object playerObj)
        {
            if (playerObj.GetType() == typeof(Player))
            {
                Player player = (Player)playerObj;
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
                //Console.ForegroundColor = ConsoleColor.White;
                return ($"HP: \x1b[38;2;{redValue};{greenValue};0m{currentHealth}/{maxHealth}\x1b[38;2;0m");
            }
            else
            {
                // stupid name ik
                Enemy player = (Enemy)playerObj;
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
                return ($"HP: \x1b[38;2;{redValue};{greenValue};0m{currentHealth}/{maxHealth}\x1b[38;2;0m");
            }
        }

        public static void UpdateVars(ref Game game)
        {
            int consoleHeight = Console.WindowHeight;
            if (game.player.sightRangeModified)
            {
                game.player.sightRange -= game.player.sightRangeModifiedBy;
                game.player.sightRangeModified = false;
            }
            else
            {
                game.player.sightRange = (consoleHeight - 14) / 2;
            }

            if (game.player.sightRange == 0)
            {
                game.player.sightRange = 6;
            }

            Player player = (Player)game.player;
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

            GridFunctions.RedGreenBluePlayerVals[0] = redValue;
            GridFunctions.RedGreenBluePlayerVals[1] = greenValue;
            GridFunctions.RedGreenBluePlayerVals[2] = 0;
        }

        public static string DrawManaBar(object playerObj)
        {
            if (playerObj.GetType() == typeof(Player))
            {
                Player player = (Player)playerObj;
                int currentMana = player.currentMana;
                int maxMana = player.ManaPoints;
                double manaPercentage = (double)currentMana / maxMana;


                int redValue, greenValue, blueValue;
                if (manaPercentage > 0.5)
                {
                    redValue = 50;
                    blueValue = (int)(255 * manaPercentage);
                    greenValue = (int)(200 * manaPercentage);
                }
                else
                {
                    redValue = (int)(50 * manaPercentage * 2);
                    blueValue = (int)(50 * manaPercentage * 2) + 120;
                    greenValue = (int)(50 * manaPercentage * 2) + 100;
                }

                string manaColor = string.Format("{0:X2}{1:X2}00", redValue, greenValue);
                return ($"MP: \x1b[38;2;{redValue};{greenValue};{blueValue}m{currentMana}/{maxMana}\x1b[0m");
            }
            else
            {
                // again, stupid name ik
                Enemy player = (Enemy)playerObj;
                int currentMana = player.currentMana;
                int maxMana = player.ManaPoints;
                double manaPercentage = (double)currentMana / maxMana;


                int redValue, greenValue, blueValue;
                if (manaPercentage > 0.5)
                {
                    redValue = 50;
                    blueValue = (int)(255 * manaPercentage);
                    greenValue = (int)(200 * manaPercentage);
                }
                else
                {
                    redValue = (int)(50 * manaPercentage * 2);
                    blueValue = (int)(50 * manaPercentage * 2) + 120;
                    greenValue = (int)(50 * manaPercentage * 2) + 100;
                }

                string manaColor = string.Format("{0:X2}{1:X2}00", redValue, greenValue);
                return ($"MP: \x1b[38;2;{redValue};{greenValue};{blueValue}m{currentMana}/{maxMana}\x1b[0m");
            }
        }

        public static void TypeText(TypeText typeText, string text)
        {
            if (typeText._inst)
            {
                Console.WriteLine($"{text}");
            }
            else if (!typeText._newLine)
            {
                Thread.Sleep(20);
                Console.Write($"{text}");
            }
            else
            {
                //Thread.Sleep(20);
                //Console.Write($"{colourScheme.generalTextCode}{text}{colourScheme.generalTextCode}");
                foreach (char c in text)
                {
                    Console.Write(c);
                    if (c != '\n' || c != ' ')
                    {
                        Thread.Sleep(typeText._typingSpeed);
                    }
                }

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
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(emptyPart + "] " + currentExp + "/" + maxExp);
        }
        
        public static string ReturnExpBar(int currentExp, int maxExp, int barLength)
        {
            double progress = (double)currentExp / maxExp;
            barLength -= 15;
            int filledLength = (int)(barLength * progress);

            string filledPart = new string('█', filledLength);
            string emptyPart = new string('-', barLength - filledLength);

            string finalToPrint = "EXP: [" + "\x1b[94m" + filledPart + "\x1b[38;2;255;255;255m";
            Console.ForegroundColor = ConsoleColor.White;
            finalToPrint += emptyPart + "] " + currentExp + "/" + maxExp;
            
            return finalToPrint;
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

        public static Player CreatePlayerInstance()
        {
            return new Player();
        }
    }

    public class LambdaJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Lambda);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var expression = reader.Value.ToString();
                // Allow scripts to use Console
                return UtilityFunctions.interpreter.Parse(expression, new Parameter("target", typeof(Player)));
            }

            throw new JsonSerializationException("Expected string value.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Serialization logic if needed, or throw an exception if not supported.
            throw new NotImplementedException("This converter does not support writing.");
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