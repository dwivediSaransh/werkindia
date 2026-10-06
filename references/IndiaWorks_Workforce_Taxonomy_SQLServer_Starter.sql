-- IndiaWorks workforce taxonomy starter schema (conceptual, SQL Server)
CREATE TABLE dbo.TaxonomyVersion (
    Id bigint IDENTITY PRIMARY KEY,
    SemanticVersion nvarchar(30) NOT NULL UNIQUE,
    EffectiveFrom datetime2 NOT NULL,
    Status nvarchar(20) NOT NULL,
    ReleaseNotes nvarchar(max) NULL
);
GO
CREATE TABLE dbo.JobFamily (
    Id bigint IDENTITY PRIMARY KEY,
    Code nvarchar(50) NOT NULL UNIQUE,
    Name nvarchar(200) NOT NULL,
    ParentId bigint NULL REFERENCES dbo.JobFamily(Id),
    TaxonomyVersionId bigint NOT NULL REFERENCES dbo.TaxonomyVersion(Id),
    IsActive bit NOT NULL CONSTRAINT DF_JobFamily_IsActive DEFAULT 1
);
GO
CREATE TABLE dbo.Role (
    Id bigint IDENTITY PRIMARY KEY,
    JobFamilyId bigint NOT NULL REFERENCES dbo.JobFamily(Id),
    CanonicalName nvarchar(200) NOT NULL,
    Slug nvarchar(200) NOT NULL,
    TaxonomyVersionId bigint NOT NULL REFERENCES dbo.TaxonomyVersion(Id),
    IsActive bit NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT 1,
    ValidFrom datetime2 NOT NULL CONSTRAINT DF_Role_ValidFrom DEFAULT sysutcdatetime(),
    ValidTo datetime2 NULL,
    CONSTRAINT UQ_Role_Version_Slug UNIQUE(TaxonomyVersionId, Slug)
);
GO
CREATE TABLE dbo.RoleAlias (
    Id bigint IDENTITY PRIMARY KEY,
    RoleId bigint NOT NULL REFERENCES dbo.Role(Id),
    Alias nvarchar(250) NOT NULL,
    LanguageCode nvarchar(10) NULL,
    RegionCode nvarchar(50) NULL,
    AliasType nvarchar(30) NULL,
    Weight decimal(6,3) NULL,
    IsDeprecated bit NOT NULL DEFAULT 0
);
GO
CREATE INDEX IX_RoleAlias_Alias ON dbo.RoleAlias(Alias) INCLUDE(RoleId, Weight);
GO
CREATE TABLE dbo.Specialisation (
    Id bigint IDENTITY PRIMARY KEY,
    RoleId bigint NOT NULL REFERENCES dbo.Role(Id),
    Name nvarchar(200) NOT NULL,
    Code nvarchar(80) NULL,
    ParentId bigint NULL REFERENCES dbo.Specialisation(Id),
    IsActive bit NOT NULL DEFAULT 1
);
GO
CREATE TABLE dbo.Skill (
    Id bigint IDENTITY PRIMARY KEY,
    CanonicalName nvarchar(200) NOT NULL,
    SkillType nvarchar(40) NOT NULL,
    ParentId bigint NULL REFERENCES dbo.Skill(Id),
    IsActive bit NOT NULL DEFAULT 1
);
GO
CREATE TABLE dbo.RoleSkillRequirement (
    RoleId bigint NOT NULL REFERENCES dbo.Role(Id),
    SpecialisationId bigint NULL REFERENCES dbo.Specialisation(Id),
    SkillId bigint NOT NULL REFERENCES dbo.Skill(Id),
    IsMandatory bit NOT NULL DEFAULT 0,
    Importance decimal(5,2) NULL,
    MinProficiency tinyint NULL,
    CONSTRAINT PK_RoleSkillRequirement PRIMARY KEY(RoleId, SkillId, SpecialisationId)
);
GO
CREATE TABLE dbo.ExternalOccupationMapping (
    Id bigint IDENTITY PRIMARY KEY,
    RoleId bigint NOT NULL REFERENCES dbo.Role(Id),
    SpecialisationId bigint NULL REFERENCES dbo.Specialisation(Id),
    Scheme nvarchar(30) NOT NULL, -- NCO, ISCO, NQR, QP, NOS, NIC where appropriate
    ExternalCode nvarchar(100) NOT NULL,
    ExternalName nvarchar(300) NULL,
    FrameworkVersion nvarchar(50) NOT NULL,
    NSQFLevel decimal(3,1) NULL,
    SourceUrl nvarchar(1000) NULL,
    VerifiedAt datetime2 NULL
);
GO
CREATE TABLE dbo.CandidateOccupation (
    CandidateId bigint NOT NULL,
    RoleId bigint NOT NULL REFERENCES dbo.Role(Id),
    SpecialisationId bigint NULL REFERENCES dbo.Specialisation(Id),
    IsPrimary bit NOT NULL DEFAULT 0,
    Priority tinyint NULL,
    ExperienceMonths int NULL,
    IsCurrent bit NOT NULL DEFAULT 0,
    VerificationStatus tinyint NOT NULL DEFAULT 0,
    CONSTRAINT PK_CandidateOccupation PRIMARY KEY(CandidateId, RoleId, SpecialisationId)
);
GO
CREATE INDEX IX_CandidateOccupation_Search ON dbo.CandidateOccupation(RoleId, SpecialisationId, ExperienceMonths, CandidateId);
GO
CREATE TABLE dbo.CandidateSkill (
    CandidateId bigint NOT NULL,
    SkillId bigint NOT NULL REFERENCES dbo.Skill(Id),
    Proficiency tinyint NULL,
    ExperienceMonths int NULL,
    LastUsedDate date NULL,
    IsVerified bit NOT NULL DEFAULT 0,
    CONSTRAINT PK_CandidateSkill PRIMARY KEY(CandidateId, SkillId)
);
GO
CREATE INDEX IX_CandidateSkill_Search ON dbo.CandidateSkill(SkillId, Proficiency, ExperienceMonths, CandidateId);
GO
-- Additional domain tables recommended: CredentialType, CandidateCredential, Industry(NIC-2025),
-- OrganisationIndustry, JobPosting, JobPostingRole, JobSkillRequirement, JobCredentialRequirement,
-- CandidateLocationPreference, CandidateAvailability, CandidateEngagementPreference,
-- CandidateConsent, CandidateAccessAudit, SubscriptionPlan, OrganisationSubscription.
