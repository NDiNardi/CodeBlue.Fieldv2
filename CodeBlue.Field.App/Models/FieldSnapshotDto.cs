namespace CodeBlue.Field.App.Models;

public sealed class FieldSnapshotDto
{
    public DateTimeOffset GeneratedAtUtc { get; init; }
    public bool IsDelta { get; init; }
    public IReadOnlyList<FieldTechnicianOption> Technicians { get; init; } = [];
    public IReadOnlyList<CustomerSummary> Customers { get; init; } = [];
    public IReadOnlyList<WorkOrderSummary> WorkOrders { get; init; } = [];
    public IReadOnlyList<ClaimSummary> Claims { get; init; } = [];
    public IReadOnlyList<Guid> DeletedCustomerIds { get; init; } = [];
    public IReadOnlyList<Guid> DeletedWorkOrderIds { get; init; } = [];
    public IReadOnlyList<Guid> DeletedClaimIds { get; init; } = [];
}
