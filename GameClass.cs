using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using CombatNamespace;
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
        public EnemyFactory enemyFactory { get; set; }
        public AttackBehaviourFactory attackBehaviourFactory { get; set; }
        public StatusFactory statusFactory { get; set; }

        public async Task initialiseGame(GameSetup gameSetup, bool testing = false)
        {
            // initialise api & chat
            OpenAIAPI api = Narrator.initialiseGPT();
            Conversation chat = Narrator.initialiseChat(api);

            // initialise itemFactory & player from api. Gets UtilityFunctions.loadedSave
            itemFactory = new ItemFactory();
            player = await Program.initializeSaveAndPlayer(gameSetup, api, chat, testing);

            // initialise attack behaviours
            attackBehaviourFactory = await gameSetup.initialiseAttackBehaviourFactoryFromNarrator(chat);

            // initialise statuses and effects of the attack behaviours. Ensure everything is covered
            statusFactory = await gameSetup.initialiseStatusFactoryFromNarrator(chat);

            // fill itemFactory
            await itemFactory.initialiseItemFactoryFromNarrator(api, chat, testing);

            // initialise inventory & equipment to XML
            await player.initialiseInventory();
            await player.initialiseEquipment();

            // rewrite player class to XML
            await player.updatePlayerStatsXML();

            // initialise & fill enemyFactory
            enemyFactory =
                await gameSetup.initialiseEnemyFactoryFromNarrator(chat, enemyFactory, attackBehaviourFactory);
            
            // error-checking to ensure statuses are initialised
            await gameSetup.GenerateUninitialisedStatuses(chat);

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