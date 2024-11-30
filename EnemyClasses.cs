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
        
        public void ReceiveAttack(int damage, int crit = 20, int manacost = 0) // DYNAMICEXPRESSO
        {
            if (Program.game.currentCombat != null)
            {
                bool didCrit = Program.game.currentCombat.didCrit(this, crit);
                if (didCrit)
                {
                    damage *= 2;
                }
                Console.WriteLine($"{Name} took {damage} damage.");
                currentHealth -= damage;
                Thread.Sleep(1000);
                Console.Clear();
            }
            else
            {
                Program.logger.Error("No current combat. Attempt to receive attack failed.");
            }
        }
        
        public void ExecuteAttack(string key, Player target)
        {
            if (Program.game.attackBehaviourFactory.attackBehaviours.TryGetValue(key, out var attackInfo))
            {
                try
                {
                    // Debugging: Ensure expression and parameters are correct
                    var expression = attackInfo.Expression; // e.g., "target.ReceiveAttack(20, 25)"
                    var parameters = new[] { new Parameter("target", typeof(Player)) };

                    // Debugging: Log the expression and parameter information
                    Program.logger.Info($"Expression to parse: {expression}");
                    Program.logger.Info($"Target type: {target.GetType().FullName}");

                    // Parse the expression with the correct context
                    var attackExpression = UtilityFunctions.interpreter.Parse(expression.ExpressionText, parameters);

                    // Invoke the parsed expression
                    attackExpression.Invoke(target); // Execute the script
                    currentMana -= attackInfo.Manacost;
                    if (currentMana < 0)
                    {
                        throw new Exception("Not enough mana to cast. error check didnt work. at executeattack in enemy");
                    }

                    // Optionally handle modifiers here or within the script itself
                    foreach (var effect in attackInfo.Statuses)
                    {
                        Program.logger.Info($"Applying effect: {effect}");
                        // APPLY STATUSES
                    }
                }
                catch (Exception ex)
                {
                    Program.logger.Error($"Error invoking attack expression: {ex.Message}");
                    Program.logger.Error($"Stack Trace: {ex.StackTrace}");
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
    }

    public interface EnemyConfig // used to store functions that each nature needs that is different
    {
        Point GetEnemyMovement(Point oldPoint, ref Game game);
    }

    public class TimidContainer : EnemyConfig
    {
        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            return new Point();
        }
    }
    
    public class NeutralContainer : EnemyConfig
    {
        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            // 50% CHANCE THEY DONT MOVE
            Random random = new Random();
            if (random.NextDouble() > 0.5)
            {
                return oldPoint;
            }
            
            bool valid = false;
            Point newPoint = UtilityFunctions.ClonePoint(oldPoint);
            List<List<Tile>> tiles = game.map.GetCurrentNode().tiles;
            // up is 1, right is 2, down is 3, left is 4
            while (!valid)
            {
                int nextMove = random.Next(1, 5);
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
                            newPoint = oldPoint;
                        }
                    }
                    catch
                    {
                        valid = false;
                    }
                }
            }
            
            return newPoint;
        }
    }
    
    public class AggressiveContainer : EnemyConfig
    {
        public Point GetEnemyMovement(Point oldPoint, ref Game game)
        {
            return new Point();
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
    

        public Enemy CreateEnemy(EnemyTemplate enemyTemplate, int level, Point pos, int ID)
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
                nature = enemyTemplate.nature,
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
        public Dictionary<AttackSlot, AttackInfo> AttackBehaviours { get; set; } = new Dictionary<AttackSlot, AttackInfo>(); // Dictionary to store attack behaviours for each slotAttackBehaviours { get; set; }
        public List<string> attackBehaviourKeys { get; set; } = new List<string>();
        public Nature nature { get; set; }
        [Newtonsoft.Json.JsonIgnore] public Dictionary<string, Status> statusMap { get; set; } = new Dictionary<string, Status>();
        
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
        
        public void RegisterAttackBehaviour(string key, string expression, List<string> statuses, string narrative, Type targetType, int manacost)
        {
            Parameter[] parameters = null;
            if (targetType == typeof(Player))
            {
                parameters = new[] { new Parameter("target", typeof(Player)) };
            } else if (targetType == typeof(Enemy))
            {
                parameters = new[] { new Parameter("target", typeof(Enemy)) };
            }
            else
            {
                throw new Exception("Invalid target type in attackbehaviourfactory");
            }
            Lambda parsedScript = UtilityFunctions.interpreter.Parse(expression, parameters);
        
            AttackInfo attackInfo = new AttackInfo(parsedScript, parsedScript.ToString(), statuses, key, narrative,  manacost);
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
                return null; // Or handle this case as needed, perhaps throwing an exception or returning a default value
            }
        }
        
        public void InitializeFromSerializedBehaviors(List<SerializableAttackBehaviour> behaviours)
        {
            foreach (var behaviour in behaviours)
            {
                RegisterAttackBehaviour(behaviour.Key, behaviour.AttackInfo.Expression.ToString(), behaviour.AttackInfo.Statuses, behaviour.AttackInfo.Narrative, typeof(Player), behaviour.AttackInfo.Manacost);
            }
        }
    }
    
    public class AttackInfo
    {
        public string Name { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        public Lambda Expression { get; set; }
       
        //[Newtonsoft.Json.JsonConverter(typeof(ExpressionConverter))] 
        public string ExpressionString { get; set; }
        
        public int Manacost { get; set; }
        
        public List<string> Statuses { get; set; }
        public string Narrative { get; set; }

        public AttackInfo(Lambda expression, string expressionString, List<string> statuses, string name, string narrative, int manacost)
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
        }
    }
    
    public class SerializableAttackBehaviour // helper class to help serialisation in json
    {
        public string Key { get; set; }
        public AttackInfo AttackInfo { get; set; }

        public SerializableAttackBehaviour(string Key, AttackInfo  AttackInfo)
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

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization of Expression is not supported.");
        }

        public override bool CanRead => false;
    }
}