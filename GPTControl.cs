using OpenAI_API;
using Microsoft.Extensions.Configuration;
using OpenAI_API.Completions;
using UtilityFunctionsNamespace;

namespace GPTControlNamespace
{
    public class Narrator
    {
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
    }
}