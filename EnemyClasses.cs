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
                Position = pos
            };

            return enemy;
        }

        public async Task initialiseEnemyFactory(GameSetup gameSetup, Conversation chat)
        {
            // enemy factory logic, use game setup for diverting to using api key
            EnemyFactory tempEnemyFactory;
            tempEnemyFactory = await gameSetup.initialiseEnemyFactoryFromNarrator(chat, this); // get gpt to write an json file. if testing, this will verify the existance of a test json file.
            
            // load temp Enemy factory properties into THIS
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
        public Dictionary<AttackSlot, AttackInfo> AttackBehaviours { get; private set; } // Dictionary to store attack behaviours for each slotAttackBehaviours { get; set; }
        
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
        
        public void InitializeEnemyTemplate(AttackBehaviourFactory attackBehaviourFactory)
        {
            var enemyTemplate = new EnemyTemplate();
            
            // register attack behaviours
            var magicSpell = attackBehaviourFactory.GetAttackInfo("MagicSpell");
            var healingSpell = attackBehaviourFactory.GetAttackInfo("HealingSpell");

            // Assigning attacks to specific slots
            enemyTemplate.AssignAttackBehavior(AttackSlot.slot1, magicSpell);
            enemyTemplate.AssignAttackBehavior(AttackSlot.slot2, healingSpell);

            // Add additional logic as necessary for more slots or different enemies
        }
    }

    public class AttackBehaviourFactory
    {
        public Dictionary<string, AttackInfo> attackBehaviours = new Dictionary<string, AttackInfo>();
        
        public void RegisterAttackBehaviour(string key, string expression, List<string> nicheEffects)
        {
            var parameters = new[] { new Parameter("target", typeof(Player)) };
            Lambda parsedScript = UtilityFunctions.interpreter.Parse(expression, parameters);
        
            AttackInfo attackInfo = new AttackInfo(parsedScript, nicheEffects);
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
        public Lambda Expression { get; set; }
        public List<string> NicheEffects { get; set; }

        public AttackInfo(Lambda expression, List<string> nicheEffects)
        {
            Expression = expression;
            NicheEffects = nicheEffects;
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