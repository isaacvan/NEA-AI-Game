using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Xml.Serialization;
using CombatNamespace;
using ItemFunctionsNamespace;
using EnemyClassesNamespace;
using GPTControlNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using OpenAI_API;
using OpenAI_API.Chat;
using TestNarratorNamespace;
using UIGenerationNamespace;

namespace GameClassNamespace
{
    public class Game
    {
        public GameSetup gameSetup { get; set; }
        public Narrator narrator { get; set; }
        public Conversation chat { get; set; }
        public OpenAIAPI api { get; set; }
        public Player player { get; set; }
        public ItemFactory itemFactory { get; set; }
        public EnemyFactory enemyFactory { get; set; }
        public AttackBehaviourFactory attackBehaviourFactory { get; set; }
        public StatusFactory statusFactory { get; set; }
        public Combat currentCombat { get; set; }
        public UIConstructer uiConstructer { get; set; }
        public Map map { get; set; }
        public GameState gameState { get; set; }

        public async Task initialiseGame(GameSetup gameSetup, bool testing = false)
        {
            // if testing, treat as loaded
            if (testing) UtilityFunctions.loadedSave = true;

            // initialise api & chat
            api = Narrator.initialiseGPT();
            Conversation chat = Narrator.initialiseChat(api);
            GameSetup normalNarrator = new Narrator();
            this.gameSetup = gameSetup;
            this.narrator = (Narrator)normalNarrator;
            this.chat = chat;


            // initialise itemFactory & player from api. Gets UtilityFunctions.loadedSave.
            // Also initialises logging and logging directory
            itemFactory = new ItemFactory();
            player = await Program.initializeSaveAndPlayer(gameSetup, api, chat, testing);

            Console.ForegroundColor = ConsoleColor.White;

            if (!UtilityFunctions.loadedSave)
            {
                Console.WriteLine("Starting new game...");

                // initialise attack behaviours
                attackBehaviourFactory = await gameSetup.initialiseAttackBehaviourFactoryFromNarrator(chat);

                // initialise statuses and effects of the attack behaviours. Ensure everything is covered
                statusFactory = await gameSetup.initialiseStatusFactoryFromNarrator(chat);

                // fill itemFactory
                await itemFactory.initialiseItemFactoryFromNarrator(api, chat, testing);

                // initialise inventory & equipment to JSON
                await player.initialiseInventory();
                await player.initialiseEquipment();

                // rewrite player class to XML
                await player.updatePlayerStatsXML();

                // initialise & fill enemyFactory
                enemyFactory =
                    await gameSetup.initialiseEnemyFactoryFromNarrator(chat, enemyFactory, attackBehaviourFactory);

                // error-checking to ensure statuses are initialised
                await gameSetup.GenerateUninitialisedAttackBehaviours(chat);
                await gameSetup.GenerateUninitialisedStatuses(chat);

                // give player the basic attack PlayerBasicAttack_(attack)
                player.PlayerAttacks[AttackSlot.slot1] = attackBehaviourFactory.attackBehaviours["PlayerBasicAttack"];

                // write player attack slots to JSON file
                await player.writePlayerAttacksToJSON();

                // initialise first map
                // GridFunctions.GenerateMap(this, gameSetup, chat);
                map = await gameSetup.GenerateMapStructure(chat, this, gameSetup); // SWITCH

                // initialise HUD details
                GridFunctions.CurrentNodeId = 0;
                GridFunctions.CurrentNodeName = map.GetCurrentNode().NodePOI;

                // generate storyline summary for loaded games
                await gameSetup.GenerateStoryLineForFuture(chat, this);

                // save empty game state for future
                gameState = new GameState(SaveName: UtilityFunctions.saveName);
                await gameState.saveStateToFile();
                
                await UtilityFunctions.writeToXMLFile(UtilityFunctions.mainDirectory + $"BaseStats{Path.DirectorySeparatorChar}" + UtilityFunctions.saveName, player);

                Console.WriteLine("Started Game.");
            }
            else
            {
                Console.WriteLine("Loading save...");
                GameSetup loadGame = new TestNarrator.GameTest1();

                // load attack behaviours
                attackBehaviourFactory = await loadGame.initialiseAttackBehaviourFactoryFromNarrator(chat);

                // load statuses and effects of the attack behaviours.
                statusFactory = await loadGame.initialiseStatusFactoryFromNarrator(chat);

                // load itemFactory
                await itemFactory.initialiseItemFactoryFromFile();

                // load inventory & equipment
                await player.equipment.readEquipment();
                await player.inventory.readInventory();
                await player.InitialiseAttacks(this);

                // load enemyFactory
                enemyFactory =
                    await loadGame.initialiseEnemyFactoryFromNarrator(chat, enemyFactory, attackBehaviourFactory);
                
                // check for files unwritten
                // await checkAllFilesForMissing(gameSetup, chat, api, testing);

                // load map
                this.map = new Map();
                await narrator.LoadGraphStructure(this, gameSetup);

                // introduce the gpt to its role. This will read in any story line prewritten
                normalNarrator.IntroduceGPT(ref chat, this);

                // get game state
                gameState = new GameState(SaveName: UtilityFunctions.saveName);
                var holder = await gameState.unloadStateFromFile(player, map);
                player = holder.Item1;
                map = holder.Item2;
                
                // initialise HUD details
                GridFunctions.CurrentNodeId = map.GetCurrentNode().NodeID;
                GridFunctions.CurrentNodeName = map.GetCurrentNode().NodePOI;

                Console.WriteLine("Loaded save.");
            }
            
            // fix map structure (links between connected nodes)
            map.fixMapStructure();

            // save current map to save
            map.saveMapStructure();

            // initialise uiConstructor & narration
            uiConstructer = new UIConstructer(player);
            this.chat = uiConstructer.InitialiseNarration(this);

            // if starting a new game (or when requested), get the ai to outline the story to the reader.
            bool testingBoolForNarrator = false;
            if (testingBoolForNarrator || !UtilityFunctions.loadedSave)
                await uiConstructer.IntroduceStoryline(this);
        }

        public async Task CheckAllFilesForMissing(GameSetup gameSetup, Conversation chat, OpenAIAPI api,
            bool testing = false)
        {
            List<string> allowedDirNames = new List<string>()
            {
                "MapStructures", "StoryLines"
            };
            string[] directoriestemp = Directory.GetDirectories(UtilityFunctions.mainDirectory);

            foreach (string directory in allowedDirNames)
            {
                switch (directory)
                {
                    case "AttackBehaviours":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName + ".json"))
                            attackBehaviourFactory = await gameSetup.initialiseAttackBehaviourFactoryFromNarrator(chat);
                        break;
                    case "MapStructures":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName + ".json"))
                        {
                            map = await gameSetup.GenerateMapStructure(chat, this, gameSetup);
                        }
                        break;
                    case "Statuses":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName + ".json"))
                            statusFactory = await gameSetup.initialiseStatusFactoryFromNarrator(chat);
                        break;
                    case "EnemyTemplates":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName + ".json"))
                            enemyFactory =
                                await gameSetup.initialiseEnemyFactoryFromNarrator(chat, enemyFactory,
                                    attackBehaviourFactory);
                        break;
                    case "ItemTemplates":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName))
                            await itemFactory.initialiseItemFactoryFromNarrator(api, chat, testing);
                        break;
                    case "StoryLines":
                        if (!Path.Exists(UtilityFunctions.mainDirectory + directory + Path.DirectorySeparatorChar +
                                         UtilityFunctions.saveName + ".json"))
                            await gameSetup.GenerateStoryLineForFuture(chat, this);
                        break;
                }
            }
        }

        public bool startCombat(List<Enemy> enemies)
        {
            Dictionary<int, Enemy> dict = new Dictionary<int, Enemy>();
            foreach (Enemy enemy in enemies)
            {
                dict.Add(enemies.IndexOf(enemy), enemy);
            }

            currentCombat = new Combat(player, dict);
            return currentCombat.beginCombat();
        }

        public void loseGame()
        {
            Console.Clear();
            UtilityFunctions.TypeText(new TypeText(), "You lost the game!");
            UtilityFunctions.TypeText(new TypeText(), "Thanks for playing!");
            UtilityFunctions.TypeText(new TypeText(), "Your save is now empty! \n(I won't actually delete it yet)");
            Environment.Exit(0);
        }
    }
}