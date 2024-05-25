using OpenAI_API;

namespace GPTControlNamespace
{
    public class GPTControl
    {
        
        public static string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        // Testing
        public static async void Test()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("API key not found. Please set the OPENAI_API_KEY environment variable.");
                return;
            }

            // initialise the client
            var openAiApi = new OpenAIAPI(apiKey);

            // Generate a simple reply
            var completionRequest = new OpenAI_API.Completions.CompletionRequest
            {
                Prompt = "Hello, how are you?",
                MaxTokens = 50
            };

            var completionResult = await openAiApi.Completions.CreateCompletionAsync(completionRequest);

            Console.WriteLine("OpenAI Response:");
            Console.WriteLine(completionResult.Completions[0].Text.Trim());
        }
    }
}