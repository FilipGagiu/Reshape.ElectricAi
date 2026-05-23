using Reshape.ElectricAi.Core.Persistence;
using Reshape.ElectricAi.Plans.Entities;

namespace Reshape.ElectricAi.Plans.Persistence.Specifications;

public sealed class PushSubscriptionByEndpointSpec : Specification<PushSubscription>
{
    public PushSubscriptionByEndpointSpec(string endpoint)
    {
        Where(x => x.Endpoint == endpoint);
    }
}
