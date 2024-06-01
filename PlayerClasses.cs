using EnemyClassesNamespace;
using UtilityFunctionsNamespace;
using GPTControlNamespace;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Cryptography;
using System.Dynamic;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Reflection;
using OpenAI_API;
using OpenAI_API.Chat;

namespace PlayerClassesNamespace
{
    [Serializable]
    public class Player
    {
        public string Class { get; set; }
        public string Race { get; set; }
        public int Health { get; set; }
        public int currentHealth { get; set; }
        public int ManaPoints { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }


        public int level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public Point playerPos;

        public Player()
        {
            maxExp = 10;
            level = 1;
            currentExp = 0;
            playerPos = new Point(0, 0);
        }

        public async Task initialisePlayerFromNarrator(OpenAIAPI api, Conversation chat, Player player)
        {
            Console.WriteLine("Creating Character...");
            
            // load prompt 5
            string prompt5 = "";
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                prompt5 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt5.txt");
            }
            catch (Exception e)
            {
                throw new Exception($"Could not find prompt file: {e}");
            }

            // get response from GPT
            string output = "";
            try
            {
                // output = await Narrator.getGPTResponse(prompt5, api, 100, 0.9);
                chat.AppendUserInput(prompt5);
                output = await chat.GetResponseFromChatbotAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get response: {e}");
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }


            //Console.WriteLine(output);


            string finalXMLText = "";
            if (string.IsNullOrEmpty(UtilityFunctions.saveSlot)) // if testing / error
            {
                // get all save file
                string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml");
                bool started = false;
                for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                {
                    if (saves.Length == i)
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant,
                            $"Save Slot save{i + 1}.xml is empty. Do you want to begin a new game? y/n",
                            UtilityFunctions.typeSpeed);
                        string load = Console.ReadLine();
                        if (load == "y")
                        {
                            string save = UtilityFunctions.mainDirectory + @$"saves\save{i + 1}.xml";
                            UtilityFunctions.saveSlot = Path.GetFileName(save);
                            UtilityFunctions.saveFile = save;
                            started = true;
                            i = UtilityFunctions.maxSaves;
                        }
                    }
                }

                if (!started)
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant,
                        "No empty save slots. Exiting Test. Press any key to leave", UtilityFunctions.typeSpeed);
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.Black;
            //Console.WriteLine(UtilityFunctions.saveFile);
            //Console.WriteLine(output);


            // design xml file
            string preText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            output = await UtilityFunctions.cleanseXML(output);
            finalXMLText = preText + "\n" + output;


            try
            {
                File.Create(UtilityFunctions.saveFile).Close();
                File.WriteAllText(UtilityFunctions.saveFile, finalXMLText);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not write to file: {e}");
            }


            // Player player with attributes
            XmlSerializer serializer = new XmlSerializer(typeof(Player));
            using (TextReader reader = new StringReader(finalXMLText))
            {
                player = (Player)serializer.Deserialize(reader);
            }


            // set player properties
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            Type playerType = typeof(Player);
            PropertyInfo[] properties = playerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);


            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(player);
                    property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property {property.Name}: {ex.Message}");
                    // Handle or log the error as necessary
                }
            }


            // set character health to max
            currentHealth = Health;
            // this.
            Console.WriteLine("Character Created!");
            Console.ForegroundColor = ConsoleColor.White;
            Thread.Sleep(500);
            Console.Clear();
        }

        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }

        public void changePlayerStats(string stat, int newValue, bool midLevelUp = false)
        {
            GetType().GetProperty(stat).SetValue(this, newValue);

            string fileName = UtilityFunctions.saveFile;


            // Load the XML document
            XDocument document = XDocument.Load(fileName);

            // Locate the element in the XML document
            XElement statElement = document.Element("Player")?.Element(stat);

            if (statElement != null)
            {
                // Update player stat
                statElement.SetValue((object)newValue);
                document.Save(fileName);
            }
            else
            {
                Console.WriteLine($"Element {stat} not found in {fileName}.");
            }

            if (!midLevelUp)
            {
                checkForLevelUp();
            }
        }


        public void checkForLevelUp()
        {
            /*while (currentExp >= maxExp)
            {
                levelUp();
            }*/
        }
    }
}