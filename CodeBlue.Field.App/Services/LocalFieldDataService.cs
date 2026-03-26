using System.Text.Json;
using CodeBlue.Field.App.Models;
using CodeBlue.Field.App.Util;

namespace CodeBlue.Field.App.Services;

public sealed class LocalFieldDataService(IBrowserStorageService storage) : IFieldDataService
{
    private const string SnapshotKey = "codeblue.field.syncSnapshot";
    private const string TechniciansKey = "codeblue.field.technicians";
    private const string WorkOrdersKey = "codeblue.field.workOrders";
    private const string CustomersKey = "codeblue.field.customers";
    private const string ClaimsKey = "codeblue.field.claims";
    private const string PendingChangesKey = "codeblue.field.pendingChanges";
    private const string OutboundSyncKey = "codeblue.field.outboundSync";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
    private readonly Dictionary<string, object> _memoryCache = new(StringComparer.Ordinal);
    private bool _hasSeeded;

    public async Task<SyncSnapshot> GetSyncSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync(SnapshotKey, FieldSeedData.CreateSnapshot);
    }

    public async Task<IReadOnlyList<WorkOrderSummary>> GetWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync<IReadOnlyList<WorkOrderSummary>>(WorkOrdersKey, static () => Array.Empty<WorkOrderSummary>());
    }

    public async Task<IReadOnlyList<FieldTechnicianOption>> GetTechniciansAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync<IReadOnlyList<FieldTechnicianOption>>(TechniciansKey, static () => Array.Empty<FieldTechnicianOption>());
    }

    public async Task<IReadOnlyList<CustomerSummary>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync<IReadOnlyList<CustomerSummary>>(CustomersKey, static () => Array.Empty<CustomerSummary>());
    }

    public async Task<IReadOnlyList<ClaimSummary>> GetClaimsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync<IReadOnlyList<ClaimSummary>>(ClaimsKey, static () => Array.Empty<ClaimSummary>());
    }

    public async Task<IReadOnlyList<PendingChange>> GetPendingChangesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync<IReadOnlyList<PendingChange>>(PendingChangesKey, static () => Array.Empty<PendingChange>());
    }

    public async Task<OutboundSyncState> GetOutboundSyncStateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();
        return await GetRequiredAsync(OutboundSyncKey, () => new OutboundSyncState());
    }

    public async Task ApplySnapshotAsync(FieldSnapshotDto snapshot, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        if (snapshot.IsDelta)
        {
            await ApplyDeltaSnapshotAsync(snapshot, cancellationToken);
            return;
        }

        var localClaims = await GetClaimsAsync(cancellationToken);
        var localWorkOrders = await GetWorkOrdersAsync(cancellationToken);
        var pendingChanges = await GetPendingChangesAsync(cancellationToken);
        var outboundState = await GetOutboundSyncStateAsync(cancellationToken);
        var pendingClaims = localClaims
            .Where(c => c.HasPendingUpload)
            .Where(c => snapshot.Claims.All(serverClaim => serverClaim.Id != c.Id))
            .ToList();
        var pendingWorkOrders = localWorkOrders
            .Where(w => w.HasPendingUpload)
            .Where(w => snapshot.WorkOrders.All(serverWorkOrder => serverWorkOrder.Id != w.Id))
            .ToList();

        foreach (var completion in outboundState.WorkOrdersToComplete)
        {
            var localWorkOrder = localWorkOrders.FirstOrDefault(w => w.Id == completion.WorkOrderId);
            if (localWorkOrder is null)
            {
                continue;
            }

            if (pendingWorkOrders.All(w => w.Id != completion.WorkOrderId)
                && snapshot.WorkOrders.Any(serverWorkOrder => serverWorkOrder.Id == completion.WorkOrderId))
            {
                pendingWorkOrders.Add(localWorkOrder);
            }
        }

        foreach (var officeAction in outboundState.ServiceRequestOfficeActions)
        {
            var localWorkOrder = localWorkOrders.FirstOrDefault(w => w.Id == officeAction.WorkOrderId);
            if (localWorkOrder is null)
            {
                continue;
            }

            if (pendingWorkOrders.All(w => w.Id != officeAction.WorkOrderId)
                && snapshot.WorkOrders.Any(serverWorkOrder => serverWorkOrder.Id == officeAction.WorkOrderId))
            {
                pendingWorkOrders.Add(localWorkOrder);
            }
        }

        var mergedClaims = snapshot.Claims.Concat(pendingClaims).ToList();
        var mergedWorkOrders = snapshot.WorkOrders
            .Select(serverWorkOrder =>
            {
                var pendingLocal = pendingWorkOrders.FirstOrDefault(w => w.Id == serverWorkOrder.Id);
                return pendingLocal ?? serverWorkOrder;
            })
            .Concat(pendingWorkOrders.Where(localWorkOrder => snapshot.WorkOrders.All(serverWorkOrder => serverWorkOrder.Id != localWorkOrder.Id)))
            .ToList();
        var nextSnapshot = new SyncSnapshot
        {
            LastSuccessfulSync = snapshot.GeneratedAtUtc,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = true,
            HasCompletedSync = true
        };

        await SaveAsync(CustomersKey, snapshot.Customers);
        await SaveAsync(TechniciansKey, snapshot.Technicians);
        await SaveAsync(WorkOrdersKey, mergedWorkOrders);
        await SaveAsync(ClaimsKey, mergedClaims);
        await SaveAsync(SnapshotKey, nextSnapshot);
    }

    private async Task ApplyDeltaSnapshotAsync(FieldSnapshotDto snapshot, CancellationToken cancellationToken)
    {
        var deletedCustomerIds = snapshot.DeletedCustomerIds.ToHashSet();
        var deletedWorkOrderIds = snapshot.DeletedWorkOrderIds.ToHashSet();
        var deletedClaimIds = snapshot.DeletedClaimIds.ToHashSet();

        var localCustomers = (await GetCustomersAsync(cancellationToken))
            .Where(customer => !deletedCustomerIds.Contains(customer.Id))
            .ToList();
        var localTechnicians = (await GetTechniciansAsync(cancellationToken)).ToList();
        var localClaims = (await GetClaimsAsync(cancellationToken))
            .Where(claim => !deletedClaimIds.Contains(claim.Id))
            .ToList();
        var localWorkOrders = (await GetWorkOrdersAsync(cancellationToken))
            .Where(workOrder => !deletedWorkOrderIds.Contains(workOrder.Id))
            .ToList();
        var pendingChanges = (await GetPendingChangesAsync(cancellationToken))
            .Where(change => !WasDeletedByServer(change, deletedCustomerIds, deletedWorkOrderIds, deletedClaimIds))
            .ToList();
        var outboundState = await GetOutboundSyncStateAsync(cancellationToken);
        var filteredOutboundState = new OutboundSyncState
        {
            ClaimsToCreate = outboundState.ClaimsToCreate
                .Where(claim => !deletedClaimIds.Contains(claim.LocalClaimId)
                    && (!claim.WorkOrderId.HasValue || !deletedWorkOrderIds.Contains(claim.WorkOrderId.Value)))
                .ToList(),
            ServiceRequestOfficeActions = outboundState.ServiceRequestOfficeActions
                .Where(workOrder => !deletedWorkOrderIds.Contains(workOrder.WorkOrderId))
                .ToList(),
            WorkOrdersToComplete = outboundState.WorkOrdersToComplete
                .Where(workOrder => !deletedWorkOrderIds.Contains(workOrder.WorkOrderId))
                .ToList()
        };

        var pendingClaims = localClaims
            .Where(c => c.HasPendingUpload)
            .Where(c => snapshot.Claims.All(serverClaim => serverClaim.Id != c.Id))
            .ToList();
        var pendingWorkOrders = localWorkOrders
            .Where(w => w.HasPendingUpload)
            .Where(w => snapshot.WorkOrders.All(serverWorkOrder => serverWorkOrder.Id != w.Id))
            .ToList();

        foreach (var completion in filteredOutboundState.WorkOrdersToComplete)
        {
            var localWorkOrder = localWorkOrders.FirstOrDefault(w => w.Id == completion.WorkOrderId);
            if (localWorkOrder is null)
            {
                continue;
            }

            if (pendingWorkOrders.All(w => w.Id != completion.WorkOrderId))
            {
                pendingWorkOrders.Add(localWorkOrder);
            }
        }

        foreach (var officeAction in filteredOutboundState.ServiceRequestOfficeActions)
        {
            var localWorkOrder = localWorkOrders.FirstOrDefault(w => w.Id == officeAction.WorkOrderId);
            if (localWorkOrder is null)
            {
                continue;
            }

            if (pendingWorkOrders.All(w => w.Id != officeAction.WorkOrderId))
            {
                pendingWorkOrders.Add(localWorkOrder);
            }
        }

        var mergedCustomers = MergeById(localCustomers, snapshot.Customers, customer => customer.Id);
        var mergedTechnicians = MergeById(localTechnicians, snapshot.Technicians, tech => tech.Id);
        var mergedClaims = MergeById(localClaims, snapshot.Claims, claim => claim.Id)
            .Where(claim => pendingClaims.All(pending => pending.Id != claim.Id))
            .Concat(pendingClaims)
            .ToList();

        var serverMergedWorkOrders = MergeById(localWorkOrders, snapshot.WorkOrders, workOrder => workOrder.Id);
        var mergedWorkOrders = serverMergedWorkOrders
            .Select(serverWorkOrder =>
            {
                var pendingLocal = pendingWorkOrders.FirstOrDefault(w => w.Id == serverWorkOrder.Id);
                return pendingLocal ?? serverWorkOrder;
            })
            .Where(workOrder => pendingWorkOrders.All(pending => pending.Id != workOrder.Id) || workOrder.HasPendingUpload)
            .Concat(pendingWorkOrders.Where(localWorkOrder => serverMergedWorkOrders.All(serverWorkOrder => serverWorkOrder.Id != localWorkOrder.Id)))
            .ToList();

        var nextSnapshot = new SyncSnapshot
        {
            LastSuccessfulSync = snapshot.GeneratedAtUtc,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = true,
            HasCompletedSync = true
        };

        await SaveAsync(CustomersKey, mergedCustomers);
        await SaveAsync(TechniciansKey, mergedTechnicians);
        await SaveAsync(WorkOrdersKey, mergedWorkOrders);
        await SaveAsync(ClaimsKey, mergedClaims);
        await SaveAsync(PendingChangesKey, pendingChanges);
        await SaveAsync(OutboundSyncKey, filteredOutboundState);
        await SaveAsync(SnapshotKey, nextSnapshot);
    }

    public async Task QueueNewClaimAsync(NewClaimDraft draft, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        var claims = (await GetClaimsAsync(cancellationToken)).ToList();
        var pendingChanges = (await GetPendingChangesAsync(cancellationToken)).ToList();
        var outboundState = await GetOutboundSyncStateAsync(cancellationToken);
        var queuedClaims = outboundState.ClaimsToCreate.ToList();
        var queuedWorkOrders = outboundState.WorkOrdersToComplete.ToList();

        var claimId = Guid.NewGuid();
        var claimNumber = $"LOCAL-{claimId.ToString("N")[..6].ToUpperInvariant()}";

        claims.Insert(0, new ClaimSummary
        {
            Id = claimId,
            WorkOrderId = draft.WorkOrderId,
            CustomerId = draft.CustomerId,
            ClaimNumber = claimNumber,
            CustomerName = draft.CustomerName,
            ServiceAddress = draft.ServiceAddress,
            ContactName = draft.ContactName,
            ContactPhone = draft.ContactPhone,
            OriginalInstallerDealer = draft.OriginalInstallerDealer,
            OriginalInstallationDate = draft.OriginalInstallationDate,
            Equipment = draft.Equipment,
            FailureDate = draft.FailureDate ?? draft.RepairDate,
            RepairDate = draft.RepairDate,
            Notes = draft.Notes,
            CompletedBy = draft.CompletedBy,
            CompletedOn = draft.RepairDate,
            Status = "New",
            ComponentCode = draft.ComponentCode,
            ModelNumber = draft.ModelNumber,
            ProductType = string.Empty,
            Product = string.Empty,
            IdSerialNumber = draft.IdSerialNumber,
            ComponentSerialNumber = draft.ComponentSerialNumber,
            ProblemComplaintReported = draft.Notes,
            ProblemFound = string.Empty,
            RepairsPerformed = string.Empty,
            SerialNumber1StorageKey = draft.Serial1Photo is null ? ClaimImageUtil.QuickClaimPlaceholderKey : string.Empty,
            SerialNumber2StorageKey = draft.Serial2Photo is null ? ClaimImageUtil.QuickClaimPlaceholderKey : string.Empty,
            HasPendingUpload = true
        });

        queuedClaims.Insert(0, new QueuedClaimCreate
        {
            LocalClaimId = claimId,
            WorkOrderId = draft.WorkOrderId,
            CustomerId = draft.CustomerId,
            WorkOrderNumber = draft.WorkOrderNumber,
            CustomerName = draft.CustomerName,
            ServiceAddress = draft.ServiceAddress,
            ContactName = draft.ContactName,
            ContactPhone = draft.ContactPhone,
            OriginalInstallerDealer = draft.OriginalInstallerDealer,
            OriginalInstallationDate = draft.OriginalInstallationDate,
            FailureDate = draft.FailureDate,
            RepairDate = draft.RepairDate,
            ComponentCode = draft.ComponentCode,
            ModelNumber = draft.ModelNumber,
            IdSerialNumber = draft.IdSerialNumber,
            ComponentSerialNumber = draft.ComponentSerialNumber,
            Notes = draft.Notes,
            CompletedBy = draft.CompletedBy,
            Serial1Photo = draft.Serial1Photo,
            Serial2Photo = draft.Serial2Photo,
            QueuedAtUtc = DateTimeOffset.UtcNow
        });

        pendingChanges.Insert(0, new PendingChange
        {
            Id = Guid.NewGuid(),
            CorrelationKey = claimId.ToString(),
            EntityType = "Claim",
            EntityIdentifier = claimNumber,
            Action = "Create",
            Summary = string.IsNullOrWhiteSpace(draft.WorkOrderNumber)
                ? $"Quick claim queued for {draft.CustomerName}."
                : $"New claim queued from {draft.WorkOrderNumber}.",
            QueuedAt = DateTimeOffset.UtcNow
        });

        var currentSnapshot = await GetSyncSnapshotAsync(cancellationToken);

        await SaveAsync(ClaimsKey, claims);
        await SaveAsync(PendingChangesKey, pendingChanges);
        await SaveAsync(OutboundSyncKey, new OutboundSyncState
        {
            ClaimsToCreate = queuedClaims,
            ServiceRequestOfficeActions = outboundState.ServiceRequestOfficeActions,
            WorkOrdersToComplete = queuedWorkOrders
        });
        await SaveAsync(SnapshotKey, new SyncSnapshot
        {
            LastSuccessfulSync = currentSnapshot.LastSuccessfulSync,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = currentSnapshot.IsOfflineModeEnabled,
            HasCompletedSync = currentSnapshot.HasCompletedSync
        });
    }

    public async Task UpdateWorkOrderScheduleAsync(Guid workOrderId, DateOnly? scheduledDate, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        var workOrders = (await GetWorkOrdersAsync(cancellationToken)).ToList();
        var workOrderIndex = workOrders.FindIndex(w => w.Id == workOrderId);
        if (workOrderIndex < 0)
        {
            return;
        }

        var workOrder = workOrders[workOrderIndex];
        int? nextScheduledOrder = scheduledDate.HasValue
            ? workOrders
                .Where(w => w.Id != workOrderId && w.ScheduledDate == scheduledDate)
                .Select(w => w.ScheduledOrder ?? 0)
                .DefaultIfEmpty(-1)
                .Max() + 1
            : null;

        workOrders[workOrderIndex] = new WorkOrderSummary
        {
            Id = workOrder.Id,
            CustomerId = workOrder.CustomerId,
            AssignedToUserId = workOrder.AssignedToUserId,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            CustomerName = workOrder.CustomerName,
            Address = workOrder.Address,
            Street1 = workOrder.Street1,
            City = workOrder.City,
            Latitude = workOrder.Latitude,
            Longitude = workOrder.Longitude,
            Status = workOrder.Status,
            Technician = workOrder.Technician,
            GateCode = workOrder.GateCode,
            AnimalsPresent = workOrder.AnimalsPresent,
            ServiceDetails = workOrder.ServiceDetails,
            DateSubmitted = workOrder.DateSubmitted,
            ScheduledDate = scheduledDate,
            ScheduledOrder = nextScheduledOrder,
            ServiceStartupDate = workOrder.ServiceStartupDate,
            ServiceContactName = workOrder.ServiceContactName,
            ServiceContactPhone = workOrder.ServiceContactPhone,
            BuilderCompanyName = workOrder.BuilderCompanyName,
            BuilderContactName = workOrder.BuilderContactName,
            BuilderContactPhone = workOrder.BuilderContactPhone,
            BuilderEmail = workOrder.BuilderEmail,
            CompletedBy = workOrder.CompletedBy,
            CompletedOn = workOrder.CompletedOn,
            HasPendingUpload = workOrder.HasPendingUpload
        };

        await SaveAsync(WorkOrdersKey, workOrders);
    }

    public async Task QueueServiceRequestOfficeActionAsync(Guid workOrderId, DateOnly scheduledDate, Guid assignedToUserId, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        var technicians = await GetTechniciansAsync(cancellationToken);
        var assignedTech = technicians.FirstOrDefault(x => x.Id == assignedToUserId);
        if (assignedTech is null)
        {
            return;
        }

        var workOrders = (await GetWorkOrdersAsync(cancellationToken)).ToList();
        var pendingChanges = (await GetPendingChangesAsync(cancellationToken)).ToList();
        var outboundState = await GetOutboundSyncStateAsync(cancellationToken);
        var queuedClaims = outboundState.ClaimsToCreate.ToList();
        var queuedOfficeActions = outboundState.ServiceRequestOfficeActions.ToList();
        var queuedWorkOrders = outboundState.WorkOrdersToComplete.ToList();

        var workOrderIndex = workOrders.FindIndex(w => w.Id == workOrderId);
        if (workOrderIndex < 0)
        {
            return;
        }

        var workOrder = workOrders[workOrderIndex];
        int nextScheduledOrder = workOrders
            .Where(w => w.Id != workOrderId && w.ScheduledDate == scheduledDate)
            .Select(w => w.ScheduledOrder ?? 0)
            .DefaultIfEmpty(-1)
            .Max() + 1;

        workOrders[workOrderIndex] = new WorkOrderSummary
        {
            Id = workOrder.Id,
            CustomerId = workOrder.CustomerId,
            AssignedToUserId = assignedTech.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            CustomerName = workOrder.CustomerName,
            Address = workOrder.Address,
            Street1 = workOrder.Street1,
            City = workOrder.City,
            Latitude = workOrder.Latitude,
            Longitude = workOrder.Longitude,
            Status = "Active",
            Technician = assignedTech.Username,
            GateCode = workOrder.GateCode,
            AnimalsPresent = workOrder.AnimalsPresent,
            ServiceDetails = workOrder.ServiceDetails,
            DateSubmitted = workOrder.DateSubmitted,
            ScheduledDate = scheduledDate,
            ScheduledOrder = nextScheduledOrder,
            ServiceStartupDate = workOrder.ServiceStartupDate,
            ServiceContactName = workOrder.ServiceContactName,
            ServiceContactPhone = workOrder.ServiceContactPhone,
            BuilderCompanyName = workOrder.BuilderCompanyName,
            BuilderContactName = workOrder.BuilderContactName,
            BuilderContactPhone = workOrder.BuilderContactPhone,
            BuilderEmail = workOrder.BuilderEmail,
            CompletedBy = workOrder.CompletedBy,
            CompletedOn = workOrder.CompletedOn,
            HasPendingUpload = true
        };

        queuedOfficeActions.RemoveAll(x => x.WorkOrderId == workOrderId);
        queuedOfficeActions.Insert(0, new QueuedServiceRequestOfficeAction
        {
            WorkOrderId = workOrder.Id,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            ScheduledDate = scheduledDate,
            AssignedToUserId = assignedTech.Id,
            AssignedToUsername = assignedTech.Username,
            EstimatedHours = null,
            JobCategory = string.Empty,
            QueuedAtUtc = DateTimeOffset.UtcNow
        });

        pendingChanges.RemoveAll(change => change.EntityType == "Work Order" && change.CorrelationKey == workOrderId.ToString());
        pendingChanges.Insert(0, new PendingChange
        {
            Id = Guid.NewGuid(),
            CorrelationKey = workOrderId.ToString(),
            EntityType = "Work Order",
            EntityIdentifier = workOrder.WorkOrderNumber,
            Action = "Schedule",
            Summary = $"Service request scheduled for {scheduledDate:MM/dd/yyyy} and assigned to {assignedTech.Username}.",
            QueuedAt = DateTimeOffset.UtcNow
        });

        var currentSnapshot = await GetSyncSnapshotAsync(cancellationToken);

        await SaveAsync(WorkOrdersKey, workOrders);
        await SaveAsync(PendingChangesKey, pendingChanges);
        await SaveAsync(OutboundSyncKey, new OutboundSyncState
        {
            ClaimsToCreate = queuedClaims,
            ServiceRequestOfficeActions = queuedOfficeActions,
            WorkOrdersToComplete = queuedWorkOrders
        });
        await SaveAsync(SnapshotKey, new SyncSnapshot
        {
            LastSuccessfulSync = currentSnapshot.LastSuccessfulSync,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = currentSnapshot.IsOfflineModeEnabled,
            HasCompletedSync = currentSnapshot.HasCompletedSync
        });
    }

    public async Task ReorderScheduledWorkOrderAsync(Guid workOrderId, int direction, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        if (direction == 0)
        {
            return;
        }

        var workOrders = (await GetWorkOrdersAsync(cancellationToken)).ToList();
        var target = workOrders.FirstOrDefault(w => w.Id == workOrderId);
        if (target?.ScheduledDate is null)
        {
            return;
        }

        var scheduledDate = target.ScheduledDate.Value;
        var dayItems = workOrders
            .Where(w => w.ScheduledDate == scheduledDate)
            .OrderBy(w => w.ScheduledOrder ?? int.MaxValue)
            .ThenBy(w => w.Street1)
            .ThenBy(w => w.Address)
            .ToList();

        var currentIndex = dayItems.FindIndex(w => w.Id == workOrderId);
        if (currentIndex < 0)
        {
            return;
        }

        var destinationIndex = Math.Clamp(currentIndex + direction, 0, dayItems.Count - 1);
        if (destinationIndex == currentIndex)
        {
            return;
        }

        var moving = dayItems[currentIndex];
        dayItems.RemoveAt(currentIndex);
        dayItems.Insert(destinationIndex, moving);

        for (var i = 0; i < dayItems.Count; i++)
        {
            var item = dayItems[i];
            var sourceIndex = workOrders.FindIndex(w => w.Id == item.Id);
            if (sourceIndex < 0)
            {
                continue;
            }

            workOrders[sourceIndex] = new WorkOrderSummary
            {
                Id = item.Id,
                CustomerId = item.CustomerId,
                WorkOrderNumber = item.WorkOrderNumber,
                CustomerName = item.CustomerName,
                Address = item.Address,
                Street1 = item.Street1,
                City = item.City,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Status = item.Status,
                Technician = item.Technician,
                GateCode = item.GateCode,
                AnimalsPresent = item.AnimalsPresent,
                ServiceDetails = item.ServiceDetails,
                DateSubmitted = item.DateSubmitted,
                ScheduledDate = item.ScheduledDate,
                ScheduledOrder = i,
                ServiceStartupDate = item.ServiceStartupDate,
                ServiceContactName = item.ServiceContactName,
                ServiceContactPhone = item.ServiceContactPhone,
                BuilderCompanyName = item.BuilderCompanyName,
                BuilderContactName = item.BuilderContactName,
                BuilderContactPhone = item.BuilderContactPhone,
                BuilderEmail = item.BuilderEmail,
                CompletedBy = item.CompletedBy,
                CompletedOn = item.CompletedOn,
                HasPendingUpload = item.HasPendingUpload
            };
        }

        await SaveAsync(WorkOrdersKey, workOrders);
    }

    public async Task QueueWorkOrderCompletionAsync(Guid workOrderId, string completedBy, DateOnly completedOn, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        var workOrders = (await GetWorkOrdersAsync(cancellationToken)).ToList();
        var claims = (await GetClaimsAsync(cancellationToken)).ToList();
        var pendingChanges = (await GetPendingChangesAsync(cancellationToken)).ToList();
        var outboundState = await GetOutboundSyncStateAsync(cancellationToken);
        var queuedClaims = outboundState.ClaimsToCreate.ToList();
        var queuedOfficeActions = outboundState.ServiceRequestOfficeActions.ToList();
        var queuedWorkOrders = outboundState.WorkOrdersToComplete.ToList();

        var workOrder = workOrders.FirstOrDefault(w => w.Id == workOrderId);
        if (workOrder is null)
        {
            return;
        }

        var updatedWorkOrder = new WorkOrderSummary
        {
            Id = workOrder.Id,
            CustomerId = workOrder.CustomerId,
            AssignedToUserId = workOrder.AssignedToUserId,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            CustomerName = workOrder.CustomerName,
            Address = workOrder.Address,
            Street1 = workOrder.Street1,
            City = workOrder.City,
            Latitude = workOrder.Latitude,
            Longitude = workOrder.Longitude,
            Status = "Completed",
            Technician = workOrder.Technician,
            GateCode = workOrder.GateCode,
            AnimalsPresent = workOrder.AnimalsPresent,
            ServiceDetails = workOrder.ServiceDetails,
            DateSubmitted = workOrder.DateSubmitted,
            ScheduledDate = workOrder.ScheduledDate,
            ScheduledOrder = workOrder.ScheduledOrder,
            ServiceStartupDate = workOrder.ServiceStartupDate,
            ServiceContactName = workOrder.ServiceContactName,
            ServiceContactPhone = workOrder.ServiceContactPhone,
            BuilderCompanyName = workOrder.BuilderCompanyName,
            BuilderContactName = workOrder.BuilderContactName,
            BuilderContactPhone = workOrder.BuilderContactPhone,
            BuilderEmail = workOrder.BuilderEmail,
            CompletedBy = completedBy,
            CompletedOn = completedOn,
            HasPendingUpload = true
        };

        var workOrderIndex = workOrders.FindIndex(w => w.Id == workOrderId);
        workOrders[workOrderIndex] = updatedWorkOrder;

        for (var i = 0; i < claims.Count; i++)
        {
            if (claims[i].WorkOrderId != workOrderId)
            {
                continue;
            }

            claims[i] = new ClaimSummary
            {
                Id = claims[i].Id,
                WorkOrderId = claims[i].WorkOrderId,
                CustomerId = claims[i].CustomerId,
                ClaimNumber = claims[i].ClaimNumber,
                CustomerName = claims[i].CustomerName,
                ServiceAddress = claims[i].ServiceAddress,
                ContactName = claims[i].ContactName,
                ContactPhone = claims[i].ContactPhone,
                OriginalInstallerDealer = claims[i].OriginalInstallerDealer,
                OriginalInstallationDate = claims[i].OriginalInstallationDate,
                Equipment = claims[i].Equipment,
                FailureDate = claims[i].FailureDate,
                RepairDate = claims[i].RepairDate,
                Notes = claims[i].Notes,
                CompletedBy = string.IsNullOrWhiteSpace(completedBy) ? claims[i].CompletedBy : completedBy,
                CompletedOn = completedOn,
                Status = claims[i].Status,
                ComponentCode = claims[i].ComponentCode,
                ModelNumber = claims[i].ModelNumber,
                ProductType = claims[i].ProductType,
                Product = claims[i].Product,
                IdSerialNumber = claims[i].IdSerialNumber,
                ComponentSerialNumber = claims[i].ComponentSerialNumber,
                ProblemComplaintReported = claims[i].ProblemComplaintReported,
                ProblemFound = claims[i].ProblemFound,
                RepairsPerformed = claims[i].RepairsPerformed,
                SerialNumber1StorageKey = claims[i].SerialNumber1StorageKey,
                SerialNumber2StorageKey = claims[i].SerialNumber2StorageKey,
                HasPendingUpload = claims[i].HasPendingUpload
            };
        }

        queuedWorkOrders.RemoveAll(w => w.WorkOrderId == workOrderId);
        queuedWorkOrders.Insert(0, new QueuedWorkOrderCompletion
        {
            WorkOrderId = workOrderId,
            WorkOrderNumber = workOrder.WorkOrderNumber,
            CompletedBy = completedBy,
            CompletedOn = completedOn,
            QueuedAtUtc = DateTimeOffset.UtcNow
        });

        pendingChanges.RemoveAll(change => change.EntityType == "Work Order" && change.EntityIdentifier == workOrder.WorkOrderNumber);
        pendingChanges.Insert(0, new PendingChange
        {
            Id = Guid.NewGuid(),
            CorrelationKey = workOrderId.ToString(),
            EntityType = "Work Order",
            EntityIdentifier = workOrder.WorkOrderNumber,
            Action = "Complete",
            Summary = $"Work order marked complete locally with {queuedClaims.Count(c => c.WorkOrderId == workOrderId)} queued claim(s).",
            QueuedAt = DateTimeOffset.UtcNow
        });

        var currentSnapshot = await GetSyncSnapshotAsync(cancellationToken);

        await SaveAsync(WorkOrdersKey, workOrders);
        await SaveAsync(ClaimsKey, claims);
        await SaveAsync(PendingChangesKey, pendingChanges);
        await SaveAsync(OutboundSyncKey, new OutboundSyncState
        {
            ClaimsToCreate = queuedClaims,
            ServiceRequestOfficeActions = queuedOfficeActions,
            WorkOrdersToComplete = queuedWorkOrders
        });
        await SaveAsync(SnapshotKey, new SyncSnapshot
        {
            LastSuccessfulSync = currentSnapshot.LastSuccessfulSync,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = currentSnapshot.IsOfflineModeEnabled,
            HasCompletedSync = currentSnapshot.HasCompletedSync
        });
    }

    public async Task ClearCompletedSyncAsync(OutboundSyncState syncedState, CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync();

        var claims = (await GetClaimsAsync(cancellationToken)).ToList();
        var workOrders = (await GetWorkOrdersAsync(cancellationToken)).ToList();
        var pendingChanges = (await GetPendingChangesAsync(cancellationToken)).ToList();
        var currentSnapshot = await GetSyncSnapshotAsync(cancellationToken);

        var syncedClaimIds = syncedState.ClaimsToCreate.Select(c => c.LocalClaimId).ToHashSet();
        var syncedWorkOrderIds = syncedState.WorkOrdersToComplete.Select(w => w.WorkOrderId).ToHashSet();
        syncedWorkOrderIds.UnionWith(syncedState.ServiceRequestOfficeActions.Select(w => w.WorkOrderId));

        claims.RemoveAll(c => syncedClaimIds.Contains(c.Id) && c.HasPendingUpload);

        for (var i = 0; i < workOrders.Count; i++)
        {
            if (!syncedWorkOrderIds.Contains(workOrders[i].Id))
            {
                continue;
            }

            workOrders[i] = new WorkOrderSummary
            {
                Id = workOrders[i].Id,
                CustomerId = workOrders[i].CustomerId,
                AssignedToUserId = workOrders[i].AssignedToUserId,
                WorkOrderNumber = workOrders[i].WorkOrderNumber,
                CustomerName = workOrders[i].CustomerName,
                Address = workOrders[i].Address,
                Street1 = workOrders[i].Street1,
                City = workOrders[i].City,
                Latitude = workOrders[i].Latitude,
                Longitude = workOrders[i].Longitude,
                Status = workOrders[i].Status,
                Technician = workOrders[i].Technician,
                GateCode = workOrders[i].GateCode,
                AnimalsPresent = workOrders[i].AnimalsPresent,
                ServiceDetails = workOrders[i].ServiceDetails,
                DateSubmitted = workOrders[i].DateSubmitted,
                ScheduledDate = workOrders[i].ScheduledDate,
                ScheduledOrder = workOrders[i].ScheduledOrder,
                ServiceStartupDate = workOrders[i].ServiceStartupDate,
                ServiceContactName = workOrders[i].ServiceContactName,
                ServiceContactPhone = workOrders[i].ServiceContactPhone,
                BuilderCompanyName = workOrders[i].BuilderCompanyName,
                BuilderContactName = workOrders[i].BuilderContactName,
                BuilderContactPhone = workOrders[i].BuilderContactPhone,
                BuilderEmail = workOrders[i].BuilderEmail,
                CompletedBy = workOrders[i].CompletedBy,
                CompletedOn = workOrders[i].CompletedOn,
                HasPendingUpload = false
            };
        }

        var syncedCorrelationKeys = syncedState.ClaimsToCreate
            .Select(c => c.LocalClaimId.ToString())
            .Concat(syncedState.ServiceRequestOfficeActions.Select(w => w.WorkOrderId.ToString()))
            .Concat(syncedState.WorkOrdersToComplete.Select(w => w.WorkOrderId.ToString()))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        pendingChanges.RemoveAll(change => !string.IsNullOrWhiteSpace(change.CorrelationKey)
            && syncedCorrelationKeys.Contains(change.CorrelationKey));

        await SaveAsync(ClaimsKey, claims);
        await SaveAsync(WorkOrdersKey, workOrders);
        await SaveAsync(PendingChangesKey, pendingChanges);
        await SaveAsync(OutboundSyncKey, new OutboundSyncState());
        await SaveAsync(SnapshotKey, new SyncSnapshot
        {
            LastSuccessfulSync = currentSnapshot.LastSuccessfulSync,
            PendingUploadCount = pendingChanges.Count,
            IsOfflineModeEnabled = currentSnapshot.IsOfflineModeEnabled
        });
    }

    private async Task EnsureSeededAsync()
    {
        if (_hasSeeded)
        {
            return;
        }

        if (await storage.GetStringAsync(WorkOrdersKey) is null)
        {
            await SaveAsync(SnapshotKey, FieldSeedData.CreateSnapshot());
            await SaveAsync(TechniciansKey, Array.Empty<FieldTechnicianOption>());
            await SaveAsync(WorkOrdersKey, Array.Empty<WorkOrderSummary>());
            await SaveAsync(CustomersKey, Array.Empty<CustomerSummary>());
            await SaveAsync(ClaimsKey, Array.Empty<ClaimSummary>());
            await SaveAsync(PendingChangesKey, Array.Empty<PendingChange>());
            await SaveAsync(OutboundSyncKey, new OutboundSyncState());
        }

        await NormalizeLegacySeedQueueAsync();
        await NormalizeLinkedStreet1Async();
        _hasSeeded = true;
    }

    private async Task NormalizeLegacySeedQueueAsync()
    {
        var pendingChanges = await GetRequiredAsync<IReadOnlyList<PendingChange>>(PendingChangesKey, FieldSeedData.CreatePendingChanges);
        var outboundState = await GetRequiredAsync(OutboundSyncKey, () => new OutboundSyncState());

        if (outboundState.ClaimsToCreate.Count > 0 || outboundState.ServiceRequestOfficeActions.Count > 0 || outboundState.WorkOrdersToComplete.Count > 0)
        {
            return;
        }

        if (pendingChanges.Count == 0)
        {
            return;
        }

        var hasOnlyLegacySeedItems = pendingChanges.All(change =>
            !string.IsNullOrWhiteSpace(change.CorrelationKey)
            && change.CorrelationKey.StartsWith("seed-", StringComparison.OrdinalIgnoreCase));

        if (!hasOnlyLegacySeedItems)
        {
            return;
        }

        await SaveAsync(PendingChangesKey, Array.Empty<PendingChange>());

        var currentSnapshot = await GetRequiredAsync(SnapshotKey, FieldSeedData.CreateSnapshot);
        await SaveAsync(SnapshotKey, new SyncSnapshot
        {
            LastSuccessfulSync = currentSnapshot.LastSuccessfulSync,
            PendingUploadCount = 0,
            IsOfflineModeEnabled = currentSnapshot.IsOfflineModeEnabled,
            HasCompletedSync = currentSnapshot.HasCompletedSync
        });
    }

    private async Task NormalizeLinkedStreet1Async()
    {
        var customers = (await GetRequiredAsync<IReadOnlyList<CustomerSummary>>(CustomersKey, FieldSeedData.CreateCustomers)).ToList();
        var workOrders = (await GetRequiredAsync<IReadOnlyList<WorkOrderSummary>>(WorkOrdersKey, FieldSeedData.CreateWorkOrders)).ToList();
        var claims = (await GetRequiredAsync<IReadOnlyList<ClaimSummary>>(ClaimsKey, FieldSeedData.CreateClaims)).ToList();

        var customerStreetLookup = customers
            .Where(c => !string.IsNullOrWhiteSpace(c.Street1))
            .ToDictionary(c => c.Id, c => c.Street1);

        var workOrderStreetLookup = workOrders
            .Where(w => !string.IsNullOrWhiteSpace(w.Street1))
            .ToDictionary(w => w.Id, w => w.Street1);

        var workOrdersChanged = false;
        for (var i = 0; i < workOrders.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(workOrders[i].Street1))
            {
                continue;
            }

            if (!customerStreetLookup.TryGetValue(workOrders[i].CustomerId, out var street1))
            {
                continue;
            }

            workOrders[i] = new WorkOrderSummary
            {
                Id = workOrders[i].Id,
                CustomerId = workOrders[i].CustomerId,
                WorkOrderNumber = workOrders[i].WorkOrderNumber,
                CustomerName = workOrders[i].CustomerName,
                Address = workOrders[i].Address,
                Street1 = street1,
                City = workOrders[i].City,
                Latitude = workOrders[i].Latitude,
                Longitude = workOrders[i].Longitude,
                Status = workOrders[i].Status,
                Technician = workOrders[i].Technician,
                GateCode = workOrders[i].GateCode,
                AnimalsPresent = workOrders[i].AnimalsPresent,
                ServiceDetails = workOrders[i].ServiceDetails,
                DateSubmitted = workOrders[i].DateSubmitted,
                ScheduledDate = workOrders[i].ScheduledDate,
                ScheduledOrder = workOrders[i].ScheduledOrder,
                ServiceStartupDate = workOrders[i].ServiceStartupDate,
                ServiceContactName = workOrders[i].ServiceContactName,
                ServiceContactPhone = workOrders[i].ServiceContactPhone,
                BuilderCompanyName = workOrders[i].BuilderCompanyName,
                BuilderContactName = workOrders[i].BuilderContactName,
                BuilderContactPhone = workOrders[i].BuilderContactPhone,
                BuilderEmail = workOrders[i].BuilderEmail,
                CompletedBy = workOrders[i].CompletedBy,
                CompletedOn = workOrders[i].CompletedOn,
                HasPendingUpload = workOrders[i].HasPendingUpload
            };

            workOrderStreetLookup[workOrders[i].Id] = street1;
            workOrdersChanged = true;
        }

        var claimsChanged = false;
        for (var i = 0; i < claims.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(claims[i].Street1))
            {
                continue;
            }

            string? street1 = null;
            var customerId = claims[i].CustomerId;
            if (customerId is Guid resolvedCustomerId)
            {
                customerStreetLookup.TryGetValue(resolvedCustomerId, out street1);
            }

            var workOrderId = claims[i].WorkOrderId;
            if (string.IsNullOrWhiteSpace(street1) && workOrderId is Guid resolvedWorkOrderId)
            {
                workOrderStreetLookup.TryGetValue(resolvedWorkOrderId, out street1);
            }

            if (string.IsNullOrWhiteSpace(street1))
            {
                continue;
            }

            claims[i] = new ClaimSummary
            {
                Id = claims[i].Id,
                WorkOrderId = claims[i].WorkOrderId,
                CustomerId = claims[i].CustomerId,
                ClaimNumber = claims[i].ClaimNumber,
                CustomerName = claims[i].CustomerName,
                ServiceAddress = claims[i].ServiceAddress,
                Street1 = street1,
                ContactName = claims[i].ContactName,
                ContactPhone = claims[i].ContactPhone,
                OriginalInstallerDealer = claims[i].OriginalInstallerDealer,
                OriginalInstallationDate = claims[i].OriginalInstallationDate,
                Equipment = claims[i].Equipment,
                FailureDate = claims[i].FailureDate,
                RepairDate = claims[i].RepairDate,
                Notes = claims[i].Notes,
                CompletedBy = claims[i].CompletedBy,
                CompletedOn = claims[i].CompletedOn,
                Status = claims[i].Status,
                ComponentCode = claims[i].ComponentCode,
                ModelNumber = claims[i].ModelNumber,
                ProductType = claims[i].ProductType,
                Product = claims[i].Product,
                IdSerialNumber = claims[i].IdSerialNumber,
                ComponentSerialNumber = claims[i].ComponentSerialNumber,
                ProblemComplaintReported = claims[i].ProblemComplaintReported,
                ProblemFound = claims[i].ProblemFound,
                RepairsPerformed = claims[i].RepairsPerformed,
                SerialNumber1StorageKey = claims[i].SerialNumber1StorageKey,
                SerialNumber2StorageKey = claims[i].SerialNumber2StorageKey,
                HasPendingUpload = claims[i].HasPendingUpload
            };

            claimsChanged = true;
        }

        if (workOrdersChanged)
        {
            await SaveAsync(WorkOrdersKey, workOrders);
        }

        if (claimsChanged)
        {
            await SaveAsync(ClaimsKey, claims);
        }
    }

    private async Task<T> GetRequiredAsync<T>(string key, Func<T> fallbackFactory)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T cachedValue)
        {
            return cachedValue;
        }

        var json = await storage.GetStringAsync(key);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (value is not null)
            {
                _memoryCache[key] = value;
                return value;
            }
        }

        var fallback = fallbackFactory();
        await SaveAsync(key, fallback);
        return fallback;
    }

    private async Task SaveAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await storage.SetStringAsync(key, json);
        _memoryCache[key] = value!;
    }

    private static IReadOnlyList<T> MergeById<T, TKey>(IReadOnlyList<T> existing, IReadOnlyList<T> incoming, Func<T, TKey> keySelector)
        where TKey : notnull
    {
        var merged = existing.ToDictionary(keySelector);
        foreach (var item in incoming)
        {
            merged[keySelector(item)] = item;
        }

        return merged.Values.ToList();
    }

    private static bool WasDeletedByServer(PendingChange change, HashSet<Guid> deletedCustomerIds, HashSet<Guid> deletedWorkOrderIds, HashSet<Guid> deletedClaimIds)
    {
        if (!Guid.TryParse(change.CorrelationKey, out var correlationId))
        {
            return false;
        }

        return deletedCustomerIds.Contains(correlationId)
            || deletedWorkOrderIds.Contains(correlationId)
            || deletedClaimIds.Contains(correlationId);
    }
}
