using backend.models;
using backend.services.interfaces;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Pgvector;
using ChatMessage = OpenAI.Chat.ChatMessage;

namespace backend.services.implementations;

public class OpenAiService(OpenAIClient client, IOptions<OpenAIOptions> options) : IOpenAiService
{
    private readonly OpenAIOptions _options = options.Value;

    public async Task<Vector> EmbedAsync(string input, CancellationToken ct)
    {
        var embed = client.GetEmbeddingClient(_options.EmbeddingModel);
        var res = await embed.GenerateEmbeddingAsync(input, cancellationToken: ct);
        
        var floats = res.Value.ToFloats().ToArray();
        return new Vector(floats);
    }

    public async Task<string> ChatAsync(string system, string user, CancellationToken ct)
    {
        var chatClient = client.GetChatClient("gpt-4.1-mini");

        var response = await chatClient.CompleteChatAsync(
            [
                ChatMessage.CreateSystemMessage(system),
                ChatMessage.CreateUserMessage(user)
            ],
            cancellationToken: ct
        );

        return response.Value.Content[0].Text;
    }
}