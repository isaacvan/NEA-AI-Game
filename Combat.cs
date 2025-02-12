using UtilityFunctionsNamespace;
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
        public Player simPlayer { get; set; }
        public Enemy simEnemy { get; set; }


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
                Enemy e = enemyValuePair.Value;
                // cahnge stats depending on level
                int l = e.Level;
                e.Strength += l - 1;
                e.Dexterity += l - 1;
                e.Intelligence += l - 1;
                e.Constitution += l - 1;
                e.Charisma += l - 1;
                e.Health += e.Constitution * 2;
                e.ManaPoints += e.Intelligence * 2;
                enemiesAlive.Add(e, true);
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

                UtilityFunctions.TypeText(new TypeText(), UtilityFunctions.universalSeperator);
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
                                UtilityFunctions.TypeText(new TypeText(),
                                    $"You do not have enough mana to cast this skill.");
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
                UtilityFunctions.TypeText(new TypeText(),
                    $"There are {enemies.Count} enemies in this battle. Which enemy would you like to target?");
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
                        UtilityFunctions.TypeText(new TypeText(),
                            $"There are {enemies.Count} enemies in this battle. Which enemy would you like to target?");
                        int i1 = 1;
                        foreach (Enemy en in enemies.Values)
                        {
                            UtilityFunctions.TypeText(new TypeText(),
                                $"{i1}: {en.Name} - {en.currentHealth}/{en.Health}");
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
            turnCount++;

            // Get the best action using MCTS
            AttackInfo bestAttack = MCTS(enemy, player, simulations: 1000, maxDepth: 8);

            // Execute the best attack
            enemy.ExecuteAttack(bestAttack.Name, player);

            //Thread.Sleep(500);
            UtilityFunctions.clearScreen(player);
        }

        // -------------------------------------------
        // Monte Carlo Tree Search (MCTS) for Enemy AI
        // -------------------------------------------
        public AttackInfo MCTS(Enemy enemy, Player player, int simulations, int maxDepth)
        {
            Random rng = new Random();
            List<AttackInfo> possibleActions = enemy.AttackBehaviours.Values.ToList();

            Dictionary<AttackInfo, double> attackScores = new Dictionary<AttackInfo, double>();

            foreach (var attack in possibleActions)
                attackScores[attack] = 0;

            for (int i = 0; i < simulations; i++)
            {
                AttackInfo chosenAttack = possibleActions[rng.Next(possibleActions.Count)];
                double score = SimulateCombat(enemy, player, chosenAttack, maxDepth);
                attackScores[chosenAttack] += score;
            }

            // Select the best attack based on highest score
            return attackScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        // -------------------------------------------
        // Combat Simulation for MCTS
        // -------------------------------------------
        public double SimulateCombat(Enemy enemy, Player player, AttackInfo firstMove, int maxDepth)
        {
            Random rng = new Random();

            // Clone enemy and player states
            simEnemy = CloneUtility.DeepClone<Enemy>(enemy);
            simPlayer = CloneUtility.DeepClone<Player>(player);
            simEnemy.RequestingOutpOnly = true;
            simPlayer.RequestingOutpOnly = true;
            simEnemy.SimulatedCombat = true;
            simPlayer.SimulatedCombat = true;

            int depth = 0;
            simEnemy.ExecuteAttack(firstMove.Name, simPlayer, true); // Simulate the first attack
            simEnemy.RequestingOutpOnly = true;

            while (simPlayer.currentHealth > 0 && simEnemy.currentHealth > 0 && depth < maxDepth)
            {
                depth++;

                if (simPlayer.currentHealth <= 0) return 1000; // High reward for defeating player

                List<int> attackSlotsIntsUsable = simPlayer.PlayerAttacks.Where(x => x.Value != null).ToList()
                    .FindAll(x => simPlayer.PlayerAttacks[x.Key].Manacost <= simPlayer.currentMana)
                    .Select(k => (int)k.Key).ToList();
                simPlayer.ExecuteAttack((AttackSlot)attackSlotsIntsUsable[rng.Next(0, attackSlotsIntsUsable.Count)],
                    simEnemy, true);
                simPlayer.RequestingOutpOnly = true;

                if (simEnemy.currentHealth <= 0) return -1000; // High penalty for losing

                List<AttackInfo> enemyAttacks = simEnemy.AttackBehaviours.Values.ToList()
                    .FindAll(x => x.Manacost <= simEnemy.currentMana);
                AttackInfo enemyMove = enemyAttacks[rng.Next(enemyAttacks.Count)];
                simEnemy.ExecuteAttack(enemyMove.Name, simPlayer, true);
                simEnemy.RequestingOutpOnly = true;
            }

            // Reward function: prioritize high damage dealt, low damage taken
            return - (enemy.Health - simEnemy.currentHealth) + (player.Health - simPlayer.currentHealth);
        }

        // -------------------------------------------
        // Utility: Clone Enemy and Player (Avoid Mutations in Simulations)
        // -------------------------------------------
        public static class CloneUtility
        {
            public static T DeepClone<T>(T original) where T : new()
            {
                if (original == null) throw new ArgumentNullException(nameof(original));

                T clone = new T();
                foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (property.CanRead && property.CanWrite)
                    {
                        object value = property.GetValue(original);
                        property.SetValue(clone, value);
                    }
                }

                return clone;
            }
        }


        public void neutralEnemyTurnAction()
        {
            turnCount++;

            // threshoolds for state based approach
            bool isLowHealth = enemy.currentHealth < (enemy.Health * 0.3);
            bool isHighMana = enemy.currentMana > (enemy.ManaPoints * 0.7);
            bool isPlayerLowHealth = player.currentHealth < (player.Health * 0.3);
            bool isPlayerDebuffed = player.statusMap.Count > 0;

            List<AttackInfo> attacks = enemy.AttackBehaviours.Values.ToList();
            AttackInfo basicAttack = attacks.FindAll(x => x.AttackType == "Attack").MinBy(x => x.Manacost) ??
                                     throw new Exception("Enemy doesn't have an attack?"); // cheapest attack
            AttackInfo? heal = attacks.Find(x => x.AttackType == "Heal") ?? null; // heal
            AttackInfo largeAttack = attacks.FindAll(x => x.AttackType == "Attack").MaxBy(x => x.Manacost) ??
                                     throw new Exception("Enemy doesn't have an attack?"); // most expensive attack
            AttackInfo? buff = attacks.Find(x => x.AttackType == "Buff") ?? null;

            // state-based decision making
            if (isLowHealth && isHighMana)
            {
                // enemy is low on health but has sufficient mana, it might heal itself
                // execute a healing ability

                if (heal == null)
                {
                    enemy.ExecuteAttack(basicAttack.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(heal.Name, player);
                }
            }
            else if (isPlayerLowHealth)
            {
                // player is weak, the enemy might attempt a finishing move
                // execute a strong attack

                enemy.ExecuteAttack(largeAttack.Name, player);
            }
            else if (isPlayerDebuffed)
            {
                // If the player has active debuffs, take advantage of the situation
                // execute a debuff-enhanced attack or buff myself

                if (enemy.currentMana > largeAttack.Manacost)
                {
                    enemy.ExecuteAttack(largeAttack.Name, player);
                }
                else if (buff != null)
                {
                    enemy.ExecuteAttack(buff.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(basicAttack.Name, player);
                }
            }
            else if (enemy.currentMana < (enemy.ManaPoints * 0.2))
            {
                // If low on mana, the enemy might conserve it or use a basic attack
                // execute a basic attack

                enemy.ExecuteAttack(basicAttack.Name, player);
            }
            else
            {
                // Default behavior: a standard attack
                // execute a random attack

                var rng = new Random();
                List<AttackInfo> randomisedAttacks = attacks.OrderBy(x => rng.Next()).ToList();
                bool attackExecuted = false;
                foreach (var attack in attacks)
                {
                    if (enemy.currentMana > attack.Manacost && !attackExecuted)
                    {
                        enemy.ExecuteAttack(attack.Name, player);
                        attackExecuted = true;
                    }
                }
            }

            //Thread.Sleep(1500);
            UtilityFunctions.clearScreen(player);
        }


        public void timidEnemyTurnAction()
        {
            turnCount++;

            // Thresholds for state-based approach
            bool isLowHealth = enemy.currentHealth < (enemy.Health * 0.4);
            bool isHighMana = enemy.currentMana > (enemy.ManaPoints * 0.6);
            bool isVeryLowHealth = enemy.currentHealth < (enemy.Health * 0.2);
            bool isPlayerLowHealth = player.currentHealth < (player.Health * 0.3);
            bool isPlayerAggressive = enemy.currentHealth > (enemy.Health * 0.7) &&
                                      player.currentHealth < (player.Health * 0.5);
            bool isManaLow = enemy.currentMana < (enemy.ManaPoints * 0.2);
            bool hasBuffs = enemy.statusMap.Count > 0;

            List<AttackInfo> attacks = enemy.AttackBehaviours.Values.ToList();
            AttackInfo basicAttack = attacks.FindAll(x => x.AttackType == "Attack").MinBy(x => x.Manacost) ??
                                     throw new Exception("Enemy doesn't have an attack?");
            AttackInfo? heal = attacks.Find(x => x.AttackType == "Heal") ?? null;
            AttackInfo? buff = attacks.Find(x => x.AttackType == "Buff") ?? null;
            AttackInfo? defenseBuff = attacks.Find(x => x.AttackType == "Defense") ?? null;
            AttackInfo? manaRestore = attacks.Find(x => x.AttackType == "ManaRegen") ?? null;
            AttackInfo? escape = attacks.Find(x => x.AttackType == "Escape") ?? null;

            // State-based decision making
            if (isVeryLowHealth)
            {
                // If very low on health, prioritize escaping or healing
                if (escape != null)
                {
                    enemy.ExecuteAttack(escape.Name, player);
                }
                else if (heal != null && enemy.currentMana > heal.Manacost)
                {
                    enemy.ExecuteAttack(heal.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(basicAttack.Name, player); // Last resort attack
                }
            }
            else if (isLowHealth)
            {
                // If low on health but not critical, attempt healing or buffing defense
                if (heal != null && enemy.currentMana > heal.Manacost)
                {
                    enemy.ExecuteAttack(heal.Name, player);
                }
                else if (defenseBuff != null && enemy.currentMana > defenseBuff.Manacost)
                {
                    enemy.ExecuteAttack(defenseBuff.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(basicAttack.Name, player);
                }
            }
            else if (isManaLow)
            {
                // If mana is running low, conserve it or regenerate if possible
                if (manaRestore != null && enemy.currentMana > manaRestore.Manacost)
                {
                    enemy.ExecuteAttack(manaRestore.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(basicAttack.Name, player);
                }
            }
            else if (isPlayerAggressive)
            {
                // If player is getting aggressive, play defensively with buffs or a counterattack
                if (buff != null && enemy.currentMana > buff.Manacost)
                {
                    enemy.ExecuteAttack(buff.Name, player);
                }
                else
                {
                    enemy.ExecuteAttack(basicAttack.Name, player);
                }
            }
            else
            {
                // Default behavior: cautious attacks with mana conservation
                var rng = new Random();
                List<AttackInfo> randomisedAttacks = attacks.OrderBy(x => rng.Next()).ToList();
                foreach (var attack in randomisedAttacks)
                {
                    if (enemy.currentMana > attack.Manacost && attack.AttackType != "Escape")
                    {
                        enemy.ExecuteAttack(attack.Name, player);
                        break;
                    }
                }
            }

            //Thread.Sleep(1500);
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