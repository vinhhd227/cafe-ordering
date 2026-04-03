namespace Api.Core.Aggregates.NotificationAggregate.Specifications;

public class NotificationConfigByTypeSpec : SingleResultSpecification<NotificationConfig>
{
    public NotificationConfigByTypeSpec(string type)
    {
        Query.Where(c => c.Type == type);
    }
}
