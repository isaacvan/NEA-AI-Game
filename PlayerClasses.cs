using EnemyClassesNamespace;
using UtilityFunctionsNamespace;
using GPTControlNamespace;
using System.Drawing;
using System.Xml.Serialization;
using System.Reflection;
using CombatNamespace;
using DynamicExpresso;
using GameClassNamespace;
using GridConfigurationNamespace;
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

        public string[] StatNames { get; set; }

        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }

        [XmlIgnore]
        public Dictionary<AttackSlot, AttackInfo> PlayerAttacks { get; set; } =
            new Dictionary<AttackSlot, AttackInfo>();

        public int Level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public Point playerPos;

        [XmlIgnore] public Inventory inventory { get; set; } = new Inventory();

        [XmlIgnore] public Equipment equipment { get; set; } = new Equipment();

        [XmlIgnore] public Dictionary<string, Status> statusMap { get; set; } = new Dictionary<string, Status>();

        [XmlIgnore] public int sightRange { get; set; }
        [XmlIgnore] public bool sightRangeModified { get; set; }
        [XmlIgnore] public int sightRangeModifiedBy { get; set; }
        [XmlIgnore] public bool RequestingOutpOnly { get; set; } = false;
        [XmlIgnore] public bool SimulatedCombat { get; set; } = false;

        public Player()
        {
            maxExp = 10;
            Level = 1;
            currentExp = 0;
            sightRange = 6;
            PlayerAttacks[AttackSlot.slot1] = null;
            PlayerAttacks[AttackSlot.slot2] = null;
            PlayerAttacks[AttackSlot.slot3] = null;
            PlayerAttacks[AttackSlot.slot4] = null;
            sightRangeModified = false;
            if (StatNames == null || StatNames.Length == 0)
            {
                StatNames = new string[]
                    { "Strength", "Dexterity", "Constitution", "Intelligence", "Charisma" };
            }
        }

        public void amendStats(Game game)
        {
            foreach (EquippableItem.EquipLocation loc in Enum.GetValues(typeof(EquippableItem.EquipLocation)))
            {
                if (equipment.EquipmentEffectsApplied.Count == 0)
                {
                    equipment.initialiseEquipmentEffectsApplied();
                }
                if (!equipment.EquipmentEffectsApplied[loc])
                {
                    if ((equipment.ArmourSlots.ContainsKey(loc) && equipment.ArmourSlots[loc] != null) ||
                        (equipment.WeaponSlots.ContainsKey(loc) && equipment.WeaponSlots[loc] != null))
                    {
                        // apply effect
                        if (loc == EquippableItem.EquipLocation.Head || loc == EquippableItem.EquipLocation.Body ||
                            loc == EquippableItem.EquipLocation.Legs)
                        {
                            // access armours
                            Armour fullArmourDetails = (Armour)game.itemFactory.createItem(
                                game.itemFactory.armourTemplates.Find(x => x.Name == equipment.ArmourSlots[loc].Name));
                            string statBonusInStr = fullArmourDetails.UniqueProperties;
                            foreach (string stat in StatNames)
                            {
                                if (statBonusInStr.ToLower().Contains(stat.ToLower()))
                                {
                                    // get and evaluate num
                                    if (int.TryParse(statBonusInStr.Substring(1, 1), out int value))
                                    {
                                        foreach (PropertyInfo info in typeof(Player).GetProperties())
                                        {
                                            if (info.Name.ToLower().Contains(stat.ToLower()))
                                            {
                                                if (int.TryParse(Convert.ToString(info.GetValue(this)),
                                                        out int prevValue))
                                                {
                                                    info.SetValue(this, prevValue + value);
                                                    equipment.EquipmentEffectsApplied[loc] = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // access other
                            Weapon fullWeaponDetails = (Weapon)game.itemFactory.createItem(
                                game.itemFactory.weaponTemplates.Find(x => x.Name == equipment.WeaponSlots[loc].Name));
                            string statBonusInStr = fullWeaponDetails.UniqueProperties;
                            foreach (string stat in StatNames)
                            {
                                if (statBonusInStr.ToLower().Contains(stat.ToLower()))
                                {
                                    // get and evaluate num
                                    if (int.TryParse(statBonusInStr.Substring(1, 1), out int value))
                                    {
                                        foreach (PropertyInfo info in typeof(Player).GetProperties())
                                        {
                                            if (info.Name.ToLower().Contains(stat.ToLower()))
                                            {
                                                int.TryParse(Convert.ToString(info.GetValue(this)), out int prevValue);
                                                info.SetValue(this, prevValue + value);
                                                equipment.EquipmentEffectsApplied[loc] = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void amendWeaponAttacks(Game game)
        {
            if (equipment.WeaponSlots[EquippableItem.EquipLocation.Weapon] == null) return;
            AttackInfo attack =
                game.attackBehaviourFactory.GetAttackInfo(equipment.WeaponSlots[EquippableItem.EquipLocation.Weapon]
                    .AttackBehaviour);
            if (!PlayerAttacks.ContainsValue(attack))
            {
                List<AttackSlot> freeSlots = new List<AttackSlot>();
                foreach (var kvp in PlayerAttacks)
                {
                    if (kvp.Value == null)
                    {
                        freeSlots.Add(kvp.Key);
                    }
                }

                PlayerAttacks[freeSlots.First()] = attack;
            }
        }

        public async Task InitialiseAttacks(Game game)
        {
            PlayerAttacks = JsonConvert.DeserializeObject<Dictionary<AttackSlot, AttackInfo>>(
                File.ReadAllText($"{UtilityFunctions.playerAttacksDir}{UtilityFunctions.saveName}.json"));

            foreach (AttackInfo attackInfo in PlayerAttacks.Values)
            {
                if (attackInfo != null)
                {
                    attackInfo.Expression = game.attackBehaviourFactory.GetAttackInfo(attackInfo.Name).Expression;
                    Type t = attackInfo.Expression.DeclaredParameters.First().Type;
                    Parameter[] parameters = null;
                    if (t == typeof(Player))
                    {
                        parameters = new[] { new Parameter("target", typeof(Enemy)) };
                        Lambda parsedScript =
                            UtilityFunctions.interpreter.Parse(attackInfo.ExpressionString, parameters);
                        var newExpression =
                            new AttackInfo(parsedScript, parsedScript.ToString(), attackInfo.Statuses, attackInfo.Name,
                                attackInfo.Narrative, attackInfo.Manacost, attackInfo.AttackType).Expression;
                        attackInfo.Expression = newExpression;
                    }
                }
            }
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

        public int ReceiveAttack(int damage, int crit = 20, int manacost = 0) // DYNAMICEXPRESSO
        {
            if (Program.game.currentCombat != null)
            {
                // Add strength + dex + int / 3
                if (!SimulatedCombat)
                {
                    var enem = Program.game.currentCombat.enemy;
                    damage += (enem.Strength + enem.Dexterity + enem.Intelligence) / 3;
                }
                else
                {
                    var enem = Program.game.currentCombat.simEnemy;
                    damage += (enem.Strength + enem.Dexterity + enem.Intelligence) / 3;
                }

                bool didCrit = Program.game.currentCombat.didCrit(Program.game.currentCombat.enemy, crit);
                if (didCrit)
                {
                    damage *= 2;
                }
                
                // reduce enemy damage as scalar
                double scalar = 0.1 + (Program.game.currentCombat.enemy.Level / 10);
                damage = Convert.ToInt16(Math.Round(damage * scalar, 0));

                //UtilityFunctions.clearScreen(Program.game.player);
                if (!RequestingOutpOnly) UtilityFunctions.TypeText(new TypeText(), $"You have taken {damage} damage.");
                currentHealth -= damage;
                if (currentHealth < 0)
                {
                    currentHealth = 0;
                    if (!SimulatedCombat) PlayerDies();
                }

                return damage;
            }
            else
            {
                Program.logger.Error("No current combat. Attempt to receive attack failed.");
                throw new Exception("No current combat.");
            }
        }

        public void ExecuteAttack(AttackSlot key, Enemy target, bool requestingOutpOnly = false)
        {
            if (Program.game.player.PlayerAttacks.TryGetValue(key, out var attackInfo))
            {
                if (requestingOutpOnly) target.RequestingOutpOnly = true;
                var expectedType = attackInfo.Expression.DeclaredParameters.First().Type;

                if (expectedType == typeof(Enemy))
                {
                    attackInfo.Expression.Invoke(target); // Correct case, target is an Enemy
                }
                else if (expectedType == typeof(Player))
                {
                    Parameter[] parameters = null;
                    parameters = new[] { new Parameter("target", typeof(Enemy)) }; // Ensure correct type
                    Lambda parsedScript = UtilityFunctions.interpreter.Parse(attackInfo.ExpressionString, parameters);
                    attackInfo.Expression = parsedScript;
                    attackInfo.Expression.Invoke(target); // Ensure correct type
                }
                else
                {
                    throw new Exception($"Unexpected attack expression target type: {expectedType}");
                }

                target.RequestingOutpOnly = false;

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

                if (!requestingOutpOnly) UtilityFunctions.TypeText(new TypeText(), "\n\nPress any key to continue...");
                if (!requestingOutpOnly) Console.ReadKey(true);
                if (!requestingOutpOnly) UtilityFunctions.clearScreen(this);
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

        public async Task initialisePlayerFromNarrator(GameSetup gameSetup, OpenAIAPI api, Conversation chat,
            bool testing = false)
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
                    Console.WriteLine(
                        $"Failed to set property in initialisePlayerFromNarrator {property.Name}: {ex.Message}");
                }
            }

            // set character health to max
            currentHealth = Health;
            currentMana = ManaPoints;
            // this.
            Program.logger.Info("Character Created");
            //Console.ForegroundColor = ConsoleColor.Black;
        }


        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }

        public void UpdateHpAndMana()
        {
            int baseHp;
            int baseMp;
            XmlSerializer serializer = new XmlSerializer(typeof(Player));
            using (TextReader reader = new StringReader(File.ReadAllText(
                       $"{UtilityFunctions.mainDirectory}BaseStats{Path.DirectorySeparatorChar}{UtilityFunctions.saveName}.xml")))
            {
                var obj = serializer.Deserialize(reader) as Player;
                baseHp = obj.Health;
                baseMp = obj.ManaPoints;
            }

            int finalHpMax = baseHp + 2 * Constitution;
            Health = finalHpMax;
            if (currentHealth > Health)
            {
                currentHealth = Health;
            }

            int finalManaPointMax = baseMp + 2 * Intelligence;
            ManaPoints = finalManaPointMax;
            if (currentMana > ManaPoints)
            {
                currentMana = ManaPoints;
            }
        }


        public void checkForLevelUp()
        {
            while (currentExp >= maxExp)
            {
                levelUp();
            }
        }

        public void levelUp()
        {
            Level++;
            Strength++;
            Intelligence++;
            Dexterity++;
            Constitution++;
            Charisma++;
            currentExp -= maxExp;
            maxExp = (maxExp * 2);
            UtilityFunctions.clearScreen(this);
            UtilityFunctions.TypeText(new TypeText(), "You leveled up!");
            UtilityFunctions.TypeText(new TypeText(), $"Level: {Level - 1} ---> {Level}");
            UtilityFunctions.TypeText(new TypeText(), $"Strength: {Strength - 1} ---> {Strength}");
            UtilityFunctions.TypeText(new TypeText(), $"Dexterity: {Dexterity - 1} ---> {Dexterity}");
            UtilityFunctions.TypeText(new TypeText(), $"Intelligence: {Intelligence - 1} ---> {Intelligence}");
            UtilityFunctions.TypeText(new TypeText(), $"Constitution: {Constitution - 1} ---> {Constitution}");
            UtilityFunctions.TypeText(new TypeText(), $"Charisma: {Charisma - 1} ---> {Charisma}");
            UpdateHpAndMana();
            Thread.Sleep(1000);
        }
    }

    public class GameState
    {
        public string saveName { get; set; }
        public Point location { get; set; }
        public int currentNodeId { get; set; }
        public int currentGraphId { get; set; }

        public GameState(string SaveName = null, Point Location = new Point(), int CurrentNodeId = 0,
            int CurrentGraphId = 0)
        {
            saveName = SaveName;
            location = Location;
            currentNodeId = CurrentNodeId;
            currentGraphId = CurrentGraphId;
        }

        public async Task saveStateToFile(Map map = null)
        {
            string path = $"{UtilityFunctions.mainDirectory}GameStates{Path.DirectorySeparatorChar}{saveName}.json";
            using (StreamWriter file = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented // Pretty print
                };
                serializer.Serialize(file, this);
                await file.FlushAsync();
            }

            if (map == null)
                return;

            string pathMap =
                $"{UtilityFunctions.mainDirectory}MapStructures{Path.DirectorySeparatorChar}{saveName}.json";
            using (StreamWriter file = File.CreateText(pathMap))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(file, map);
                await file.FlushAsync();
            }

            await Program.saveGameToAllStoragesAsync();
        }

        public async Task<(Player, Map)> unloadStateFromFile(Player player, Map map)
        {
            string path = $"{UtilityFunctions.mainDirectory}GameStates{Path.DirectorySeparatorChar}{saveName}.json";
            try
            {
                GameState thisState = JsonConvert.DeserializeObject<GameState>(File.ReadAllText(path));
                player.playerPos = thisState.location;
                map.CurrentGraphPointer = thisState.currentGraphId;
                map.Graphs[map.CurrentGraphPointer].CurrentNodePointer = thisState.currentNodeId;

                saveName = thisState.saveName;
                location = thisState.location;
                currentNodeId = thisState.currentNodeId;
                currentGraphId = thisState.currentGraphId;
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException)
                {
                    saveStateToFile();
                }
            }

            return (player, map);
        }
    }
}