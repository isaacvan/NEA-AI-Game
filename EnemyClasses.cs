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
    }

    // ENEMY FACTORY USAGE
    // Enemy enemy = game.enemyFactory.CreateEnemy(game.enemyFactory.enemyTemplates[0], 1, new Point(0, 0));
    // ^ - To create an enemy of template[0], level 1 at (0, 0)
    // enemy.AttackBehaviours[AttackSlot.slot1] gives the Name, Expression and Statuses of the attack at a given slot
    
    public class EnemyFactory
    {
        public List<string> enemyTypes { get; set; }
        //[JsonPropertyName("enemy")]
        public List<EnemyTemplate> enemyTemplates { get; set; }
    

        public Enemy CreateEnemy(EnemyTemplate enemyTemplate, int level, Point pos)
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
                AttackBehaviours = enemyTemplate.AttackBehaviours
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
        public List<string> AttackBehaviourKeys { get; set; } = new List<string>();
        public Nature nature { get; set; }
        
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
        
        public void RegisterAttackBehaviour(string key, string expression, List<string> statuses, string narrative)
        {
            var parameters = new[] { new Parameter("target", typeof(Player)) };
            Lambda parsedScript = UtilityFunctions.interpreter.Parse(expression, parameters);
        
            AttackInfo attackInfo = new AttackInfo(parsedScript, parsedScript.ToString(), statuses, key, narrative);
            attackBehaviours[key] = attackInfo;
        }
        
        public void ExecuteAttack(string key, Player target)
        {
            if (attackBehaviours.TryGetValue(key, out var attackInfo))
            {
                attackInfo.Expression.Invoke(target); // Execute the script
                // Optionally handle modifiers here or within the script itself
                foreach (var effect in attackInfo.Statuses)
                {
                    Program.logger.Info($"Applying effect: {effect}");
                }
            }
            else
            {
                Program.logger.Info($"No attack behavior found for key: {key}");
            }
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
                RegisterAttackBehaviour(behaviour.Key, behaviour.AttackInfo.Expression.ToString(), behaviour.AttackInfo.Statuses, behaviour.AttackInfo.Narrative);
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
        
        public List<string> Statuses { get; set; }
        public string Narrative { get; set; }

        public AttackInfo(Lambda expression, string expressionString, List<string> statuses, string name, string narrative)
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