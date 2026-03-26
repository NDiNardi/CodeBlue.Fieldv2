using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public static class FieldSeedData
{
    public static IReadOnlyList<FieldTechnicianOption> CreateTechnicians() =>
    [
        new FieldTechnicianOption
        {
            Id = Guid.Parse("a2da5c9b-0a0a-45fc-9289-8c527f629001"),
            Username = "Nick"
        },
        new FieldTechnicianOption
        {
            Id = Guid.Parse("c8fc7448-0b95-4a04-b5be-cd50b43da002"),
            Username = "Field Lead"
        },
        new FieldTechnicianOption
        {
            Id = Guid.Parse("fcb56ac3-b1db-4d13-9f1c-3da38a857003"),
            Username = "Chris"
        }
    ];

    public static SyncSnapshot CreateSnapshot() => new()
    {
        LastSuccessfulSync = default,
        PendingUploadCount = 0,
        IsOfflineModeEnabled = true,
        HasCompletedSync = false
    };

    public static IReadOnlyList<WorkOrderSummary> CreateWorkOrders() =>
    [
        new WorkOrderSummary
        {
            Id = Guid.Parse("a5270e73-1405-4df4-b8de-44fa9b9e6501"),
            CustomerId = Guid.Parse("26c3be67-b16f-4468-a2bf-34b63093a05d"),
            AssignedToUserId = Guid.Parse("a2da5c9b-0a0a-45fc-9289-8c527f629001"),
            WorkOrderNumber = "WO-10482",
            CustomerName = "North Ridge Estates",
            Address = "4450 North Ridge Court, Hayward, CA 94542",
            City = "Hayward",
            Latitude = 37.6598m,
            Longitude = -122.0808m,
            Status = "Active",
            Technician = "Nick",
            GateCode = "1479",
            AnimalsPresent = false,
              ServiceDetails = "Inspect heater ignition issue and verify automation reset after overnight outage.",
              DateSubmitted = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
              ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
              ScheduledOrder = 0,
              ServiceStartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            ServiceContactName = "Riley Gomez",
            ServiceContactPhone = "(510) 555-0142",
            BuilderCompanyName = "North Ridge Builders",
            BuilderContactName = "Tom Alvarez",
            BuilderContactPhone = "(510) 555-0199",
            BuilderEmail = "tom@nrbuilders.example",
            HasPendingUpload = false
        },
        new WorkOrderSummary
        {
            Id = Guid.Parse("88d14d04-02fd-4034-abf7-fda5c3915ccf"),
            CustomerId = Guid.Parse("1905d8c3-ad12-4105-8d40-d74e457d62c4"),
            AssignedToUserId = Guid.Parse("a2da5c9b-0a0a-45fc-9289-8c527f629001"),
            WorkOrderNumber = "WO-10483",
            CustomerName = "Sierra Vista HOA",
            Address = "982 Sierra Vista Lane, Pleasanton, CA 94588",
            City = "Pleasanton",
            Latitude = 37.6755m,
            Longitude = -121.8747m,
            Status = "Completed",
            Technician = "Nick",
            GateCode = "5560",
            AnimalsPresent = true,
              ServiceDetails = "Confirmed pump seal failure, replaced housing gasket, and documented startup readings.",
              DateSubmitted = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
              ScheduledDate = DateOnly.FromDateTime(DateTime.Today),
              ScheduledOrder = 1,
              ServiceStartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
            ServiceContactName = "Marla Dixon",
            ServiceContactPhone = "(925) 555-0120",
            BuilderCompanyName = "Vista Communities",
            BuilderContactName = "Jen Park",
            BuilderContactPhone = "(925) 555-0155",
            BuilderEmail = "jen@vistacommunities.example",
            CompletedBy = "Nick",
            CompletedOn = DateOnly.FromDateTime(DateTime.Today),
            HasPendingUpload = true
        },
        new WorkOrderSummary
        {
            Id = Guid.Parse("eecc98d8-208e-4f41-a4d5-f4407af9ffab"),
            CustomerId = Guid.Parse("155d780c-5d3d-481c-a309-d58db1cf9f8c"),
            AssignedToUserId = Guid.Parse("c8fc7448-0b95-4a04-b5be-cd50b43da002"),
            WorkOrderNumber = "WO-10491",
            CustomerName = "Canyon Oaks Residence",
            Address = "117 Canyon Oaks Drive, Dublin, CA 94568",
            City = "Dublin",
            Latitude = 37.7058m,
            Longitude = -121.9101m,
            Status = "Pending",
            Technician = "Field Lead",
            GateCode = "",
            AnimalsPresent = false,
              ServiceDetails = "New service request for recurring automation fault and intermittent heating complaint.",
              DateSubmitted = DateOnly.FromDateTime(DateTime.Today),
              ScheduledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
              ScheduledOrder = 0,
              ServiceStartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            ServiceContactName = "Chris Morgan",
            ServiceContactPhone = "(925) 555-0188",
            BuilderCompanyName = "Canyon Oaks Custom Homes",
            BuilderContactName = "Paula Kent",
            BuilderContactPhone = "(925) 555-0102",
            BuilderEmail = "pkent@canyonoaks.example",
            HasPendingUpload = false
        }
    ];

    public static IReadOnlyList<CustomerSummary> CreateCustomers() =>
    [
        new CustomerSummary
        {
            Id = Guid.Parse("26c3be67-b16f-4468-a2bf-34b63093a05d"),
            Name = "North Ridge Estates",
            Address = "4450 North Ridge Court, Hayward, CA 94542",
            Street1 = "4450 North Ridge Court",
            City = "Hayward",
            State = "CA",
            Zip = "94542",
            Phone = "(510) 555-0142",
            ContactName = "Riley Gomez",
            ContactEmail = "riley@northridge.example",
            GateCodes = "1479",
            AnimalsPresent = false,
            OriginalInstallerDealer = "CodeBlue Pools",
            StartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            Latitude = 37.6598m,
            Longitude = -122.0808m,
            OpenWorkOrderCount = 2
        },
        new CustomerSummary
        {
            Id = Guid.Parse("1905d8c3-ad12-4105-8d40-d74e457d62c4"),
            Name = "Sierra Vista HOA",
            Address = "982 Sierra Vista Lane, Pleasanton, CA 94588",
            Street1 = "982 Sierra Vista Lane",
            City = "Pleasanton",
            State = "CA",
            Zip = "94588",
            Phone = "(925) 555-0120",
            ContactName = "Marla Dixon",
            ContactEmail = "marla@sierravista.example",
            GateCodes = "5560",
            AnimalsPresent = true,
            OriginalInstallerDealer = "Blue Horizon Installers",
            StartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
            Latitude = 37.6755m,
            Longitude = -121.8747m,
            OpenWorkOrderCount = 1
        },
        new CustomerSummary
        {
            Id = Guid.Parse("155d780c-5d3d-481c-a309-d58db1cf9f8c"),
            Name = "Canyon Oaks Residence",
            Address = "117 Canyon Oaks Drive, Dublin, CA 94568",
            Street1 = "117 Canyon Oaks Drive",
            City = "Dublin",
            State = "CA",
            Zip = "94568",
            Phone = "(925) 555-0188",
            ContactName = "Chris Morgan",
            ContactEmail = "chris@canyonoaks.example",
            GateCodes = "",
            AnimalsPresent = false,
            OriginalInstallerDealer = "CodeBlue Pools",
            StartupDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            Latitude = 37.7058m,
            Longitude = -121.9101m,
            OpenWorkOrderCount = 3
        }
    ];

    public static IReadOnlyList<ClaimSummary> CreateClaims() =>
    [
        new ClaimSummary
        {
            Id = Guid.Parse("e027d7ac-14ea-4e63-b3a0-617499919935"),
            WorkOrderId = Guid.Parse("a5270e73-1405-4df4-b8de-44fa9b9e6501"),
            CustomerId = Guid.Parse("26c3be67-b16f-4468-a2bf-34b63093a05d"),
            ClaimNumber = "CL-23019",
            CustomerName = "North Ridge Estates",
            ServiceAddress = "4450 North Ridge Court, Hayward, CA 94542",
            ContactName = "Riley Gomez",
            ContactPhone = "(510) 555-0142",
            OriginalInstallerDealer = "CodeBlue Pools",
            OriginalInstallationDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            Equipment = "Heater",
            FailureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            RepairDate = DateOnly.FromDateTime(DateTime.Today),
            Notes = "Unit tripped ignition lockout after rain. Documented corrosion and queued heater photos.",
            CompletedBy = "Nick",
            CompletedOn = DateOnly.FromDateTime(DateTime.Today),
            Status = "New",
            ComponentCode = "HTR-IGN",
            ModelNumber = "H400FDN",
            ProductType = "Heater",
            Product = "Hayward Universal H-Series",
            IdSerialNumber = "ID-44710",
            ComponentSerialNumber = "CMP-11209",
            ProblemComplaintReported = "No heat call after storm.",
            ProblemFound = "Ignition assembly fouled and burner tray corroded.",
            RepairsPerformed = "Cleaned ignition path and documented heater for claim review.",
            HasPendingUpload = true
        },
        new ClaimSummary
        {
            Id = Guid.Parse("b432987d-bd0c-45ef-82fc-f60e64c50ccc"),
            WorkOrderId = Guid.Parse("88d14d04-02fd-4034-abf7-fda5c3915ccf"),
            CustomerId = Guid.Parse("1905d8c3-ad12-4105-8d40-d74e457d62c4"),
            ClaimNumber = "CL-23022",
            CustomerName = "Sierra Vista HOA",
            ServiceAddress = "982 Sierra Vista Lane, Pleasanton, CA 94588",
            ContactName = "Marla Dixon",
            ContactPhone = "(925) 555-0120",
            OriginalInstallerDealer = "Blue Horizon Installers",
            OriginalInstallationDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-3)),
            Equipment = "Pump",
            FailureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4)),
            RepairDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            Notes = "Replaced leaking seal assembly. Serial fields captured and ready for office filing.",
            CompletedBy = "Nick",
            CompletedOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            Status = "Pending",
            ComponentCode = "PMP-SEAL",
            ModelNumber = "SP32950VSP",
            ProductType = "Pump",
            Product = "Hayward TriStar VS",
            IdSerialNumber = "ID-55102",
            ComponentSerialNumber = "CMP-66221",
            ProblemComplaintReported = "Pump losing prime overnight.",
            ProblemFound = "Primary seal leaking into motor housing.",
            RepairsPerformed = "Installed seal kit and verified pressure stability.",
            HasPendingUpload = false
        },
        new ClaimSummary
        {
            Id = Guid.Parse("3fc6aa8e-a2d5-4784-8ea4-9b2f708ce978"),
            CustomerId = Guid.Parse("155d780c-5d3d-481c-a309-d58db1cf9f8c"),
            ClaimNumber = "CL-23031",
            CustomerName = "Canyon Oaks Residence",
            ServiceAddress = "117 Canyon Oaks Drive, Dublin, CA 94568",
            ContactName = "Chris Morgan",
            ContactPhone = "(925) 555-0188",
            OriginalInstallerDealer = "CodeBlue Pools",
            OriginalInstallationDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            Equipment = "Automation",
            FailureDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)),
            RepairDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2).AddDays(2)),
            Notes = "Historic control board claim used for equipment lookup and repeat issue comparison.",
            CompletedBy = "Field Lead",
            CompletedOn = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2).AddDays(2)),
            Status = "Approved",
            ComponentCode = "AUTO-CTRL",
            ModelNumber = "OMNI-PL-100",
            ProductType = "Automation",
            Product = "Hayward OmniPL",
            IdSerialNumber = "ID-88271",
            ComponentSerialNumber = "CMP-78214",
            ProblemComplaintReported = "Panel locking up during freeze cycles.",
            ProblemFound = "Control board communication failure.",
            RepairsPerformed = "Replaced controller and reloaded configuration.",
            HasPendingUpload = false
        }
    ];

    public static IReadOnlyList<PendingChange> CreatePendingChanges() => [];
}
