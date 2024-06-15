using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Emgu.CV.Ocl;
using EnemyClassesNamespace;
using MainNamespace;

namespace CombatNamespace
{
    public class Combat
    {
        public Player player { get; set; }
        public Enemy? enemy { get; set; }
        public Dictionary<int, Enemy> enemies { get; set; } // key is ID, value is Enemy
        public int turnCount { get; set; } = 1;
        public int playerTurn { get; set; } = 1;
        public int enemyTurn { get; set; } = 2;
        public int enemiesTurnCount { get; set; } = 1; // count of which enemy's turn it is
        public bool playerTurnBool { get; set; } = true;
        public bool enemyTurnBool { get; set; } = false;
        public bool playerAlive { get; set; } = true;
        public bool enemyAlive { get; set; } = true;
        public bool playerWon { get; set; } = false;
        public bool enemyWon { get; set; } = false;
        public bool playerDied { get; set; } = false;
        public bool enemyDied { get; set; } = false;
        public bool playerRan { get; set; } = false;
        public bool enemyRan { get; set; } = false;
        // more

        public Combat(Player player, Dictionary<int, Enemy> enemies)
        {
            if (enemies.Count == 0)
            {
                throw new Exception("No enemies provided.");
            } else if (enemies.Count == 1)
            {
                this.enemy = enemies[0];
            } else if (enemies.Count > 1)
            {
                this.enemies = enemies;
            }
            if (player != null)
            {
                this.player = player;
            }
            else
            {
                throw new Exception("Player cannot be null.");
            }
        }

        public bool beginCombat()
        {
            while (playerAlive && enemyAlive) {
                if (playerTurnBool) {
                    playerTurnAction();
                    
                    playerTurnBool = false;
                } else if (enemyTurnBool) {
                    for (int i = 0; i < enemies.Keys.Count; i++) {
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
                }
            }

            if (playerAlive && !enemyAlive) {
                playerWon = true;
                return playerWon;
            } else if (!playerAlive && enemyAlive) {
                enemyWon = true;
                return false;
            }
            else
            {
                MainNamespace.Program.logger.Info(
                    $"Unexpected result. playerAlive: {playerAlive}, enemyAlive: {enemyAlive}");
                return false;
            }
        }
        
        public void playerTurnAction()
        {
            // player turn logic
            turnCount++;

            string turnResult = playerTurnUI();
            // more
        }

        public string playerTurnUI()
        {
            return "";
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
            
            // more
        }

        public void displayCombatUI()
        {
            // combat ui
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
            if (crit >= r.Next(0, 100)) {
                return true;
            }
            return false;
        }
    }

    public class StatusFactory
    {
        public List<Status> statusList { get; set; } = new List<Status>();
        
        public static Status CreateStatus(string name, string duration, string type, bool increase, bool percentBool, int? intensityNumber, int? intensityPercent, bool stackable, bool refreshable, int chanceToApplyPercent, string description) 
        {
            if (percentBool)
            {
                if (intensityPercent == null) {
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