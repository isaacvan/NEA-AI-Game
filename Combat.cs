﻿using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using DynamicExpresso;
using Emgu.CV.Ocl;
using EnemyClassesNamespace;
using MainNamespace;
using NLog;
using Program = MainNamespace.Program;

namespace CombatNamespace
{
    /*
     * USAGE:
     * Enemy enemy =
                        game.enemyFactory.CreateEnemy(game.enemyFactory.enemyTemplates["Rogue AI"], 1, new Point(0, 0));
                    Enemy enemy2 = game.enemyFactory.CreateEnemy(game.enemyFactory.enemyTemplates["Rogue AI"], 1, new Point(0, 0));
                    List<Enemy> enemiesToFight = new List<Enemy>() { enemy, enemy2 };
                    bool outcome = game.startCombat(enemiesToFight);
     */
    public class Combat
    {
        public Player player { get; set; }
        public Enemy enemy { get; set; }
        public Dictionary<int, Enemy> enemies { get; set; } // key is ID, value is Enemy
        public Dictionary<Enemy, bool> enemiesAlive = new Dictionary<Enemy, bool>();
        public int turnCount { get; set; } = 1;
        public int playerTurn { get; set; } = 1;
        public int enemyTurn { get; set; } = 2;
        public int enemiesTurnCount { get; set; } = 1; // count of which enemy's turn it is
        public bool playerTurnBool { get; set; } = true;
        public bool enemyTurnBool { get; set; } = false;
        public bool playerAlive { get; set; } = true;
        public AttackSlot lastSlotUsed { get; set; }


        public enum CombatMenuState
        {
            Player,
            Inventory,
            Enemy,
            Run
        }

        public Combat(Player playerInp, Dictionary<int, Enemy> enemiesInp)
        {
            if (enemiesInp.Count == 0)
            {
                throw new Exception("No enemies provided.");
            }
            else if (enemiesInp.Count >= 1)
            {
                this.enemy = enemiesInp[0];
                this.enemies = enemiesInp;
            }

            if (playerInp != null)
            {
                this.player = playerInp;
            }
            else
            {
                throw new Exception("Player cannot be null.");
            }

            foreach (KeyValuePair<int, Enemy> enemyValuePair in enemiesInp)
            {
                enemiesAlive.Add(enemyValuePair.Value, true);
            }
        }

        public bool beginCombat()
        {
            Program.logger.Info($"Combat started. Player vs {enemies.Count} enemies.");
            foreach (KeyValuePair<int, Enemy> enemy in enemies)
            {
                Program.logger.Info($"Name: {enemy.Value.Name}, Nature: {enemy.Value.nature.ToString()}");
            }

            bool check = checkForEndOfCombat();

            while (!check)
            {
                if (playerTurnBool)
                {
                    playerTurnAction();
                    enemyTurnBool = true;
                    playerTurnBool = false;
                }
                else if (enemyTurnBool && !playerTurnBool)
                {
                    for (int i = 0; i < enemies.Keys.Count; i++)
                    {
                        enemy = enemies[i];
                        switch (enemies[i].nature.ToString().ToLower())
                        {
                            case "aggressive":
                                aggressiveEnemyTurnAction();
                                break;
                            case "neutral":
                                neutralEnemyTurnAction();
                                break;
                            case "timid":
                                timidEnemyTurnAction();
                                break;
                            default:
                                throw new Exception("Invalid enemy nature.");
                        }
                    }

                    enemyTurnBool = false;
                    playerTurnBool = true;
                }

                check = checkForEndOfCombat();
            }

            if (playerAlive && !enemiesAlive.ContainsValue(true))
            {
                return true; // player won
            }
            else if (!playerAlive)
            {
                return false; // enemy(s) won
            }
            else
            {
                MainNamespace.Program.logger.Info(
                    $"Unexpected result. playerAlive: {playerAlive}, enemyAlive: {enemiesAlive.ContainsValue(true)}");
                return false;
            }
        }

        public bool checkForEndOfCombat()
        {
            foreach (Enemy enemy in enemiesAlive.Keys)
            {
                if (enemy.currentHealth <= 0)
                {
                    enemiesAlive[enemy] = false;
                }
            }
            
            if (player.currentHealth <= 0)
            {
                // player is dead
                player.PlayerDies();
                return true;
            }
            else if (player.currentHealth > 0 && !enemiesAlive.ContainsValue(true))
            {
                // player has won
                return true;
            }
            else if (player.currentHealth > 0 && enemiesAlive.ContainsValue(true))
            {
                // continue combat
                return false;
            }
            else
            {
                return false;
            }
        }

        public void playerTurnAction()
        {
            // player turn logic
            turnCount++;

            Program.game.uiConstructer.displayCombatUI(enemies);

            // more

            AttackInfo? attackInfo = null;
            bool valid = false;
            int slotIndex = 1;
            while (!valid)
            {
                // UI
                int i = 1;
                // UtilityFunctions.TypeText(new TypeText(), )
                UtilityFunctions.TypeText(new TypeText(), "These are your attacks:");
                foreach (AttackInfo attack in player.PlayerAttacks.Values)
                {
                    if (attack != null)
                    {
                        UtilityFunctions.TypeText(new TypeText(newLine: true), $"Slot #{i} ---> {attack.Name}");
                        i++;
                    }
                }
                
                UtilityFunctions.TypeText(new TypeText(), "\n" + UtilityFunctions.universalSeperator);
                UtilityFunctions.TypeText(new TypeText(), "Please enter an attack");
                
                try
                {
                    int input = Convert.ToInt16(Console.ReadKey(true).KeyChar.ToString());
                    if (input < 1 || input > player.PlayerAttacks.Values.Count)
                    {
                        throw null;
                    }
                    else
                    {
                        AttackSlot attackSlot = (AttackSlot)Enum.Parse(typeof(AttackSlot), $"slot{input}");
                        this.lastSlotUsed = attackSlot;
                        attackInfo = player.PlayerAttacks[attackSlot];
                        IEnumerable<Parameter> parameters = attackInfo.Expression.DeclaredParameters;
                        try
                        {
                            if (player.currentMana - Convert.ToInt16(parameters.Last().Value) < 0)
                            {
                                // player doesnt have enough mana
                                UtilityFunctions.TypeText(new TypeText(), $"You do not have enough mana to cast this skill.");
                            }
                            else
                            {
                                slotIndex = input;
                                valid = true;
                            }
                        }
                        catch
                        {
                            Program.logger.Info("Last param in receiveAttack() isn't manacost. changed by accident?");
                        }

                        
                    }
                }
                catch
                {
                    UtilityFunctions.clearScreen(player);
                    UtilityFunctions.TypeText(new TypeText(), UtilityFunctions.universalSeperator);
                    UtilityFunctions.TypeText(new TypeText(), 
                        $"Invalid input. Input should range from 1 - {player.PlayerAttacks.Values.Count}.");
                }
            }


            if (attackInfo == null)
            {
                throw new Exception("Attack info cannot be null. In PlayerTurnAction.");
            }

            Enemy target = null;

            if (enemies.Count > 1) // AND ISNT AOE TARGETTING
            {
                UtilityFunctions.clearScreen(player);
                UtilityFunctions.TypeText(new TypeText(), $"There are {enemies.Count} enemies in this battle. Which enemy would you like to target?");
                int i = 1;
                foreach (Enemy en in enemies.Values)
                {
                    UtilityFunctions.TypeText(new TypeText(), $"{i}: {en.Name} - {en.currentHealth}/{en.Health}");
                    i++;
                }

                bool v = false;
                while (!v)
                {
                    try
                    {
                        int inp = Convert.ToInt16(Console.ReadLine());
                        UtilityFunctions.clearScreen(player);
                        if (inp > enemies.Count || inp < 1)
                        {
                            throw null;
                        }

                        try
                        {
                            target = enemies[inp - 1];
                        }
                        catch
                        {
                            throw new Exception("External error");
                        }

                        v = true;
                    }
                    catch
                    {
                        UtilityFunctions.TypeText(new TypeText(), "Invalid input.");
                        Thread.Sleep(500);
                        UtilityFunctions.clearScreen(player);
                        UtilityFunctions.TypeText(new TypeText(), $"There are {enemies.Count} enemies in this battle. Which enemy would you like to target?");
                        int i1 = 1;
                        foreach (Enemy en in enemies.Values)
                        {
                            UtilityFunctions.TypeText(new TypeText(), $"{i1}: {en.Name} - {en.currentHealth}/{en.Health}");
                            i1++;
                        }
                    }
                }
            }

            if (target == null)
            {
                target = (Enemy)enemy;
            }
            
            UtilityFunctions.clearScreen(this.player);

            UtilityFunctions.TypeText(new TypeText(), $"You used {attackInfo.Name}!");
            Enum.TryParse($"Slot{slotIndex}", out AttackSlot slot);
            player.ExecuteAttack(slot, target);
        }


        public void aggressiveEnemyTurnAction()
        {
            // enemy turn logic. Implement logic based off enemy natures.
            turnCount++;

            // more
        }

        public void neutralEnemyTurnAction()
        {
            // enemy turn logic. Implement logic based off enemy natures.
            turnCount++;

            // more
        }

        public void timidEnemyTurnAction()
        {
            // enemy turn logic
            turnCount++;
            Enemy thisEnemy = (Enemy)enemy;
            // more
            string chosenAttack = thisEnemy.AttackBehaviours[AttackSlot.slot1].Name;
            Console.WriteLine($"Enemy used {chosenAttack}!");
            thisEnemy.ExecuteAttack(chosenAttack, player);
            Thread.Sleep(1500);
            UtilityFunctions.clearScreen(player);
        }


        public bool didCrit(object caster, int crit)
        {
            // logic that assesses caster stats to increase crit %
            switch (caster.GetType().Name.ToLower())
            {
                case "player":
                    // player crit logic
                    break;
                case "enemy":
                    // enemy logic
                    break;
                default:
                    throw new Exception("Invalid caster type.");
            }

            // more logic

            Random r = new Random();
            if (crit >= r.Next(0, 100))
            {
                return true;
            }

            return false;
        }

        public int DamageConverterFromLevel(int damage, int level)
        {
            if (level >= 1 && level <= 10)
            {
                try
                {
                    double lvlDouble = Convert.ToDouble(level);
                    double mult = (double)(lvlDouble / 10);
                    return Convert.ToInt16(Convert.ToDouble(damage) * mult);
                }
                catch
                {
                    throw new Exception("float to int multiplication thrown error in damageConverter in combat.cs");
                }
            }

            if (level > 10 && level <= 20)
            {
                try
                {
                    double lvlDouble = Convert.ToDouble(level);
                    double mult = (double)(lvlDouble / 10);
                    return Convert.ToInt16(Convert.ToDouble(damage) * mult);
                }
                catch
                {
                    throw new Exception("float to int multiplication thrown error in damageConverter in combat.cs");
                }
            }

            return damage;
        }
    }

    public class StatusFactory
    {
        public List<Status> statusList { get; set; } = new List<Status>();

        public static Status CreateStatus(string name, string duration, string type, bool increase, bool percentBool,
            int? intensityNumber, int? intensityPercent, bool stackable, bool refreshable, int chanceToApplyPercent,
            string description)
        {
            if (percentBool)
            {
                if (intensityPercent == null)
                {
                    throw new Exception("Intensity percent cannot be null if percent bool is true.");
                }
                else
                {
                    intensityNumber = null;
                    intensityPercent = (int)(intensityPercent / 100);
                }
            }
            else
            {
                if (intensityNumber == null)
                {
                    throw new Exception("Intensity number cannot be null if percent bool is false.");
                }
                else
                {
                    intensityPercent = null;
                    intensityNumber = (int)intensityNumber;
                }
            }

            return new Status
            {
                Name = name,
                Duration = int.Parse(duration),
                Type = type,
                PercentBool = percentBool,
                Increase = increase,
                IntensityNumber = intensityNumber,
                IntensityPercent = intensityPercent,
                Stackable = stackable,
                Refreshable = refreshable,
                ChanceToApplyPercent = chanceToApplyPercent,
                Description = description
            };
        }
    }

    public class Status
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public string Type { get; set; }
        public bool Increase { get; set; }
        public bool PercentBool { get; set; }
        public int? IntensityNumber { get; set; }
        public int? IntensityPercent { get; set; }
        public bool Stackable { get; set; }
        public bool Refreshable { get; set; }
        public int ChanceToApplyPercent { get; set; }
        public string Description { get; set; }
    }
}