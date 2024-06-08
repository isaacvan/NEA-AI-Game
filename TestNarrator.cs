
using System.Net.Mime;
using System.Reflection;
using System.Xml.Serialization;
using Emgu.CV.Aruco;
using EnemyClassesNamespace;
using GPTControlNamespace;
using Newtonsoft.Json;
using NLog;
using OpenAI_API.Chat;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace TestNarratorNamespace
{
    public class TestNarrator
    {
        public class GameTest1 : GameSetup
        {
            private static Logger logger = LogManager.GetCurrentClassLogger();
            
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
            Console.WriteLine("Enemy Factory Initialised");
            return enemyFactoryToBeReturned;
            }

            public async Task<AttackBehaviourFactory> initialiseAttackBehaviourFactoryFromNarrator(Conversation chat)
            {
                UtilityFunctions.attackBehaviourTemplateSpecificDirectory = UtilityFunctions.attackBehaviourTemplateDir + UtilityFunctions.saveName + ".json";
                AttackBehaviourFactory attackBehaviourFactoryToBeReturned = new AttackBehaviourFactory();

                try
                {
                    // deserialise file into a new AttackBehaviourFactory
                    //attackBehaviourFactoryToBeReturned =
                    //    await UtilityFunctions.readFromJSONFile<AttackBehaviourFactory>(
                    //        UtilityFunctions.attackBehaviourTemplateSpecificDirectory);
                    string json = File.ReadAllText(UtilityFunctions.attackBehaviourTemplateSpecificDirectory);
                    var settings = new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> { new LambdaJsonConverter() }
                    };
                    Dictionary<string, AttackInfo> attackBehaviours = JsonConvert.DeserializeObject<Dictionary<string, AttackInfo>>(json, settings);
                    List<SerializableAttackBehaviour> items = new List<SerializableAttackBehaviour>();
                    foreach (KeyValuePair<string, AttackInfo> kvp in attackBehaviours)
                    {
                        items.Add(new SerializableAttackBehaviour(kvp.Key, kvp.Value));
                    }
                    attackBehaviourFactoryToBeReturned.InitializeFromSerializedBehaviors(items);
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (attackBehaviourFactoryToBeReturned == null)
                {
                    throw new Exception("Attack behaviour factory is null");
                }
                
                return attackBehaviourFactoryToBeReturned;
            }
        };
    }
}