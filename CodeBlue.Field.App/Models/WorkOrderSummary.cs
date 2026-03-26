namespace CodeBlue.Field.App.Models;

public sealed class WorkOrderSummary
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Street1 { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Technician { get; init; } = string.Empty;
    public string GateCode { get; init; } = string.Empty;
    public bool AnimalsPresent { get; init; }
    public string ServiceDetails { get; init; } = string.Empty;
    public DateOnly DateSubmitted { get; init; }
    public int? RouteGroup { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public int? ScheduledOrder { get; init; }
    public DateOnly? ServiceStartupDate { get; init; }
    public string ServiceContactName { get; init; } = string.Empty;
    public string ServiceContactPhone { get; init; } = string.Empty;
    public string BuilderCompanyName { get; init; } = string.Empty;
    public string BuilderContactName { get; init; } = string.Empty;
    public string BuilderContactPhone { get; init; } = string.Empty;
    public string BuilderEmail { get; init; } = string.Empty;
    public string CompletedBy { get; init; } = string.Empty;
    public DateOnly? CompletedOn { get; init; }
    public bool HasPendingUpload { get; init; }
}
