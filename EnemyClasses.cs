using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CombatNamespace;
using DynamicExpresso;
using GameClassNamespace;
using GPTControlNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API.Chat;

namespace EnemyClassesNamespace
{
    public enum AttackSlot
    {
        slot1,
        slot2,
        slot3,
        slot4
    }

    public enum Nature
    {
        aggressive,
        neutral,
        timid
    }

    public class Enemy : EnemyTemplate
    {
        public int currentHealth { get; set; }
        public int currentMana { get; set; }
        public int Level { get; set; }
        public Point Position { get; set; }
        public int Id { get; set; }
        [Newtonsoft.Json.JsonIgnore] public bool RequestingOutpOnly { get; set; } = false;
        [Newtonsoft.Json.JsonIgnore] public bool SimulatedCombat { get; set; } = false;


        public int ReceiveAttack(int damage, int crit = 20, int manacost = 0) // DYNAMICEXPRESSO
        {
            if (Program.game.currentCombat != null)
            {
                // Add strength + dex + int / 3
                if (!SimulatedCombat)
                {
                    var plyr = Program.game.player;
                    damage += (plyr.Strength + plyr.Dexterity + plyr.Intelligence) / 3;
                }
                else
                {
                    var plyr = Program.game.currentCombat.simPlayer;
                    damage += (plyr.Strength + plyr.Dexterity + plyr.Intelligence) / 3;
                }


                bool didCrit = Program.game.currentCombat.didCrit(this, crit);
                if (didCrit)
                {
                    damage *= 2;
                }

                if (!RequestingOutpOnly)
                {
                    UtilityFunctions.TypeText(new TypeText(),
                        $"Your {Program.game.player.Class} {Program.game.player.PlayerAttacks[Program.game.currentCombat.lastSlotUsed].Narrative}");
                    UtilityFunctions.TypeText(new TypeText(), $"{Name} took {damage} damage.");
                }
                    
                currentHealth -= damage;
                // Thread.Sleep(1000);
                //UtilityFunctions.clearScreen(Program.game.player);

                return damage;
            }
            else
            {
                Program.logger.Error("No current combat. Attempt to receive attack failed.");
                throw new Exception("No current combat.");
            }
        }

        public void ExecuteAttack(string key, Player target, bool requestingOutpOnly = false)
        {
            if (Program.game.attackBehaviourFactory.attackBehaviours.TryGetValue(key, out var attackInfo))
            {
                try
                {
                    if (!requestingOutpOnly) UtilityFunctions.TypeText(new TypeText(), $"Enemy used {key}!");

                    // Debugging: Ensure expression and parameters are correct
                    var expression = attackInfo.Expression; // e.g., "target.ReceiveAttack(20, 25)"
                    var parameters = new[] { new Parameter("target", typeof(Player)) };

                    // Debugging: Log the expression and parameter information
                    if (!requestingOutpOnly) Program.logger.Info($"Expression to parse: {expression}");
                    if (!requestingOutpOnly) Program.logger.Info($"Target type: {target.GetType().FullName}");

                    // Parse the expression with the correct context
                    var attackExpression = UtilityFunctions.interpreter.Parse(expression.ExpressionText, parameters);

                    // Invoke the parsed expression
                    if (currentMana - attackInfo.Manacost < 0)
                    {
                        if (!requestingOutpOnly) UtilityFunctions.TypeText(new TypeText(), $"The enemy could not afford to cast...\n+10 mana");
                        currentMana += 10;
                    }
                    else
                    {
                        if (requestingOutpOnly) target.RequestingOutpOnly = true;
                        attackExpression.Invoke(target); // Execute the script
                        target.RequestingOutpOnly = false;
                        
                        currentMana -= attackInfo.Manacost;
                    }

                    // Optionally handle modifiers here or within the script itself
                    foreach (var effect in attackInfo.Statuses)
                    {
                        if (!requestingOutpOnly) Program.logger.Info($"Applying effect: {effect}");
                        // APPLY STATUSES
                    }

                    if (!requestingOutpOnly) UtilityFunctions.TypeText(new TypeText(), "\n\nPress any key to continue...");
                    if (!requestingOutpOnly) Console.ReadKey(true);
                    if (!requestingOutpOnly) UtilityFunctions.clearScreen(target);
                }
                catch (Exception ex)
                {
                    Program.logger.Error($"Error invoking attack expression: {ex.Message}");
                    Program.logger.Error($"Stack Trace: {ex.StackTrace}");
                    throw new Exception($"Check logs: {ex.Message}");
                }
            }
            else
            {
                Program.logger.Info($"No attack behavior found for key: {key}");
            }
        }
    }

    public class EnemySpawn
    {
        public Point spawnPoint { get; set; } // null once spawned
        public Nature nature { get; set; }
        public string name { get; set; }
        public bool boss { get; set; }
        public int id { get; set; }
        public Point currentLocation { get; set; } // null when not spawned yet
        public bool alive { get; set; }
    }

    public interface EnemyConfig // used to store functions that each nature needs that is different
    {
        Point GetEnemyMovement(Point oldPoint, ref Game game);
        int EnemyMovementLogic(Point playerPos, Game game, Point currentEnemyPos);
    }

    public class TimidContainer : EnemyConfig
    {
        public static Random random = new Random();

        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            bool valid = false;
            Point newPoint = UtilityFunctions.ClonePoint(oldPoint);
            List<List<Tile>> tiles = game.map.GetCurrentNode().tiles;
            // up is 1, right is 2, down is 3, left is 4

            int maxAttempts = 100; // Prevent infinite loops
            int attempts = 0;

            while (!valid && attempts < maxAttempts)
            {
                int nextMove = this.EnemyMovementLogic(game.player.playerPos, game, oldPoint);
                string input = "";
                switch (nextMove)
                {
                    case 1:
                        newPoint.Y -= 1;
                        input = "w";
                        break;
                    case 2:
                        newPoint.X += 1;
                        input = "d";
                        break;
                    case 3:
                        newPoint.Y += 1;
                        input = "s";
                        break;
                    case 4:
                        newPoint.X -= 1;
                        input = "a";
                        break;
                    case -1:
                        return oldPoint; // dont move
                }

                if (GridFunctions.CheckIfOutOfBounds(
                        game.map.Graphs[game.map.Graphs.Count - 1]
                            .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles,
                        oldPoint, input[0].ToString()))
                {
                    Tile possibleTile;
                    try
                    {
                        possibleTile = tiles[newPoint.X][newPoint.Y];
                        if (possibleTile.walkable == true && possibleTile.enemyOnTile == null &&
                            possibleTile.tileDesc == "Empty")
                        {
                            valid = true;
                        }
                        else
                        {
                            valid = false;
                            newPoint = UtilityFunctions.ClonePoint(oldPoint);
                        }
                    }
                    catch
                    {
                        valid = false;
                    }
                }
                else
                {
                }
                // Thread

                attempts++;
            }

            return newPoint;
        }

        public int EnemyMovementLogic(Point playerPos, Game game, Point currentEnemyPos)
        {
            var tiles = game.map.GetCurrentNode().tiles;


            int sightRadius = 5;
            int distanceToPlayer =
                Math.Abs(playerPos.X - currentEnemyPos.X) + Math.Abs(playerPos.Y - currentEnemyPos.Y);

            if (distanceToPlayer > sightRadius)
            {
                return random.Next(1, 5);
            }

            // now that enemy is within radius where they are running
            // 20% CHANCE THEY DONT MOV (so can be caught)
            if (random.NextDouble() > 0.8)
            {
                return -1;
            }

            int horizontalDistance = playerPos.X - currentEnemyPos.X;
            int verticalDistance = playerPos.Y - currentEnemyPos.Y;

            if (Math.Abs(horizontalDistance) > Math.Abs(verticalDistance))
            {
                if (horizontalDistance > 0 && currentEnemyPos.X + 1 < tiles.Count &&
                    tiles[currentEnemyPos.X + 1][currentEnemyPos.Y].walkable)
                    return 4;
                else if (horizontalDistance < 0 && currentEnemyPos.X - 1 >= 0 &&
                         tiles[currentEnemyPos.X - 1][currentEnemyPos.Y].walkable)
                    return 2;
            }

            if (verticalDistance > 0 && currentEnemyPos.Y + 1 < tiles[0].Count &&
                tiles[currentEnemyPos.X][currentEnemyPos.Y + 1].walkable)
                return 1;
            else if (verticalDistance < 0 && currentEnemyPos.Y - 1 >= 0 &&
                     tiles[currentEnemyPos.X][currentEnemyPos.Y - 1].walkable)
                return 3;

            return -1;
        }
    }

    public class NeutralContainer : EnemyConfig
    {
        public static Random random = new Random();

        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            // Static Random to prevent reinitialization

            // 50% chance the enemy doesn't move
            if (random.NextDouble() > 0.5)
            {
                return oldPoint;
            }

            bool valid = false;
            Point newPoint = new Point(oldPoint.X, oldPoint.Y); // Avoid unnecessary cloning
            var currentNode = game.map.GetCurrentNode(); // Cache frequently used structure
            List<List<Tile>> tiles = currentNode.tiles;

            int maxAttempts = 100; // Prevent infinite loops
            int attempts = 0;

            while (!valid && attempts < maxAttempts)
            {
                attempts++;

                int nextMove = EnemyMovementLogic(game.player.playerPos, game, oldPoint);
                string input = "";
                switch (nextMove)
                {
                    case 1: // Up
                        newPoint.Y -= 1;
                        input = "w";
                        break;
                    case 2: // Right
                        newPoint.X += 1;
                        input = "d";
                        break;
                    case 3: // Down
                        newPoint.Y += 1;
                        input = "s";
                        break;
                    case 4: // Left
                        newPoint.X -= 1;
                        input = "a";
                        break;
                }

                // Validate move
                if (GridFunctions.CheckIfOutOfBounds(tiles, oldPoint, input))
                {
                    try
                    {
                        Tile possibleTile = tiles[newPoint.X][newPoint.Y];
                        if (possibleTile.walkable && possibleTile.enemyOnTile == null &&
                            possibleTile.tileDesc == "Empty")
                        {
                            valid = true;
                        }
                        else
                        {
                            valid = false;
                            newPoint = new Point(oldPoint.X, oldPoint.Y); // Reset to original
                        }
                    }
                    catch
                    {
                        valid = false;
                        newPoint = new Point(oldPoint.X, oldPoint.Y); // Reset to original
                    }
                }
            }

            // Fallback if no valid move found
            if (!valid)
            {
                return oldPoint;
            }

            return newPoint;
        }


        public int EnemyMovementLogic(Point playerPos, Game game, Point currentEnemyPos)
        {
            return random.Next(1, 5);
        }
    }

    public class AggressiveContainer : EnemyConfig
    {
        public static Random random = new Random();

        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            bool valid = false;
            Point newPoint = UtilityFunctions.ClonePoint(oldPoint);
            List<List<Tile>> tiles = game.map.GetCurrentNode().tiles;
            // up is 1, right is 2, down is 3, left is 4

            int attempts = 0;
            int maxAttempts = 100;

            while (!valid && attempts < maxAttempts)
            {
                int nextMove = EnemyMovementLogic(game.player.playerPos, game, oldPoint);
                string input = "";
                switch (nextMove)
                {
                    case 1:
                        newPoint.Y -= 1;
                        input = "w";
                        break;
                    case 2:
                        newPoint.X += 1;
                        input = "d";
                        break;
                    case 3:
                        newPoint.Y += 1;
                        input = "s";
                        break;
                    case 4:
                        newPoint.X -= 1;
                        input = "a";
                        break;
                    case -1:
                        return oldPoint; // dpont move
                }

                if (GridFunctions.CheckIfOutOfBounds(
                        game.map.Graphs[game.map.Graphs.Count - 1]
                            .Nodes[game.map.Graphs[game.map.Graphs.Count - 1].CurrentNodePointer].tiles,
                        oldPoint, input[0].ToString()))
                {
                    Tile possibleTile;
                    try
                    {
                        possibleTile = tiles[newPoint.X][newPoint.Y];
                        if (possibleTile.walkable == true && possibleTile.enemyOnTile == null &&
                            possibleTile.tileDesc == "Empty")
                        {
                            valid = true;
                        }
                        else
                        {
                            valid = false;
                            newPoint = UtilityFunctions.ClonePoint(oldPoint);
                        }
                    }
                    catch
                    {
                        valid = false;
                    }
                }

                attempts++;
            }

            return newPoint;
        }

        public int EnemyMovementLogic(Point playerPos, Game game, Point currentEnemyPos)
        {
            var tiles = game.map.GetCurrentNode().tiles;

            int sightRadius = 10;
            int distanceToPlayer =
                Math.Abs(playerPos.X - currentEnemyPos.X) + Math.Abs(playerPos.Y - currentEnemyPos.Y);

            if (distanceToPlayer > sightRadius)
            {
                return random.Next(1, 5);
            }

            // 20% CHANCE THEY DONT MOVE (so player can escape sometimes)
            if (random.NextDouble() > 0.8)
            {
                return -1;
            }

            int horizontalDistance = playerPos.X - currentEnemyPos.X;
            int verticalDistance = playerPos.Y - currentEnemyPos.Y;

            if (Math.Abs(horizontalDistance) > Math.Abs(verticalDistance))
            {
                if (horizontalDistance > 0 && currentEnemyPos.X + 1 < tiles.Count &&
                    tiles[currentEnemyPos.X + 1][currentEnemyPos.Y].walkable)
                    return 2;
                else if (horizontalDistance < 0 && currentEnemyPos.X - 1 >= 0 &&
                         tiles[currentEnemyPos.X - 1][currentEnemyPos.Y].walkable)
                    return 4;
            }

            if (verticalDistance > 0 && currentEnemyPos.Y + 1 < tiles[0].Count &&
                tiles[currentEnemyPos.X][currentEnemyPos.Y + 1].walkable)
                return 3;
            else if (verticalDistance < 0 && currentEnemyPos.Y - 1 >= 0 &&
                     tiles[currentEnemyPos.X][currentEnemyPos.Y - 1].walkable)
                return 1;

            return -1;
        }
    }

    // ENEMY FACTORY USAGE
    // Enemy enemy = game.enemyFactory.CreateEnemy(game.enemyFactory.enemyTemplates[0], 1, new Point(0, 0));
    // ^ - To create an enemy of template[0], level 1 at (0, 0)
    // enemy.AttackBehaviours[AttackSlot.slot1] gives the Name, Expression and Statuses of the attack at a given slot

    public class EnemyFactory
    {
        public List<string> enemyTypes { get; set; }

        //[JsonPropertyName("enemy")]
        public Dictionary<string, EnemyTemplate> enemyTemplates { get; set; }


        public Enemy CreateEnemy(EnemyTemplate enemyTemplate, int level, Point pos, int ID, Nature? nature = null, bool boss = false)
        {
            Enemy enemy = new Enemy
            {
                Name = enemyTemplate.Name,
                Health = enemyTemplate.Health,
                currentHealth = enemyTemplate.Health,
                ManaPoints = enemyTemplate.ManaPoints,
                currentMana = enemyTemplate.ManaPoints,
                Strength = enemyTemplate.Strength,
                Intelligence = enemyTemplate.Intelligence,
                Dexterity = enemyTemplate.Dexterity,
                Constitution = enemyTemplate.Constitution,
                Charisma = enemyTemplate.Charisma,
                Level = level,
                Position = pos,
                AttackBehaviours = enemyTemplate.AttackBehaviours,
                nature = nature ?? (boss ? Nature.aggressive : enemyTemplate.nature),
                Id = ID
            };

            return enemy;
        }
    }

    public class EnemyTemplate
    {
        public string Name { get; set; }
        public int Health { get; set; }
        public int ManaPoints { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }

        public Dictionary<AttackSlot, AttackInfo> AttackBehaviours { get; set; } =
            new Dictionary<AttackSlot, AttackInfo>(); // Dictionary to store attack behaviours for each slotAttackBehaviours { get; set; }

        public List<string> attackBehaviourKeys { get; set; } = new List<string>();
        public Nature nature { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, Status> statusMap { get; set; } = new Dictionary<string, Status>();

        public EnemyTemplate()
        {
            AttackBehaviours[AttackSlot.slot1] = null;
            AttackBehaviours[AttackSlot.slot2] = null;
            AttackBehaviours[AttackSlot.slot3] = null;
            AttackBehaviours[AttackSlot.slot4] = null;
        }

        public void AssignAttackBehavior(AttackSlot slot, AttackInfo behavior)
        {
            if (AttackBehaviours[slot] == null)
            {
                AttackBehaviours[slot] = behavior; // Replace the existing behavior
            }
            else
            {
                throw new Exception($"Attack behavior for slot {slot} already exists.");
            }
        }

        public AttackSlot? getNextAvailableAttackSlot()
        {
            if (AttackBehaviours[AttackSlot.slot1] == null)
                return AttackSlot.slot1;
            else if (AttackBehaviours[AttackSlot.slot2] == null)
                return AttackSlot.slot2;
            else if (AttackBehaviours[AttackSlot.slot3] == null)
                return AttackSlot.slot3;
            else if (AttackBehaviours[AttackSlot.slot4] == null)
                return AttackSlot.slot4;
            else
                return null;
        }
    }

    public class AttackBehaviourFactory
    {
        public Dictionary<string, AttackInfo> attackBehaviours = new Dictionary<string, AttackInfo>();

        [Newtonsoft.Json.JsonConstructor]
        public AttackBehaviourFactory()
        {
        }

        public void RegisterAttackBehaviour(string key, string expression, List<string> statuses, string narrative,
            Type targetType, int manacost, string attackType)
        {
            Parameter[] parameters = null;
            if (targetType == typeof(Player))
            {
                parameters = new[] { new Parameter("target", typeof(Player)) };
            }
            else if (targetType == typeof(Enemy))
            {
                parameters = new[] { new Parameter("target", typeof(Enemy)) };
            }
            else
            {
                throw new Exception("Invalid target type in attackbehaviourfactory");
            }

            Lambda parsedScript = UtilityFunctions.interpreter.Parse(expression, parameters);

            AttackInfo attackInfo =
                new AttackInfo(parsedScript, parsedScript.ToString(), statuses, key, narrative, manacost, attackType);
            attackBehaviours[key] = attackInfo;
        }

        public AttackInfo GetAttackInfo(string key)
        {
            if (attackBehaviours.TryGetValue(key, out AttackInfo attackInfo))
            {
                return attackInfo;
            }
            else
            {
                Console.WriteLine($"No attack behavior found for key: {key}");
                return
                    null; // Or handle this case as needed, perhaps throwing an exception or returning a default value
            }
        }

        public void InitializeFromSerializedBehaviors(List<SerializableAttackBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null || (behaviour.AttackInfo.ExpressionString == null && behaviour.AttackInfo.Expression == null))
                {
                    Program.logger.Info(
                        $"Null behaviour spotted: {behaviour.AttackInfo.Name ?? "Fully Null"}. Will attempt fix in uninitialised fix");
                }
                else
                {
                    RegisterAttackBehaviour(behaviour.Key,
                        behaviour.AttackInfo.ExpressionString ?? behaviour.AttackInfo.Expression.ToString(),
                        behaviour.AttackInfo.Statuses, behaviour.AttackInfo.Narrative, typeof(Player),
                        behaviour.AttackInfo.Manacost, behaviour.AttackInfo.AttackType);
                }
            }
        }
    }

    public class AttackInfo
    {
        public string Name { get; set; }

        [Newtonsoft.Json.JsonConverter(typeof(LambdaJsonConverter))] public Lambda Expression { get; set; }

        //[Newtonsoft.Json.JsonConverter(typeof(ExpressionConverter))] 
        public string ExpressionString { get; set; }

        public int Manacost { get; set; }

        public List<string> Statuses { get; set; }
        public string Narrative { get; set; }
        public string AttackType { get; set; } // Attack, Heal, Buff, Debuff

        public AttackInfo()
        {
        }

        public AttackInfo(Lambda expression, string expressionString, List<string> statuses, string name,
            string narrative, int manacost, string attackType)
        {
            Expression = expression;
            ExpressionString = expressionString;
            if (ExpressionString == null)
            {
                Program.logger.Info("ExpressionString is null");
            }

            Statuses = statuses;
            Name = name;
            Narrative = narrative;
            Manacost = manacost;
            AttackType = attackType;
        }
    }

    public class SerializableAttackBehaviour // helper class to help serialisation in json
    {
        public string Key { get; set; }
        public AttackInfo AttackInfo { get; set; }

        public SerializableAttackBehaviour(string Key, AttackInfo AttackInfo)
        {
            this.Key = Key;
            this.AttackInfo = AttackInfo;
        }
    }

    public class ExpressionConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Expression) || objectType.IsSubclassOf(typeof(Expression));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Expression exp = value as Expression;
            if (exp != null)
            {
                writer.WriteValue(exp.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization of Expression is not supported.");
        }

        public override bool CanRead => false;
    }
}