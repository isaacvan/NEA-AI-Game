﻿using PlayerClassesNamespace;
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
using TestNarratorNamespace;

namespace GameClassNamespace
{
    public class Game
    {
        public Player player { get; set; }
        public ItemFactory itemFactory { get; set; }
        public EnemyFactory enemyFactory { get; set; }
        public AttackBehaviourFactory attackBehaviourFactory { get; set; }
        public StatusFactory statusFactory { get; set; }
        public Combat currentCombat { get; set; }

        public async Task initialiseGame(GameSetup gameSetup, bool testing = false)
        {
            // initialise api & chat
            OpenAIAPI api = Narrator.initialiseGPT();
            Conversation chat = Narrator.initialiseChat(api);
            GameSetup normalNarrator = new Narrator();

            // initialise itemFactory & player from api. Gets UtilityFunctions.loadedSave
            itemFactory = new ItemFactory();
            player = await Program.initializeSaveAndPlayer(gameSetup, api, chat, testing);

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
                
                // initialise map
                
                
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
                await player.initialiseInventory();
                await player.initialiseEquipment();

                // load enemyFactory
                enemyFactory =
                    await loadGame.initialiseEnemyFactoryFromNarrator(chat, enemyFactory, attackBehaviourFactory);

                // load map
                
                
                Console.WriteLine("Loaded save.");
            }
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