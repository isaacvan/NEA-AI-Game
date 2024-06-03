using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using System.Collections.Generic;
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
            OpenAIAPI api = Narrator.initialiseGPT();
            Conversation chat = Narrator.initialiseChat(api);
            itemFactory = new ItemFactory();
            player = await Program.initializeSaveAndPlayer(gameSetup, api, chat, testing);
            await itemFactory.initialiseItemFactoryFromNarrator(api, chat, testing);
            await player.initialiseInventory();
            await player.initialiseEquipment();
            
            
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
}

