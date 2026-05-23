using System.Threading.Channels;
using Reshape.ElectricAi.Core.Dtos;

namespace Reshape.ElectricAi.LiveFeed.Broadcasting;

internal sealed record FeedSubscription(
    Guid SubscriptionId,
    Guid UserId,
    UserFeedPrefs Prefs,
    Channel<FeedEventEnvelope> Channel);
