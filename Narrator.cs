using System.Drawing;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CombatNamespace;
using EnemyClassesNamespace;
using GameClassNamespace;
using GridConfigurationNamespace;
using ItemFunctionsNamespace;
using MainNamespace;
using OpenAI_API;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using Exception = System.Exception;

namespace GPTControlNamespace
{
    public static class NarrationTypeWriter
    {
        private static Task BackgroundTask;
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static List<string> Narrations { get; set; }
        private static int MovesSinceLastNarration { get; set; }

        static NarrationTypeWriter()
        {
            BackgroundTask = Task.Run(async () => await BackgroundWorkAsync(CancellationTokenSource.Token));
            Narrations = new List<string>();
            MovesSinceLastNarration = 0;
        }

        public static void Start() // just in case not already started
        {
            if (BackgroundTask == null)
            {
                BackgroundTask = Task.Run(async () => await BackgroundWorkAsync(CancellationTokenSource.Token));
            }

            if (Narrations == null)
            {
                Narrations = new List<string>();
            }

            MovesSinceLastNarration = 0;
        }

        public static void Stop()
        {
            CancellationTokenSource.Cancel();
        }

        public static void IncrementMovesSinceLastNarration()
        {
            MovesSinceLastNarration++;
        }

        private static async Task BackgroundWorkAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Game localGame;
                    lock (Program.game)
                    {
                        while (!Program.gameStarted) Thread.Sleep(1000);
                        localGame = Program.game; // Thread-safe copy of the game
                    }

                    var Narration = await RequestNarrative(localGame, 25, Narrations).ConfigureAwait(false);
                    Narrations.Add(Narration.Item2);
                    if (Narration.Item1)
                    {
                        await localGame.uiConstructer.TypeNarration(Narration.Item2).ConfigureAwait(false);
                    }

                    lock (Program.game)
                    {
                        Program.game = localGame;
                    }

                    // wait till moves since last narration reaches 20
                    // then reset the moves
                    await Task.Run(async () =>
                    {
                        while (MovesSinceLastNarration < 20) await Task.Delay(10);
                        MovesSinceLastNarration = 0;
                    }).ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Background task was canceled.");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<(bool, string)> RequestNarrative(Game localGame, int maxTok, List<string> Narrations)
        {
            List<EnemySpawn> enemies = localGame.map.GetCurrentNode().enemies;
            List<string> enemyTypes = localGame.enemyFactory.enemyTypes;
            string enemyCount = "";
            foreach (string t in enemyTypes)
            {
                enemyCount += $"{t}: {enemies.Count(e => e.name == t)} ";
            }

            List<Node> nodes = localGame.map.Graphs[localGame.map.CurrentGraphPointer].Nodes;
            List<string> NodeNames = new List<string>();
            List<int> NodeIds = new List<int>();
            List<int> ConnectedNodes = localGame.map.GetCurrentNode().ConnectedNodes;
            foreach (Node node in nodes)
            {
                NodeNames.Add(node.NodePOI);
                NodeIds.Add(node.NodeID);
            }

            List<string> FinalUpcomingNodes = new List<string>();

            foreach (int id in ConnectedNodes)
            {
                if (nodes.Find(n => n.NodeID == id) != null)
                {
                    if (nodes.Find(n => n.NodeID == id).NodeDepth > localGame.map.GetCurrentNode().NodeDepth)
                    {
                        FinalUpcomingNodes.Add(nodes.Find(n => n.NodeID == id).NodePOI);
                    }
                }
            }


            string prompt2 = "Narrate:";
            List<string> linesToAdd = new List<string>()
            {
                $"Location: {localGame.map.GetCurrentNode().NodePOI}",
                $"Upcoming Locations: {string.Join(", ", FinalUpcomingNodes)}",
                $"Progression: Map segment {localGame.map.CurrentGraphPointer} / {UtilityFunctions.maxGraphDepth}",
                $"Player Health: {localGame.player.currentHealth} / {localGame.player.Health}",
                $"Player Inventory: {string.Join(", ", localGame.player.inventory.Items)}",
                $"Enemies in Room: {enemyCount}",
                $"Objective: {localGame.map.GetCurrentNode().Obj.ObjectiveName}",
                $"Tension Level: Narrator's choice",
                $"Narrative Arc: Narrator's choice"
            };
            foreach (string line in linesToAdd)
            {
                prompt2 += $"\n{line}";
            }

            if (Narrations.Count > 0)
            {
                prompt2 += "\n\nHere is a list of your previous narrations in order:";
                foreach (string n in Narrations)
                {
                    prompt2 += $"\n{n}";
                }
            }

            localGame.chat.AppendUserInput(prompt2);
            string output = await localGame.narrator.GetGPTOutput(localGame.chat, "Narrative", maxTok)
                .ConfigureAwait(false);

            if (output.ToLower() == "false" || (output.ToLower().Contains("false") && output.Length < 10))
            {
                return (false, output);
            }
            else
            {
                return (true, output);
            }
        }

        public static async Task<bool> HandleObjectiveTextAdventure(Objective objective, Game game)
        {
            Console.Clear();
            int promptIndex = 0;

            while (!objective.IsCompleted && promptIndex < objective.NarrativePrompts.Count)
            {
                Console.WriteLine(objective.NarrativePrompts[promptIndex]);

                // wait for the players input.
                string playerInput = Console.ReadLine();

                // process the input.
                bool continueAdventure = objective.OnInteraction(game.player, game, playerInput);

                if (!continueAdventure)
                {
                    Console.WriteLine("The adventure ends here.");
                    break;
                }

                promptIndex++;
            }

            return objective.IsCompleted;
        }
    }


    public interface GameSetup
    {
        public void chooseSave();
        public void IntroduceGPT(ref Conversation chat, Game game);
        Task<Player> generateMainXml(Conversation chat, string prompt5, Player player);

        Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat, EnemyFactory enemyFactory,
            AttackBehaviourFactory attackBehaviourFactory);

        Task<AttackBehaviourFactory> initialiseAttackBehaviourFactoryFromNarrator(Conversation chat);
        Task<StatusFactory> initialiseStatusFactoryFromNarrator(Conversation chat);
        Task GenerateUninitialisedStatuses(Conversation chat);
        Task GenerateUninitialisedAttackBehaviours(Conversation chat);
        Task<Game> GenerateGraphStructure(Conversation chat, Game game, GameSetup gameSetup, int Id);
        Task<Map> GenerateMapStructure(Conversation chat, Game game, GameSetup gameSetup);
        Task<Graph> PopulateNodesWithTiles(Graph graph, Game game, bool loaded = false);
        Task GenerateStoryLineForFuture(Conversation chat, Game game);
        Task<Objective> GenerateInitialObjective(Game game, Node node);
    }


    public class Narrator : GameSetup
    {
        private OpenAIAPI api;
        private Conversation chat;
        
        

        public async Task<Objective> GenerateInitialObjective(Game game, Node node)
        {
            string prompt = File.ReadAllText(UtilityFunctions.promptPath + "Prompt11.txt");
            prompt = Regex.Replace(prompt, @"{node.NodePOI}", node.NodePOI);
            prompt = Regex.Replace(prompt, @"{node.NodeID}", node.NodeID.ToString());

            game.chat.AppendUserInput(prompt);

            string aiResponse = await game.narrator.GetGPTOutput(game.chat, $"GenerateInitialObjective:{Regex.Replace(node.NodePOI, " ", "")}");
            aiResponse = await UtilityFunctions.FixJson(aiResponse);

            // ensure JSON is valid and parse it into an Objective object
            try
            {
                // deserialize 
                Objective generatedObjective = JsonConvert.DeserializeObject<Objective>(aiResponse);

                // add the generated objective to the node
                node.Obj = generatedObjective;
                return generatedObjective;
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to parse the AI response into an Objective: " + ex.Message);
            }
        }

        public async Task GenerateStoryLineForFuture(Conversation chat, Game game)
        {
            chat.AppendUserInput(
                "As the narrator, your storyline might need to be continued from a different version of yourwelf, where you don't have the memory of the storyline you will make the user follow. To fix this, please output a summary of the whole storyline, so that when fed to another narrator, they can continue the story just as you would.");
            string outp = await game.narrator.GetGPTOutput(chat, "Story Line");
            File.WriteAllText(
                $"{UtilityFunctions.mainDirectory}StoryLines{Path.DirectorySeparatorChar}{UtilityFunctions.saveName}.txt",
                outp);
        }

        public void IntroduceGPT(ref Conversation chat, Game game)
        {
            string enemyNamesToSelectFrom = string.Join(", ", game.enemyFactory.enemyTemplates.Keys);
            List<ItemTemplate> templates = new List<ItemTemplate>();
            templates.AddRange(game.itemFactory.weaponTemplates);
            //string itemNamesToSelectFrom = string.Join(", ", )
            chat.AppendUserInput(File.ReadAllText($"{UtilityFunctions.promptPath}Prompt3.txt"));
            chat.AppendUserInput(
                $"In this story, these are the names of the enemies that can feature: {enemyNamesToSelectFrom}");
            chat.AppendUserInput(
                "Remember that you are trying to tell a story. As such, try to follow a story arc / narrative, bearing in mind how far through the game the user is.");
            if (File.Exists(
                    $"{UtilityFunctions.mainDirectory}{Path.DirectorySeparatorChar}StoryLines{Path.DirectorySeparatorChar}{UtilityFunctions.saveName}.txt"))
                chat.AppendUserInput(
                    $"This is the storyline that you are following: {File.ReadAllText($"{UtilityFunctions.mainDirectory}{Path.DirectorySeparatorChar}StoryLines{Path.DirectorySeparatorChar}{UtilityFunctions.saveName}.txt")}");
            // chat.AppendUserInput($"Finally, also consider that you could be telling a story from halfway through due to them loading an old savefile.");
        }

        public async Task<Graph> PopulateNodesWithTiles(Graph graph, Game game, bool loaded = false)
        {
            Graph graphToReturn = new Graph(graph.Id, new List<Node>(), GridFunctions.LastestGraphDepth);
            foreach (var node in graph.Nodes)
            {
                Node newNode = GridFunctions.PopulateNodeWithTiles(node, graph);
                newNode.InitialiseEnemies(game);
                newNode = GridFunctions.AddStructures(newNode);
                graphToReturn.Nodes.Add(newNode);
            }

            graphToReturn.SetEntryAndExits();
            await graphToReturn.AddObjectivesToNodes(loaded);

            return graphToReturn;
        }

        public async Task<Map> GenerateMapStructure(Conversation chat, Game game, GameSetup gameSetup)
        {
            if (game.map == null)
            {
                game.map = new Map();
                game.map.Graphs = new List<Graph>();
                await game.map.AppendGraph(GenerateGraphStructure(chat, game, gameSetup, 0).GetAwaiter().GetResult()
                    .map.Graphs[game.map.Graphs.Count - 1]);
                // game.map.CurrentNode = game.map.Graphs[game.map.Graphs.Count - 1].Nodes[0];
            }

            return game.map;
        }

        public async Task<Game> LoadGraphStructure(Game game, GameSetup gameSetup)
        {
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Loading graph structure...");

            UtilityFunctions.mapsSpecificDirectory = UtilityFunctions.mapsDir + UtilityFunctions.saveName + ".json";

            string output = File.ReadAllText(UtilityFunctions.mapsSpecificDirectory);
            if (game.map == null) game.map = new Map();
            game.map = JsonConvert.DeserializeObject<Map>(output);
            game.map.Graphs[game.map.Graphs.Count - 1] =
                await PopulateNodesWithTiles(game.map.Graphs[game.map.Graphs.Count - 1], game, true);
            if (game.map.GetCurrentNode().Obj == null)
            {
                await game.map.GetCurrentNode().AddObjectiveToNode(false);
            }
            return game;
        }

        public async Task<Game> GenerateGraphStructure(Conversation chat, Game game, GameSetup gameSetup, int Id)
        {
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Generating graph structure...");

            string prompt = "";
            string output = "";

            // TESTING
            bool test = false;
            if (test)
            {
                // testing
                output =
                    "{\n  \"Id\": 0,\n  \"Nodes\": [\n    {\n      \"NodeID\": 0,\n      \"NodeDepth\": 0,\n      \"NodeHeight\": 30,\n      \"NodeWidth\": 30,\n      \"ConnectedNodes\": [1, 2],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\"],\n      \"NodePOI\": \"Starting Point\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 1,\n      \"NodeDepth\": 1,\n      \"NodeHeight\": 25,\n      \"NodeWidth\": 25,\n      \"ConnectedNodes\": [0, 3, 4],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\", \"undirected\"],\n      \"NodePOI\": \"Abandoned Outpost\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 2,\n      \"NodeDepth\": 1,\n      \"NodeHeight\": 20,\n      \"NodeWidth\": 20,\n      \"ConnectedNodes\": [0, 4],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\"],\n      \"NodePOI\": \"Ancient Ruins\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 3,\n      \"NodeDepth\": 2,\n      \"NodeHeight\": 40,\n      \"NodeWidth\": 35,\n      \"ConnectedNodes\": [1, 5],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\"],\n      \"NodePOI\": \"Mystical Forest\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 4,\n      \"NodeDepth\": 2,\n      \"NodeHeight\": 20,\n      \"NodeWidth\": 25,\n      \"ConnectedNodes\": [1, 2, 5],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\", \"undirected\"],\n      \"NodePOI\": \"Raging River\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 5,\n      \"NodeDepth\": 3,\n      \"NodeHeight\": 45,\n      \"NodeWidth\": 40,\n      \"ConnectedNodes\": [3, 4, 6, 7],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\", \"undirected\", \"undirected\"],\n      \"NodePOI\": \"Crossroads\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 6,\n      \"NodeDepth\": 4,\n      \"NodeHeight\": 50,\n      \"NodeWidth\": 50,\n      \"ConnectedNodes\": [5, 8],\n      \"ConnectedNodesEdges\": [\"undirected\", \"undirected\"],\n      \"NodePOI\": \"Lost City\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 7,\n      \"NodeDepth\": 4,\n      \"NodeHeight\": 30,\n      \"NodeWidth\": 30,\n      \"ConnectedNodes\": [5],\n      \"ConnectedNodesEdges\": [\"undirected\"],\n      \"NodePOI\": \"Desolate Camp\",\n      \"Milestone\": false\n    },\n    {\n      \"NodeID\": 8,\n      \"NodeDepth\": 5,\n      \"NodeHeight\": 60,\n      \"NodeWidth\": 60,\n      \"ConnectedNodes\": [6],\n      \"ConnectedNodesEdges\": [\"undirected\"],\n      \"NodePOI\": \"Timeless Dungeon\",\n      \"Milestone\": true\n    }\n  ]\n}";
            }
            else
            {
                try
                {
                    prompt = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt1.txt");

                    if (UtilityFunctions.maxNodeDepth == 0)
                    {
                        UtilityFunctions.maxNodeDepth = 5; // testing purposes
                    }

                    prompt = $"{prompt}{Id}";
                    prompt =
                        $"{prompt}\nThe maximum nodeDepth you should go up to (and the milestone should have) is {UtilityFunctions.maxNodeDepth}.";
                    // ADD EXTRA DETAILS DEPENDING ON WHAT NUMBER GRAPH WE ARE ONE SO IT KNOWS IT IS CONTINUING THE PREVIOUS GRAPHS

                    chat.AppendUserInput(prompt);
                    output = await GetGPTOutput(chat, "GraphStructure"); // 26s
                    output = await UtilityFunctions.FixJson(output);
                }
                catch (Exception e)
                {
                    throw e;
                }

                UtilityFunctions.mapsSpecificDirectory = UtilityFunctions.mapsDir + UtilityFunctions.saveName + ".json";
                if (game.map.Graphs == null || game.map.Graphs.Count == 0 || game.map == null)
                {
                    game.map = new Map();
                    game.map.Graphs = new List<Graph>();
                }
            }

            Graph graph = JsonConvert.DeserializeObject<Graph>(output);
            graph = await PopulateNodesWithTiles(graph, game);
            game.map.Graphs.Add(graph);
            string mapInJson = JsonConvert.SerializeObject(game.map);

            File.Create(UtilityFunctions.mapsSpecificDirectory).Close();
            File.WriteAllText(UtilityFunctions.mapsSpecificDirectory, mapInJson);

            return game;
        }

        // Testing
        public static OpenAIAPI initialiseGPT()
        {
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Initialising GPT...");
            //Thread.Sleep(500);
            string? apiKey = System.Environment.GetEnvironmentVariable("API_KEY");
            Console.WriteLine($"ENV API Key: {apiKey}");
            if (apiKey == null)
            {
                Console.WriteLine("ENV API Key is not set, trying secrets");
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();
                apiKey = config["API_KEY"];
                if (apiKey != null)
                {
                    Console.WriteLine("Secret found");
                }
                else
                {
                    throw new Exception("No GPT API key! Set API_KEY env variable");
                }
            }

            System.Environment.SetEnvironmentVariable("API_KEY", apiKey);

            OpenAIAPI api = new OpenAIAPI(apiKey);


            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "Initialised GPT.");
            //Thread.Sleep(500);
            Console.Clear();

            return api;
        }

        public async Task<string> GetGPTOutput(Conversation chat, string title, int? maxTokens = null)
        {
            if (maxTokens != null)
            {
                chat.RequestParameters.MaxTokens = maxTokens;
                chat.AppendUserInput(
                    $"The max tokens for the next output is {maxTokens}. Ensure that your output makes sense within {maxTokens} tokens.");
            }
            else chat.RequestParameters.MaxTokens = null;

            // chat.RequestParameters.Temperature = 0.9;
            string output = "";
            bool completed = false;
            while (!completed)
            {
                try
                {
                    output = await chat.GetResponseFromChatbotAsync();
                    completed = true;
                }
                catch (HttpRequestException e)
                {
                    UtilityFunctions.TypeText(new TypeText(typingSpeed: 10, newLine: false), ".");
                    Thread.Sleep(1000);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            string logContents = "";
            logContents +=
                $"INPUT: {chat.Messages[chat.Messages.Count - 2].Content}\n\nOUTPUT: {chat.Messages[chat.Messages.Count - 1].Content}";

            try
            {
                File.Create(UtilityFunctions.logsSpecificDirectory + $@"{Path.DirectorySeparatorChar}{title}" +
                            ".txt").Close();
                File.WriteAllText(
                    UtilityFunctions.logsSpecificDirectory + $@"{Path.DirectorySeparatorChar}{title}" + ".txt",
                    logContents);
            }
            catch (Exception e)
            {
                throw new Exception("Error creating log file: " + e.Message);
            }

            return output;
        }

        public static Conversation initialiseChat(OpenAIAPI api)
        {
            Conversation chat = api.Chat.CreateConversation();
            Model model = Model.GPT4;
            chat.Model = model;
            // chat.RequestParameters.Temperature = 0.9;
            return chat;
        }

        public void chooseSave()
        {
            bool gameStarted = false;
            bool saveChosen = false;
            while (!saveChosen)
            {
                saveChosen = Program.menu(gameStarted, saveChosen); // displays the menu
            }
        }

        public async Task<Player> generateMainXml(Conversation chat, string prompt5, Player player)
        {
            // get user input
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "As the player, you get to add a level of context for your generated story.\nThis can be any request, such as 'Begin as a knight in a medieval setting'.\nIf you would like a completely random story, just type 'Random':\n");
            UtilityFunctions.playerContextInput = Console.ReadLine();
            if (string.IsNullOrEmpty(UtilityFunctions.playerContextInput.Trim()))
            {
                UtilityFunctions.playerContextInput = "Random";
            }

            prompt5 += "\n" + UtilityFunctions.playerContextInput;

            // get GPT response

            // get the user desired length of the game
            UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                "You can also choose how long you wish the game to be. You can enter 2 numbers in the format of 'X,Y': the first indicating the 'length' of each segment to the game, and the next deciding how many segments there are. If you enter the word 'standard', this will be set to 5 and 10");
            string lengthInp = Console.ReadLine();

            try
            {
                if (lengthInp.ToLower() != "standard")
                {
                    string[] lengths = lengthInp.Split(",");
                    UtilityFunctions.maxNodeDepth = int.Parse(lengths[0]);
                    UtilityFunctions.maxGraphDepth = int.Parse(lengths[1]);
                }
                else
                {
                    UtilityFunctions.maxNodeDepth = UtilityFunctions.stdNodeDepth;
                    UtilityFunctions.maxGraphDepth = UtilityFunctions.stdGraphDepth;
                }
            }
            catch
            {
                // could be an error but just set it to standard
                UtilityFunctions.maxNodeDepth = UtilityFunctions.stdNodeDepth;
                UtilityFunctions.maxGraphDepth = UtilityFunctions.stdGraphDepth;
            }

            string output;
            try
            {
                // output = await Narrator.getGPTResponse(prompt5, api, 100, 0.9);
                chat.AppendUserInput(prompt5);
                output = await GetGPTOutput(chat, "PlayerCharacterGen");
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get response: {e}");
            }

            Thread.Sleep(500);
            Console.Clear();

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }


            //Console.WriteLine(output);


            if (string.IsNullOrEmpty(UtilityFunctions.saveSlot)) // if testing / error
            {
                // get all save file
                string[] saves =
                    Directory.GetFiles(UtilityFunctions.mainDirectory + $@"Characters{Path.DirectorySeparatorChar}",
                        "*.xml");
                bool started = false;
                for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                {
                    if (saves.Length == i)
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
                            started = true;
                            i = UtilityFunctions.maxSaves;
                        }
                    }
                }

                if (!started)
                {
                    UtilityFunctions.TypeText(new TypeText(UtilityFunctions.Instant, UtilityFunctions.typeSpeed),
                        "No empty save slots. Exiting Test. Press any key to leave");
                    Console.ReadLine();
                    await Program.saveGameToAllStoragesAsync();
                    Environment.Exit(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.Black;
            //Console.WriteLine(UtilityFunctions.saveFile);
            //Console.WriteLine(output);


            // design xml file
            string preText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            output = await UtilityFunctions.cleanseXML(output);
            string finalXMLText = "";
            finalXMLText = preText + "\n" + output;


            try
            {
                File.Create(UtilityFunctions.saveFile).Close();
                File.WriteAllText(UtilityFunctions.saveFile, finalXMLText);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not write to file: {e}");
            }


            // Player player with attributes
            XmlSerializer serializer = new XmlSerializer(typeof(Player));
            Player loadedPlayer;
            using (TextReader reader = new StringReader(finalXMLText))
            {
                loadedPlayer = (Player)serializer.Deserialize(reader);
            }


            // set player properties
            if (loadedPlayer == null)
                throw new ArgumentNullException("Null player");

            Type playerType = typeof(Player);
            PropertyInfo[] properties = playerType.GetProperties();


            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(loadedPlayer);
                    property.SetValue(player, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in GenerateMainXML {property.Name}: {ex.Message}");
                    // Handle or log the error as necessary
                }
            }

            return player;
        }

        public async Task<EnemyFactory> initialiseEnemyFactoryFromNarrator(Conversation chat,
            EnemyFactory enemyFactory,
            AttackBehaviourFactory attackBehaviourFactory)
        {
            // function to generate a json file representing the enemies and initialise an enemyFactory
            // function to generate a json file representing the enemies and initialise an enemyFactory
            Program.logger.Info("Initialising Enemy Factory...");

            string output = "";
            try
            {
                string prompt4 = File.ReadAllText(UtilityFunctions.promptPath + "Prompt4.txt");
                chat.AppendUserInput(prompt4);
                output = await GetGPTOutput(chat, "EnemyFactory");
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
                    UtilityFunctions.enemyTemplateDir + UtilityFunctions.saveName + ".json";
                if (File.Exists(UtilityFunctions.enemyTemplateSpecificDirectory))
                {
                    File.Delete(UtilityFunctions.enemyTemplateSpecificDirectory);
                    // cahnge to throw error
                    Console.WriteLine("Old save not deleted. Deleting old save then continuing");
                }

                // deserialise file into a new EnemyFactory
            }
            catch (Exception e)
            {
                throw e;
            }

            // testing
            // Console.WriteLine(output);

            output = await UtilityFunctions.FixJson(output);

            // Console.WriteLine(output);

            // create file to be written to
            File.Create(UtilityFunctions.enemyTemplateSpecificDirectory).Close();

            //File.WriteAllText(UtilityFunctions.enemyTemplateSpecificDirectory, output);
            using (StreamWriter writer = new StreamWriter(UtilityFunctions.enemyTemplateSpecificDirectory))
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

            foreach (KeyValuePair<string, AttackInfo> attackBehaviour in attackBehaviourFactory.attackBehaviours)
            {
                // load attack behaviours into enemy templates
                foreach (KeyValuePair<string, EnemyTemplate> enemyTemplate in enemyFactoryToBeReturned
                             .enemyTemplates)
                {
                    if (enemyTemplate.Value.attackBehaviourKeys
                        .Contains(attackBehaviour
                            .Key)) // if the attack labels attached to this template contain the given label for this attackbehaviour
                    {
                        // load in the attack behaviour
                        AttackSlot? attackSlotNullable = enemyTemplate.Value.getNextAvailableAttackSlot();
                        if (attackSlotNullable == null)
                        {
                            throw new Exception("No available attack slots found for enemy template " +
                                                enemyTemplate.Value.Name);
                        }

                        AttackSlot attackSlot = (AttackSlot)attackSlotNullable; // ensure it isnt null

                        // add the attack behaviour to the enemy templat
                        enemyTemplate.Value.AttackBehaviours[attackSlot] = attackBehaviour.Value;
                    }
                }
            }

            Program.logger.Info("Enemy Factory Initialised");

            return enemyFactoryToBeReturned;
        }

        public async Task<AttackBehaviourFactory> initialiseAttackBehaviourFactoryFromNarrator(Conversation chat)
        {
            // enemy factory logic, use game setup for diverting to using api key
            AttackBehaviourFactory tempAttackBehaviourFactory = new AttackBehaviourFactory();

            string output = "";
            try
            {
                string prompt7 = File.ReadAllText(UtilityFunctions.promptPath + "Prompt7.txt");
                chat.AppendUserInput(prompt7);
                output = await GetGPTOutput(chat, "AttackBehaviourFactory");
            }
            catch (Exception e)
            {
                throw e;
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }
            // testing
            // Console.WriteLine(output);

            output = await UtilityFunctions.FixJson(output);

            // testing
            // Console.WriteLine(output);
            // assign path
            UtilityFunctions.attackBehaviourTemplateSpecificDirectory =
                UtilityFunctions.attackBehaviourTemplateDir + UtilityFunctions.saveName + ".json";

            // create file to be written to
            File.Create(UtilityFunctions.attackBehaviourTemplateSpecificDirectory).Close();

            File.WriteAllText(UtilityFunctions.attackBehaviourTemplateSpecificDirectory, output);

            // deserialise into an attackbehaviour factory
            try
            {
                string json = File.ReadAllText(UtilityFunctions.attackBehaviourTemplateSpecificDirectory);
                var settings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new LambdaJsonConverter() },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                Dictionary<string, AttackInfo> attackBehaviours =
                    JsonConvert.DeserializeObject<Dictionary<string, AttackInfo>>(json, settings);
                if (attackBehaviours == null)
                {
                    Program.logger.Info("No attack behaviours could be deserialized from the provided JSON.");
                    // attackBehaviours = new Dictionary<string, AttackInfo>();  // Initialize to prevent further errors
                    Program.logger.Info(json);
                    throw new Exception("No attack behaviours could be deserialized from the provided JSON.");
                }

                List<SerializableAttackBehaviour> items = new List<SerializableAttackBehaviour>();
                foreach (KeyValuePair<string, AttackInfo> kvp in attackBehaviours)
                {
                    items.Add(new SerializableAttackBehaviour(kvp.Key, kvp.Value));
                }

                tempAttackBehaviourFactory.InitializeFromSerializedBehaviors(items);
            }
            catch (Exception e)
            {
                throw new Exception($"An error occurred while initializing attack behaviours: {e.Message}");
            }

            if (tempAttackBehaviourFactory == null)
            {
                throw new Exception("Attack behaviour factory is null");
            }

            return tempAttackBehaviourFactory;
        }

        public async Task<StatusFactory> initialiseStatusFactoryFromNarrator(Conversation chat)
        {
            // status factory logic, use game setup for diverting to using api key
            StatusFactory tempStatusFactory = new StatusFactory();

            string output = "";
            try
            {
                string prompt8 = File.ReadAllText(UtilityFunctions.promptPath + "Prompt8.txt");
                chat.AppendUserInput(prompt8);
                output = await GetGPTOutput(chat, "StatusFactory");
            }
            catch (Exception e)
            {
                throw e;
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }
            // testing
            // Console.WriteLine(output);

            output = await UtilityFunctions.FixJson(output);
            // testing
            // Console.WriteLine(output);
            // assign path
            UtilityFunctions.statusesSpecificDirectory =
                UtilityFunctions.statusesDir + UtilityFunctions.saveName + ".json";

            // create file to be written to
            File.Create(UtilityFunctions.statusesSpecificDirectory).Close();
            File.WriteAllText(UtilityFunctions.statusesSpecificDirectory, output);
            // deserialise into a status factory
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
            Program.logger.Info("Generating Uninitialised Statuses...");

            // get all unique statuses from enemy templates

            List<string> uninitialisedStatuses = new List<string>();

            foreach (KeyValuePair<string, EnemyTemplate> enemyTemplate in Program.game.enemyFactory.enemyTemplates)
            {
                foreach (PropertyInfo property in typeof(EnemyTemplate).GetProperties())
                {
                    if (property.Name == "AttackBehaviours")
                    {
                        foreach (AttackSlot slot in Enum.GetValues(typeof(AttackSlot)))
                        {
                            if (enemyTemplate.Value.AttackBehaviours[slot] != null)
                            {
                                foreach (string statusName in enemyTemplate.Value.AttackBehaviours[slot].Statuses)
                                {
                                    List<string> statusNamesList = new List<string>();
                                    foreach (Status status1 in Program.game.statusFactory.statusList)
                                    {
                                        statusNamesList.Add(status1.Name);
                                    }

                                    Status status = new Status();

                                    try
                                    {
                                        status =
                                            Program.game.statusFactory.statusList[
                                                Array.IndexOf(statusNamesList.ToArray(), statusName)];
                                    }
                                    catch
                                    {
                                        // add to unititialsed statuses
                                        if (uninitialisedStatuses.Contains(statusName) == false)
                                        {
                                            uninitialisedStatuses.Add(statusName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (WeaponTemplate weaponTemplate in Program.game.itemFactory.weaponTemplates)
            {
                foreach (PropertyInfo property in typeof(WeaponTemplate).GetProperties())
                {
                    if (property.Name == "Statuses")
                    {
                        foreach (string statusName in weaponTemplate.StatusNames)
                        {
                            List<string> statusNamesList = new List<string>();
                            foreach (Status status1 in Program.game.statusFactory.statusList)
                            {
                                statusNamesList.Add(status1.Name);
                            }

                            Status status = new Status();

                            try
                            {
                                status =
                                    Program.game.statusFactory.statusList[
                                        Array.IndexOf(statusNamesList.ToArray(), statusName)];
                            }
                            catch
                            {
                                if (uninitialisedStatuses.Contains(statusName) == false)
                                {
                                    uninitialisedStatuses.Add(statusName);
                                }
                            }
                        }
                    }
                }
            }

            if (uninitialisedStatuses.Count == 0)
            {
                Program.logger.Info("No uninitialised statuses found");
                return;
            }

            string output = "";
            try
            {
                string prompt9 = File.ReadAllText(
                    @$"{UtilityFunctions.promptPath}Prompt9.txt");
                foreach (string uninitialisedStatus in uninitialisedStatuses)
                {
                    prompt9 += uninitialisedStatus + "\n";
                }

                chat.AppendUserInput(prompt9);
                output = await GetGPTOutput(chat, "UninitialisedStatuses");
            }
            catch (Exception e)
            {
                throw e;
            }

            output = await UtilityFunctions.FixJson(output);

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            StatusFactory? tempStatusFactory = new StatusFactory();
            tempStatusFactory =
                JsonConvert.DeserializeObject<StatusFactory>(
                    output, settings);

            if (tempStatusFactory == null)
            {
                throw new Exception("Status factory is null, in GenerateUninitialisedStatuses");
            }

            // add statuses in temp to game.StatusFactory

            foreach (Status status in tempStatusFactory.statusList)
            {
                Program.game.statusFactory.statusList.Add(status);
            }

            string newStatusFactory = JsonConvert.SerializeObject(Program.game.statusFactory);
            File.WriteAllText(UtilityFunctions.statusesSpecificDirectory, newStatusFactory);

            Program.logger.Info("Status Factory Initialised");
        }

        public async Task GenerateUninitialisedAttackBehaviours(Conversation chat)
        {
            Program.logger.Info("Generating Uninitialised Attack Behaviours...");

            List<string> uninitialisedAttackBehaviours = new List<string>();
            List<string> initialisedAttackBehaviours =
                Program.game.attackBehaviourFactory.attackBehaviours.Keys.ToList();

            foreach (WeaponTemplate weaponTemplate in Program.game.itemFactory.weaponTemplates)
            {
                // checks through the weapon attack behaviours
                foreach (PropertyInfo property in typeof(WeaponTemplate).GetProperties())
                {
                    if (property.Name == "AttackBehaviour")
                    {
                        if (initialisedAttackBehaviours.Contains(weaponTemplate.AttackBehaviour) == false )
                        {
                            uninitialisedAttackBehaviours.Add(weaponTemplate.AttackBehaviour);
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, EnemyTemplate> enemyTemplate in Program.game.enemyFactory.enemyTemplates)
            {
                // checks through enemy template attack behaviours
                foreach (PropertyInfo property in typeof(EnemyTemplate).GetProperties())
                {
                    if (property.Name == "AttackBehaviours")
                    {
                        foreach (AttackSlot slot in Enum.GetValues(typeof(AttackSlot)))
                        {
                            if (enemyTemplate.Value.AttackBehaviours[slot] != null)
                            {
                                if (initialisedAttackBehaviours.Contains(
                                        enemyTemplate.Value.AttackBehaviours[slot].Name) == false)
                                {
                                    uninitialisedAttackBehaviours.Add(enemyTemplate.Value.AttackBehaviours[slot]
                                        .Name);
                                }
                            }
                        }
                    }
                }
            }
            
            // check for null attack expressions
            List<AttackInfo> behavioursToRemove = new List<AttackInfo>();
            foreach (var attackBehaviour in Program.game.attackBehaviourFactory.attackBehaviours)
            {
                if (string.IsNullOrEmpty(attackBehaviour.Value.ExpressionString))
                {
                    uninitialisedAttackBehaviours.Add(attackBehaviour.Key);
                    behavioursToRemove.Add(attackBehaviour.Value);
                }
            }

            foreach (var behaviour in behavioursToRemove)
            {
                Program.game.attackBehaviourFactory.attackBehaviours.Remove(behaviour.Name);
            }

            if (uninitialisedAttackBehaviours.Count == 0)
            {
                Program.logger.Info("No uninitialised attack behaviours found");
                return;
            }

            string output = "";
            try
            {
                string prompt10 = File.ReadAllText(
                    @$"{UtilityFunctions.promptPath}Prompt10.txt");
                foreach (string uninitialisedAttack in uninitialisedAttackBehaviours)
                {
                    prompt10 += uninitialisedAttack + "\n";
                }

                chat.AppendUserInput(prompt10);
                output = await chat.GetResponseFromChatbotAsync();
            }
            catch (Exception e)
            {
                throw e;
            }

            output = await UtilityFunctions.FixJson(output);
            
            AttackBehaviourFactory tempAttackBehaviourFactory =
                JsonConvert.DeserializeObject<AttackBehaviourFactory>(
                    output);

            if (tempAttackBehaviourFactory == null)
            {
                throw new Exception(
                    "TempAttackBehaviourFactory factory is null, in GenerateUninitialisedAttackBehaviours");
            }

            // add statuses in temp to game.StatusFactory
            
            List<SerializableAttackBehaviour> items = new List<SerializableAttackBehaviour>();
            foreach (KeyValuePair<string, AttackInfo> kvp in tempAttackBehaviourFactory.attackBehaviours)
            {
                items.Add(new SerializableAttackBehaviour(kvp.Key, kvp.Value));
            }
            
            Program.game.attackBehaviourFactory.InitializeFromSerializedBehaviors(items);
            File.WriteAllText(UtilityFunctions.attackBehaviourTemplateSpecificDirectory, JsonConvert.SerializeObject(Program.game.attackBehaviourFactory));

            return;

            foreach (AttackInfo attackBehaviour in tempAttackBehaviourFactory.attackBehaviours.Values)
            {
                if (Program.game.attackBehaviourFactory.attackBehaviours.ContainsKey(attackBehaviour.Name))
                    Program.game.attackBehaviourFactory.attackBehaviours.Remove(attackBehaviour.Name);
                Program.game.attackBehaviourFactory.attackBehaviours.TryAdd(attackBehaviour.Name, attackBehaviour);
            }

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string newAttackBehaviourFactory =
                JsonConvert.SerializeObject(Program.game.attackBehaviourFactory, settings);

            File.WriteAllText(UtilityFunctions.attackBehaviourTemplateSpecificDirectory, newAttackBehaviourFactory);

            Program.logger.Info("Uninitialised Attack Behaviours Initialised");
        }
    }
}