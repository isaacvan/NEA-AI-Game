using EnemyClassesNamespace;
using UtilityFunctionsNamespace;
using GPTControlNamespace;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Cryptography;
using System.Dynamic;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Reflection;
using CombatNamespace;
using DynamicExpresso;
using Emgu.CV.Dnn;
using GameClassNamespace;
using ItemFunctionsNamespace;
using MainNamespace;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenAI_API;
using OpenAI_API.Chat;

namespace PlayerClassesNamespace
{
    [Serializable]
    public class Player
    {
        public string Class { get; set; }
        public string Race { get; set; }
        public int Health { get; set; }
        public int currentHealth { get; set; }
        public int ManaPoints { get; set; }
        public int currentMana { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }

        [XmlIgnore] public Dictionary<AttackSlot, AttackInfo> PlayerAttacks { get; set; } =
            new Dictionary<AttackSlot, AttackInfo>();
        public int Level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public Point playerPos;

        [XmlIgnore] public Inventory inventory { get; set; } = new Inventory();

        [XmlIgnore] public Equipment equipment { get; set; } = new Equipment();

        [XmlIgnore] public Dictionary<string, Status> statusMap { get; set; } = new Dictionary<string, Status>();

        public Player()
        {
            maxExp = 10;
            Level = 1;
            currentExp = 0;
            playerPos = new Point(0, 0);
            PlayerAttacks[AttackSlot.slot1] = null;
            PlayerAttacks[AttackSlot.slot2] = null;
            PlayerAttacks[AttackSlot.slot3] = null;
            PlayerAttacks[AttackSlot.slot4] = null;
        }
        
        public void EquipItem(EquippableItem.EquipLocation slot, Item item)
        {
            equipment.EquipItem(slot, item, inventory);
        }

        public void UnequipItem(EquippableItem.EquipLocation slot)
        {
            equipment.UnequipItem(slot, inventory);
        }

        public void AddItem(Item item)
        {
            inventory.AddItem(item);
        }
        
        public void RemoveItem(Item item)
        {
            inventory.RemoveItem(item);
        }

        public void ReceiveAttack(int damage, int crit = 20, int manacost = 0) // DYNAMICEXPRESSO
        {
            if (Program.game.currentCombat != null)
            {
                damage = Program.game.currentCombat.DamageConverterFromLevel(damage, Level);
                bool didCrit = Program.game.currentCombat.didCrit(Program.game.currentCombat.enemy, crit);
                if (didCrit)
                {
                    damage *= 2;
                }
                Console.WriteLine($"You have taken {damage} damage.");
                currentHealth -= damage;
                if (currentHealth < 0)
                {
                    currentHealth = 0;
                    PlayerDies();
                }
            }
            else
            {
                Program.logger.Error("No current combat. Attempt to receive attack failed.");
            }
        }
        
        public void ExecuteAttack(string key, Enemy target)
        {
            if (Program.game.attackBehaviourFactory.attackBehaviours.TryGetValue(key, out var attackInfo))
            {
                attackInfo.Expression.Invoke(target); // Execute the script
                currentMana -= attackInfo.Manacost;
                if (currentMana < 0)
                {
                    throw new Exception("Not enough mana to cast. error check didnt work. at executeattack in player");
                }
                

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

        public void PlayerDies()
        {
            Console.Clear();
            Console.WriteLine("You have died. Game over.");
            Thread.Sleep(3000);
            Environment.Exit(0);
        }

        public void ApplyStatus(string statusName, int turns) // DYNAMICEXPRESSO
        {
            Status status = new Status();
            
        }

        public async Task writePlayerAttacksToJSON()
        {
            string path = UtilityFunctions.playerAttacksSpecificDirectory;
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter> { new ExpressionConverter(), new StringEnumConverter() },
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            try
            {
                string json = JsonConvert.SerializeObject(PlayerAttacks, settings);
                using (var writer = new StreamWriter(path))
                {
                    await writer.WriteAsync(json);
                }
                Program.logger.Info("Player attacks data has been written to JSON successfully.");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to write player attacks to JSON: " + ex.Message);
            }
        }

        public async Task<Dictionary<AttackSlot, AttackInfo>> readPlayerAttacksFromJSON()
        {
            string path = UtilityFunctions.playerAttacksSpecificDirectory;
            try
            {
                // deserialise from path into PlayerAttacks
                string json = await File.ReadAllTextAsync(path);
                PlayerAttacks = JsonConvert.DeserializeObject<Dictionary<AttackSlot, AttackInfo>>(json);
                return PlayerAttacks;
            }
            catch (Exception e)
            {
                throw new Exception($"Error reading from JSON file in readPlayerAttacksFromJSON: {e}");
            }
        }

        public async Task initialiseInventory()
        {
            await inventory.updateInventoryJSON();
        }
        
        public async Task initialiseEquipment()
        {
            await equipment.updateEquipmentJSON();
        }

        public async Task updatePlayerStatsXML()
        {
            string path = UtilityFunctions.saveFile;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error writing to XML file in updatePlayerStatsXML: {e}");
            }
        }
        
        public void updatePlayerStatsXMLSync()
        {
            string path = UtilityFunctions.saveFile;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error writing to XML file in updatePlayerStatsXML: {e}");
            }
        }

        public async Task initialisePlayerFromNarrator(GameSetup gameSetup, OpenAIAPI api, Conversation chat, bool testing = false)
        {
            Program.logger.Info("Creating Character...");


            // load prompt 5
            string prompt5 = "";
            Console.ForegroundColor = ConsoleColor.Black;
            try
            {
                prompt5 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt5.txt");
            }
            catch (Exception e)
            {
                throw new Exception($"Could not find prompt file: {e}");
            }

            // get response from GPT
            string output = "";
            Player tempPlayer = await gameSetup.generateMainXml(chat, prompt5, this);
            //Console.WriteLine($"TempPlayer stat charisma: {tempPlayer.Charisma}");
            
            // assign this player to tempPlayer
            PropertyInfo[] properties = typeof(Player).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(tempPlayer);
                    PropertyInfo thisProperty = this.GetType().GetProperty(property.Name);

                    if (thisProperty != null && thisProperty.CanWrite)
                    {
                        thisProperty.SetValue(this, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in initialisePlayerFromNarrator {property.Name}: {ex.Message}");
                }
            }

            // set character health to max
            currentHealth = Health;
            currentMana = ManaPoints;
            // this.
            Program.logger.Info("Character Created");
            Console.ForegroundColor = ConsoleColor.Black;
        }


        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }


        public void checkForLevelUp()
        {
            /*while (currentExp >= maxExp)
            {
                levelUp();
            }*/
        }
    }
}