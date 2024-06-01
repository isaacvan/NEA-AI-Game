using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using GridConfigurationNamespace;
using GPTControlNamespace;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Emgu.CV.Dnn;
using ItemFunctionsNamespace;
using Microsoft.Extensions.Configuration;
using OpenAI_API;
using OpenAI_API.Chat;
using Model = OpenAI_API.Models.Model;

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

    
    
    static async Task Main(string[] args)
    {
        EnableColors();
        OpenAIAPI api = Narrator.initialiseGPT();
        var chat = api.Chat.CreateConversation();
        chat.Model = Model.GPT4_Turbo;
        chat.RequestParameters.Temperature = 0.9;
        string debugPointEntry = "game";
        Player player;
        switch (debugPointEntry)
        {
            case "testing":
                //Directory.CreateDirectory(@"C:\Users\isaac\RiderProjects\NEA-AI-Game\ItemTemplates\save1");
                // create directory for this game
                if (!Directory.Exists(@"C:\Users\isaac\RiderProjects\NEA-AI-Game\ItemTemplates\save1"))
                {
                    try
                    {
                        Directory.CreateDirectory(@"C:\Users\isaac\RiderProjects\NEA-AI-Game\ItemTemplates\save1");
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                else
                {
                    throw new Exception("Item template directory already exists.");
                }
                break;
            case "game":
                player = await initializeGame(api, chat);
                await ItemFactory.initialiseItemFactoryFromNarrator(api, chat);
                // NEXT STEPS
                // ITEM PROMPT TO APPLY TO ONE SPECIFIC CLASS
                // INVENTORY CLASS
                // EVERYTHING TO DO WITH ITEMS
                
                
                
                Console.ReadLine();
                //gridLoop(player);
                break;
            default:
                Environment.FailFast($"debuggingPointEntry Invalid");
                break;
        }
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

    static bool menu(bool gameStarted, bool saveChosen)
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
                                "No empty save slots. Please choose a different option.", UtilityFunctions.typeSpeed);
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

    static async Task<Player> initializeGame(OpenAIAPI api, Conversation chat)
    {
        // Task.Run(async () =>
        // {
        //     await YourAsyncMethod();
        // }).Wait();
        Console.CursorVisible = false;
        bool gameStarted = false;
        bool saveChosen = false;
        while (!saveChosen)
        {
            saveChosen = menu(gameStarted, saveChosen); // displays the menu
        }

        Player player;
        
        if (UtilityFunctions.loadedSave)
        {
            // put player where they were back into the game. If it's a new save, ignore.
            //string chosenClass = UtilityFunctions.chooseClass();
            //player = UtilityFunctions.CreatePlayerInstance(chosenClass);
            // deserialize the utilityfucntions.saveFile and load it into player
            XmlSerializer serializer1 = new XmlSerializer(typeof(Player));
            using (TextReader reader = new StreamReader(UtilityFunctions.saveFile))
            {
                player = (Player)serializer1.Deserialize(reader);
            }
            // UtilityFunctions.clearScreen(player); // clears the screen and pastes exp bar
            
            
            
            throw new Exception("Not implemented yet");

        }
        else
        {
            player = UtilityFunctions.CreatePlayerInstance();
            // GridFunctions.CreateGrid(@"D:\isaac\Documents\Code Projects\GridSaves");
        }
        
        Console.Clear();
        
        
        // call api and initialise narrator, getting charactcer details
        player.initialisePlayerFromNarrator(api, chat, player);
        
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
        UtilityFunctions.TypeText(UtilityFunctions.Instant, "[2] Change type of music.\n", UtilityFunctions.typeSpeed);
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
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Clearing all saves...\n",
                            UtilityFunctions.typeSpeed);
                        string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml");
                        foreach (string save in saves)
                        {
                            File.Delete(save);
                        }

                        string[] templates =
                            Directory.GetDirectories($@"{UtilityFunctions.mainDirectory}ItemTemplates");
                        foreach (string template in templates)
                        {
                            Directory.Delete(template, true);
                        }

                        string[] files = Directory.GetFiles($@"{UtilityFunctions.mainDirectory}ItemTemplates");
                        foreach (string file in files)
                        {
                            File.Delete(file);
                        }
                       

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