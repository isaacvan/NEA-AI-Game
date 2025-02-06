using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using GridConfigurationNamespace;
using GPTControlNamespace;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using CombatNamespace;
using Emgu.CV.Aruco;
using Emgu.CV.Structure;
using ItemFunctionsNamespace;
using OpenAI_API;
using OpenAI_API.Chat;
using GameClassNamespace;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Targets;
using TestNarratorNamespace;
using EnemyTemplate = EnemyClassesNamespace.EnemyTemplate;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MainNamespace
{
    class Program
    {
        public static Logger logger = LogManager.GetCurrentClassLogger();
        public static bool testing = false;
        public static Game game;
        public static volatile bool gameStarted = false;

        // one game costs £0.30 currently with GPT-4o. 3 minutes to generate.

        /* ---------------------------------------------------------------------------------------------------------
        // NEXT STEPS
        //
        //
        //
        // ITEMS
        // - make items actually change player stats
        // OVERWRITE ITEMS THAT ARE NULL IN BEHAVIOURS
        //
        // DUNGEON MASTER ADDITIONS
        // - get narrator to start affecting variables like enemy levels, sight range, your sight range etc etc depending on map
        // - Make way for player to get more attacks
        //
        //
        // - if narrative lines empty, give it desc
        //
        // NEXT - ENEMY COMBAT AI
        // implement the natures for each type of enemy
        // design each ai system in combat
        // basic enemy attack back
        //
        //
        // FINAL TWEAKS
        // make game fully playable so that they can complete 1 "storyline"
        // QOL - menu in game, rgb personalisation etc
        // ramp up difficulty to make game actually challenging
        //----------------------------------------------------------------------------------------------------------
        */


        // ------------------------------------------------------------------------------------------------------------
        // CURRENT STATE
        // TESTING - works.
        // GAME - loadGame works.
        // GAME - game works, with very occasional api errors in generation.
        // ----------------------------------------------------------------------------------------------------------

        static async Task Main(string[] args)
        {
            //UtilityFunctions.mainDirectory = UtilityFunctions.getBaseDir();
            UtilityFunctions.mainDirectory = @"/Users/18vanenckevorti/RiderProjects/NEA-AI-Game/";


            // testing NLog setup:
            // NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Debug;
            // NLog.Common.InternalLogger.LogToConsole = true;
            // NLog.Common.InternalLogger.LogFile = @"c:\temp\nlog-internal.txt"; // On Linux one can use "/home/nlog-internal.txt"
            // TODO: apparently if we get NLog.config in the right place it should be found automatically, but for now, this is a workaround:
            NLog.LogManager.Configuration =
                new NLog.Config.XmlLoggingConfiguration(UtilityFunctionsNamespace.UtilityFunctions.mainDirectory +
                                                        @$"{Path.DirectorySeparatorChar}NLog.config");
            // NLog.LogManager.Configuration.AddTarget(new FileTarget(UtilityFunctionsNamespace.UtilityFunctions.mainDirectory + "\\output.log"));

            logger.Info("Program started");

            EnableColors();
            game = new Game();
            Console.CancelKeyPress += MyHandler; // triggers save on forced exit
            string mode = args.Length > 0 ? args[0] : "game";
            logger.Info($"Running mode={mode}");
            switch (mode)
            {
                case "testing":
                    testing = true;
                    await game.initialiseGame(new TestNarrator.GameTest1(), true);
                    // new Narrator().GenerateGraphStructure(Narrator.initialiseChat(Narrator.initialiseGPT()), new Game(), new TestNarrator.GameTest1(), 0);
                    Console.Clear();
                    logger.Info("Test Mode");

                    // game.map.Graphs[0].Nodes[0] = GridFunctions.FillNode(game.map.Graphs[0].Nodes[0]);

                    game.player.PlayerAttacks[AttackSlot.slot1] =
                        game.attackBehaviourFactory.attackBehaviours["PlayerBasicAttack"];

                    gameStarted = true;
                    GamePlayLoop(ref game);


                    Console.ReadLine();
                    break;
                case "game":
                    await game.initialiseGame(new Narrator());
                    Console.Clear();
                    logger.Info("Game mode");


                    //GridFunctions.GenerateMap(game.map);

                    // start game
                    // UtilityFunctions.DisplayAllEnemyTemplatesWithDetails();

                    gameStarted = true;
                    GamePlayLoop(ref game);

                    Console.ReadLine();
                    break;
                default:
                    Environment.FailFast($"debuggingPointEntry Invalid");
                    break;
            }
        }


        // [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.SByte[]; size: 598MB")]
        public static void GamePlayLoop(ref Game game)
        {
            UtilityFunctions.UpdateVars(ref game);
            if (UtilityFunctions.loadedSave)
            {
                game.map.SetCurrentNodeTilesContents(GridFunctions.PlacePlayer(
                    game.player.playerPos,
                    game.map.GetCurrentNode().tiles, ref game));
            }
            else
            {
                game.map.SetCurrentNodeTilesContents(GridFunctions.PlacePlayer(
                    GridFunctions.GetPlayerStartPos(ref game),
                    game.map.GetCurrentNode().tiles, ref game));
            }

            GridFunctions.DrawWholeNode(game);
            int IdOfNextNode = -1;
            string input = Console.ReadKey().Key.ToString();
            Tile oldTile = null;

            // start narrator
            NarrationTypeWriter.Start();

            bool GameRunning = true;
            while (GameRunning) // while overall game running
            {
                //UtilityFunctions.clearScreen(game.player);
                UtilityFunctions.UpdateVars(ref game); // updates player rgb vars

                while (!GetAllowedInputs("MoveCharacterMenuMapOptions").Contains(input[0].ToString()) ||
                       !GridFunctions.CheckIfOutOfBounds(
                           game.map.Graphs[game.map.Graphs.Count - 1]
                               .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles,
                           game.player.playerPos, input[0].ToString()))
                {
                    UtilityFunctions.clearScreen(game.player);
                    GridFunctions.DrawWholeNode(game);
                    // Console.WriteLine("Please enter a valid input");
                    input = Console.ReadKey(true).KeyChar.ToString();
                }

                // move player, if it isnt a movement then do something else with the input
                if (!GridFunctions.MovePlayer(input, ref game.player.playerPos, ref game, ref oldTile))
                    AssessOtherInputs(input, ref game);

                // register player movement
                UpdateMoveCounter(ref game);

                // then move enemies
                MoveEnemies(ref game);

                // CHECK FOR EVENTS
                int oldId = game.map.GetCurrentNode().NodeID;
                CheckForEventsTriggered(ref game, ref IdOfNextNode, ref oldTile);

                // check for a new node
                if (GridFunctions.CheckIfNewNode(game.map.GetCurrentNode().tiles, game.player.playerPos))
                    GridFunctions.UpdateToNewNode(ref game, IdOfNextNode, ref oldTile, oldId);

                // draw the updated grid
                GridFunctions.DrawWholeNode(game);

                // save final gameState
                game.gameState.saveStateToFile(game.map);
                saveGameToAllStoragesAsync();

                // get next input
                input = Console.ReadKey(true).KeyChar.ToString();
            }
        }

        public static void UpdateMoveCounter(ref Game game)
        {
            NarrationTypeWriter.IncrementMovesSinceLastNarration();
            game.gameState.location = game.player.playerPos;
        }


        public static void MoveEnemies(ref Game game)
        {
            var enemies = game.map.GetCurrentNode().enemies;
            foreach (EnemySpawn enemy in enemies)
            {
                Point oldPoint = new Point();
                Point newPoint = new Point();
                if (enemy.spawnPoint != Point.Empty && enemy.currentLocation == Point.Empty)
                {
                    oldPoint = (Point)enemy.spawnPoint;
                }
                else if (enemy.currentLocation != Point.Empty && enemy.spawnPoint == Point.Empty)
                {
                    oldPoint = (Point)enemy.currentLocation;
                }
                else
                {
                    throw new Exception("SpawnPoint and currentLocation both shouldnt be empty");
                }

                EnemyConfig container = null;
                if (enemy.nature == Nature.timid)
                {
                    container = new TimidContainer();
                }
                else if (enemy.nature == Nature.neutral)
                {
                    container = new NeutralContainer();
                }
                else if (enemy.nature == Nature.aggressive)
                {
                    container = new AggressiveContainer();
                }

                if (container != null)
                {
                    newPoint = container.GetEnemyMovement(oldPoint, ref game); // SETS NEW POINT
                    if (newPoint == Point.Empty)
                    {
                        newPoint = oldPoint;
                    }
                    else if (newPoint == oldPoint)
                    {
                        newPoint = oldPoint;
                    }
                }
                else
                {
                    throw new Exception("container shouldnt be null. no nature found?");
                }

                enemy.currentLocation = newPoint;
                enemy.spawnPoint = Point.Empty;
                try
                {
                    GridFunctions.MoveEnemy(oldPoint, newPoint, ref game);
                }
                catch (IndexOutOfRangeException)
                {
                    // dont move that sht
                }
                
            }
        }

        public static void AssessOtherInputs(string input, ref Game game)
        {
            if (GetAllowedInputs("CharacterMenu").Contains(input))
            {
                game.uiConstructer.drawCharacterMenu(game);
                UtilityFunctions.TypeText(new TypeText(), "\nPress any key to continue...");
                Console.ReadKey(true);
            }

            if (GetAllowedInputs("Map").Contains(input))
            {
                game.uiConstructer.DrawMap(game.map.Graphs[game.map.CurrentGraphPointer]);
            }

            if (GetAllowedInputs("Options").Contains(input))
            {
                (bool, Game?) result = options(true, true, game);
                game = result.Item2;
                if (!result.Item1)
                {
                    Environment.Exit(0);
                }
            }
        }

        public static void CheckForEventsTriggered(ref Game game, ref int IdOfNextNode, ref Tile oldTile)
        {
            Point pos = game.player.playerPos;
            Tile tile = game.map.GetCurrentNode().tiles[pos.X][pos.Y];
            if (tile.tileDesc == "NodeExit") // CHECK FOR IF ON A NODE BOUNDARY
            {
                if (tile.exitNodePointerId != null && tile.entryNodePointerId == null)
                {
                    IdOfNextNode = (int)tile.exitNodePointerId;
                }
                else if (tile.exitNodePointerId == null && tile.entryNodePointerId != null)
                {
                    IdOfNextNode = (int)tile.entryNodePointerId;
                }
                else
                {
                    throw new Exception("ExitNode was not found, no pointer id was found");
                }
            }
            else
            {
                IdOfNextNode = -1;
            }

            if (tile.enemyOnTile != null)
            {
                // START COMBAT
                UtilityFunctions.clearScreen(game.player);
                bool outcome = game.startCombat(new List<Enemy>() { tile.enemyOnTile });
                if (outcome)
                {
                    tile.tileChar = GridFunctions.CharsToMeanings["Player"][0];
                    oldTile.tileChar = GridFunctions.CharsToMeanings["Empty"][0];
                    oldTile.rgb = new Rgb(255, 255, 255);

                    game.player.currentExp += (tile.enemyOnTile.Level + 1) * 10 *
                                              (game.map.Graphs[game.map.CurrentGraphPointer].GraphDepth + 1);
                    game.player.checkForLevelUp();

                    int EnemyId = tile.enemyOnTile.Id;
                    for (var i = 0; i < game.map.GetCurrentNode().enemies.Count; i++)
                    {
                        if (game.map.GetCurrentNode().enemies[i].id == EnemyId)
                        {
                            game.map.GetCurrentNode().enemies.ElementAt(i).alive = false;
                        }
                    }

                    // game.map.GetCurrentNode().enemies.Find(e => e.id == tile.enemyOnTile.Id).alive = false;

                    tile.enemyOnTile = null;
                    oldTile.enemyOnTile = null;
                }
                else
                {
                    // END GAME
                    game.loseGame();
                }
            }

            if (tile.objective is { IsCompleted: false })
            {
                tile.objective.BeginObjective(ref game);
            }

            // IF NEW GRAPH
            // GridFunctions.LastestGraphDepth++;
            // game.map.CurrentGraphPointer++;
        }

        public static string GetAllowedInputs(string condition)
        {
            string toReturn = "";

            if (condition.Contains("Move"))
            {
                toReturn += "WASDwasd";
            }

            if (condition.Contains("Attack"))
            {
                toReturn += "1234"; // etc etc
            }

            if (condition.Contains("CharacterMenu"))
            {
                toReturn += "Cc";
            }

            if (condition.Contains("Test"))
            {
                toReturn += "Pp";
            }

            if (condition.Contains("Map"))
            {
                toReturn += "Mm";
            }

            if (condition.Contains("Options"))
            {
                toReturn += "Oo";
            }

            return toReturn;
        }


        protected static void MyHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.Clear();
            Console.WriteLine("Exiting system due to external process kill or shutdown");

            // prevent application from terminating immediately
            args.Cancel = true;

            if (game.player != null)
            {
                // saveGameToAllStoragesSync();
            }

            // Thread.Sleep(1000);

            // exit smoothly
            Environment.Exit(0);
        }

        public static async Task saveGameToAllStoragesAsync()
        {
            try
            {
                if (game.gameState != null)
                {
                    await game.gameState.saveStateToFile();
                }
                // checks before saving

                if (game.player != null)
                {
                    if (game.player.inventory != null)
                    {
                        await game.player.inventory.updateInventoryJSON();
                        // Console.WriteLine("Inventory saved successfully.");
                    }

                    if (game.player.equipment != null)
                    {
                        await game.player.equipment.updateEquipmentJSON();
                        //Console.WriteLine("Equipment saved successfully.");
                    }

                    await game.player.updatePlayerStatsXML();
                    //Console.WriteLine("Player stats saved successfully.");
                }

                //Console.WriteLine("All game data saved successfully.");

                if (game.map != null)
                {
                    game.map.saveMapStructure();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game data: {ex.Message}");
                throw; // Rethrow if you want to handle this exception at a higher level or log it
            }

            // every aspect that needs to be saved
            // map state, game state, story progress etc needs to be saved
        }

        public static void saveGameToAllStoragesSync()
        {
            Console.WriteLine("Saving game data...");
            try
            {
                if (game.player != null)
                {
                    if (game.player.inventory != null)
                    {
                        game.player.inventory.updateInventoryJSONSync();
                        Console.WriteLine("Inventory saved successfully.");
                    }

                    if (game.player.equipment != null)
                    {
                        game.player.equipment.updateEquipmentJSONSync();
                        Console.WriteLine("Equipment saved successfully.");
                    }

                    game.player.updatePlayerStatsXMLSync();
                    Console.WriteLine("Player stats saved successfully.");
                }

                Console.WriteLine("All game data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game data: {ex}");
                throw; // Rethrow if you want to handle this exception at a higher level or log it
            }

            // every aspect that needs to be saved
            // map state, game state, story progress etc needs to be saved
        }

        public static bool menu(bool gameStarted, bool saveChosen, bool testing = false)
        {
            if (saveChosen == false && gameStarted == false)
            {
                UtilityFunctions.clearScreen(null);
                Console.SetWindowSize(80, 15); // Adjust the window size to fit your preference
                Console.Title = "Dungeon Crawler Menu";
                int speed = 0;
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}╔═══════════════════════════════════════════════════════════════════════╗");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                               {UtilityFunctions.colourScheme.menuAccentCode}Torment{UtilityFunctions.colourScheme.menuMainCode}                                 ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                 [1] Start Game          [2] Load Save                 ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                 [3] Options             [4] Quit                      ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.menuMainCode}╚═══════════════════════════════════════════════════════════════════════╝{UtilityFunctions.colourScheme.generalTextCode}");
                Console.WriteLine();
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, typingSpeed: speed),
                    $"{UtilityFunctions.colourScheme.generalTextCode}Choose an option: ");
                int choice;
                if (int.TryParse(Convert.ToString(Console.ReadKey(true).KeyChar), out choice))
                {
                    switch (choice)
                    {
                        case 1:
                            UtilityFunctions.clearScreen(null);
                            List<string> saves = Directory
                                .GetFiles(UtilityFunctions.mainDirectory + $@"Characters{Path.DirectorySeparatorChar}",
                                    "*.xml")
                                .ToList();
                            saves.Remove(
                                $@"{UtilityFunctions.mainDirectory}Characters{Path.DirectorySeparatorChar}saveExample.xml");
                            bool started = false;
                            for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                            {
                                if (saves.Count == i)
                                {
                                    UtilityFunctions.TypeText(
                                        new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                                        $"Save Slot save{i + 1}.xml is empty. Do you want to begin a new game? y/n");
                                    string load = Console.ReadLine();
                                    if (load == "y")
                                    {
                                        string save = UtilityFunctions.mainDirectory +
                                                      @$"Characters{Path.DirectorySeparatorChar}save{i + 1}.xml";
                                        UtilityFunctions.saveSlot = Path.GetFileName(save);
                                        UtilityFunctions.saveFile = save;
                                        UtilityFunctions.saveName = $"save{i + 1}";
                                        saveChosen = true;
                                        started = true;
                                        break;
                                    }
                                }
                            }

                            if (!started)
                            {
                                UtilityFunctions.TypeText(
                                    new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                                    "No empty save slots. Please choose a different option.");
                                Thread.Sleep(1000);
                            }

                            // Start the game
                            break;
                        case 2:
                            getLoadedSaveName(gameStarted, saveChosen);
                            saveChosen = true;
                            UtilityFunctions.loadedSave = true;
                            // Load a saved game
                            break;
                        case 3:
                            bool outcome = options(gameStarted, saveChosen).Item1;
                            if (outcome)
                            {
                                saveChosen = true;
                            }

                            break;
                        // Open options menu
                        case 4:
                            Console.Clear();
                            saveGameToAllStoragesAsync().GetAwaiter().GetResult();
                            Environment.Exit(0);
                            // Exit the game
                            break;
                        default:
                            UtilityFunctions.TypeText(
                                new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                                "Invalid choice. Please select a valid option.");
                            Thread.Sleep(1000);
                            break;
                    }
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                        "Invalid input. Please enter a valid number.");
                }
            }

            return saveChosen;
        }

        public static async Task<Player> initializeSaveAndPlayer(GameSetup gameSetup, OpenAIAPI api, Conversation chat,
            bool testing = false)
        {
            // Task.Run(async () =>
            // {
            //     await YourAsyncMethod();
            // }).Wait();
            Console.CursorVisible = false;
            gameSetup.chooseSave();

            UtilityFunctions.logsSpecificDirectory = @$"{UtilityFunctions.logsDir}{UtilityFunctions.saveName}";
            UtilityFunctions.enemyTemplateSpecificDirectory =
                UtilityFunctions.enemyTemplateDir + UtilityFunctions.saveName + ".json";

            // initialise GPT logging
            UtilityFunctions.initialiseGPTLogging();

            Player player;

            if (UtilityFunctions.loadedSave) // IF LOADED
            {
                // put player where they were back into the game. If it's a new save, ignore.
                //string chosenClass = UtilityFunctions.chooseClass();
                //player = UtilityFunctions.CreatePlayerInstance(chosenClass);
                // deserialize the utilityfucntions.saveFile and load it into player


                // will create a function called getLoadedSaveName()
                player = await UtilityFunctions.readFromXMLFile<Player>(UtilityFunctions.saveFile, new Player());
            }
            else // if not loaded
            {
                player = UtilityFunctions.CreatePlayerInstance(); // returns new empty player
                // GridFunctions.CreateGrid(@"D:\isaac\Documents\Code Projects\GridSaves");
                await player.initialisePlayerFromNarrator(gameSetup, api, chat, testing);
            }

            Console.Clear();

            // call api and initialise narrator, getting charactcer details
            // MAIN INITIALISE FUNCTION FOR CHARACTER

            UtilityFunctions.playerAttacksSpecificDirectory =
                UtilityFunctions.playerAttacksDir + UtilityFunctions.saveName + ".json";

            return player;
        }


        public static void loadGame()
        {
            // load all game aspects
        }


        static void getLoadedSaveName(bool gameStarted, bool saveChosen)
        {
            bool startedGame = false;
            while (!startedGame)
            {
                while (UtilityFunctions.saveSlot == "")
                {
                    bool valid = false;
                    string saveSlot = "";
                    while (!valid)
                    {
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "Enter a save slot. These are your options:");

                        List<string> saveNameList =
                            Directory.GetFiles(UtilityFunctions.mainDirectory + "Characters", "*.xml").ToList();

                        if (!UtilityFunctions.showExampleInSaves)
                        {
                            saveNameList.Remove(
                                $@"{UtilityFunctions.mainDirectory}Characters{Path.DirectorySeparatorChar}saveExample.xml");
                        }

                        List<string> saveNameListWithoutExt = new List<string>();
                        Console.WriteLine("-------------------------");

                        foreach (string save in saveNameList)
                        {
                            saveNameListWithoutExt.Add(Path.GetFileNameWithoutExtension(save));
                            Console.WriteLine($"{Path.GetFileNameWithoutExtension(save)}");
                        }

                        Console.WriteLine("-------------------------");
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "Enter a save slot: ");
                        string saveNameUnchecked = Console.ReadLine();

                        if (saveNameUnchecked.Contains("save") && saveNameListWithoutExt.Contains(saveNameUnchecked))
                        {
                            saveSlot = saveNameUnchecked;
                            valid = true;
                        }
                    }

                    UtilityFunctions.saveSlot = saveSlot + ".xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory +
                                                $@"Characters{Path.DirectorySeparatorChar}" + (saveSlot) + ".xml";
                    UtilityFunctions.saveName = saveSlot;
                }

                startedGame = true;
                UtilityFunctions.clearScreen(null);
            }
        }

        // doing wipe all Characters / options then start doesnt work because options menu sends you back to menu, and you end up having to go back to the end of menu
        // where it automatically returns false. either get load to return a value or get rid of all menu rerouts inside options so that it sends you back to the menu 
        // already in, instead of generating a new one.

        // I think I'm doing it wrong, but I'm not sure how to fix it.

        public static (bool, Game?) options(bool gameStarted, bool saveChosen, Game game = null)
        {
            UtilityFunctions.clearScreen(null);
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Options menu.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[1] Back to game.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[2] Change type of music.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[3] Change difficulty.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[4] Change type of sound effects.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[5] Exit game.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[6] Clear all saves.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "[7] Set example save.\n");
            if (gameStarted)
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                    "[8] Reset Save to unused state.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Choose an option: ");
            int choice;
            if (int.TryParse(Console.ReadLine(), out choice))
            {
                switch (choice)
                {
                    case 1:
                        bool outcome = menu(gameStarted, saveChosen);
                        return (outcome, game);
                    // Back to main menu
                    case 2:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "No Music yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of music
                    case 3:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "No Difficulty yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change difficulty
                    case 4:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "No Sound Effects yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of sound effects
                    case 6:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "Are you sure you want to clear all Characters? y/n\n");
                        string clearSaves = Console.ReadLine();
                        if (clearSaves == "y")
                        {
                            DeleteAllSaves();

                            Thread.Sleep(1000);
                            // return options(gameStarted, saveChosen);
                            bool outcome1 = menu(gameStarted, saveChosen);
                            return (outcome1, game);
                        }
                        else
                        {
                            UtilityFunctions.clearScreen(null);
                            UtilityFunctions.TypeText(
                                new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                                "Saves not cleared.\n");
                            Thread.Sleep(1000);
                            return options(gameStarted, saveChosen);
                        }

                        return options(gameStarted, saveChosen);
                    case 5:
                        Console.Clear();
                        saveGameToAllStoragesAsync().GetAwaiter().GetResult();
                        Environment.Exit(0);
                        return (false, game);
                    case 7:
                        SetExampleSaves();
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    case 8:
                        if (gameStarted)
                        {
                            game = ResetGameState(game).GetAwaiter().GetResult();
                            Thread.Sleep(1000);
                            return options(false, saveChosen, game);
                        }

                        return options(gameStarted, saveChosen);
                    default:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                            "Invalid option.\n");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                }
            }
            else
            {
                UtilityFunctions.clearScreen(null);
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                    "Invalid option.\n");
                Thread.Sleep(1000);
                return options(gameStarted, saveChosen);
            }
        }

        public static async Task<Game> ResetGameState(Game game)
        {
            // reset current save objectgives, enemies, etc. player inv, equipment, everything except the assets
            
            NarrationTypeWriter.Stop();

            bool obtainedSaveName = false;
            string finalPathName = "";
            while (!obtainedSaveName)
            {
                Console.Clear();
                Console.WriteLine("Which save would you like to reset?");
                int index = 1;
                List<string> pathNames = Directory.GetFiles(UtilityFunctions.mainDirectory + "GameStates", "*.json")
                    .Select(pathName => Path.GetFileNameWithoutExtension(pathName)).ToList();
                foreach (string pathName in pathNames)
                {
                    Console.WriteLine($"{index}: {pathName}");
                    index++;
                }

                string inp = Console.ReadLine();
                if (int.TryParse(inp, out int gameIndex))
                {
                    try
                    {
                        finalPathName = pathNames[gameIndex - 1];
                        obtainedSaveName = true;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Console.Clear();
                        Console.WriteLine("Invalid game index.\n");
                    }
                }
                else
                {
                    if (pathNames.Contains(inp))
                    {
                        finalPathName = inp;
                        obtainedSaveName = true;
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine("Invalid game index.\n");
                    }
                }
            }
            
            // reset gamState
            

            // reset map
            game.gameState.currentGraphId = 0;
            game.map.CurrentGraphPointer = 0;
            game.gameState.currentNodeId = 0;
            game.map.Graphs[game.map.CurrentGraphPointer].CurrentNodePointer = 0;
            foreach (Node n in game.map.Graphs[game.map.CurrentGraphPointer].Nodes)
            {
                n.enemies = null;
                n.InitialiseEnemies(game);
                if (n.Obj != null)
                {
                    n.Obj.IsCompleted = false;
                    string temp = n.Obj.NarrativePrompts[0];
                    n.Obj.NarrativePrompts.Clear();
                    n.Obj.NarrativePrompts.Add(temp);
                }
            }
            
            

            // gotten save to reset, now start resetting.
            game.player = await UtilityFunctions.readFromXMLFile<Player>(UtilityFunctions.mainDirectory + $"BaseStats{Path.DirectorySeparatorChar}" + finalPathName + ".xml", new Player());
            game.player.Health = 100;
            game.player.currentHealth = 100;
            game.player.PlayerAttacks[AttackSlot.slot1] =
                game.attackBehaviourFactory.attackBehaviours["PlayerBasicAttack"];

            game.player.playerPos = GridFunctions.GetPlayerStartPos(ref game);
            game.gameState.location = game.player.playerPos;

            game.player.equipment = new Equipment();
            game.player.inventory = new Inventory();
            await game.player.initialiseEquipment();
            await game.player.initialiseInventory();
            
            // await game.gameState.saveStateToFile();
            await saveGameToAllStoragesAsync();

            return game;
        }

        public static void SetExampleSaves()
        {
            // this function will overwrite every saveExample.(ext) to a save(int), as inputted.
            List<string> directories = new List<string>();
            string main = UtilityFunctions.mainDirectory;
            directories.Add(@$"{main}AttackBehaviours{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}Characters{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}CharacterAttacks{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}EnemyTemplates{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}Equipments{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}Inventories{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}Statuses{Path.DirectorySeparatorChar}");
            directories.Add(@$"{main}MapStructures{Path.DirectorySeparatorChar}");

            Console.Clear();
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Enter a save (e.g: save1)\n");
            string saveName = Console.ReadLine();

            try
            {
                foreach (string dir in directories)
                {
                    string examplePathNoExt = dir + "saveExample";
                    if (!File.Exists(examplePathNoExt + ".xml") && !File.Exists(examplePathNoExt + ".json"))
                    {
                        File.Create(examplePathNoExt).Close();
                        Program.logger.Info($"No example file. Created a blank.");
                    }

                    string pathToReadNoExt = dir + saveName;

                    bool json = true;

                    try
                    {
                        // if no error, file is json
                        JsonConvert.DeserializeObject(File.ReadAllText(pathToReadNoExt + ".json"));
                    }
                    catch
                    {
                        // if error, file is xml
                        json = false;
                    }

                    // copy the example file to the new save file
                    if (json)
                    {
                        File.Copy(pathToReadNoExt + ".json", examplePathNoExt + ".json", true);
                    }
                    else
                    {
                        File.Copy(pathToReadNoExt + ".xml", examplePathNoExt + ".xml", true);
                    }

                    Program.logger.Info($"Copied {pathToReadNoExt} to {examplePathNoExt}");
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            try
            {
                List<string> itemTemplatePaths =
                    Directory.GetFiles(UtilityFunctions.mainDirectory + $@"ItemTemplates{Path.DirectorySeparatorChar}" +
                                       saveName).ToList();
                foreach (string path in itemTemplatePaths)
                {
                    List<string> newPaths = Directory
                        .GetFiles(UtilityFunctions.mainDirectory +
                                  $@"ItemTemplates{Path.DirectorySeparatorChar}saveExamples").ToList();
                    foreach (string newPath in newPaths)
                    {
                        if (Path.GetFileNameWithoutExtension(path) == Path.GetFileNameWithoutExtension(newPath))
                        {
                            File.Copy(path, newPath, true);
                            Program.logger.Info($"Copied {path} to {newPath}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Done.\n");
            Thread.Sleep(1000);
        }

        public static void DeleteAllSaves()
        {
            // delete Characters
            UtilityFunctions.clearScreen(null);
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Clearing all Characters...\n");
            string[] saves =
                Directory.GetFiles(UtilityFunctions.mainDirectory + $@"Characters{Path.DirectorySeparatorChar}",
                    "*.xml");
            foreach (string save in saves)
            {
                if (Path.GetFileNameWithoutExtension(save) != "saveExample")
                {
                    File.Delete(save);
                }
            }

            // delete itemTemplates
            string[] templates =
                Directory.GetDirectories($@"{UtilityFunctions.mainDirectory}ItemTemplates");
            foreach (string template in templates)
            {
                DirectoryInfo info = new DirectoryInfo(template);
                if (info.Name != "saveExamples")
                {
                    try
                    {
                        Directory.Delete(template, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Couldn't delete file: {e}");
                        Console.ReadLine();
                    }
                }
            }

            // delete inventories
            string[] inventories =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}Inventories", searchPattern: "*.json");
            foreach (string inventory in inventories)
            {
                if (Path.GetFileNameWithoutExtension(inventory) != "saveExample")
                {
                    File.Delete(inventory);
                }
            }

            // delete equipments
            string[] equipments =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}Equipments", searchPattern: "*.json");
            foreach (string equipment in equipments)
            {
                if (Path.GetFileNameWithoutExtension(equipment) != "saveExample")
                {
                    File.Delete(equipment);
                }
            }

            // delete enemy templates
            List<string> enemyTemplates =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}EnemyTemplates", searchPattern: "*.json")
                    .ToList();
            foreach (string xmlfile in Directory.GetFiles($@"{UtilityFunctions.mainDirectory}EnemyTemplates",
                         searchPattern: "*.xml"))
            {
                enemyTemplates.Add(xmlfile);
            }

            foreach (string enemyTemplate in enemyTemplates)
            {
                if (Path.GetFileNameWithoutExtension(enemyTemplate) != "saveExample")
                {
                    File.Delete(enemyTemplate);
                }
            }

            // delete statuses
            string[] statuses =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}Statuses", searchPattern: "*.json");
            foreach (string status in statuses)
            {
                if (Path.GetFileNameWithoutExtension(status) != "saveExample")
                {
                    File.Delete(status);
                }
            }

            // delete attackbehaviours
            string[] attackBehaviours =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}AttackBehaviours", searchPattern: "*.json");
            foreach (string attackBehaviour in attackBehaviours)
            {
                if (Path.GetFileNameWithoutExtension(attackBehaviour) != "saveExample")
                {
                    File.Delete(attackBehaviour);
                }
            }

            // delete characterattacks
            string[] characterAttacks =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}CharacterAttacks", searchPattern: "*.json");
            foreach (string characterAttack in characterAttacks)
            {
                if (Path.GetFileNameWithoutExtension(characterAttack) != "saveExample")
                {
                    File.Delete(characterAttack);
                }
            }

            // delete logs
            string[] logDirs = Directory.GetDirectories($@"{UtilityFunctions.mainDirectory}Logs");
            foreach (string logDir in logDirs)
            {
                if (Path.GetFileNameWithoutExtension(logDir) != "saveExample")
                {
                    Directory.Delete(logDir, true);
                }
            }

            // delete map structures
            string[] maps = Directory.GetFiles($@"{UtilityFunctions.mainDirectory}MapStructures",
                searchPattern: "*.json");
            foreach (string map in maps)
            {
                if (Path.GetFileNameWithoutExtension(map) != "saveExample")
                {
                    File.Delete(map);
                }
            }

            // delete storyLines
            string[] storylines =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}Storylines", searchPattern: "*.txt");
            foreach (string storyline in storylines)
            {
                if (Path.GetFileNameWithoutExtension(storyline) != "saveExample")
                {
                    File.Delete(storyline);
                }
            }

            // delete gameStates
            string[] states =
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}GameStates", searchPattern: "*.json");
            foreach (var state in states)
            {
                if (Path.GetFileNameWithoutExtension(state) != "saveExample")
                {
                    File.Delete(state);
                }
            }
        }

        // Constants for standard output handle and enabling virtual terminal processing
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        // Importing necessary Windows API functions
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        static void EnableColors()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);

                if (GetConsoleMode(handle, out uint mode))
                {
                    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(handle, mode);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
            }
        } //\x1b[38;2;r;g;bm
    }
}