using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using GridConfigurationNamespace;
using GPTControlNamespace;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using ItemFunctionsNamespace;
using OpenAI_API;
using OpenAI_API.Chat;
using GameClassNamespace;

namespace MainNamespace
{
    class Program
    {
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

        
        
        // ---------------------------------------------------------------------------------------------------------
        // NEXT STEPS
        // same thing with equipment, ensure this is updated every time equipment changes. ensure equip and unequip work.
        // cleean code, make GameSetups.cs
        // go through and fix bool testing usages with GameSetup usage
        // ENEMY GENERATION. see UML class diagram. clear and restart enemy classes
        //----------------------------------------------------------------------------------------------------------


        class GameTest1 : GameSetup {
            public void chooseSave()
            {
                UtilityFunctions.saveSlot = "saveExample.xml";
                UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"saves\saveExample.xml";
                UtilityFunctions.saveName = "saveExample";
            }

            public async Task generateMainXml(Conversation chat, string prompt5)
            {
                string filePath = $@"{UtilityFunctions.mainDirectory}saves\saveExample.xml";
                
                // Player player with attributes
                if (string.IsNullOrEmpty(filePath))
                    throw new ArgumentException("File path must not be null or empty.");

                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"The file '{filePath}' does not exist.");

                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                Player loadedPlayer;
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    loadedPlayer = (Player)serializer.Deserialize(fileStream);
                }

                // set player properties
                if (loadedPlayer == null)
                    throw new ArgumentNullException(nameof(loadedPlayer));

                Type playerType = typeof(Player);
                PropertyInfo[] properties = playerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);


                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        object value = property.GetValue(loadedPlayer);
                        property.SetValue(this, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to set property {property.Name}: {ex.Message}");
                        // Handle or log the error as necessary
                    }
                }
            }
        };

        class GameTest2 : GameTest1 {
            public void initialisePlayer(Player player)
            {
                player.Class = "Wolfman";
            }
        };
        
        static async Task Main(string[] args)
        {
            EnableColors();
            Game game = new Game();
            string debugPointEntry = "game";
            switch (debugPointEntry)
            {
                case "testing":
                    await game.initialiseGame(new GameTest1(), true);
                    game.player.EquipItem(EquippableItem.EquipLocation.Body, game.itemFactory.createItem(game.itemFactory.armourTemplates[0]));
                    game.player.EquipItem(game.itemFactory.weaponTemplates[0].ItemEquipLocation, game.itemFactory.createItem(game.itemFactory.weaponTemplates[0]));
                    game.player.AddItem(game.itemFactory.createItem(game.itemFactory.consumableTemplates[0]));
                    game.player.equipment.updateEquipmentJSON();
                    game.player.inventory.updateInventoryJSON();
                    
                    Console.ReadLine();
                    
                    break;
                case "game":
                    await game.initialiseGame(new Narrator());
                    
                    // start game
                    Console.ReadLine();
                    //gridLoop(player);
                    
                    
                    
                    break;
                default:
                    Environment.FailFast($"debuggingPointEntry Invalid");
                    break;
            }
        }



        public static void saveGameToAllStorages(Game game)
        {

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
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}╔═══════════════════════════════════════════════════════════════════════╗",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                               {UtilityFunctions.colourScheme.menuAccentCode}Torment{UtilityFunctions.colourScheme.menuMainCode}                                 ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                 [1] Start Game          [2] Load Save                 ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                 [3] Options             [4] Quit                      ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}║                                                                       ║",
                    UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.menuMainCode}╚═══════════════════════════════════════════════════════════════════════╝{UtilityFunctions.colourScheme.generalTextCode}",
                    UtilityFunctions.typeSpeed);
                Console.WriteLine();
                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                    $"{UtilityFunctions.colourScheme.generalTextCode}Choose an option: ", UtilityFunctions.typeSpeed);
                int choice;
                if (int.TryParse(Console.ReadLine(), out choice))
                {
                    switch (choice)
                    {
                        case 1:
                            UtilityFunctions.clearScreen(null);
                            List<string> saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml")
                                .ToList();
                            saves.Remove($@"{UtilityFunctions.mainDirectory}saves\saveExample.xml");
                            bool started = false;
                            for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                            {
                                if (saves.Count == i)
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
                                        UtilityFunctions.saveName = $"save{i + 1}";
                                        saveChosen = true;
                                        started = true;
                                        break;
                                    }
                                }
                            }

                            if (!started)
                            {
                                UtilityFunctions.TypeText(UtilityFunctions.Instant,
                                    "No empty save slots. Please choose a different option.",
                                    UtilityFunctions.typeSpeed);
                                Thread.Sleep(1000);
                            }

                            // Start the game
                            break;
                        case 2:
                            loadGame(gameStarted, saveChosen);
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
                            Environment.Exit(0);
                            // Exit the game
                            break;
                        default:
                            UtilityFunctions.TypeText(UtilityFunctions.Instant,
                                "Invalid choice. Please select a valid option.", UtilityFunctions.typeSpeed);
                            Thread.Sleep(1000);
                            break;
                    }
                }
                else
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid number.",
                        UtilityFunctions.typeSpeed);
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

            Player player;

            if (UtilityFunctions.loadedSave) // IF LOADED
            {
                // put player where they were back into the game. If it's a new save, ignore.
                //string chosenClass = UtilityFunctions.chooseClass();
                //player = UtilityFunctions.CreatePlayerInstance(chosenClass);
                // deserialize the utilityfucntions.saveFile and load it into player



                // will create a function called loadGame()

                try
                {
                    XmlSerializer serializer1 = new XmlSerializer(typeof(Player));
                    using (TextReader reader = new StreamReader(UtilityFunctions.saveFile))
                    {
                        player = (Player)serializer1.Deserialize(reader);
                    }
                }
                catch
                {
                    throw new Exception("Not implemented yet");
                }
                // UtilityFunctions.clearScreen(player); // clears the screen and pastes exp bar



                throw new Exception("Not implemented yet");

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




        static void loadGame(bool gameStarted, bool saveChosen)
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
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Choose a save slot:\n",
                            UtilityFunctions.typeSpeed);

                        for (int i = 0;
                             i < Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml").Length;
                             i++)
                        {
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"{i + 1}. Save Slot {i + 1}\n",
                                UtilityFunctions.typeSpeed);
                        }

                        saveSlot = Console.ReadLine();

                        if (Convert.ToInt32(saveSlot) > 0 && Convert.ToInt32(saveSlot) <= UtilityFunctions.maxSaves)
                        {
                            valid = true;
                        }
                        else
                        {
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input.",
                                UtilityFunctions.typeSpeed);
                            Thread.Sleep(1000);
                        }
                    }

                    UtilityFunctions.saveSlot = "save" + (saveSlot) + ".xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"saves\save" + (saveSlot) + ".xml";
                    UtilityFunctions.saveName = "save" + (saveSlot);
                }

                startedGame = true;
                UtilityFunctions.clearScreen(null);
            }
        }

        // doing wipe all saves / options then start doesnt work because options menu sends you back to menu, and you end up having to go back to the end of menu
        // where it automatically returns false. either get load to return a value or get rid of all menu rerouts inside options so that it sends you back to the menu 
        // already in, instead of generating a new one.

        // I think I'm doing it wrong, but I'm not sure how to fix it.
        // code whisperer, do you have any ideas how?
        // could you write me a // message explaining how?
        //       

        public static bool options(bool gameStarted, bool saveChosen)
        {
            UtilityFunctions.clearScreen(null);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Options menu.\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[1] Back to game.\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[2] Change type of music.\n",
                UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[3] Change difficulty.\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[4] Change type of sound effects.\n",
                UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[6] Clear all Saves.\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "[5] Exit game.\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Choose an option: ", UtilityFunctions.typeSpeed);
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
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Music yet, check back later.",
                            UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of music
                    case 3:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Difficulty yet, check back later.",
                            UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change difficulty
                    case 4:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Sound Effects yet, check back later.",
                            UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    // Change type of sound effects
                    case 6:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant,
                            "Are you sure you want to clear all saves? y/n\n", UtilityFunctions.typeSpeed);
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
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Saves not cleared.\n",
                                UtilityFunctions.typeSpeed);
                            Thread.Sleep(1000);
                            return options(gameStarted, saveChosen);
                        }

                        return options(gameStarted, saveChosen);
                    case 5:
                        Console.Clear();
                        Environment.Exit(0);
                        return false;
                        ;
                    default:
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid option.\n",
                            UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                }
            }
            else
            {
                UtilityFunctions.clearScreen(null);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid option.\n", UtilityFunctions.typeSpeed);
                Thread.Sleep(1000);
                return options(gameStarted, saveChosen);
            }
        }

        public static void DeleteAllSaves()
        {
            // delete saves
            UtilityFunctions.clearScreen(null);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Clearing all saves...\n",
                UtilityFunctions.typeSpeed);
            string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml");
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
        }

        static void DoBleed(Enemy enemy, Player player)
        {
            int bleedDamage = (player.level * (5 + enemy.roundBonus));
            int oldHealth = enemy.currentHealth;
            enemy.currentHealth -= bleedDamage;
            UtilityFunctions.TypeText(UtilityFunctions.Instant,
                "\x1b[31mBleed\x1b[0m applied, dealt \x1b[31m" + bleedDamage + " damage.\x1b[0m",
                UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant,
                $" The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.",
                UtilityFunctions.typeSpeed);
        }

        static void DoBurning(Enemy enemy, Player player)
        {
            if (enemy.defense > -50)
            {
                enemy.defense -= 10 * enemy.burningTurns;
            }
            else if (enemy.defense > -70)
            {
                enemy.defense -= 5 * enemy.burningTurns;
            }
            else
            {
                enemy.defense -= 2 * enemy.burningTurns;
            }
        }

        static void DoMadness(Enemy enemy, Player player)
        {
            /*  int healthLost = player.maxHealth - player.currentHealth;
              int multi =
              if (((healthLost / player.maxHealth) * 100) > )
           */
        }
    }
}