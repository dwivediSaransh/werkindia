using System.Text.Json;
using IndiaWorks.Domain.E04;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed.Models;
using Microsoft.EntityFrameworkCore;

namespace IndiaWorks.Infrastructure.Persistence.Seed;

public static partial class CreatePlanSeedImporter
{
    public static async Task SeedAsync(
        AppDbContext db,
        string seedJson,
        CancellationToken ct = default)
    {
        var dataset = JsonSerializer.Deserialize<PlanSeedDataset>(seedJson, JsonOptions);

        if (dataset is null)
        {
            throw new SeedDataException("Plan seed dataset could not be deserialized.");
        }

        var codeToEntitlementDefId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var codeToPlanId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var planVersionKeyToId = new Dictionary<(Guid PlanId, BillingInterval Interval), Guid>();

        foreach (var row in dataset.Rows)
        {
            switch (row.EntityType)
            {
                case "EntitlementDefinition":
                    await ImportEntitlementDefinitionAsync(db, row, codeToEntitlementDefId, ct);
                    break;
                case "SubscriptionPlan":
                    await ImportSubscriptionPlanAsync(db, row, codeToPlanId, ct);
                    break;
                case "PlanVersion":
                    await ImportPlanVersionAsync(db, row, codeToPlanId, planVersionKeyToId, ct);
                    break;
                case "PlanEntitlement":
                    await ImportPlanEntitlementAsync(db, row, codeToPlanId, planVersionKeyToId, codeToEntitlementDefId, ct);
                    break;
                default:
                    throw new SeedDataException($"Unknown entityType '{row.EntityType}'.");
            }
        }
    }

    private static async Task ImportEntitlementDefinitionAsync(
        AppDbContext db,
        PlanSeedRow row,
        Dictionary<string, string> codeToId,
        CancellationToken ct)
    {
        if (row.Code is null)
            throw new SeedDataException("EntitlementDefinition row missing code.");
        if (row.Name is null)
            throw new SeedDataException($"EntitlementDefinition '{row.Code}' missing name.");
        if (row.Unit is null)
            throw new SeedDataException($"EntitlementDefinition '{row.Code}' missing unit.");
        if (row.ResetCadence is null)
            throw new SeedDataException($"EntitlementDefinition '{row.Code}' missing resetCadence.");

        var existing = await db.EntitlementDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Code;
            return;
        }

        var unit = ParseEntitlementUnit(row.Unit);
        var resetCadence = ParseResetCadence(row.ResetCadence);
        var entitlement = EntitlementDefinition.Create(
            row.Code, row.Name, unit, resetCadence,
            string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim());

        db.EntitlementDefinitions.Add(entitlement);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = entitlement.Code;
    }

    private static async Task ImportSubscriptionPlanAsync(
        AppDbContext db,
        PlanSeedRow row,
        Dictionary<string, Guid> codeToId,
        CancellationToken ct)
    {
        if (row.Code is null)
            throw new SeedDataException("SubscriptionPlan row missing code.");
        if (row.Name is null)
            throw new SeedDataException($"SubscriptionPlan '{row.Code}' missing name.");

        var existing = await db.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == row.Code, ct);

        if (existing is not null)
        {
            codeToId[row.Code] = existing.Id;
            return;
        }

        var plan = SubscriptionPlan.Create(row.Code, row.Name, row.Description);
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        codeToId[row.Code] = plan.Id;
    }

    private static async Task ImportPlanVersionAsync(
        AppDbContext db,
        PlanSeedRow row,
        Dictionary<string, Guid> codeToPlanId,
        Dictionary<(Guid PlanId, BillingInterval Interval), Guid> planVersionKeyToId,
        CancellationToken ct)
    {
        if (row.PlanCode is null)
            throw new SeedDataException("PlanVersion row missing planCode.");
        if (row.Interval is null)
            throw new SeedDataException($"PlanVersion for plan '{row.PlanCode}' missing interval.");
        if (row.Currency is null)
            throw new SeedDataException($"PlanVersion for plan '{row.PlanCode}' missing currency.");

        if (!codeToPlanId.TryGetValue(row.PlanCode, out var planId))
            throw new SeedDataException($"PlanVersion references unknown planCode '{row.PlanCode}'.");

        var interval = ParseBillingInterval(row.Interval);
        var key = (planId, interval);

        if (planVersionKeyToId.ContainsKey(key))
            return;

        var version = SubscriptionPlanVersion.Create(planId, interval, row.Currency, row.Amount, row.TrialDays);
        db.SubscriptionPlanVersions.Add(version);
        await db.SaveChangesAsync(ct);
        planVersionKeyToId[key] = version.Id;
    }

    private static async Task ImportPlanEntitlementAsync(
        AppDbContext db,
        PlanSeedRow row,
        Dictionary<string, Guid> codeToPlanId,
        Dictionary<(Guid PlanId, BillingInterval Interval), Guid> planVersionKeyToId,
        Dictionary<string, string> codeToEntitlementDefId,
        CancellationToken ct)
    {
        if (row.PlanCode is null)
            throw new SeedDataException("PlanEntitlement row missing planCode.");
        if (row.Interval is null)
            throw new SeedDataException("PlanEntitlement row missing interval.");
        if (row.EntitlementCode is null)
            throw new SeedDataException("PlanEntitlement row missing entitlementCode.");

        if (!codeToPlanId.TryGetValue(row.PlanCode, out var planId))
            throw new SeedDataException($"PlanEntitlement references unknown planCode '{row.PlanCode}'.");

        var interval = ParseBillingInterval(row.Interval);
        var key = (planId, interval);

        if (!planVersionKeyToId.TryGetValue(key, out var planVersionId))
            throw new SeedDataException(
                $"PlanEntitlement for plan '{row.PlanCode}' interval '{row.Interval}': plan version not found. Ensure PlanVersion rows precede PlanEntitlement rows.");

        if (!codeToEntitlementDefId.ContainsKey(row.EntitlementCode))
            throw new SeedDataException($"PlanEntitlement references unknown entitlementCode '{row.EntitlementCode}'.");

        var existing = await db.PlanEntitlements.AsNoTracking()
            .FirstOrDefaultAsync(pe => pe.PlanVersionId == planVersionId && pe.EntitlementDefinitionCode == row.EntitlementCode, ct);

        if (existing is not null)
            return;

        var planVersion = await db.SubscriptionPlanVersions.FindAsync(new object[] { planVersionId }, ct);
        var entitlementDef = await db.EntitlementDefinitions.FindAsync(new object[] { row.EntitlementCode }, ct);

        if (planVersion is null)
            throw new SeedDataException($"PlanEntitlement: plan version {planVersionId} not found in database.");
        if (entitlementDef is null)
            throw new SeedDataException($"PlanEntitlement: entitlement definition '{row.EntitlementCode}' not found in database.");

        var entitlement = PlanEntitlement.Create(planVersion, entitlementDef, row.Enabled, row.Limit);
        db.PlanEntitlements.Add(entitlement);
        await db.SaveChangesAsync(ct);
    }

    private static BillingInterval ParseBillingInterval(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "monthly" => BillingInterval.Monthly,
            "yearly" => BillingInterval.Yearly,
            "trial" => BillingInterval.Trial,
            "onetime" => BillingInterval.OneTime,
            _ => throw new SeedDataException($"Unknown BillingInterval '{value}'.")
        };
    }

    private static EntitlementUnit ParseEntitlementUnit(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "boolean" => EntitlementUnit.Boolean,
            "count" => EntitlementUnit.Count,
            "credit" => EntitlementUnit.Credit,
            "seat" => EntitlementUnit.Seat,
            "gb" => EntitlementUnit.Gb,
            "apicall" => EntitlementUnit.ApiCall,
            _ => throw new SeedDataException($"Unknown EntitlementUnit '{value}'.")
        };
    }

    private static ResetCadence ParseResetCadence(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "none" => ResetCadence.None,
            "daily" => ResetCadence.Daily,
            "monthly" => ResetCadence.Monthly,
            "yearly" => ResetCadence.Yearly,
            "billingperiod" => ResetCadence.BillingPeriod,
            _ => throw new SeedDataException($"Unknown ResetCadence '{value}'.")
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}