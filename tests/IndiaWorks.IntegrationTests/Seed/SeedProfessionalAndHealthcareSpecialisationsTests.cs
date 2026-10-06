using System.IO;
using IndiaWorks.Domain.E03;
using IndiaWorks.Infrastructure.Persistence;
using IndiaWorks.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Xunit;

namespace IndiaWorks.IntegrationTests.Seed;

public sealed class SeedProfessionalAndHealthcareSpecialisationsTests
{
    [Fact]
    public async Task SeedRunner_SeedsProfessionalAndHealthcareSpecialisationsAndCredentials()
    {
        await using var context = new AppDbContext(CreateInMemoryOptions());
        await context.Database.EnsureCreatedAsync();

        var seedJson = await ReadSeedJsonAsync();
        await CreateVersionedTaxonomySeedImporter.SeedAsync(context, seedJson);

        var version = await context.TaxonomyVersions.AsNoTracking().FirstOrDefaultAsync();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version.SemanticVersion);
        Assert.Equal(TaxonomyVersionStatus.Active, version.Status);

        var professionalRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "professional-consultant" && r.TaxonomyVersionId == version.Id);
        var nurseRole = await context.Roles.AsNoTracking()
            .FirstAsync(r => r.Slug == "registered-nurse" && r.TaxonomyVersionId == version.Id);

        // Verify all expected CredentialTypes were seeded
        var credentialTypes = await context.CredentialTypes.AsNoTracking()
            .Where(c => c.TaxonomyVersionId == version.Id)
            .OrderBy(c => c.Code)
            .ToListAsync();
        Assert.NotEmpty(credentialTypes);

        // Verify accountant specialisations
        var accountantSpecialisations = await context.Specialisations.AsNoTracking()
            .Where(s => s.RoleId == professionalRole.Id && s.Code == "ACCOUNTANT")
            .ToListAsync();
        Assert.NotEmpty(accountantSpecialisations);

        var taxSpecialist = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "TAX_SPECIALIST");
        Assert.Equal("Tax Specialist", taxSpecialist.Name);

        // Verify sales-related specialisations
        var salesRep = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "SALES_REPRESENTATIVE");
        Assert.Equal("Sales Representative", salesRep.Name);

        var bizDev = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "BUSINESS_DEVELOPER");
        Assert.Equal("Business Developer", bizDev.Name);

        // Verify HR specialisations
        var hrGeneralist = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "HR_GENERALIST");
        Assert.Equal("HR Generalist", hrGeneralist.Name);

        var recruitmentSpec = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "RECRUITMENT_SPECIALIST");
        Assert.Equal("Recruitment Specialist", recruitmentSpec.Name);

        // Verify software specialisations
        var softwareDev = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "SOFTWARE_DEVELOPER");
        Assert.Equal("Software Developer", softwareDev.Name);

        var fullStack = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "FULL_STACK_DEVELOPER");
        Assert.Equal("Full Stack Developer", fullStack.Name);

        // Verify teacher specialisations
        var teacher = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "TEACHER");
        Assert.Equal("Teacher", teacher.Name);

        var tutor = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == professionalRole.Id && s.Code == "TUTOR");
        Assert.Equal("Tutor", tutor.Name);

        // Verify nursing specialisations
        var staffNurse = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "STAFF_NURSE");
        Assert.Equal("Staff Nurse", staffNurse.Name);

        var theatreNurse = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "OPERATING_THEATRE_NURSE");
        Assert.Equal("Operating Theatre Nurse", theatreNurse.Name);

        var communityNurse = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "COMMUNITY_HEALTH_NURSE");
        Assert.Equal("Community Health Nurse", communityNurse.Name);

        // Verify lab specialisations
        var labTech = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "LAB_TECHNICIAN");
        Assert.Equal("Lab Technician", labTech.Name);

        var pathologyLab = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "PATHOLOGY_LAB_TECHNICIAN");
        Assert.Equal("Pathology Lab Technician", pathologyLab.Name);

        // Verify radiology specialisation
        var radiologyTech = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "RADIOLOGY_TECHNICIAN");
        Assert.Equal("Radiology Technician", radiologyTech.Name);

        // Verify medical specialisations
        var physician = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "PHYSICIAN");
        Assert.Equal("Physician", physician.Name);

        var gp = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "GENERAL_PRACTITIONER");
        Assert.Equal("General Practitioner", gp.Name);

        // Verify paramedic specialisation
        var paramedic = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "PARAMEDIC_SPECIALIST");
        Assert.Equal("Paramedic", paramedic.Name);

        // Verify pharmacy specialisation
        var pharmacist = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "PHARMACIST");
        Assert.Equal("Pharmacist", pharmacist.Name);

        // Verify physiotherapy specialisation
        var physio = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "PHYSIOTHERAPIST");
        Assert.Equal("Physiotherapist", physio.Name);

        // Verify dental specialisation
        var dentalHygienist = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "DENTAL_HYGIENIST");
        Assert.Equal("Dental Hygienist", dentalHygienist.Name);

        // Verify optometry specialisation
        var optometrist = await context.Specialisations.AsNoTracking()
            .FirstAsync(s => s.RoleId == nurseRole.Id && s.Code == "OPTOMETRIST");
        Assert.Equal("Optometrist", optometrist.Name);
    }

    [Fact]
    public void SeedDataset_ContainsNoForbiddenSecretOrContactData()
    {
        var seedJson = File.ReadAllText(System.IO.Path.Combine(GetRepoRoot(), "src", "backend",
            "IndiaWorks.Infrastructure", "Persistence", "Seed",
            "seed-professional-and-healthcare-specialisations.json"));

        var lower = seedJson.ToLowerInvariant();
        Assert.DoesNotContain("+91", lower);
        Assert.DoesNotContain("@", lower);
        Assert.DoesNotContain("otp", lower);
        Assert.DoesNotContain("token", lower);
        Assert.DoesNotContain("aadh", lower);
        Assert.DoesNotContain("pan ", lower);

        var dataset = System.Text.Json.JsonSerializer.Deserialize<TaxonomySeedDataset>(
            seedJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.NotNull(dataset);
        Assert.All(dataset.Rows, row =>
        {
            Assert.DoesNotContain("@", row.Name);
            Assert.DoesNotContain("+", row.Name);
            if (row.Description is not null)
            {
                Assert.DoesNotContain("@", row.Description);
                Assert.DoesNotContain("+", row.Description);
            }
        });
    }

    private static DbContextOptions<AppDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(SeedProfessionalAndHealthcareSpecialisationsTests))
            .Options;
    }

    private static async Task<string> ReadSeedJsonAsync()
    {
        var repoRoot = GetRepoRoot();
        var path = System.IO.Path.Combine(repoRoot, "src", "backend", "IndiaWorks.Infrastructure",
            "Persistence", "Seed", "seed-professional-and-healthcare-specialisations.json");
        return await File.ReadAllTextAsync(path);
    }

    private static string GetRepoRoot()
    {
        var assemblyLocation = typeof(SeedProfessionalAndHealthcareSpecialisationsTests).Assembly.Location;
        var testProjectDir = System.IO.Path.GetDirectoryName(assemblyLocation)!;
        var candidate = System.IO.Path.GetFullPath(System.IO.Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
        return candidate;
    }
}
