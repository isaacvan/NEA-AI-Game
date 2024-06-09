﻿using System.Net.Mime;
using System.Reflection;
using System.Xml.Serialization;
using CombatNamespace;
using Emgu.CV.Aruco;
using EnemyClassesNamespace;
using GPTControlNamespace;
using MainNamespace;
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

            public async Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat,
                EnemyFactory enemyFactory, AttackBehaviourFactory attackBehaviourFactory)
            {
                // test code here, once fully working will copy over to main narrator class
                // function to generate a json file representing the enemies and initialise an enemyFactory
                Program.logger.Info("Initialising Enemy Factory...");

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

                foreach (KeyValuePair<string, AttackInfo> attackBehaviour in attackBehaviourFactory.attackBehaviours)
                {
                    // load attack behaviours into enemy templates
                    foreach (EnemyTemplate enemyTemplate in enemyFactoryToBeReturned.enemyTemplates)
                    {
                        if (enemyTemplate.AttackBehaviourKeys
                            .Contains(attackBehaviour
                                .Key)) // if the attack labels attached to this template contain the given label for this attackbehaviour
                        {
                            // load in the attack behaviour
                            AttackSlot? attackSlotNullable = enemyTemplate.getNextAvailableAttackSlot();
                            if (attackSlotNullable == null)
                            {
                                throw new Exception("No available attack slots found for enemy template " +
                                                    enemyTemplate.Name);
                            }

                            AttackSlot attackSlot = (AttackSlot)attackSlotNullable; // ensure it isnt null

                            // add the attack behaviour to the enemy templat
                            enemyTemplate.AttackBehaviours[attackSlot] = attackBehaviour.Value;
                        }
                    }
                }

                Program.logger.Info("Enemy Factory Initialised");
                return enemyFactoryToBeReturned;
            }

            public async Task<AttackBehaviourFactory> initialiseAttackBehaviourFactoryFromNarrator(Conversation chat)
            {
                UtilityFunctions.attackBehaviourTemplateSpecificDirectory =
                    UtilityFunctions.attackBehaviourTemplateDir + UtilityFunctions.saveName + ".json";
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
                    Dictionary<string, AttackInfo> attackBehaviours =
                        JsonConvert.DeserializeObject<Dictionary<string, AttackInfo>>(json, settings);
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

            public async Task<StatusFactory> initialiseStatusFactoryFromNarrator(Conversation chat)
            {
                // status factory logic, use game setup for diverting to using api key
                StatusFactory tempStatusFactory = new StatusFactory();

                UtilityFunctions.statusesSpecificDirectory =
                    UtilityFunctions.statusesDir + UtilityFunctions.saveName + ".json";

                try
                {
                    tempStatusFactory = await UtilityFunctions.readFromJSONFile<StatusFactory>(
                        UtilityFunctions.statusesSpecificDirectory);
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (tempStatusFactory == null)
                {
                    throw new Exception("Status factory is null");
                }

                return tempStatusFactory;
            }

            public async Task GenerateUninitialisedStatuses(Conversation chat)
            {
                Program.logger.Info("Example file has uninitialsed statuses. Leaving uninitialised statuses as null. Prone for error.");
            }
        }
    }
}