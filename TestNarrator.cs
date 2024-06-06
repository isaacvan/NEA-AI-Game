
using System.Reflection;
using System.Xml.Serialization;
using EnemyClassesNamespace;
using GPTControlNamespace;
using OpenAI_API.Chat;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace TestNarratorNamespace
{
    public class TestNarrator
    {
        public class GameTest1 : GameSetup
        {
            public void chooseSave()
            {
                UtilityFunctions.saveSlot = "saveExample.xml";
                UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"Characters\saveExample.xml";
                UtilityFunctions.saveName = "saveExample";
            }

            public async Task<Player> generateMainXml(Conversation chat, string prompt5, Player player)
            {
                string filePath = $@"{UtilityFunctions.mainDirectory}Characters\saveExample.xml";

                // Player player with attributes
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path must not be null or empty.");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The file '{filePath}' does not exist.");

                // deserialise xml
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                Player loadedPlayer;
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    loadedPlayer = (Player)serializer.Deserialize(fileStream);
                }

                //Console.WriteLine($"Loaded player charisma1: {loadedPlayer.Charisma}");

                // set player properties
                if (loadedPlayer == null)
                    throw new ArgumentNullException(nameof(loadedPlayer));

                Type playerType = typeof(Player);
                PropertyInfo[] properties = playerType.GetProperties();
                //Console.WriteLine($"NumOfProperties: {properties.Length}");

                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        object value = property.GetValue(loadedPlayer);
                        property.SetValue(player, value);
                        //Console.WriteLine($"Set property {property.Name} to {value}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to set property in GenerateMainXML {property.Name}: {ex.Message}");
                        // Handle or log the error as necessary
                    }
                }

                //Console.WriteLine($"Player charisma2: {player.Charisma}");

                return player;
            }

            public async Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat, EnemyFactory enemyFactory)
            {
                // test code here, once fully working will copy over to main narrator class
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
                    UtilityFunctions.enemyTemplateDir + UtilityFunctions.saveSlot;
                if (File.Exists(UtilityFunctions.enemyTemplateSpecificDirectory))
                {
                    File.Delete(UtilityFunctions.enemyTemplateSpecificDirectory);
                    Console.WriteLine("Old save not deleted. Deleting old save then continuing");
                }
                    File.Create(UtilityFunctions.enemyTemplateSpecificDirectory);
            }
            catch (Exception e)
            {
                throw e;
            }

            Console.WriteLine(output);
            
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

            return enemyFactoryToBeReturned;
            }
        };
    }
}