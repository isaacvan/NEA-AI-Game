using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using GameClassNamespace;
using GPTControlNamespace;
using Newtonsoft.Json;
using OpenAI_API.Chat;

namespace EnemyClassesNamespace
{
    public enum AttackSlots
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

        public Enemy CreateEnemy(string name, int health, int manaPoints, int strength, int intelligence, int dex, int constitution, int charisma, List<string> attackTypes, int lvl, Point pos)
        {
            Enemy enemy = new Enemy
            {
                Name = name,
                Health = health,
                currentHealth = health,
                ManaPoints = manaPoints,
                currentMana = manaPoints,
                Strength = strength,
                Intelligence = intelligence,
                Dexterity = dex,
                Constitution = constitution,
                Charisma = charisma,
                Level = lvl,
                Position = pos,
                AttackBehaviours = new Dictionary<AttackSlots, IAttackBehaviour>
                {
                    { AttackSlots.slot1, AttackBehaviourFactory.CreateAttackBehaviour(attackTypes[0]) },
                    { AttackSlots.slot2, AttackBehaviourFactory.CreateAttackBehaviour(attackTypes[1]) },
                    { AttackSlots.slot3, AttackBehaviourFactory.CreateAttackBehaviour(attackTypes[2]) },
                    { AttackSlots.slot4, AttackBehaviourFactory.CreateAttackBehaviour(attackTypes[3]) }
                }
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
        
        [Newtonsoft.Json.JsonIgnore]
        public AttackSlots AttackSlot { get; set; }
        
        public Dictionary<AttackSlots, IAttackBehaviour> AttackBehaviours { get; set; }
    }

    public class DynamicAttack : IAttackBehaviour
    {
        private readonly Action _attackAction;

        public DynamicAttack(Action attackAction)
        {
            _attackAction = attackAction;
        }

        public void Attack()
        {
            _attackAction();
        }
    }

    public class AttackBehaviourFactory
    {
        private static readonly Dictionary<string, Func<IAttackBehaviour>> attackBehaviours = new Dictionary<string, Func<IAttackBehaviour>>();

        public static void RegisterAttackBehaviour(string attackType, Func<IAttackBehaviour> createBehaviour)
        {
            if (!attackBehaviours.ContainsKey(attackType))
            {
                attackBehaviours[attackType] = createBehaviour;
            }
        }

        public static IAttackBehaviour CreateAttackBehaviour(string attackType)
        {
            if (attackBehaviours.ContainsKey(attackType))
            {
                return attackBehaviours[attackType]();
            }

            throw new ArgumentException("Invalid attack type");
        }
    }

    public interface IAttackBehaviour
    {
        void Attack();
    }
}