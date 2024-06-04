using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using ItemFunctionsNamespace;
using EnemyClassesNamespace;
using GPTControlNamespace;
using MainNamespace;
using OpenAI_API;
using OpenAI_API.Chat;

namespace GameClassNamespace
{
    public class Game
    {
        public Player player { get; set; }
        public ItemFactory itemFactory { get; set; }

        public async Task initialiseGame(GameSetup gameSetup, bool testing = false)
        {
            // initialise api & chat
            OpenAIAPI api = Narrator.initialiseGPT();
            Conversation chat = Narrator.initialiseChat(api);
            
            // initialise itemFactory & player from api
            itemFactory = new ItemFactory();
            player = await Program.initializeSaveAndPlayer(gameSetup, api, chat, testing);
            
            // fill itemFactory
            await itemFactory.initialiseItemFactoryFromNarrator(api, chat, testing);
            
            // initialise inventory & equipment to XML
            await player.initialiseInventory();
            await player.initialiseEquipment();
            
            // rewrite player class to XML
            await player.updatePlayerStatsXML();
            
            
            // initialise enemies
            // initialise map
        }

        public static void saveGame()
        {
            
        }

        public static void loadGame()
        {

        }

        public static void startGame()
        {

        }
    }
    
    class GameTest1 : GameSetup
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
        };

        class GameTest2 : GameTest1
        {
            public void initialisePlayer(Player player)
            {
                player.Class = "Wolfman";
            }
        };
}

