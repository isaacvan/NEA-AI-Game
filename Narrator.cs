using System.Reflection;
using System.Xml.Serialization;
using EnemyClassesNamespace;
using MainNamespace;
using OpenAI_API;
using Microsoft.Extensions.Configuration;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace GPTControlNamespace
{
    public interface GameSetup
    {
        public void chooseSave();

        Task<Player> generateMainXml(Conversation chat, string prompt5, Player player);

        Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat, EnemyFactory enemyFactory);
    }
    
   
    public class Narrator : GameSetup
    {
        private OpenAIAPI api; 
        private Conversation chat;
        
        // Testing
        public static OpenAIAPI initialiseGPT()
        {
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Initialising GPT...", UtilityFunctions.typeSpeed);
            //Thread.Sleep(500);
            string? apiKey = System.Environment.GetEnvironmentVariable("API_KEY");
            Console.WriteLine($"ENV API Key: {apiKey}");
            if (apiKey == null)
            {
                Console.WriteLine("ENV API Key is not set, trying secrets");
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();
                apiKey = config["API_KEY"];
                if (apiKey != null)
                {
                    Console.WriteLine("Secret found");
                }
                else
                {
                    throw new Exception("No GPT API key! Set API_KEY env variable");
                }
            }
            System.Environment.SetEnvironmentVariable("API_KEY", apiKey);
            
            OpenAIAPI api = new OpenAIAPI(apiKey);
            
            
            
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Initialised GPT.", UtilityFunctions.typeSpeed);
            //Thread.Sleep(500);
            Console.Clear();
            
            return api;
        }

        public static Conversation initialiseChat(OpenAIAPI api)
        {
            Conversation chat = api.Chat.CreateConversation();
            chat.Model = Model.GPT4_Turbo;
            chat.RequestParameters.Temperature = 0.9;
            return chat;
        }

        public void chooseSave()
        {
            bool gameStarted = false;
            bool saveChosen = false;
            while (!saveChosen)
            {
                saveChosen = Program.menu(gameStarted, saveChosen); // displays the menu
            }
        }
        
        public async Task<Player> generateMainXml(Conversation chat, string prompt5, Player player)
        {
            string output;
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


            if (string.IsNullOrEmpty(UtilityFunctions.saveSlot)) // if testing / error
            {
                // get all save file
                string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"Characters\", "*.xml");
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
                            string save = UtilityFunctions.mainDirectory + @$"Characters\save{i + 1}.xml";
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
                    await Program.saveGameToAllStoragesAsync();
                    Environment.Exit(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.Black;
            //Console.WriteLine(UtilityFunctions.saveFile);
            //Console.WriteLine(output);


            // design xml file
            string preText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            output = await UtilityFunctions.cleanseXML(output);
            string finalXMLText = "";
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
            Player loadedPlayer;
            using (TextReader reader = new StringReader(finalXMLText))
            {
                loadedPlayer = (Player)serializer.Deserialize(reader);
            }


            // set player properties
            if (loadedPlayer == null)
                throw new ArgumentNullException("Null player");

            Type playerType = typeof(Player);
            PropertyInfo[] properties = playerType.GetProperties();


            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(loadedPlayer);
                    property.SetValue(player, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in GenerateMainXML {property.Name}: {ex.Message}");
                    // Handle or log the error as necessary
                }
            }

            return player;
        }

        public async Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat, EnemyFactory enemyFactory)
        {
            // function to generate a json file representing the enemies and initialise an enemyFactory
            // function to generate a json file representing the enemies and initialise an enemyFactory
            Console.WriteLine("Initialising Enemy Factory...");

            string output = "";
            try
            {
                string prompt4 = File.ReadAllText(UtilityFunctions.promptPath + "Prompt4.txt");
                chat.AppendUserInput(prompt4);
                output = await chat.GetResponseFromChatbotAsync();
            }
            catch (Exception e)
            {
                throw e;
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }

            try
            {
                UtilityFunctions.enemyTemplateSpecificDirectory =
                    UtilityFunctions.enemyTemplateDir + UtilityFunctions.saveName + ".json";
                if (File.Exists(UtilityFunctions.enemyTemplateSpecificDirectory))
                {
                    File.Delete(UtilityFunctions.enemyTemplateSpecificDirectory);
                    // cahnge to throw error
                    Console.WriteLine("Old save not deleted. Deleting old save then continuing");
                }

                // deserialise file into a new EnemyFactory
            }
            catch (Exception e)
            {
                throw e;
            }

            // testing
            Console.WriteLine(output);
            
            output = await UtilityFunctions.FixJson(output);
            
            Console.WriteLine(output);
            
            // create file to be written to
            File.Create(UtilityFunctions.enemyTemplateSpecificDirectory).Close();
            
            //File.WriteAllText(UtilityFunctions.enemyTemplateSpecificDirectory, output);
            using (StreamWriter  writer = new StreamWriter(UtilityFunctions.enemyTemplateSpecificDirectory))
            {
                await writer.WriteAsync(output);
            }

            EnemyFactory enemyFactoryToBeReturned;
            try
            {
                
                // deserialise file into a new EnemyFactory
                enemyFactoryToBeReturned = await UtilityFunctions.readFromJSONFile<EnemyFactory>(
                    UtilityFunctions.enemyTemplateSpecificDirectory);
            }
            catch (Exception e)
            {
                throw e;
            }

            if (enemyFactoryToBeReturned == null)
            {
                throw new Exception("Enemy factory is null");
            }

            return new EnemyFactory();
            }
    }
}