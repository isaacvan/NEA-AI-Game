using System.ComponentModel;
using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using GridConfigurationNamespace;
using GPTControlNamespace;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using CombatNamespace;
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
        public static Game game;
        
        // ---------------------------------------------------------------------------------------------------------
        // NEXT STEPS
        // DYNAMICEXPRESSO SETUP - STATUSES; give AI more command freedom? crits %? attack details (e.g Direct, AOE)
        // code to apply a Status status to an object (Player or Enemy), evaluating components
        // change statusList into a dictionary?? might be fiddly - would need to change prompt and json structure
        
        // LOADING GAMR. for testing ease. Player IDs. load all classes from already made files.
        // - LOOK AT GAMECLASS !LoadedSave
        // Player needs to have multiple moves: use enemy attack behaviours?
        // combat namespace
        //----------------------------------------------------------------------------------------------------------
        
        
        // ------------------------------------------------------------------------------------------------------------
        // CURRENT STATE
        // possibly working? enemy template writing to and deserialisation needs checking
        // ----------------------------------------------------------------------------------------------------------
        
        static async Task Main(string[] args)
        {
            // testing NLog setup:
            // NLog.Common.InternalLogger.LogLevel = NLog.LogLevel.Debug;
            // NLog.Common.InternalLogger.LogToConsole = true;
            // NLog.Common.InternalLogger.LogFile = @"c:\temp\nlog-internal.txt"; // On Linux one can use "/home/nlog-internal.txt"
            // TODO: apparently if we get NLog.config in the right place it should be found automatically, but for now, this is a workaround:
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(UtilityFunctionsNamespace.UtilityFunctions.mainDirectory + "\\NLog.config");
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
                    await game.initialiseGame(new TestNarrator.GameTest1(), true);
                        
                    
                    
                    Console.ReadLine();
                    break;
                case "game":
                    await game.initialiseGame(new Narrator());
                    
                    // start game
                    Console.WriteLine(game.enemyFactory.enemyTypes[0]);
                    
                    Console.ReadLine();
                    break;
                default:
                    Environment.FailFast($"debuggingPointEntry Invalid");
                    break;
            }
        }
        
        
        protected static void MyHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Exiting system due to external process kill or shutdown");
            
            // prevent application from terminating immediately
            args.Cancel = true;
            
            saveGameToAllStoragesSync();
            Thread.Sleep(3000);
            
            // exit smoothly
            Environment.Exit(0);
        }

        static void test()
        {
            Console.WriteLine("test");
        }
        
        public static async Task saveGameToAllStoragesAsync()
        {
            try
            {
                // checks before saving
                
                if (game.player != null)
                {
                    if (game.player.inventory != null)
                    {
                        await game.player.inventory.updateInventoryJSON();
                        Console.WriteLine("Inventory saved successfully.");
                    }

                    if (game.player.equipment != null)
                    {
                        await game.player.equipment.updateEquipmentJSON();
                        Console.WriteLine("Equipment saved successfully.");
                    }

                    await game.player.updatePlayerStatsXML();
                    Console.WriteLine("Player stats saved successfully.");
                }

                Console.WriteLine("All game data saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game data: {ex.Message}");
                throw;  // Rethrow if you want to handle this exception at a higher level or log it
            }
            
            // every aspect that needs to be saved
            // map state, game state, story progress etc needs to be saved
        }
        
        public static void saveGameToAllStoragesSync()
        {
            Console.WriteLine("Saving game data...");
            try {
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
                throw;  // Rethrow if you want to handle this exception at a higher level or log it
            }
            
            // every aspect that needs to be saved
            // map state, game state, story progress etc needs to be saved
        }
        
        static void gridLoop(Player player)
        {
            string file = UtilityFunctions.mainDirectory + @"GridSaves\save1.json";
            Point playerloc = new Point(0, 0);
            if (player != null)
            {
                player.changePlayerPos(playerloc);
            }
            else
            {
            }

            List<List<Tile>> grid = GridFunctions.CreateGrid(20, 20);
            GridFunctions.SaveGrid(grid, file);
            //GridFunctions.PrintGrid(grid, playerloc, playerloc, sightRange.X, sightRange.Y, player);
            Point sightRange = new Point(4, 4);
            GridFunctions.PrintGrid(grid, playerloc, playerloc, sightRange.X, sightRange.Y, player);
            ConsoleKeyInfo keyInfo;
            bool gameRunning = true;
            while (gameRunning)
            {
                do
                {
                    keyInfo = Console.ReadKey();

                    // Check if the key is not Enter or Escape
                    if (keyInfo.Key != ConsoleKey.Enter && keyInfo.Key != ConsoleKey.Escape)
                    {
                        Point newplayerloc = move(playerloc, keyInfo.KeyChar, grid);
                        if (player != null)
                        {
                            player.changePlayerPos(newplayerloc);
                        }

                        //GridFunctions.PrintGrid(grid, playerloc, newplayerloc, sightRange.X, sightRange.Y, player);
                        if (GridFunctions.PrintGrid(grid, playerloc, newplayerloc, sightRange.X, sightRange.Y, player))
                        {
                            playerloc = newplayerloc;
                        }
                        else
                        {
                        }
                    }
                } while (keyInfo.Key != ConsoleKey.Q);

                gameRunning = options(true, true);
                GridFunctions.PrintGrid(grid, playerloc, playerloc, sightRange.X, sightRange.Y, player);
            }
        }

        static Point move(Point loc, char inp, List<List<Tile>> grid)
        {
            bool moved = false;
            while (!moved)
            {
                switch (inp)
                {
                    case 'w':
                        if (loc.Y != 0)
                        {
                            loc.Y -= 1;
                            moved = true;
                            return loc;
                        }
                        else
                        {
                            return loc;
                        }
                    case 'a':
                        if (loc.X != 0)
                        {
                            loc.X -= 1;
                            moved = true;
                            return loc;
                        }
                        else
                        {
                            return loc;
                        }
                    case 's':
                        if (loc.Y != grid[loc.X].Count - 1)
                        {
                            loc.Y += 1;
                            moved = true;
                            return loc;
                        }
                        else
                        {
                            return loc;
                        }
                    case 'd':
                        if (loc.X != grid.Count - 1)
                        {
                            loc.X += 1;
                            moved = true;
                            return loc;
                        }
                        else
                        {
                            return loc;
                        }
                    default:
                        moved = true;
                        break;
                }
            }

            return loc;
        }

        public static bool menu(bool gameStarted, bool saveChosen, bool testing = false)
        {
            if (saveChosen == false && gameStarted == false)
            {
                UtilityFunctions.clearScreen(null);
                Console.SetWindowSize(80, 15); // Adjust the window size to fit your preference
                Console.Title = "Dungeon Crawler Menu";
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}╔═══════════════════════════════════════════════════════════════════════╗");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                               {UtilityFunctions.colourScheme.menuAccentCode}Torment{UtilityFunctions.colourScheme.menuMainCode}                                 ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                 [1] Start Game          [2] Load Save                 ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                 [3] Options             [4] Quit                      ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║");
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.menuMainCode}╚═══════════════════════════════════════════════════════════════════════╝{UtilityFunctions.colourScheme.generalTextCode}");
                Console.WriteLine();
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{UtilityFunctions.colourScheme.generalTextCode}Choose an option: ");
                int choice;
                if (int.TryParse(Console.ReadLine(), out choice))
                {
                    switch (choice)
                    {
                        case 1:
                            UtilityFunctions.clearScreen(null);
                            List<string> saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"Characters\", "*.xml")
                                .ToList();
                            saves.Remove($@"{UtilityFunctions.mainDirectory}Characters\saveExample.xml");
                            bool started = false;
                            for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                            {
                                if (saves.Count == i)
                                {
                                    UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"Save Slot save{i + 1}.xml is empty. Do you want to begin a new game? y/n");
                                    string load = Console.ReadLine();
                                    if (load == "y")
                                    {
                                        string save = UtilityFunctions.mainDirectory + @$"Characters\save{i + 1}.xml";
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
                                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "No empty save slots. Please choose a different option.");
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
                            bool outcome = options(gameStarted, saveChosen);
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
                            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Invalid choice. Please select a valid option.");
                            Thread.Sleep(1000);
                            break;
                    }
                }
                else
                {
                    UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Invalid input. Please enter a valid number.");
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
            
            UtilityFunctions.enemyTemplateSpecificDirectory =
                UtilityFunctions.enemyTemplateDir + UtilityFunctions.saveName + ".json";

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
            }

            Console.Clear();


            // call api and initialise narrator, getting charactcer details
            // MAIN INITIALISE FUNCTION FOR CHARACTER
            player.initialisePlayerFromNarrator(gameSetup, api, chat, testing);

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
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Choose a save slot:\n");

                        for (int i = 0;
                             i < Directory.GetFiles(UtilityFunctions.mainDirectory + @"Characters\", "*.xml").Length;
                             i++)
                        {
                            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), $"{i + 1}. Save Slot {i + 1}\n");
                        }

                        saveSlot = Console.ReadLine();

                        if (Convert.ToInt32(saveSlot) > 0 && Convert.ToInt32(saveSlot) <= UtilityFunctions.maxSaves)
                        {
                            valid = true;
                        }
                        else
                        {
                            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Invalid input.");
                            Thread.Sleep(1000);
                        }
                    }

                    UtilityFunctions.saveSlot = "save" + (saveSlot) + ".xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"Characters\save" + (saveSlot) + ".xml";
                    UtilityFunctions.saveName = "save" + (saveSlot);
                }

                startedGame = true;
                UtilityFunctions.clearScreen(null);
            }
        }

        // doing wipe all Characters / options then start doesnt work because options menu sends you back to menu, and you end up having to go back to the end of menu
        // where it automatically returns false. either get load to return a value or get rid of all menu rerouts inside options so that it sends you back to the menu 
        // already in, instead of generating a new one.

        // I think I'm doing it wrong, but I'm not sure how to fix it.
        // code whisperer, do you have any ideas how?
        // could you write me a // message explaining how?
        //       

        public static bool options(bool gameStarted, bool saveChosen)
        {
            UtilityFunctions.clearScreen(null);
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Options menu.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[1] Back to game.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[2] Change type of music.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[3] Change difficulty.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[4] Change type of sound effects.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[5] Exit game.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[6] Clear all saves.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "[7] Set example save.\n");
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Choose an option: ");
            int choice;
            if (int.TryParse(Console.ReadLine(), out choice))
            {
                switch (choice)
                {
                    case 1:
                        bool outcome = menu(gameStarted, saveChosen);
                        return outcome;
                    // Back to main menu
                    case 2:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "No Music yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of music
                    case 3:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "No Difficulty yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change difficulty
                    case 4:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "No Sound Effects yet, check back later.");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of sound effects
                    case 6:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Are you sure you want to clear all Characters? y/n\n");
                        string clearSaves = Console.ReadLine();
                        if (clearSaves == "y")
                        {

                            DeleteAllSaves();

                            Thread.Sleep(1000);
                            // return options(gameStarted, saveChosen);
                            bool outcome1 = menu(gameStarted, saveChosen);
                            return outcome1;
                        }
                        else
                        {
                            UtilityFunctions.clearScreen(null);
                            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Saves not cleared.\n");
                            Thread.Sleep(1000);
                            return options(gameStarted, saveChosen);
                        }

                        return options(gameStarted, saveChosen);
                    case 5:
                        Console.Clear();
                        saveGameToAllStoragesAsync().GetAwaiter().GetResult();
                        Environment.Exit(0);
                        return false;
                    case 7:
                        SetExampleSaves();
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    default:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Invalid option.\n");
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                }
            }
            else
            {
                UtilityFunctions.clearScreen(null);
                UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Invalid option.\n");
                Thread.Sleep(1000);
                return options(gameStarted, saveChosen);
            }
        }

        public static void SetExampleSaves()
        {
            // this function will overwrite every saveExample.(ext) to a save(int), as inputted.
            List<string> directories = new List<string>();
            string main = UtilityFunctions.mainDirectory;
            directories.Add(@$"{main}AttackBehaviours\");
            directories.Add(@$"{main}Characters\");
            directories.Add(@$"{main}EnemyTemplates\");
            directories.Add(@$"{main}Equipments\");
            directories.Add(@$"{main}Inventories\");
            directories.Add(@$"{main}Statuses\");
            
            Console.Clear();
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Enter a save (e.g: save1)\n");
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
                    { // if error, file is xml
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
                    Directory.GetFiles(UtilityFunctions.mainDirectory + @"ItemTemplates\" + saveName).ToList();
                foreach (string path in itemTemplatePaths)
                {
                    List<string> newPaths = Directory
                        .GetFiles(UtilityFunctions.mainDirectory + @"ItemTemplates\saveExamples").ToList();
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
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed), "Clearing all Characters...\n");
            string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"Characters\", "*.xml");
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
                Directory.GetFiles($@"{UtilityFunctions.mainDirectory}EnemyTemplates", searchPattern: "*.json").ToList();
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
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);

            if (GetConsoleMode(handle, out uint mode))
            {
                mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                SetConsoleMode(handle, mode);
            }
        } //\x1b[38;2;r;g;bm
    }
}