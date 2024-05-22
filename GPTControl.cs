using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GPTControlNamespace
{
    public class GPTControl
    {
        public static async Task generate()
        {
            // Your API key
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"Secret\appsettings.json");

            var configuration = builder.Build();
            string apiKey = configuration["OpenAI:ApiKey"];

            // The API endpoint
            string endpoint = "https://api.openai.com/v1/images";

            // Sample request payload (replace with your prompt and parameters)
            string requestData = "{\"prompt\": \"a cat sitting on a table\", \"max_width\": 600, \"max_height\": 400}";

            // Create HttpClient instance
            using (var client = new HttpClient())
            {
                // Set authorization header
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Set request content type
                HttpContent content = new StringContent(requestData);

                // Send POST request
                HttpResponseMessage response = await client.PostAsync(endpoint, content);

                // Check if request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read response content (image data)
                    byte[] imageData = await response.Content.ReadAsByteArrayAsync();

                    // Save the image to a file (or process/display it as needed)
                    File.WriteAllBytes(
                        "C:\\Users\\isaac\\RiderProjects\\NEA-AI-Game\\GeneratedImages\\generated_image.png",
                        imageData);

                    Console.WriteLine("Image generated successfully.");
                }
                else
                {
                    Console.WriteLine($"Request failed with status code {response.StatusCode}");
                }
            }
        }
        
        public static async Task keyCheck()
        {
            Console.WriteLine("here");
            // Your API key
            string apiKey = "sk-proj-ZMcUeP0skGgI7AK61BwvT3BlbkFJGY92olItqv6zSJ588C8w";

            // The API endpoint for listing engines (does not require special permissions)
            string endpoint = "https://api.openai.com/v1/engines";

            // Create HttpClient instance
            using (var client = new HttpClient())
            {
                Console.WriteLine("here");
                // Set authorization header
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                try
                {
                    Console.WriteLine("here");
                    // Send GET request
                    HttpResponseMessage response = null;
                    
                    try
                    {
                        Console.WriteLine("wtf");
                        response = await client.GetAsync(endpoint);
                        Console.WriteLine("wtf");
                        // Rest of the code
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"HTTP request failed: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    }
                    
                    Console.WriteLine("fes");
                    
                    // Check if request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("API key is valid.");
                    }
                    else
                    {
                        Console.WriteLine($"API key is invalid. Error status code: {response.StatusCode}");
                    }
                    
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Failed to connect to the API. Please check your network connection.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
            }
        }
    }
}