using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UtilityFunctionsNamespace;
using EnemyClassesNamespace;
using PlayerClassesNamespace;
using GridConfigurationNamespace;
using System.Xml.Linq;
using System.Drawing;
using System.Net.Http.Headers;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.Text;

partial class Program
{   // ERRORS CLEAR ALL SAVES
    // CLASS COLOUR SCHEME R G B AND D



    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool GetConsoleMode(IntPtr handle, out int mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetStdHandle(int handle);



    //\x1b[38;2;r;g;bm
    public static ColourScheme colourScheme = new ColourScheme(UtilityFunctions.colourSchemeIndex);


    static void Main(string[] args)
    {
        string debugPointEntry = "game";
        Player player;


        switch (debugPointEntry)
        {
            case "testing":
                gridLoop(null);
                break;
            case "game":
                player = initializeGame();
                gridLoop(player);
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
        List<List<Tile>> grid = GridFunctions.CreateGrid(20, 20, file);
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

            } while (keyInfo.Key != ConsoleKey.Escape);
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

    // public static async Task YourAsyncMethod()
    // {
    //     // bool keyPressed = false;

    //     while (true)
    //     {
    //         if (Console.KeyAvailable)
    //         {
    //             ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
    //             if (keyInfo.Key == ConsoleKey.Enter)
    //             {
    //                 quickEnd = true;
    //             }

    //         }

    //         await Task.Delay(500);
    //         quickEnd = false;
    //     }
    // }

    // public static void UtilityFunctions.TypeText(UtilityFunctions.Instant, string text, int typingSpeed)
    // {
    //     for (int i = 0; i < text.Length; i++)
    //     {
    //         Console.Write(text[i]);
    //         Thread.Sleep(typingSpeed);

    //         if (quickEnd)
    //         {
    //             string restOfText = text.Substring(i + 1);
    //             Console.Write(restOfText);
    //             break;
    //         }
    //     }
    //     Console.Write("\n");
    // }




    static bool menu(bool gameStarted, bool saveChosen)
    {
        if (saveChosen == false && gameStarted == false)
        {
            UtilityFunctions.clearScreen(null);

            Console.SetWindowSize(80, 15); // Adjust the window size to fit your preference
            Console.Title = "Dungeon Crawler Menu";

            /*
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "╔═══════════════════════════════════════════════════════════════════════╗", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                               ", UtilityFunctions.typeSpeed, false);
            Console.ForegroundColor = ConsoleColor.Cyan;
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Torment", UtilityFunctions.typeSpeed, false);
            Console.ForegroundColor = ConsoleColor.White;
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "                                 ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                 [1] Start Game          [2] Load Save                 ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                 [3] Options             [4] Quit                      ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "╚═══════════════════════════════════════════════════════════════════════╝", UtilityFunctions.typeSpeed);
            */
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"{colourScheme.menuMainCode}╔═══════════════════════════════════════════════════════════════════════╗", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"║                               {colourScheme.menuAccentCode}Torment{colourScheme.menuMainCode}                                 ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                 [1] Start Game          [2] Load Save                 ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                 [3] Options             [4] Quit                      ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "║                                                                       ║", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"╚═══════════════════════════════════════════════════════════════════════╝{colourScheme.generalTextCode}", UtilityFunctions.typeSpeed);
            Console.WriteLine();

            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Choose an option: ", UtilityFunctions.typeSpeed);





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
                                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Save Slot save{i + 1}.xml is empty. Do you want to begin a new game? y/n", UtilityFunctions.typeSpeed);
                                string load = Console.ReadLine();
                                if (load == "y")
                                {
                                    string save = UtilityFunctions.mainDirectory + @$"saves\save{i + 1}.xml";
                                    UtilityFunctions.saveSlot = Path.GetFileName(save);
                                    UtilityFunctions.saveFile = save;
                                    saveChosen = true;
                                    started = true;
                                    break;


                                }
                            }
                        }
                        if (!started)
                        {
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, "No empty save slots. Please choose a different option.", UtilityFunctions.typeSpeed);
                            Thread.Sleep(1000);
                        }
                        // Start the game
                        break;
                    case 2:
                        if (loadGame(gameStarted, saveChosen))
                        {
                            saveChosen = true;
                        }
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
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid choice. Please select a valid option.", UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        break;
                }
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid number.", UtilityFunctions.typeSpeed);
            }
        }
        return saveChosen;
    }

    static Player initializeGame()
    {
        // Task.Run(async () =>
        // {
        //     await YourAsyncMethod();
        // }).Wait();

        bool gameStarted = false;
        bool saveChosen = false;

        while (!saveChosen)
        {
            saveChosen = menu(gameStarted, saveChosen); // displays the menu
        }

        Player player;


        if (UtilityFunctions.loadedSave)
        { // put player where they were back into the game. If it's a new save, ignore.

            // DO SOMETHING ELSE (NOT A PRIORITY CURRENTLY)
            // broken rn player = UtilityFunctions.loadPlayerFromFile();
            string chosenClass = UtilityFunctions.chooseClass();
            player = UtilityFunctions.CreatePlayerInstance(chosenClass);
        }
        else
        {
            string chosenClass = UtilityFunctions.chooseClass();
            player = UtilityFunctions.CreatePlayerInstance(chosenClass);
            // GridFunctions.CreateGrid(@"D:\isaac\Documents\Code Projects\GridSaves");
        }

        XmlSerializer serializer = new XmlSerializer(typeof(Player));
        using (TextWriter writer = new StreamWriter(UtilityFunctions.saveFile))
        {
            serializer.Serialize(writer, player);
        }

        UtilityFunctions.clearScreen(player); // clears the screen and pastes exp bar

        return player;
    }

    static bool loadGame(bool gameStarted, bool saveChosen)
    {
        bool startedGame = false;
        while (!startedGame)
        {

            while (UtilityFunctions.saveSlot == "")
            {
                UtilityFunctions.clearScreen(null);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Choose a save slot:\n", UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "1. Save Slot 1\n", UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "2. Save Slot 2\n", UtilityFunctions.typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "3. Save Slot 3\n", UtilityFunctions.typeSpeed);
                string saveSlot = Console.ReadLine();

                if (saveSlot == "1")
                {
                    UtilityFunctions.saveSlot = "save1.xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"saves\save1.xml";
                }
                else if (saveSlot == "2")
                {
                    UtilityFunctions.saveSlot = "save2.xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"saves\save2.xml";
                }
                else if (saveSlot == "3")
                {
                    UtilityFunctions.saveSlot = "save3.xml";
                    UtilityFunctions.saveFile = UtilityFunctions.mainDirectory + @"saves\save3.xml";
                }
                else
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid input.\n", UtilityFunctions.typeSpeed);
                    Thread.Sleep(1000);
                }

            }

            UtilityFunctions.clearScreen(null);

            if (File.ReadAllLines(UtilityFunctions.saveFile)[0] == "active")
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Save file found!\n", UtilityFunctions.typeSpeed);
                bool startedTemp = false;
                while (!startedTemp)
                {
                    UtilityFunctions.clearScreen(null);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Would you like to load your save? y/n\n", UtilityFunctions.typeSpeed);
                    string loadSave = Console.ReadLine();
                    if (loadSave == "y")
                    {
                        startedTemp = true;
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Loading save...\n", UtilityFunctions.typeSpeed);
                        UtilityFunctions.loadSave(UtilityFunctions.saveFile); // return true but make loadedGame true
                        return true;
                    }
                    else if (loadSave == "n")
                    {
                        startedTemp = true;
                        bool startedTemp1 = false;
                        while (!startedTemp1)
                        {
                            UtilityFunctions.clearScreen(null);
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Would you like to delete the current save? y/n\n", UtilityFunctions.typeSpeed);
                            string overrideSave = Console.ReadLine();
                            if (overrideSave == "y")
                            {
                                startedTemp1 = true;

                                UtilityFunctions.clearScreen(null);
                                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Overriding save...\n", UtilityFunctions.typeSpeed);
                                UtilityFunctions.overrideSave(UtilityFunctions.saveFile); // deletes the save file and replaces it with an empty one
                                return false;

                            }
                            else if (overrideSave == "n")
                            {
                                startedTemp1 = true;
                                UtilityFunctions.clearScreen(null);
                                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Save not overridden.\n", UtilityFunctions.typeSpeed); // send back to menu
                                return false;
                            }
                            else
                            {
                                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid input.\n", UtilityFunctions.typeSpeed);
                                Thread.Sleep(1000);
                            }
                        }
                    }
                    else
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid input.\n", UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                UtilityFunctions.clearScreen(null);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Would you like to start a new save? y/n\n", UtilityFunctions.typeSpeed);
                string newSave = Console.ReadLine();
                if (newSave == "y")
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Starting a new save.\n", UtilityFunctions.typeSpeed);
                    string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.txt");
                    foreach (string save in saves)
                    {
                        if (File.ReadAllLines(save)[0] == "empty")
                        {
                            UtilityFunctions.clearScreen(null);
                            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Save Slot " + save.Substring(39) + " is empty. Do you want to begin a new game? y/n", UtilityFunctions.typeSpeed);
                            string load = Console.ReadLine();
                            if (load == "y")
                            {
                                UtilityFunctions.saveSlot = save.Substring(39); // starts a game as allows access, but loadedGame will be left as false
                                UtilityFunctions.saveFile = save;
                                saveChosen = true;
                                return true;


                            }
                            else if (load == "n")
                            {
                                return false; // back to menu

                            }
                            else
                            {
                                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid input.\n", UtilityFunctions.typeSpeed);
                            }// INPUT SANITISEFHUISHFUIHSUIF

                        }
                    }
                    // Start the game
                    break;

                }
                else if (newSave == "n")
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Save not started.\n", UtilityFunctions.typeSpeed);
                    menu(saveChosen, startedGame);
                    break;
                }
                else
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid input. Please enter a valid input.\n", UtilityFunctions.typeSpeed);
                }

            }
        }
        return saveChosen;
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
        UtilityFunctions.TypeText(UtilityFunctions.Instant, "[4] Change type of sound effects.\n", UtilityFunctions.typeSpeed);
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
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Music yet, check back later.", UtilityFunctions.typeSpeed);
                    Thread.Sleep(1000);
                    return options(gameStarted, saveChosen);
                // Change type of music
                case 3:
                    UtilityFunctions.clearScreen(null);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Difficulty yet, check back later.", UtilityFunctions.typeSpeed);
                    Thread.Sleep(1000);
                    return options(gameStarted, saveChosen);
                // Change difficulty
                case 4:
                    UtilityFunctions.clearScreen(null);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "No Sound Effects yet, check back later.", UtilityFunctions.typeSpeed);
                    Thread.Sleep(1000);
                    return options(gameStarted, saveChosen);
                // Change type of sound effects

                case 6:

                    UtilityFunctions.clearScreen(null);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Are you sure you want to clear all saves? y/n\n", UtilityFunctions.typeSpeed);
                    string clearSaves = Console.ReadLine();
                    if (clearSaves == "y")
                    {
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Clearing all saves...\n", UtilityFunctions.typeSpeed);
                        string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml");
                        foreach (string save in saves)
                        {
                            File.Delete(save);
                        }
                        Thread.Sleep(1000);
                        // return options(gameStarted, saveChosen);
                        bool outcome1 = menu(gameStarted, saveChosen);
                        return outcome1;
                    }
                    else
                    {
                        UtilityFunctions.clearScreen(null);
                        UtilityFunctions.TypeText(UtilityFunctions.Instant, "Saves not cleared.\n", UtilityFunctions.typeSpeed);
                        Thread.Sleep(1000);
                        return options(gameStarted, saveChosen);
                    }

                    return options(gameStarted, saveChosen);
                case 5:
                    Console.Clear();
                    Environment.Exit(0);
                    return false; ;
                default:
                    UtilityFunctions.clearScreen(null);
                    UtilityFunctions.TypeText(UtilityFunctions.Instant, "Invalid option.\n", UtilityFunctions.typeSpeed);
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
        UtilityFunctions.TypeText(UtilityFunctions.Instant, "\x1b[31mBleed\x1b[0m applied, dealt \x1b[31m" + bleedDamage + " damage.\x1b[0m", UtilityFunctions.typeSpeed);
        UtilityFunctions.TypeText(UtilityFunctions.Instant, $" The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", UtilityFunctions.typeSpeed);
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
