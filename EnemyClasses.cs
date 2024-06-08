using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
    // enemy.AttackBehaviours[AttackSlot.slot1] gives the Name, Expression and NicheEffects of the attack at a given slot
    
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
        public Dictionary<AttackSlot, AttackInfo> AttackBehaviours { get; set; } // Dictionary to store attack behaviours for each slotAttackBehaviours { get; set; }
        public List<string> AttackBehaviourKeys { get; set; } = new List<string>();
        
        public EnemyTemplate()
        {
            AttackBehaviours = new Dictionary<AttackSlot, AttackInfo>();
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
            if (!AttackBehaviours.ContainsKey(AttackSlot.slot1))
                return AttackSlot.slot1;
            else if (!AttackBehaviours.ContainsKey(AttackSlot.slot2))
                return AttackSlot.slot2;
            else if (!AttackBehaviours.ContainsKey(AttackSlot.slot3))
                return AttackSlot.slot3;
            else if (!AttackBehaviours.ContainsKey(AttackSlot.slot4))
                return AttackSlot.slot4;
            else
                return null;
        }
    }

    public class AttackBehaviourFactory
    {
        public Dictionary<string, AttackInfo> attackBehaviours = new Dictionary<string, AttackInfo>();
        
        public void RegisterAttackBehaviour(string key, string expression, List<string> nicheEffects)
        {
            var parameters = new[] { new Parameter("target", typeof(Player)) };
            Lambda parsedScript = UtilityFunctions.interpreter.Parse(expression, parameters);
        
            AttackInfo attackInfo = new AttackInfo(parsedScript, nicheEffects, key);
            attackBehaviours[key] = attackInfo;
        }
        
        public void ExecuteAttack(string key, Player target)
        {
            if (attackBehaviours.TryGetValue(key, out var attackInfo))
            {
                attackInfo.Expression.Invoke(target); // Execute the script
                // Optionally handle modifiers here or within the script itself
                foreach (var effect in attackInfo.NicheEffects)
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
                RegisterAttackBehaviour(behaviour.Key, behaviour.AttackInfo.Expression.ToString(), behaviour.AttackInfo.NicheEffects);
            }
        }
    }
    
    public class AttackInfo
    {
        public string Name { get; set; }
        public Lambda Expression { get; set; }
        public List<string> NicheEffects { get; set; }

        public AttackInfo(Lambda expression, List<string> nicheEffects, string name)
        {
            Expression = expression;
            NicheEffects = nicheEffects;
            Name = name;
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
}