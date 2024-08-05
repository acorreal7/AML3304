using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {

        // Create builder
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
        });
        builder.Services.AddSingleton<HttpClient>();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        var app = builder.Build();
        app.UseHttpsRedirection();
        app.UseCors();

        // Dictionary to store conversations
        var conversations = new ConcurrentDictionary<string, List<Message>>();

        // Endpoint to process chat messages
        app.MapPost("/chat", async (HttpRequest request, HttpResponse response, HttpClient httpClient, IConfiguration configuration) =>
        {
            try
            {

                // Read configuration values
                var apiUrl = configuration["API_URL"];
                var apiKey = configuration["API_KEY"];
                var requestBody = await request.ReadFromJsonAsync<ChatRequest>();

                // Get conversation history
                var conversationId = requestBody.ConversationId;
                var userInput = requestBody.Text;
                var conversation = conversations.GetOrAdd(conversationId, new List<Message>());

                // Create a list of messages to send to the API
                var messages = new List<Phi3Message>();
                if (conversation.Count == 0)
                {
                    messages.Add(new Phi3Message
                    {
                        Role = "assistant",
                        Content = "You are an expert in project management and the unified process, trained to provide knowledgeable and practical guidance to individuals seeking help.\\n\r\nYour primary objective is to engage in meaningful conversations about project management and the unified process, asking insightful questions and offering thoughtful, professional responses.\\n\r\nAvoid discussing or answering questions that are unrelated to project management or the unified process, such as topics about mental health or other unrelated subjects.\\n\r\nAlways base your responses on the entire context of the conversation, ensuring you remember and reference all previous messages to provide continuity and relevance.\\n\r\nHandle all user data with the utmost confidentiality and remind users to be cautious about sharing sensitive personal information.\\n\r\nMaintain a professional and informative tone throughout all interactions, ensuring users feel supported and well-informed.\\n\r\nEncourage users to express their thoughts and questions openly, validate their experiences, and offer guidance or strategies when appropriate.\\n\r\nIf a user deviates from the topic of project management or the unified process, gently steer the conversation back to relevant topics to provide the most effective support."
                    });
                }

                foreach (var message in conversation)
                {
                    messages.Add(new Phi3Message { Role = "user", Content = message.User });
                    messages.Add(new Phi3Message { Role = "assistant", Content = message.Assistant });
                }
                messages.Add(new Phi3Message { Role = "user", Content = userInput });

                // Define headers and content for the API request
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var data = new
                {
                    messages,
                    frequency_penalty = 0,
                    presence_penalty = 0,
                    max_tokens = 512,
                    seed = 42,
                    stop = (string)null,
                    stream = false,
                    temperature = 0,
                    top_p = 1,
                    response_format = new { type = "text" }
                };

                // Create request content
                var jsonContent = JsonConvert.SerializeObject(data);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send request to the API
                var apiResponse = await httpClient.PostAsync(apiUrl, content);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    var errorContent = await apiResponse.Content.ReadAsStringAsync();
                    response.StatusCode = (int)apiResponse.StatusCode;
                    await response.WriteAsJsonAsync(new { error = true, message = apiResponse.ReasonPhrase, details = errorContent });
                    return;
                }

                // Process API response
                var result = await apiResponse.Content.ReadFromJsonAsync<ApiResponse>();
                var assistantResponse = result.Choices[0].Message.Content;

                // Add user and assistant messages to the conversation
                conversation.Add(new Message { User = userInput, Assistant = assistantResponse });

                // Return the assistant response
                await response.WriteAsJsonAsync(new { text = assistantResponse });

            }
            catch (Exception ex)
            {
                response.StatusCode = StatusCodes.Status500InternalServerError;
                await response.WriteAsJsonAsync(new { error = true, message = ex.Message });
            }
        });

        app.Run();
    }

}

/// <summary>
/// Class to represent a chat request
/// </summary>
public class ChatRequest
{

    /// <summary>
    /// The text of the message
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// ID of the conversation
    /// </summary>
    public string ConversationId { get; set; }

}

/// <summary>
/// 
/// </summary>
public class Message
{

    /// <summary>
    /// Role of the message
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// User message
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Assistant message
    /// </summary>
    public string Assistant { get; set; }

}

/// <summary>
/// Represents a message to send to the Azure API
/// </summary>
public class Phi3Message
{

    /// <summary>
    /// Role of the message
    /// </summary>
    [JsonProperty("role")]
    public string Role { get; set; }

    /// <summary>
    /// Content of the message
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; set; }

}

/// <summary>
/// Class to represent the response from the Azure API
/// </summary>
public class ApiResponse
{
    public List<Choice> Choices { get; set; }
}

/// <summary>
/// Represents a choice in the response from the Azure API
/// </summary>
public class Choice
{
    public Phi3Message Message { get; set; }
}
