using Reshape.ElectricAi.Infrastructure.Persistence;

namespace Reshape.ElectricAi.AiChat.Persistence;

public sealed class ChatRepository<T>(ChatDbContext context)
    : EfRepository<ChatDbContext, T>(context)
    where T : class;
