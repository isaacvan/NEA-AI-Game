﻿using System.Reflection;
using System.Xml.Serialization;
using MainNamespace;
using OpenAI_API;
using Microsoft.Extensions.Configuration;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace GPTControlNamespace
{
    public interface GameSetup
    {
        public void chooseSave();

        Task generateMainXml(Conversation chat, string prompt5);
    }
    
   
    public class Narrator : GameSetup
    {
        private OpenAIAPI api; 
        private Conversation chat;
        
        // Testing
        public static OpenAIAPI initialiseGPT()
        {
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Initialising GPT...", UtilityFunctions.typeSpeed);
            //Thread.Sleep(500);
            string? apiKey = System.Environment.GetEnvironmentVariable("API_KEY");
            Console.WriteLine($"ENV API Key: {apiKey}");
            if (apiKey == null)
            {
                Console.WriteLine("ENV API Key is not set, trying secrets");
                var config = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();
                apiKey = config["API_KEY"];
                if (apiKey != null)
                {
                    Console.WriteLine("Secret found");
                }
                else
                {
                    throw new Exception("No GPT API key! Set API_KEY env variable");
                }
            }
            System.Environment.SetEnvironmentVariable("API_KEY", apiKey);
            
            OpenAIAPI api = new OpenAIAPI(apiKey);
            
            
            
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Initialised GPT.", UtilityFunctions.typeSpeed);
            //Thread.Sleep(500);
            Console.Clear();
            
            return api;
        }

        public static Conversation initialiseChat(OpenAIAPI api)
        {
            Conversation chat = api.Chat.CreateConversation();
            chat.Model = Model.GPT4_Turbo;
            chat.RequestParameters.Temperature = 0.9;
            return chat;
        }

        public void chooseSave()
        {
            bool gameStarted = false;
            bool saveChosen = false;
            while (!saveChosen)
            {
                saveChosen = Program.menu(gameStarted, saveChosen); // displays the menu
            }
        }
        
        public async Task generateMainXml(Conversation chat, string prompt5)
        {
            string output;
            try
            {
                // output = await Narrator.getGPTResponse(prompt5, api, 100, 0.9);
                chat.AppendUserInput(prompt5);
                output = await chat.GetResponseFromChatbotAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get response: {e}");
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }


            //Console.WriteLine(output);


            if (string.IsNullOrEmpty(UtilityFunctions.saveSlot)) // if testing / error
            {
                // get all save file
                string[] saves = Directory.GetFiles(UtilityFunctions.mainDirectory + @"saves\", "*.xml");
                bool started = false;
                for (int i = 0; i < UtilityFunctions.maxSaves; i++)
                {
                    if (saves.Length == i)
                    {
                        UtilityFunctions.TypeText(UtilityFunctions.Instant,
                            $"Save Slot save{i + 1}.xml is empty. Do you want to begin a new game? y/n",
                            UtilityFunctions.typeSpeed);
                        string load = Console.ReadLine();
                        if (load == "y")
                        {
                            string save = UtilityFunctions.mainDirectory + @$"saves\save{i + 1}.xml";
                            UtilityFunctions.saveSlot = Path.GetFileName(save);
                            UtilityFunctions.saveFile = save;
                            started = true;
                            i = UtilityFunctions.maxSaves;
                        }
                    }
                }

                if (!started)
                {
                    UtilityFunctions.TypeText(UtilityFunctions.Instant,
                        "No empty save slots. Exiting Test. Press any key to leave", UtilityFunctions.typeSpeed);
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }

            Console.ForegroundColor = ConsoleColor.Black;
            //Console.WriteLine(UtilityFunctions.saveFile);
            //Console.WriteLine(output);


            // design xml file
            string preText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            output = await UtilityFunctions.cleanseXML(output);
            string finalXMLText = "";
            finalXMLText = preText + "\n" + output;


            try
            {
                File.Create(UtilityFunctions.saveFile).Close();
                File.WriteAllText(UtilityFunctions.saveFile, finalXMLText);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not write to file: {e}");
            }


            // Player player with attributes
            XmlSerializer serializer = new XmlSerializer(typeof(Player));
            Player loadedPlayer;
            using (TextReader reader = new StringReader(finalXMLText))
            {
                loadedPlayer = (Player)serializer.Deserialize(reader);
            }


            // set player properties
            if (loadedPlayer == null)
                throw new ArgumentNullException("Null player");

            Type playerType = typeof(Player);
            PropertyInfo[] properties = playerType.GetProperties(BindingFlags.Public | BindingFlags.Instance);


            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(loadedPlayer);
                    property.SetValue(this, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property {property.Name}: {ex.Message}");
                    // Handle or log the error as necessary
                }
            }
        }

    }
}