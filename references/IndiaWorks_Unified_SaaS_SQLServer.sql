/*
 IndiaWorks Unified SaaS - SQL Server relationship reference v7
 Architectural reference only, not a final migration script. EF Core code-first migrations are the production schema authority.
 Do not execute this file against any environment. Model relationships in EF Core and generate reviewed migrations instead.
*/

CREATE TABLE dbo.UserAccount (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Status VARCHAR(30) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_UserAccount_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAt DATETIME2(3) NULL,
    RowVersion ROWVERSION
);

CREATE TABLE dbo.LoginIdentifier (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserAccountId BIGINT NOT NULL,
    IdentifierType VARCHAR(20) NOT NULL, -- MOBILE / EMAIL
    NormalizedValue NVARCHAR(320) NOT NULL,
    VerifiedAt DATETIME2(3) NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_LoginIdentifier_IsPrimary DEFAULT 0,
    CONSTRAINT FK_LoginIdentifier_UserAccount FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT UQ_LoginIdentifier UNIQUE(IdentifierType, NormalizedValue)
);

CREATE TABLE dbo.OtpChallenge (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserAccountId BIGINT NULL,
    Purpose VARCHAR(30) NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    DestinationHash VARBINARY(32) NOT NULL,
    OtpHash VARBINARY(64) NOT NULL,
    ExpiresAt DATETIME2(3) NOT NULL,
    AttemptCount INT NOT NULL DEFAULT 0,
    MaxAttempts INT NOT NULL DEFAULT 5,
    Status VARCHAR(20) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    VerifiedAt DATETIME2(3) NULL,
    CONSTRAINT FK_OtpChallenge_UserAccount FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_OtpChallenge_Status_ExpiresAt ON dbo.OtpChallenge(Status, ExpiresAt);

CREATE TABLE dbo.Organisation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    LegalName NVARCHAR(250) NOT NULL,
    TradeName NVARCHAR(250) NULL,
    OrganisationType VARCHAR(50) NULL,
    EmployerSector VARCHAR(30) NULL, -- PRIVATE/GOVERNMENT/PSU/NGO/HOUSEHOLD
    EmployeeBand VARCHAR(30) NULL,
    Nic2025Code VARCHAR(10) NULL,
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION
);

CREATE TABLE dbo.OrganisationIdentifier (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    IdentifierType VARCHAR(20) NOT NULL, -- PAN/GSTIN/CIN/LLPIN
    IdentifierValueEncrypted VARBINARY(MAX) NULL,
    IdentifierHash VARBINARY(32) NOT NULL,
    MaskedDisplay NVARCHAR(50) NULL,
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    VerificationSource VARCHAR(50) NULL,
    VerifiedAt DATETIME2(3) NULL,
    CONSTRAINT FK_OrganisationIdentifier_Organisation FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT UQ_OrganisationIdentifier UNIQUE(IdentifierType, IdentifierHash)
);

CREATE TABLE dbo.OrganisationMembership (
    OrganisationId BIGINT NOT NULL,
    UserAccountId BIGINT NOT NULL,
    Role VARCHAR(30) NOT NULL, -- OWNER/ADMIN/RECRUITER/VIEWER/BILLING_ADMIN
    Status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(OrganisationId, UserAccountId),
    CONSTRAINT FK_OrgMembership_Organisation FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_OrgMembership_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);

CREATE TABLE dbo.Geography (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ParentId BIGINT NULL,
    GeoType VARCHAR(20) NOT NULL, -- COUNTRY/STATE/DISTRICT/CITY/LOCALITY/PIN
    Code NVARCHAR(30) NULL,
    Name NVARCHAR(200) NOT NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    CONSTRAINT FK_Geography_Parent FOREIGN KEY(ParentId) REFERENCES dbo.Geography(Id)
);

CREATE TABLE dbo.OrganisationLocation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    LocationType VARCHAR(30) NOT NULL, -- REGISTERED_OFFICE/HEAD_OFFICE/BRANCH/HIRING_OFFICE
    GeographyId BIGINT NULL,
    AddressLine1 NVARCHAR(250) NULL,
    AddressLine2 NVARCHAR(250) NULL,
    PostalCode NVARCHAR(12) NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_OrganisationLocation_Organisation FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_OrganisationLocation_Geography FOREIGN KEY(GeographyId) REFERENCES dbo.Geography(Id)
);

CREATE TABLE dbo.TaxonomyVersion (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VersionName VARCHAR(30) NOT NULL UNIQUE,
    EffectiveFrom DATE NOT NULL,
    EffectiveTo DATE NULL,
    Status VARCHAR(20) NOT NULL
);

CREATE TABLE dbo.JobFamily (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TaxonomyVersionId INT NOT NULL,
    ParentId BIGINT NULL,
    Code VARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_JobFamily_TaxVersion FOREIGN KEY(TaxonomyVersionId) REFERENCES dbo.TaxonomyVersion(Id),
    CONSTRAINT FK_JobFamily_Parent FOREIGN KEY(ParentId) REFERENCES dbo.JobFamily(Id),
    CONSTRAINT UQ_JobFamily UNIQUE(TaxonomyVersionId, Code)
);

CREATE TABLE dbo.Role (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TaxonomyVersionId INT NOT NULL,
    JobFamilyId BIGINT NOT NULL,
    Code VARCHAR(80) NOT NULL,
    CanonicalName NVARCHAR(200) NOT NULL,
    Nco2015Code VARCHAR(20) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ValidFrom DATE NULL,
    ValidTo DATE NULL,
    CONSTRAINT FK_Role_TaxVersion FOREIGN KEY(TaxonomyVersionId) REFERENCES dbo.TaxonomyVersion(Id),
    CONSTRAINT FK_Role_JobFamily FOREIGN KEY(JobFamilyId) REFERENCES dbo.JobFamily(Id),
    CONSTRAINT UQ_Role UNIQUE(TaxonomyVersionId, Code)
);

CREATE TABLE dbo.RoleAlias (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    Alias NVARCHAR(200) NOT NULL,
    LanguageCode VARCHAR(10) NULL,
    RegionCode VARCHAR(20) NULL,
    AliasType VARCHAR(30) NULL,
    Weight DECIMAL(5,2) NOT NULL DEFAULT 1,
    CONSTRAINT FK_RoleAlias_Role FOREIGN KEY(RoleId) REFERENCES dbo.Role(Id)
);
CREATE INDEX IX_RoleAlias_Alias ON dbo.RoleAlias(Alias);

CREATE TABLE dbo.Specialisation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    ParentId BIGINT NULL,
    Code VARCHAR(80) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Specialisation_Role FOREIGN KEY(RoleId) REFERENCES dbo.Role(Id),
    CONSTRAINT FK_Specialisation_Parent FOREIGN KEY(ParentId) REFERENCES dbo.Specialisation(Id),
    CONSTRAINT UQ_Specialisation UNIQUE(RoleId, Code)
);

CREATE TABLE dbo.WorkContext (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    Code VARCHAR(80) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_WorkContext_Role FOREIGN KEY(RoleId) REFERENCES dbo.Role(Id),
    CONSTRAINT UQ_WorkContext UNIQUE(RoleId, Code)
);

CREATE TABLE dbo.Skill (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ParentId BIGINT NULL,
    Code VARCHAR(80) NOT NULL UNIQUE,
    CanonicalName NVARCHAR(200) NOT NULL,
    SkillType VARCHAR(30) NOT NULL,
    CONSTRAINT FK_Skill_Parent FOREIGN KEY(ParentId) REFERENCES dbo.Skill(Id)
);

CREATE TABLE dbo.CredentialType (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(80) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL,
    IssuingAuthorityType VARCHAR(50) NULL
);

CREATE TABLE dbo.AssetType (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(80) NOT NULL UNIQUE,
    Name NVARCHAR(200) NOT NULL
);

CREATE TABLE dbo.JobPosting (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    TaxonomyVersionId INT NOT NULL,
    Status VARCHAR(40) NOT NULL DEFAULT 'DRAFT',
    EngagementType VARCHAR(30) NOT NULL,
    WorkMode VARCHAR(30) NOT NULL,
    VacancyCount INT NOT NULL,
    HiringUrgency VARCHAR(30) NOT NULL,
    TargetStartDate DATE NULL,
    ApplicationCloseDate DATE NULL,
    Description NVARCHAR(MAX) NOT NULL,
    FirstPublishedAt DATETIME2(3) NULL,
    PublishedAt DATETIME2(3) NULL,
    FilledAt DATETIME2(3) NULL,
    ClosedAt DATETIME2(3) NULL,
    CreatedByUserAccountId BIGINT NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    MaintainedAt DATETIME2(3) NULL,
    RowVersion ROWVERSION,
    CONSTRAINT CK_JobPosting_VacancyCount CHECK(VacancyCount > 0),
    CONSTRAINT FK_JobPosting_Organisation FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_JobPosting_TaxVersion FOREIGN KEY(TaxonomyVersionId) REFERENCES dbo.TaxonomyVersion(Id),
    CONSTRAINT FK_JobPosting_CreatedBy FOREIGN KEY(CreatedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_JobPosting_Org_Status ON dbo.JobPosting(OrganisationId, Status, CreatedAt DESC);

CREATE TABLE dbo.JobPostingRole (
    JobPostingId BIGINT NOT NULL PRIMARY KEY,
    RoleId BIGINT NOT NULL,
    SpecialisationId BIGINT NULL,
    WorkContextId BIGINT NULL,
    CONSTRAINT FK_JobPostingRole_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobPostingRole_Role FOREIGN KEY(RoleId) REFERENCES dbo.Role(Id),
    CONSTRAINT FK_JobPostingRole_Specialisation FOREIGN KEY(SpecialisationId) REFERENCES dbo.Specialisation(Id),
    CONSTRAINT FK_JobPostingRole_WorkContext FOREIGN KEY(WorkContextId) REFERENCES dbo.WorkContext(Id)
);

CREATE TABLE dbo.JobLocation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    GeographyId BIGINT NULL,
    AddressLine1 NVARCHAR(250) NULL,
    AddressLine2 NVARCHAR(250) NULL,
    PostalCode NVARCHAR(12) NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    CandidateRadiusKm DECIMAL(8,2) NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_JobLocation_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobLocation_Geo FOREIGN KEY(GeographyId) REFERENCES dbo.Geography(Id)
);
CREATE INDEX IX_JobLocation_Geography ON dbo.JobLocation(GeographyId, JobPostingId);

CREATE TABLE dbo.JobExperienceRequirement (
    JobPostingId BIGINT NOT NULL PRIMARY KEY,
    ExperiencePolicy VARCHAR(30) NOT NULL,
    MinTotalMonths INT NULL,
    MaxTotalMonths INT NULL,
    MinRelevantMonths INT NULL,
    MaxRelevantMonths INT NULL,
    CONSTRAINT FK_JobExperience_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobSkillRequirement (
    JobPostingId BIGINT NOT NULL,
    SkillId BIGINT NOT NULL,
    RequirementLevel VARCHAR(20) NOT NULL,
    MinProficiency VARCHAR(30) NULL,
    MinMonthsExperience INT NULL,
    ImportanceWeight DECIMAL(5,2) NULL,
    PRIMARY KEY(JobPostingId, SkillId),
    CONSTRAINT FK_JobSkill_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobSkill_Skill FOREIGN KEY(SkillId) REFERENCES dbo.Skill(Id)
);

CREATE TABLE dbo.JobCredentialRequirement (
    JobPostingId BIGINT NOT NULL,
    CredentialTypeId BIGINT NOT NULL,
    RequirementLevel VARCHAR(20) NOT NULL,
    MustBeValidOnStartDate BIT NOT NULL DEFAULT 1,
    VerifiedOnly BIT NOT NULL DEFAULT 0,
    MetadataJson NVARCHAR(MAX) NULL,
    PRIMARY KEY(JobPostingId, CredentialTypeId),
    CONSTRAINT FK_JobCred_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobCred_Type FOREIGN KEY(CredentialTypeId) REFERENCES dbo.CredentialType(Id),
    CONSTRAINT CK_JobCred_MetadataJson CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);

CREATE TABLE dbo.JobAssetRequirement (
    JobPostingId BIGINT NOT NULL,
    AssetTypeId BIGINT NOT NULL,
    RequirementLevel VARCHAR(20) NOT NULL,
    OwnershipRequired BIT NOT NULL DEFAULT 0,
    PRIMARY KEY(JobPostingId, AssetTypeId),
    CONSTRAINT FK_JobAsset_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobAsset_Type FOREIGN KEY(AssetTypeId) REFERENCES dbo.AssetType(Id)
);

CREATE TABLE dbo.JobLanguageRequirement (
    JobPostingId BIGINT NOT NULL,
    LanguageCode VARCHAR(10) NOT NULL,
    RequirementLevel VARCHAR(20) NOT NULL,
    MinProficiency VARCHAR(30) NULL,
    PRIMARY KEY(JobPostingId, LanguageCode),
    CONSTRAINT FK_JobLanguage_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobEducationRequirement (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    EducationLevelCode VARCHAR(30) NOT NULL,
    FieldOfStudyCode VARCHAR(50) NULL,
    RequirementLevel VARCHAR(20) NOT NULL,
    CONSTRAINT FK_JobEducation_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobSchedule (
    JobPostingId BIGINT NOT NULL PRIMARY KEY,
    ShiftType VARCHAR(30) NULL,
    StartTime TIME NULL,
    EndTime TIME NULL,
    WeekdaysMask TINYINT NULL, -- application-level bit mask Mon..Sun
    WeeklyHours DECIMAL(5,2) NULL,
    IsRotational BIT NOT NULL DEFAULT 0,
    IsNightShift BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_JobSchedule_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.CompensationPackage (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL UNIQUE,
    Currency CHAR(3) NOT NULL DEFAULT 'INR',
    PayFrequency VARCHAR(20) NOT NULL,
    FixedMin DECIMAL(18,2) NULL,
    FixedMax DECIMAL(18,2) NULL,
    GrossMin DECIMAL(18,2) NULL,
    GrossMax DECIMAL(18,2) NULL,
    DisplayedInHandMin DECIMAL(18,2) NULL,
    DisplayedInHandMax DECIMAL(18,2) NULL,
    CONSTRAINT FK_CompPackage_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.CompensationComponent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CompensationPackageId BIGINT NOT NULL,
    ComponentType VARCHAR(30) NOT NULL, -- BONUS/INCENTIVE/ALLOWANCE/COMMISSION/OVERTIME
    MinAmount DECIMAL(18,2) NULL,
    MaxAmount DECIMAL(18,2) NULL,
    Frequency VARCHAR(20) NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT FK_CompComponent_Package FOREIGN KEY(CompensationPackageId) REFERENCES dbo.CompensationPackage(Id)
);

CREATE TABLE dbo.JobContactPolicy (
    JobPostingId BIGINT NOT NULL PRIMARY KEY,
    CandidateMayCall BIT NOT NULL DEFAULT 0,
    CandidateMayMessage BIT NOT NULL DEFAULT 1,
    ShowRecruiterNumber BIT NOT NULL DEFAULT 0,
    MaskedNumber BIT NOT NULL DEFAULT 1,
    CustomInstructions NVARCHAR(500) NULL,
    CONSTRAINT FK_JobContactPolicy_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobContactWindow (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    Weekday TINYINT NOT NULL,
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    CONSTRAINT FK_JobContactWindow_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT CK_JobContactWindow_Weekday CHECK(Weekday BETWEEN 1 AND 7)
);

CREATE TABLE dbo.CandidateProfile (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserAccountId BIGINT NOT NULL UNIQUE,
    DisplayName NVARCHAR(200) NULL,
    ProfileStatus VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
    VisibilityStatus VARCHAR(30) NOT NULL DEFAULT 'LIMITED',
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CandidateProfile_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);

CREATE TABLE dbo.CandidateOccupation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    RoleId BIGINT NOT NULL,
    SpecialisationId BIGINT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    IsCurrent BIT NOT NULL DEFAULT 0,
    ExperienceMonths INT NOT NULL DEFAULT 0,
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    CONSTRAINT FK_CandidateOccupation_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateOccupation_Role FOREIGN KEY(RoleId) REFERENCES dbo.Role(Id),
    CONSTRAINT FK_CandidateOccupation_Specialisation FOREIGN KEY(SpecialisationId) REFERENCES dbo.Specialisation(Id)
);
CREATE UNIQUE INDEX UQ_CandidateOccupation_NoSpec ON dbo.CandidateOccupation(CandidateProfileId, RoleId) WHERE SpecialisationId IS NULL;
CREATE UNIQUE INDEX UQ_CandidateOccupation_WithSpec ON dbo.CandidateOccupation(CandidateProfileId, RoleId, SpecialisationId) WHERE SpecialisationId IS NOT NULL;

CREATE TABLE dbo.CandidateSkill (
    CandidateProfileId BIGINT NOT NULL,
    SkillId BIGINT NOT NULL,
    Proficiency VARCHAR(30) NULL,
    ExperienceMonths INT NULL,
    LastUsedDate DATE NULL,
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    PRIMARY KEY(CandidateProfileId, SkillId),
    CONSTRAINT FK_CandidateSkill_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateSkill_Skill FOREIGN KEY(SkillId) REFERENCES dbo.Skill(Id)
);

CREATE TABLE dbo.CandidateCredential (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    CredentialTypeId BIGINT NOT NULL,
    Issuer NVARCHAR(200) NULL,
    CredentialNumberEncrypted VARBINARY(MAX) NULL,
    CredentialNumberHash VARBINARY(32) NULL,
    IssueDate DATE NULL,
    ExpiryDate DATE NULL,
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    VerifiedAt DATETIME2(3) NULL,
    MetadataJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_CandidateCredential_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateCredential_Type FOREIGN KEY(CredentialTypeId) REFERENCES dbo.CredentialType(Id),
    CONSTRAINT CK_CandidateCredential_MetadataJson CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);
CREATE INDEX IX_CandidateCredential_Eligibility ON dbo.CandidateCredential(CredentialTypeId, VerificationStatus, ExpiryDate);

CREATE TABLE dbo.CandidateAsset (
    CandidateProfileId BIGINT NOT NULL,
    AssetTypeId BIGINT NOT NULL,
    OwnershipStatus VARCHAR(20) NOT NULL, -- OWNED/ACCESS/RENTAL
    VerificationStatus VARCHAR(30) NOT NULL DEFAULT 'UNVERIFIED',
    PRIMARY KEY(CandidateProfileId, AssetTypeId),
    CONSTRAINT FK_CandidateAsset_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateAsset_Type FOREIGN KEY(AssetTypeId) REFERENCES dbo.AssetType(Id)
);

CREATE TABLE dbo.CandidateConsent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    Purpose VARCHAR(50) NOT NULL,
    NoticeVersion VARCHAR(30) NOT NULL,
    Status VARCHAR(20) NOT NULL,
    GrantedAt DATETIME2(3) NULL,
    WithdrawnAt DATETIME2(3) NULL,
    ExpiresAt DATETIME2(3) NULL,
    Source VARCHAR(30) NULL,
    CONSTRAINT FK_CandidateConsent_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);

CREATE TABLE dbo.CandidateAccessAudit (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    EmployerUserAccountId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NULL,
    Action VARCHAR(30) NOT NULL, -- SEARCH_RESULT/PROFILE_VIEW/CONTACT_REVEAL/CV_DOWNLOAD/EXPORT
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Audit_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_Audit_User FOREIGN KEY(EmployerUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_Audit_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_Audit_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);
CREATE INDEX IX_Audit_Candidate_Time ON dbo.CandidateAccessAudit(CandidateProfileId, OccurredAt DESC);

CREATE TABLE dbo.JobPostingStatusHistory (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    Status VARCHAR(40) NOT NULL,
    ChangedByUserAccountId BIGINT NOT NULL,
    Reason NVARCHAR(500) NULL,
    ChangedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_StatusHistory_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_StatusHistory_User FOREIGN KEY(ChangedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);

/*
 Recommended operational pattern:
 - SQL Server remains canonical.
 - Build a denormalised candidate search document/index for GraphQL candidateSearch.
 - Apply hard eligibility filters before semantic/ranking logic.
 - Contact reveal must enforce organisation subscription/entitlement + candidate visibility/consent + audit write.
*/

/*
 IndiaWorks Candidate Onboarding & Job Application - SQL Server extension v4
 Prerequisite: IndiaWorks Employer/Auth/Job Posting Taxonomy SQL Server v3.
 This is an architectural starter, not a production migration script.
*/

-- Candidate root: keep sparse/progressive. Mobile remains in dbo.LoginIdentifier.
IF COL_LENGTH('dbo.CandidateProfile', 'OnboardingState') IS NULL
    ALTER TABLE dbo.CandidateProfile ADD OnboardingState VARCHAR(30) NOT NULL
        CONSTRAINT DF_CandidateProfile_OnboardingState DEFAULT 'OTP_VERIFIED';
IF COL_LENGTH('dbo.CandidateProfile', 'ProfileCompletenessPct') IS NULL
    ALTER TABLE dbo.CandidateProfile ADD ProfileCompletenessPct TINYINT NOT NULL
        CONSTRAINT DF_CandidateProfile_Completeness DEFAULT 0;
IF COL_LENGTH('dbo.CandidateProfile', 'DefaultGeographyId') IS NULL
    ALTER TABLE dbo.CandidateProfile ADD DefaultGeographyId BIGINT NULL;
IF COL_LENGTH('dbo.CandidateProfile', 'DiscoverabilityStatus') IS NULL
    ALTER TABLE dbo.CandidateProfile ADD DiscoverabilityStatus VARCHAR(40) NOT NULL
        CONSTRAINT DF_CandidateProfile_Discoverability DEFAULT 'APPLIED_JOBS_ONLY';
IF COL_LENGTH('dbo.CandidateProfile', 'LastActiveAt') IS NULL
    ALTER TABLE dbo.CandidateProfile ADD LastActiveAt DATETIME2(3) NULL;

-- Add FK only when not already present in migration framework.
-- ALTER TABLE dbo.CandidateProfile ADD CONSTRAINT FK_CandidateProfile_DefaultGeo
--   FOREIGN KEY(DefaultGeographyId) REFERENCES dbo.Geography(Id);

CREATE TABLE dbo.CandidateEducation (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    EducationLevelCode VARCHAR(30) NOT NULL,
    CourseCode VARCHAR(60) NULL,
    FieldOfStudy NVARCHAR(150) NULL,
    Institution NVARCHAR(250) NULL,
    CompletionYear SMALLINT NULL,
    IsHighest BIT NOT NULL DEFAULT 0,
    IsVerified BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_CandidateEducation_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);
CREATE INDEX IX_CandidateEducation_Candidate_Level ON dbo.CandidateEducation(CandidateProfileId, EducationLevelCode);

CREATE TABLE dbo.CandidateLanguage (
    CandidateProfileId BIGINT NOT NULL,
    LanguageCode VARCHAR(10) NOT NULL,
    SpeakingLevel VARCHAR(20) NULL,
    ReadingLevel VARCHAR(20) NULL,
    WritingLevel VARCHAR(20) NULL,
    PRIMARY KEY(CandidateProfileId, LanguageCode),
    CONSTRAINT FK_CandidateLanguage_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);

CREATE TABLE dbo.CandidateWorkPreference (
    CandidateProfileId BIGINT NOT NULL PRIMARY KEY,
    PreferredEmploymentTypesJson NVARCHAR(MAX) NULL,
    PreferredWorkModesJson NVARCHAR(MAX) NULL,
    PreferredShiftTypesJson NVARCHAR(MAX) NULL,
    ExpectedMonthlyMin DECIMAL(18,2) NULL,
    ExpectedMonthlyMax DECIMAL(18,2) NULL,
    NoticePeriodDays SMALLINT NULL,
    AvailableFrom DATE NULL,
    WillingToRelocate BIT NULL,
    UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CandidateWorkPreference_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT CK_CandidateWorkPreference_EmpTypesJson CHECK(PreferredEmploymentTypesJson IS NULL OR ISJSON(PreferredEmploymentTypesJson)=1),
    CONSTRAINT CK_CandidateWorkPreference_WorkModesJson CHECK(PreferredWorkModesJson IS NULL OR ISJSON(PreferredWorkModesJson)=1),
    CONSTRAINT CK_CandidateWorkPreference_ShiftsJson CHECK(PreferredShiftTypesJson IS NULL OR ISJSON(PreferredShiftTypesJson)=1)
);

CREATE TABLE dbo.CandidateLocationPreference (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    GeographyId BIGINT NOT NULL,
    RadiusKm DECIMAL(8,2) NULL,
    Priority SMALLINT NOT NULL DEFAULT 1,
    IsCurrentLocation BIT NOT NULL DEFAULT 0,
    WillingToRelocateHere BIT NULL,
    CONSTRAINT FK_CandidateLocationPreference_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateLocationPreference_Geo FOREIGN KEY(GeographyId) REFERENCES dbo.Geography(Id)
);
CREATE INDEX IX_CandidateLocationPreference_Geo ON dbo.CandidateLocationPreference(GeographyId, CandidateProfileId);

CREATE TABLE dbo.NotificationPreference (
    CandidateProfileId BIGINT NOT NULL,
    Channel VARCHAR(20) NOT NULL, -- PUSH/SMS/WHATSAPP/EMAIL
    Status VARCHAR(20) NOT NULL,  -- OPTED_IN/OPTED_OUT/PAUSED
    UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(CandidateProfileId, Channel),
    CONSTRAINT FK_NotificationPreference_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);

CREATE TABLE dbo.JobAlertSubscription (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    Name NVARCHAR(150) NULL,
    FilterJson NVARCHAR(MAX) NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    Frequency VARCHAR(20) NOT NULL, -- INSTANT/DAILY/WEEKLY/CUSTOM
    IsActive BIT NOT NULL DEFAULT 1,
    LastDeliveredAt DATETIME2(3) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobAlert_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT CK_JobAlert_FilterJson CHECK(ISJSON(FilterJson)=1)
);
CREATE INDEX IX_JobAlert_ActiveChannel ON dbo.JobAlertSubscription(IsActive, Channel, CandidateProfileId);

CREATE TABLE dbo.SavedJob (
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    SavedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(CandidateProfileId, JobPostingId),
    CONSTRAINT FK_SavedJob_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_SavedJob_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobView (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NULL,
    JobPostingId BIGINT NOT NULL,
    SessionId UNIQUEIDENTIFIER NULL,
    Source VARCHAR(30) NULL,
    ViewedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobView_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_JobView_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);
CREATE INDEX IX_JobView_Candidate_Time ON dbo.JobView(CandidateProfileId, ViewedAt DESC);
CREATE INDEX IX_JobView_Job_Time ON dbo.JobView(JobPostingId, ViewedAt DESC);

CREATE TABLE dbo.JobShare (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NULL,
    JobPostingId BIGINT NOT NULL,
    Channel VARCHAR(20) NOT NULL, -- WHATSAPP/FACEBOOK/TELEGRAM/COPY_LINK/OTHER
    SharedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobShare_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_JobShare_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.CandidateApplication (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'APPLIED',
    Source VARCHAR(30) NULL, -- JOB_FEED/SEARCH/SAVED/ALERT/SHARE/DIRECT
    AppliedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastStatusAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    WithdrawnAt DATETIME2(3) NULL,
    CandidateProfileVersion BIGINT NULL,
    JobPostingVersion VARBINARY(8) NULL,
    RowVersion ROWVERSION,
    CONSTRAINT FK_CandidateApplication_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateApplication_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT UQ_CandidateApplication UNIQUE(CandidateProfileId, JobPostingId)
);
CREATE INDEX IX_CandidateApplication_Candidate_Time ON dbo.CandidateApplication(CandidateProfileId, AppliedAt DESC);
CREATE INDEX IX_CandidateApplication_Job_Status_Time ON dbo.CandidateApplication(JobPostingId, Status, AppliedAt DESC);

CREATE TABLE dbo.ApplicationSnapshot (
    CandidateApplicationId BIGINT NOT NULL PRIMARY KEY,
    CandidateSnapshotJson NVARCHAR(MAX) NOT NULL,
    JobSnapshotJson NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ApplicationSnapshot_Application FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id),
    CONSTRAINT CK_ApplicationSnapshot_CandidateJson CHECK(ISJSON(CandidateSnapshotJson)=1),
    CONSTRAINT CK_ApplicationSnapshot_JobJson CHECK(ISJSON(JobSnapshotJson)=1)
);

CREATE TABLE dbo.ApplicationRequirementAnswer (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateApplicationId BIGINT NOT NULL,
    RequirementType VARCHAR(30) NOT NULL, -- SKILL/CREDENTIAL/ASSET/EDUCATION/LANGUAGE/SCREENING
    RequirementKey NVARCHAR(120) NOT NULL,
    AnswerJson NVARCHAR(MAX) NULL,
    EvaluationStatus VARCHAR(20) NULL, -- PASS/WARN/FAIL/UNKNOWN
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ApplicationAnswer_Application FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id),
    CONSTRAINT CK_ApplicationAnswer_Json CHECK(AnswerJson IS NULL OR ISJSON(AnswerJson)=1)
);
CREATE INDEX IX_ApplicationAnswer_Application ON dbo.ApplicationRequirementAnswer(CandidateApplicationId, RequirementType);

CREATE TABLE dbo.ApplicationEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateApplicationId BIGINT NOT NULL,
    EventType VARCHAR(40) NOT NULL,
    ActorType VARCHAR(20) NOT NULL, -- CANDIDATE/EMPLOYER/SYSTEM
    ActorUserAccountId BIGINT NULL,
    DetailsJson NVARCHAR(MAX) NULL,
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ApplicationEvent_Application FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id),
    CONSTRAINT FK_ApplicationEvent_Actor FOREIGN KEY(ActorUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT CK_ApplicationEvent_Json CHECK(DetailsJson IS NULL OR ISJSON(DetailsJson)=1)
);
CREATE INDEX IX_ApplicationEvent_App_Time ON dbo.ApplicationEvent(CandidateApplicationId, OccurredAt);

CREATE TABLE dbo.CandidateJobMatchEvaluation (
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    EligibilityStatus VARCHAR(30) NOT NULL, -- ELIGIBLE/ELIGIBLE_WITH_WARNINGS/BLOCKED/UNKNOWN
    Score DECIMAL(7,4) NULL,
    HardBlocksJson NVARCHAR(MAX) NULL,
    WarningsJson NVARCHAR(MAX) NULL,
    MissingFieldsJson NVARCHAR(MAX) NULL,
    ExplanationJson NVARCHAR(MAX) NULL,
    EvaluatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(3) NULL,
    PRIMARY KEY(CandidateProfileId, JobPostingId),
    CONSTRAINT FK_MatchEval_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_MatchEval_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT CK_MatchEval_HardBlocks CHECK(HardBlocksJson IS NULL OR ISJSON(HardBlocksJson)=1),
    CONSTRAINT CK_MatchEval_Warnings CHECK(WarningsJson IS NULL OR ISJSON(WarningsJson)=1),
    CONSTRAINT CK_MatchEval_Missing CHECK(MissingFieldsJson IS NULL OR ISJSON(MissingFieldsJson)=1),
    CONSTRAINT CK_MatchEval_Explanation CHECK(ExplanationJson IS NULL OR ISJSON(ExplanationJson)=1)
);

CREATE TABLE dbo.CandidateContactEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    CandidateApplicationId BIGINT NULL,
    Channel VARCHAR(20) NOT NULL, -- CALL/WHATSAPP/CHAT/SMS
    Outcome VARCHAR(30) NULL,
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CandidateContact_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateContact_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_CandidateContact_Application FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id)
);
CREATE INDEX IX_CandidateContact_Candidate_Time ON dbo.CandidateContactEvent(CandidateProfileId, OccurredAt DESC);

CREATE TABLE dbo.CandidateResume (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    SourceType VARCHAR(20) NOT NULL, -- GENERATED/UPLOADED
    StorageKey NVARCHAR(500) NOT NULL,
    TemplateCode VARCHAR(50) NULL,
    LanguageCode VARCHAR(10) NULL,
    ProfileVersion BIGINT NULL,
    IsCurrent BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_CandidateResume_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);
CREATE INDEX IX_CandidateResume_Current ON dbo.CandidateResume(CandidateProfileId, IsCurrent, CreatedAt DESC);

/*
Application gating recommendation:
- Keep RequirementLevel (REQUIRED/PREFERRED/OPTIONAL) separate from ApplicationGate (BLOCK/WARN/NONE).
- Add ApplicationGate to job requirement tables in the migration that owns those tables.
- Default WARN for frontline preference mismatches; BLOCK only explicit legal/safety/credential prerequisites.

Privacy recommendation:
- CandidateConsent purposes should distinguish JOB_APPLICATION_PROCESSING,
  EMPLOYER_CONTACT, TALENT_POOL_DISCOVERY, WHATSAPP_JOB_ALERTS and IDENTITY_VERIFICATION.
- Default CandidateProfile.DiscoverabilityStatus = APPLIED_JOBS_ONLY.
*/


/* =====================================================================
   IndiaWorks Unified SaaS commercial extension v5
   Builds on Employer/Auth/Job Posting v3 + Candidate Application v4.
   Architectural starter: use proper migration/versioning framework in production.
   ===================================================================== */

CREATE TABLE dbo.SubscriptionPlan (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(60) NOT NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.SubscriptionPlanVersion (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    SubscriptionPlanId BIGINT NOT NULL,
    Version INT NOT NULL,
    BillingInterval VARCHAR(20) NOT NULL, -- TRIAL/MONTHLY/YEARLY/ONE_TIME
    Currency CHAR(3) NOT NULL DEFAULT 'INR',
    Amount DECIMAL(18,2) NULL,
    TrialDays SMALLINT NULL,
    EffectiveFrom DATETIME2(3) NOT NULL,
    EffectiveTo DATETIME2(3) NULL,
    IsPublic BIT NOT NULL DEFAULT 1,
    MetadataJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_PlanVersion_Plan FOREIGN KEY(SubscriptionPlanId) REFERENCES dbo.SubscriptionPlan(Id),
    CONSTRAINT UQ_PlanVersion UNIQUE(SubscriptionPlanId, Version),
    CONSTRAINT CK_PlanVersion_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);

CREATE TABLE dbo.EntitlementDefinition (
    Code VARCHAR(80) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    UnitType VARCHAR(20) NOT NULL, -- BOOLEAN/COUNT/CREDIT/SEAT/GB/API_CALL
    DefaultResetCadence VARCHAR(20) NOT NULL DEFAULT 'BILLING_PERIOD',
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.PlanEntitlement (
    SubscriptionPlanVersionId BIGINT NOT NULL,
    EntitlementCode VARCHAR(80) NOT NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    LimitValue DECIMAL(18,4) NULL, -- NULL means no numeric limit when enabled
    ResetCadence VARCHAR(20) NOT NULL,
    EnforcementMode VARCHAR(20) NOT NULL DEFAULT 'HARD', -- HARD/SOFT/METER_ONLY
    MetadataJson NVARCHAR(MAX) NULL,
    PRIMARY KEY(SubscriptionPlanVersionId, EntitlementCode),
    CONSTRAINT FK_PlanEntitlement_Version FOREIGN KEY(SubscriptionPlanVersionId) REFERENCES dbo.SubscriptionPlanVersion(Id),
    CONSTRAINT FK_PlanEntitlement_Def FOREIGN KEY(EntitlementCode) REFERENCES dbo.EntitlementDefinition(Code),
    CONSTRAINT CK_PlanEntitlement_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);

CREATE TABLE dbo.PaymentProviderCustomer (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    Provider VARCHAR(30) NOT NULL,
    ProviderCustomerId NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_PaymentCustomer_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT UQ_PaymentCustomer UNIQUE(Provider, ProviderCustomerId),
    CONSTRAINT UQ_PaymentCustomer_OrgProvider UNIQUE(OrganisationId, Provider)
);

CREATE TABLE dbo.OrganisationSubscription (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    SubscriptionPlanVersionId BIGINT NOT NULL,
    Status VARCHAR(30) NOT NULL, -- TRIALING/ACTIVE/PAST_DUE/SUSPENDED/CANCEL_AT_PERIOD_END/CANCELLED/EXPIRED
    Provider VARCHAR(30) NULL,
    ProviderSubscriptionId NVARCHAR(200) NULL,
    TrialStartedAt DATETIME2(3) NULL,
    TrialEndsAt DATETIME2(3) NULL,
    CurrentPeriodStart DATETIME2(3) NULL,
    CurrentPeriodEnd DATETIME2(3) NULL,
    CancelAtPeriodEnd BIT NOT NULL DEFAULT 0,
    CancelledAt DATETIME2(3) NULL,
    StartedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    EndedAt DATETIME2(3) NULL,
    RowVersion ROWVERSION,
    CONSTRAINT FK_OrgSubscription_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_OrgSubscription_PlanVersion FOREIGN KEY(SubscriptionPlanVersionId) REFERENCES dbo.SubscriptionPlanVersion(Id)
);
CREATE INDEX IX_OrgSubscription_Org_Status ON dbo.OrganisationSubscription(OrganisationId, Status, CurrentPeriodEnd DESC);
CREATE UNIQUE INDEX UX_OrgSubscription_Provider ON dbo.OrganisationSubscription(Provider, ProviderSubscriptionId) WHERE ProviderSubscriptionId IS NOT NULL;

CREATE TABLE dbo.EntitlementUsageEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    OrganisationSubscriptionId BIGINT NULL,
    EntitlementCode VARCHAR(80) NOT NULL,
    Quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    UsageWindowStart DATETIME2(3) NULL,
    UsageWindowEnd DATETIME2(3) NULL,
    ReferenceType VARCHAR(40) NULL, -- JOB/CANDIDATE/SEARCH/PROMOTION/EXPORT/API
    ReferenceId NVARCHAR(100) NULL,
    IdempotencyKey VARCHAR(120) NOT NULL,
    ActorUserAccountId BIGINT NULL,
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    MetadataJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_UsageEvent_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_UsageEvent_Sub FOREIGN KEY(OrganisationSubscriptionId) REFERENCES dbo.OrganisationSubscription(Id),
    CONSTRAINT FK_UsageEvent_Entitlement FOREIGN KEY(EntitlementCode) REFERENCES dbo.EntitlementDefinition(Code),
    CONSTRAINT FK_UsageEvent_Actor FOREIGN KEY(ActorUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT UQ_UsageEvent_Idempotency UNIQUE(OrganisationId, EntitlementCode, IdempotencyKey),
    CONSTRAINT CK_UsageEvent_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);
CREATE INDEX IX_UsageEvent_Window ON dbo.EntitlementUsageEvent(OrganisationId, EntitlementCode, OccurredAt);

CREATE TABLE dbo.BillingInvoice (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    OrganisationSubscriptionId BIGINT NULL,
    Provider VARCHAR(30) NOT NULL,
    ProviderInvoiceId NVARCHAR(200) NOT NULL,
    InvoiceNumber NVARCHAR(100) NULL,
    Status VARCHAR(30) NOT NULL,
    Currency CHAR(3) NOT NULL,
    Subtotal DECIMAL(18,2) NULL,
    TaxAmount DECIMAL(18,2) NULL,
    Total DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL,
    DueAt DATETIME2(3) NULL,
    PaidAt DATETIME2(3) NULL,
    HostedInvoiceUrl NVARCHAR(1000) NULL,
    CONSTRAINT FK_BillingInvoice_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_BillingInvoice_Sub FOREIGN KEY(OrganisationSubscriptionId) REFERENCES dbo.OrganisationSubscription(Id),
    CONSTRAINT UQ_BillingInvoice_Provider UNIQUE(Provider, ProviderInvoiceId)
);

CREATE TABLE dbo.PaymentTransaction (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    BillingInvoiceId BIGINT NULL,
    Provider VARCHAR(30) NOT NULL,
    ProviderTransactionId NVARCHAR(200) NOT NULL,
    TransactionType VARCHAR(30) NOT NULL, -- PAYMENT/REFUND/ADJUSTMENT
    Status VARCHAR(30) NOT NULL,
    Currency CHAR(3) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    FailureCode NVARCHAR(100) NULL,
    FailureMessage NVARCHAR(500) NULL,
    OccurredAt DATETIME2(3) NOT NULL,
    CONSTRAINT FK_PaymentTxn_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_PaymentTxn_Invoice FOREIGN KEY(BillingInvoiceId) REFERENCES dbo.BillingInvoice(Id),
    CONSTRAINT UQ_PaymentTxn_Provider UNIQUE(Provider, ProviderTransactionId)
);

CREATE TABLE dbo.BillingWebhookInbox (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Provider VARCHAR(30) NOT NULL,
    ProviderEventId NVARCHAR(200) NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    SignatureVerified BIT NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    ProviderOccurredAt DATETIME2(3) NULL,
    ReceivedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ProcessStatus VARCHAR(20) NOT NULL DEFAULT 'RECEIVED',
    ProcessedAt DATETIME2(3) NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    CONSTRAINT UQ_WebhookInbox UNIQUE(Provider, ProviderEventId),
    CONSTRAINT CK_WebhookInbox_Json CHECK(ISJSON(PayloadJson)=1)
);

CREATE TABLE dbo.PromotionProduct (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(60) NOT NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    PlacementType VARCHAR(40) NOT NULL, -- BOOSTED_SEARCH/FEATURED_FEED/URGENT_BADGE
    DefaultDurationHours INT NOT NULL,
    CreditCost DECIMAL(18,4) NULL,
    PriceAmount DECIMAL(18,2) NULL,
    Currency CHAR(3) NULL,
    MaxBoost DECIMAL(8,4) NOT NULL DEFAULT 0.15,
    IsActive BIT NOT NULL DEFAULT 1,
    EligibilityPolicyJson NVARCHAR(MAX) NULL,
    CONSTRAINT CK_PromotionProduct_Policy CHECK(EligibilityPolicyJson IS NULL OR ISJSON(EligibilityPolicyJson)=1)
);

CREATE TABLE dbo.JobPromotion (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    PromotionProductId BIGINT NOT NULL,
    Status VARCHAR(20) NOT NULL, -- SCHEDULED/ACTIVE/PAUSED/COMPLETED/CANCELLED
    Source VARCHAR(30) NOT NULL, -- PLAN_CREDIT/ADD_ON/ADMIN
    BoostValue DECIMAL(8,4) NOT NULL,
    StartsAt DATETIME2(3) NOT NULL,
    EndsAt DATETIME2(3) NOT NULL,
    EntitlementUsageEventId BIGINT NULL,
    CreatedByUserAccountId BIGINT NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobPromotion_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_JobPromotion_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobPromotion_Product FOREIGN KEY(PromotionProductId) REFERENCES dbo.PromotionProduct(Id),
    CONSTRAINT FK_JobPromotion_Usage FOREIGN KEY(EntitlementUsageEventId) REFERENCES dbo.EntitlementUsageEvent(Id),
    CONSTRAINT FK_JobPromotion_User FOREIGN KEY(CreatedByUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT CK_JobPromotion_Dates CHECK(EndsAt > StartsAt)
);
CREATE INDEX IX_JobPromotion_Active ON dbo.JobPromotion(JobPostingId, Status, StartsAt, EndsAt);

CREATE TABLE dbo.PromotionAttributionEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPromotionId BIGINT NOT NULL,
    CandidateProfileId BIGINT NULL,
    EventType VARCHAR(20) NOT NULL, -- IMPRESSION/VIEW/APPLY/CONTACT
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    SessionId UNIQUEIDENTIFIER NULL,
    CONSTRAINT FK_PromoAttr_Promotion FOREIGN KEY(JobPromotionId) REFERENCES dbo.JobPromotion(Id),
    CONSTRAINT FK_PromoAttr_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);
CREATE INDEX IX_PromoAttr_Promo_Event ON dbo.PromotionAttributionEvent(JobPromotionId, EventType, OccurredAt);

CREATE TABLE dbo.CandidateShortlist (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NULL,
    CreatedByUserAccountId BIGINT NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Shortlist_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_Shortlist_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_Shortlist_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_Shortlist_User FOREIGN KEY(CreatedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE UNIQUE INDEX UQ_Shortlist_OrgCandidate_NoJob ON dbo.CandidateShortlist(OrganisationId, CandidateProfileId) WHERE JobPostingId IS NULL;
CREATE UNIQUE INDEX UQ_Shortlist_OrgCandidate_Job ON dbo.CandidateShortlist(OrganisationId, CandidateProfileId, JobPostingId) WHERE JobPostingId IS NOT NULL;

CREATE TABLE dbo.CandidateContactReveal (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    EmployerUserAccountId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NULL,
    EntitlementUsageEventId BIGINT NOT NULL,
    RevealScope VARCHAR(30) NOT NULL DEFAULT 'PHONE_EMAIL',
    IdempotencyKey VARCHAR(120) NOT NULL,
    RevealedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Reveal_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_Reveal_User FOREIGN KEY(EmployerUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_Reveal_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_Reveal_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_Reveal_Usage FOREIGN KEY(EntitlementUsageEventId) REFERENCES dbo.EntitlementUsageEvent(Id),
    CONSTRAINT UQ_Reveal_Idempotency UNIQUE(OrganisationId, IdempotencyKey)
);
CREATE INDEX IX_Reveal_Org_Time ON dbo.CandidateContactReveal(OrganisationId, RevealedAt DESC);

CREATE TABLE dbo.CandidateSearchAudit (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    EmployerUserAccountId BIGINT NOT NULL,
    JobPostingId BIGINT NULL,
    FilterHash VARBINARY(32) NULL,
    ResultCount INT NULL,
    EntitlementUsageEventId BIGINT NULL,
    SearchedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SearchAudit_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_SearchAudit_User FOREIGN KEY(EmployerUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_SearchAudit_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_SearchAudit_Usage FOREIGN KEY(EntitlementUsageEventId) REFERENCES dbo.EntitlementUsageEvent(Id)
);

/* Recommended seed entitlement codes:
 ACTIVE_JOBS, JOB_POSTS_PER_PERIOD, CANDIDATE_SEARCH, CANDIDATE_PROFILE_VIEW,
 CONTACT_REVEAL, RECRUITER_SEAT, PROMOTION_CREDIT, CV_DOWNLOAD, BULK_EXPORT,
 API_ACCESS, ADVANCED_ANALYTICS.

 Ranking rule:
 hard eligibility -> structured relevance -> quality/freshness -> bounded promotion boost.
 Never allow JobPromotion to override candidate consent, credential/legal gates, job state or location hard rules.
*/


/* ============================================================================
   v6 PRODUCT / TECHNICAL-HEAD GAP ADDITIONS (retained in v7)
   ============================================================================ */

/* ---------- Authentication session, device and recovery ---------- */
CREATE TABLE dbo.UserDevice (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserAccountId BIGINT NOT NULL,
    DeviceFingerprintHash VARBINARY(32) NULL,
    Platform VARCHAR(30) NULL, -- ANDROID/IOS/WEB
    DeviceName NVARCHAR(150) NULL,
    AppVersion VARCHAR(30) NULL,
    FirstSeenAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastSeenAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    IsTrusted BIT NOT NULL DEFAULT 0,
    RevokedAt DATETIME2(3) NULL,
    CONSTRAINT FK_UserDevice_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_UserDevice_User_LastSeen ON dbo.UserDevice(UserAccountId, LastSeenAt DESC);

CREATE TABLE dbo.UserSession (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserAccountId BIGINT NOT NULL,
    UserDeviceId UNIQUEIDENTIFIER NULL,
    ActivePersona VARCHAR(20) NULL, -- CANDIDATE/EMPLOYER/ADMIN, presentation context only
    RefreshTokenHash VARBINARY(64) NOT NULL,
    RefreshTokenFamilyId UNIQUEIDENTIFIER NOT NULL,
    RefreshTokenVersion INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastUsedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(3) NOT NULL,
    RevokedAt DATETIME2(3) NULL,
    RevokedReason VARCHAR(50) NULL,
    IpHash VARBINARY(32) NULL,
    UserAgentHash VARBINARY(32) NULL,
    CONSTRAINT FK_UserSession_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_UserSession_Device FOREIGN KEY(UserDeviceId) REFERENCES dbo.UserDevice(Id)
);
CREATE UNIQUE INDEX UQ_UserSession_RefreshHash ON dbo.UserSession(RefreshTokenHash);
CREATE INDEX IX_UserSession_User_Active ON dbo.UserSession(UserAccountId, ExpiresAt DESC) WHERE RevokedAt IS NULL;

CREATE TABLE dbo.OtpDeliveryAttempt (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OtpChallengeId UNIQUEIDENTIFIER NOT NULL,
    Provider VARCHAR(40) NULL,
    ProviderMessageId NVARCHAR(150) NULL,
    DeliveryStatus VARCHAR(30) NOT NULL,
    AttemptNo SMALLINT NOT NULL,
    SentAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ProviderResponseCode NVARCHAR(50) NULL,
    CONSTRAINT FK_OtpDelivery_Challenge FOREIGN KEY(OtpChallengeId) REFERENCES dbo.OtpChallenge(Id)
);
CREATE INDEX IX_OtpDelivery_Challenge ON dbo.OtpDeliveryAttempt(OtpChallengeId, AttemptNo);

CREATE TABLE dbo.LoginIdentifierChangeRequest (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserAccountId BIGINT NOT NULL,
    IdentifierType VARCHAR(20) NOT NULL,
    OldNormalizedValueHash VARBINARY(32) NULL,
    NewNormalizedValueHash VARBINARY(32) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING', -- PENDING/VERIFIED/COMPLETED/CANCELLED/CONFLICT
    RequestedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    VerifiedAt DATETIME2(3) NULL,
    CompletedAt DATETIME2(3) NULL,
    CONSTRAINT FK_IdentifierChange_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);

CREATE TABLE dbo.AuthEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserAccountId BIGINT NULL,
    UserSessionId UNIQUEIDENTIFIER NULL,
    EventType VARCHAR(40) NOT NULL, -- OTP_REQUESTED/OTP_VERIFIED/LOGIN/REFRESH/LOGOUT/REVOKE/IDENTIFIER_CHANGED
    Outcome VARCHAR(20) NOT NULL,
    RiskCode VARCHAR(40) NULL,
    IpHash VARBINARY(32) NULL,
    DeviceFingerprintHash VARBINARY(32) NULL,
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuthEvent_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_AuthEvent_Session FOREIGN KEY(UserSessionId) REFERENCES dbo.UserSession(Id)
);
CREATE INDEX IX_AuthEvent_User_Time ON dbo.AuthEvent(UserAccountId, OccurredAt DESC);

/* ---------- Employer team invitation ---------- */
CREATE TABLE dbo.OrganisationInvitation (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    OrganisationId BIGINT NOT NULL,
    InvitedByUserAccountId BIGINT NOT NULL,
    InviteeIdentifierType VARCHAR(20) NOT NULL,
    InviteeIdentifierHash VARBINARY(32) NOT NULL,
    Role VARCHAR(30) NOT NULL,
    TokenHash VARBINARY(64) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    ExpiresAt DATETIME2(3) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    AcceptedByUserAccountId BIGINT NULL,
    AcceptedAt DATETIME2(3) NULL,
    RevokedAt DATETIME2(3) NULL,
    CONSTRAINT FK_OrgInvite_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_OrgInvite_Inviter FOREIGN KEY(InvitedByUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_OrgInvite_Acceptor FOREIGN KEY(AcceptedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_OrgInvite_Org_Status ON dbo.OrganisationInvitation(OrganisationId, Status, ExpiresAt);

/* ---------- Job detail, publication, moderation, benefits, screening ---------- */
ALTER TABLE dbo.JobPosting ADD
    DisplayTitle NVARCHAR(200) NULL,
    SourceType VARCHAR(30) NULL, -- DIRECT_EMPLOYER/STAFFING/PARTNER/GIG
    ExpiresAt DATETIME2(3) NULL,
    LastRefreshedAt DATETIME2(3) NULL;

CREATE TABLE dbo.JobPublicationEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    EventType VARCHAR(30) NOT NULL, -- PUBLISH/REFRESH/PAUSE/RESUME/CLOSE/EXPIRE
    ActorUserAccountId BIGINT NULL,
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DetailsJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_JobPublicationEvent_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobPublicationEvent_User FOREIGN KEY(ActorUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT CK_JobPublicationEvent_Json CHECK(DetailsJson IS NULL OR ISJSON(DetailsJson)=1)
);
CREATE INDEX IX_JobPublicationEvent_Job_Time ON dbo.JobPublicationEvent(JobPostingId, OccurredAt DESC);

CREATE TABLE dbo.JobModerationReview (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    Status VARCHAR(30) NOT NULL, -- PENDING_REVIEW/APPROVED/REJECTED/SUSPENDED
    ReasonCode VARCHAR(60) NULL,
    Notes NVARCHAR(1000) NULL,
    ReviewedByUserAccountId BIGINT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DecidedAt DATETIME2(3) NULL,
    CONSTRAINT FK_JobModeration_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobModeration_User FOREIGN KEY(ReviewedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_JobModeration_Job_Time ON dbo.JobModerationReview(JobPostingId, CreatedAt DESC);
CREATE INDEX IX_JobModeration_Status ON dbo.JobModerationReview(Status, CreatedAt);

CREATE TABLE dbo.BenefitType (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(60) NOT NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL
);

CREATE TABLE dbo.JobBenefit (
    JobPostingId BIGINT NOT NULL,
    BenefitTypeId BIGINT NOT NULL,
    Details NVARCHAR(500) NULL,
    PRIMARY KEY(JobPostingId, BenefitTypeId),
    CONSTRAINT FK_JobBenefit_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobBenefit_Type FOREIGN KEY(BenefitTypeId) REFERENCES dbo.BenefitType(Id)
);

CREATE TABLE dbo.JobScreeningQuestion (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    QuestionKey VARCHAR(80) NOT NULL,
    Prompt NVARCHAR(500) NOT NULL,
    AnswerType VARCHAR(20) NOT NULL, -- BOOLEAN/SINGLE_SELECT/MULTI_SELECT/TEXT/NUMBER
    OptionsJson NVARCHAR(MAX) NULL,
    IsRequired BIT NOT NULL DEFAULT 0,
    ApplicationGate VARCHAR(20) NOT NULL DEFAULT 'WARN',
    DisplayOrder SMALLINT NOT NULL DEFAULT 0,
    CONSTRAINT FK_JobScreeningQuestion_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT UQ_JobScreeningQuestion UNIQUE(JobPostingId, QuestionKey),
    CONSTRAINT CK_JobScreeningQuestion_Options CHECK(OptionsJson IS NULL OR ISJSON(OptionsJson)=1)
);

CREATE TABLE dbo.JobInterviewWindow (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobPostingId BIGINT NOT NULL,
    InterviewType VARCHAR(20) NOT NULL, -- WALK_IN/PHONE/VIDEO/SCHEDULED
    StartAt DATETIME2(3) NULL,
    EndAt DATETIME2(3) NULL,
    GeographyId BIGINT NULL,
    AddressText NVARCHAR(500) NULL,
    Instructions NVARCHAR(1000) NULL,
    AddressVisibility VARCHAR(20) NOT NULL DEFAULT 'PUBLIC',
    CONSTRAINT FK_JobInterviewWindow_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobInterviewWindow_Geo FOREIGN KEY(GeographyId) REFERENCES dbo.Geography(Id)
);

/* ---------- Public/share links and attribution ---------- */
CREATE TABLE dbo.JobPublicLink (
    JobPostingId BIGINT NOT NULL PRIMARY KEY,
    PublicCode VARCHAR(32) NOT NULL UNIQUE,
    Slug NVARCHAR(220) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobPublicLink_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id)
);

CREATE TABLE dbo.JobShareLink (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    JobPostingId BIGINT NOT NULL,
    CandidateProfileId BIGINT NULL,
    Channel VARCHAR(20) NOT NULL,
    AttributionTokenHash VARBINARY(32) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt DATETIME2(3) NULL,
    CONSTRAINT FK_JobShareLink_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobShareLink_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id)
);
CREATE INDEX IX_JobShareLink_Job ON dbo.JobShareLink(JobPostingId, CreatedAt DESC);

/* ---------- Search analytics / impressions ---------- */
CREATE TABLE dbo.JobImpression (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NULL,
    JobPostingId BIGINT NOT NULL,
    SessionId UNIQUEIDENTIFIER NULL,
    SearchRequestId UNIQUEIDENTIFIER NULL,
    Source VARCHAR(30) NOT NULL, -- FEED/SEARCH/ALERT/SHARE/SIMILAR/PROMOTION
    Position INT NULL,
    PromotionId BIGINT NULL,
    ImpressedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobImpression_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_JobImpression_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_JobImpression_Promotion FOREIGN KEY(PromotionId) REFERENCES dbo.JobPromotion(Id)
);
CREATE INDEX IX_JobImpression_Job_Time ON dbo.JobImpression(JobPostingId, ImpressedAt DESC);
CREATE INDEX IX_JobImpression_Candidate_Time ON dbo.JobImpression(CandidateProfileId, ImpressedAt DESC);

CREATE TABLE dbo.JobSearchEvent (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    CandidateProfileId BIGINT NULL,
    SessionId UNIQUEIDENTIFIER NULL,
    FilterSchemaVersion SMALLINT NOT NULL DEFAULT 2,
    FilterHash VARBINARY(32) NOT NULL,
    QueryTextHash VARBINARY(32) NULL,
    GeographyId BIGINT NULL,
    ResultCount INT NULL,
    SortCode VARCHAR(30) NULL,
    SearchIndexVersion VARCHAR(40) NULL,
    SearchedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobSearchEvent_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_JobSearchEvent_Geo FOREIGN KEY(GeographyId) REFERENCES dbo.Geography(Id)
);
CREATE INDEX IX_JobSearchEvent_Candidate_Time ON dbo.JobSearchEvent(CandidateProfileId, SearchedAt DESC);

/* ---------- Saved search, alerts and delivery history ---------- */
CREATE TABLE dbo.SavedJobSearch (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateProfileId BIGINT NOT NULL,
    Name NVARCHAR(150) NULL,
    FilterSchemaVersion SMALLINT NOT NULL DEFAULT 2,
    FilterJson NVARCHAR(MAX) NOT NULL,
    SortCode VARCHAR(30) NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_SavedJobSearch_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT CK_SavedJobSearch_Filter CHECK(ISJSON(FilterJson)=1)
);
CREATE INDEX IX_SavedJobSearch_Candidate ON dbo.SavedJobSearch(CandidateProfileId, UpdatedAt DESC);

ALTER TABLE dbo.JobAlertSubscription ADD
    SavedJobSearchId BIGINT NULL,
    TimeZoneId VARCHAR(80) NULL,
    QuietHoursStart TIME NULL,
    QuietHoursEnd TIME NULL,
    LastEvaluatedAt DATETIME2(3) NULL,
    ConsecutiveFailureCount INT NOT NULL CONSTRAINT DF_JobAlert_Failures DEFAULT 0;
ALTER TABLE dbo.JobAlertSubscription ADD CONSTRAINT FK_JobAlert_SavedSearch FOREIGN KEY(SavedJobSearchId) REFERENCES dbo.SavedJobSearch(Id);

CREATE TABLE dbo.JobAlertRun (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobAlertSubscriptionId BIGINT NOT NULL,
    WatermarkFrom DATETIME2(3) NULL,
    WatermarkTo DATETIME2(3) NOT NULL,
    MatchedCount INT NOT NULL DEFAULT 0,
    Status VARCHAR(20) NOT NULL,
    StartedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt DATETIME2(3) NULL,
    ErrorCode VARCHAR(50) NULL,
    CONSTRAINT FK_JobAlertRun_Alert FOREIGN KEY(JobAlertSubscriptionId) REFERENCES dbo.JobAlertSubscription(Id)
);
CREATE INDEX IX_JobAlertRun_Alert_Time ON dbo.JobAlertRun(JobAlertSubscriptionId, StartedAt DESC);

CREATE TABLE dbo.JobAlertDelivery (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobAlertRunId BIGINT NOT NULL,
    JobAlertSubscriptionId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    DeliveryWindowKey VARCHAR(50) NOT NULL,
    ProviderMessageId NVARCHAR(150) NULL,
    Status VARCHAR(20) NOT NULL, -- QUEUED/SENT/DELIVERED/FAILED/OPENED/CLICKED
    QueuedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    SentAt DATETIME2(3) NULL,
    DeliveredAt DATETIME2(3) NULL,
    OpenedAt DATETIME2(3) NULL,
    ClickedAt DATETIME2(3) NULL,
    CONSTRAINT FK_JobAlertDelivery_Run FOREIGN KEY(JobAlertRunId) REFERENCES dbo.JobAlertRun(Id),
    CONSTRAINT FK_JobAlertDelivery_Alert FOREIGN KEY(JobAlertSubscriptionId) REFERENCES dbo.JobAlertSubscription(Id),
    CONSTRAINT FK_JobAlertDelivery_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_JobAlertDelivery_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT UQ_JobAlertDelivery_Dedupe UNIQUE(JobAlertSubscriptionId, JobPostingId, Channel, DeliveryWindowKey)
);
CREATE INDEX IX_JobAlertDelivery_Candidate_Time ON dbo.JobAlertDelivery(CandidateProfileId, QueuedAt DESC);

CREATE TABLE dbo.NotificationDelivery (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserAccountId BIGINT NOT NULL,
    NotificationType VARCHAR(50) NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    TemplateCode VARCHAR(60) NULL,
    EntityType VARCHAR(30) NULL,
    EntityId BIGINT NULL,
    ProviderMessageId NVARCHAR(150) NULL,
    Status VARCHAR(20) NOT NULL,
    QueuedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    SentAt DATETIME2(3) NULL,
    DeliveredAt DATETIME2(3) NULL,
    ReadAt DATETIME2(3) NULL,
    CONSTRAINT FK_NotificationDelivery_User FOREIGN KEY(UserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_NotificationDelivery_User_Time ON dbo.NotificationDelivery(UserAccountId, QueuedAt DESC);

/* ---------- Organisation <-> candidate CRM relationship ---------- */
CREATE TABLE dbo.EmployerCandidateRelationship (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    Stage VARCHAR(30) NOT NULL DEFAULT 'NEW',
    Source VARCHAR(30) NULL, -- APPLICATION/SEARCH/REVEAL/REFERRAL/IMPORT
    OwnerUserAccountId BIGINT NULL,
    FirstInteractionAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastInteractionAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    DoNotContact BIT NOT NULL DEFAULT 0,
    BlockedAt DATETIME2(3) NULL,
    RowVersion ROWVERSION,
    CONSTRAINT FK_EmpCandidateRel_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_EmpCandidateRel_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_EmpCandidateRel_Owner FOREIGN KEY(OwnerUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT UQ_EmpCandidateRel UNIQUE(OrganisationId, CandidateProfileId)
);
CREATE INDEX IX_EmpCandidateRel_Org_Stage ON dbo.EmployerCandidateRelationship(OrganisationId, Stage, LastInteractionAt DESC);

CREATE TABLE dbo.EmployerCandidateRelationshipJob (
    EmployerCandidateRelationshipId BIGINT NOT NULL,
    JobPostingId BIGINT NOT NULL,
    CandidateApplicationId BIGINT NULL,
    LinkedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(EmployerCandidateRelationshipId, JobPostingId),
    CONSTRAINT FK_EmpCandidateRelJob_Rel FOREIGN KEY(EmployerCandidateRelationshipId) REFERENCES dbo.EmployerCandidateRelationship(Id),
    CONSTRAINT FK_EmpCandidateRelJob_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_EmpCandidateRelJob_App FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id)
);

CREATE TABLE dbo.EmployerCandidateNote (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    EmployerCandidateRelationshipId BIGINT NOT NULL,
    AuthorUserAccountId BIGINT NOT NULL,
    Note NVARCHAR(2000) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EmpCandidateNote_Rel FOREIGN KEY(EmployerCandidateRelationshipId) REFERENCES dbo.EmployerCandidateRelationship(Id),
    CONSTRAINT FK_EmpCandidateNote_User FOREIGN KEY(AuthorUserAccountId) REFERENCES dbo.UserAccount(Id)
);

CREATE TABLE dbo.EmployerCandidateTag (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    Name NVARCHAR(80) NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EmpCandidateTag_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT UQ_EmpCandidateTag UNIQUE(OrganisationId, Name)
);

CREATE TABLE dbo.EmployerCandidateTagAssignment (
    EmployerCandidateRelationshipId BIGINT NOT NULL,
    EmployerCandidateTagId BIGINT NOT NULL,
    AssignedByUserAccountId BIGINT NOT NULL,
    AssignedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(EmployerCandidateRelationshipId, EmployerCandidateTagId),
    CONSTRAINT FK_EmpCandidateTagAssign_Rel FOREIGN KEY(EmployerCandidateRelationshipId) REFERENCES dbo.EmployerCandidateRelationship(Id),
    CONSTRAINT FK_EmpCandidateTagAssign_Tag FOREIGN KEY(EmployerCandidateTagId) REFERENCES dbo.EmployerCandidateTag(Id),
    CONSTRAINT FK_EmpCandidateTagAssign_User FOREIGN KEY(AssignedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);

/* ---------- Interview and offer funnel ---------- */
CREATE TABLE dbo.Interview (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateApplicationId BIGINT NOT NULL,
    OrganisationId BIGINT NOT NULL,
    ScheduledByUserAccountId BIGINT NOT NULL,
    InterviewType VARCHAR(20) NOT NULL,
    ScheduledStartAt DATETIME2(3) NOT NULL,
    ScheduledEndAt DATETIME2(3) NULL,
    TimeZoneId VARCHAR(80) NULL,
    LocationText NVARCHAR(500) NULL,
    MeetingUrlEncrypted VARBINARY(MAX) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    Outcome VARCHAR(30) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Interview_App FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id),
    CONSTRAINT FK_Interview_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_Interview_User FOREIGN KEY(ScheduledByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_Interview_App_Start ON dbo.Interview(CandidateApplicationId, ScheduledStartAt);

CREATE TABLE dbo.JobOffer (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CandidateApplicationId BIGINT NOT NULL,
    OrganisationId BIGINT NOT NULL,
    CreatedByUserAccountId BIGINT NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'DRAFT', -- DRAFT/SENT/ACCEPTED/DECLINED/WITHDRAWN/EXPIRED
    OfferedCompensationJson NVARCHAR(MAX) NULL,
    ProposedStartDate DATE NULL,
    ExpiresAt DATETIME2(3) NULL,
    SentAt DATETIME2(3) NULL,
    RespondedAt DATETIME2(3) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_JobOffer_App FOREIGN KEY(CandidateApplicationId) REFERENCES dbo.CandidateApplication(Id),
    CONSTRAINT FK_JobOffer_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_JobOffer_User FOREIGN KEY(CreatedByUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT CK_JobOffer_CompJson CHECK(OfferedCompensationJson IS NULL OR ISJSON(OfferedCompensationJson)=1)
);

/* ---------- Blocking, reporting and moderation cases ---------- */
CREATE TABLE dbo.CandidateOrganisationBlock (
    CandidateProfileId BIGINT NOT NULL,
    OrganisationId BIGINT NOT NULL,
    ReasonCode VARCHAR(50) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY(CandidateProfileId, OrganisationId),
    CONSTRAINT FK_CandidateOrgBlock_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidateOrgBlock_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id)
);

CREATE TABLE dbo.MarketplaceReport (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    ReporterUserAccountId BIGINT NOT NULL,
    TargetType VARCHAR(30) NOT NULL, -- JOB/ORGANISATION/CANDIDATE/USER
    TargetId BIGINT NOT NULL,
    ReasonCode VARCHAR(60) NOT NULL,
    Details NVARCHAR(2000) NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    ResolvedAt DATETIME2(3) NULL,
    ResolvedByUserAccountId BIGINT NULL,
    CONSTRAINT FK_MarketplaceReport_Reporter FOREIGN KEY(ReporterUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_MarketplaceReport_Resolver FOREIGN KEY(ResolvedByUserAccountId) REFERENCES dbo.UserAccount(Id)
);
CREATE INDEX IX_MarketplaceReport_Status_Target ON dbo.MarketplaceReport(Status, TargetType, TargetId);

/* v6 search note:
   Facet counts are generated from the denormalized search projection/index, not by
   joining canonical OLTP tables for every mobile scroll. Cache facet responses by
   normalized filter hash + geography + taxonomy version + search-index version.
*/


/* ============================================================================
   v7 COMMERCIAL GATE: FIVE FREE DISTINCT CANDIDATES PER ORGANISATION
   ----------------------------------------------------------------------------
   Business rule:
   - Search/applicant list cards are masked and do NOT consume the allowance.
   - First five DISTINCT candidate unlocks are free for the organisation/legal entity.
   - Same candidate reopened across jobs/recruiters counts once.
   - Sixth distinct candidate requires an active paid CANDIDATE_UNLOCK entitlement.
   - Free allowance never resets after cancellation/renewal/account recreation.
   - Contact reveal remains separately audited and subject to candidate consent/block rules.
   ============================================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.EntitlementDefinition WHERE Code='CANDIDATE_UNLOCK')
BEGIN
    INSERT INTO dbo.EntitlementDefinition(Code, Name, UnitType, DefaultResetCadence, Description, IsActive)
    VALUES('CANDIDATE_UNLOCK', N'Candidate profile unlock', 'COUNT', 'BILLING_PERIOD',
           N'Full employer access to one distinct candidate profile. FREE_EVALUATION uses limit 5 with reset cadence NONE.', 1);
END;
GO

CREATE TABLE dbo.OrganisationCandidateUnlock (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    CandidateProfileId BIGINT NOT NULL,
    FirstJobPostingId BIGINT NULL,
    FirstUnlockedByUserAccountId BIGINT NOT NULL,
    AccessSource VARCHAR(30) NOT NULL, -- FREE_ALLOWANCE/PAID_ENTITLEMENT/ADD_ON/ADMIN
    EntitlementUsageEventId BIGINT NULL,
    IdempotencyKey VARCHAR(120) NOT NULL,
    FirstUnlockedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    LastAccessedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    AccessCount INT NOT NULL CONSTRAINT DF_OrgCandidateUnlock_AccessCount DEFAULT 1,
    MetadataJson NVARCHAR(MAX) NULL,
    RowVersion ROWVERSION,
    CONSTRAINT FK_OrgCandidateUnlock_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_OrgCandidateUnlock_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_OrgCandidateUnlock_Job FOREIGN KEY(FirstJobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_OrgCandidateUnlock_User FOREIGN KEY(FirstUnlockedByUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_OrgCandidateUnlock_Usage FOREIGN KEY(EntitlementUsageEventId) REFERENCES dbo.EntitlementUsageEvent(Id),
    CONSTRAINT UQ_OrgCandidateUnlock_Distinct UNIQUE(OrganisationId, CandidateProfileId),
    CONSTRAINT UQ_OrgCandidateUnlock_Idempotency UNIQUE(OrganisationId, IdempotencyKey),
    CONSTRAINT CK_OrgCandidateUnlock_Source CHECK(AccessSource IN ('FREE_ALLOWANCE','PAID_ENTITLEMENT','ADD_ON','ADMIN')),
    CONSTRAINT CK_OrgCandidateUnlock_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);
GO
CREATE INDEX IX_OrgCandidateUnlock_Org_Time ON dbo.OrganisationCandidateUnlock(OrganisationId, FirstUnlockedAt DESC);
GO

CREATE TABLE dbo.CandidatePaywallEvent (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    OrganisationId BIGINT NOT NULL,
    EmployerUserAccountId BIGINT NOT NULL,
    CandidateProfileId BIGINT NULL,
    JobPostingId BIGINT NULL,
    EventType VARCHAR(40) NOT NULL, -- PAYWALL_SHOWN/CHECKOUT_STARTED/PAYMENT_COMPLETED/UNLOCK_RETRIED
    FreeLimit SMALLINT NOT NULL DEFAULT 5,
    FreeUsed SMALLINT NOT NULL,
    SubscriptionStatus VARCHAR(30) NULL,
    PlanVersionId BIGINT NULL,
    CorrelationId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    OccurredAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    MetadataJson NVARCHAR(MAX) NULL,
    CONSTRAINT FK_CandidatePaywall_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT FK_CandidatePaywall_User FOREIGN KEY(EmployerUserAccountId) REFERENCES dbo.UserAccount(Id),
    CONSTRAINT FK_CandidatePaywall_Candidate FOREIGN KEY(CandidateProfileId) REFERENCES dbo.CandidateProfile(Id),
    CONSTRAINT FK_CandidatePaywall_Job FOREIGN KEY(JobPostingId) REFERENCES dbo.JobPosting(Id),
    CONSTRAINT FK_CandidatePaywall_PlanVersion FOREIGN KEY(PlanVersionId) REFERENCES dbo.SubscriptionPlanVersion(Id),
    CONSTRAINT CK_CandidatePaywall_Metadata CHECK(MetadataJson IS NULL OR ISJSON(MetadataJson)=1)
);
GO
CREATE INDEX IX_CandidatePaywall_Org_Time ON dbo.CandidatePaywallEvent(OrganisationId, OccurredAt DESC);
GO

/* Optional organisation-level free-evaluation eligibility anchor.
   This persists even after subscription churn and prevents per-user/per-job resets. */
CREATE TABLE dbo.OrganisationCommercialAccessState (
    OrganisationId BIGINT NOT NULL PRIMARY KEY,
    CommercialIdentityFingerprint VARBINARY(32) NULL,
    FreeCandidateUnlockLimit SMALLINT NOT NULL CONSTRAINT DF_OrgAccess_FreeLimit DEFAULT 5,
    FreeAllowanceGrantedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    FreeAllowanceRevokedAt DATETIME2(3) NULL,
    FreeAllowanceOverrideReason NVARCHAR(500) NULL,
    UpdatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    RowVersion ROWVERSION,
    CONSTRAINT FK_OrgAccessState_Org FOREIGN KEY(OrganisationId) REFERENCES dbo.Organisation(Id),
    CONSTRAINT CK_OrgAccess_FreeLimit CHECK(FreeCandidateUnlockLimit >= 0)
);
GO
CREATE INDEX IX_OrgAccess_Fingerprint ON dbo.OrganisationCommercialAccessState(CommercialIdentityFingerprint) WHERE CommercialIdentityFingerprint IS NOT NULL;
GO

/* Candidate access decision is an application-service transaction, not a client decision.
   Pseudocode:
     1. authorize membership/RBAC and candidate privacy/block state
     2. if OrganisationCandidateUnlock exists => ALREADY_UNLOCKED (no meter)
     3. lock organisation commercial-access row
     4. count FREE_ALLOWANCE distinct unlocks for organisation
     5. if free count < 5 => insert FREE_ALLOWANCE unlock + CANDIDATE_UNLOCK usage event (reset NONE)
     6. else resolve active paid/add-on CANDIDATE_UNLOCK entitlement
     7. if allowed => insert PAID_ENTITLEMENT/ADD_ON unlock + usage event
     8. else insert CandidatePaywallEvent(PAYWALL_SHOWN) and return PAYWALL_REQUIRED
   The unique (OrganisationId, CandidateProfileId) constraint makes repeated/retried access non-consuming.
*/

/* Recommended FREE_EVALUATION plan seed semantics:
   Entitlement: CANDIDATE_UNLOCK, IsEnabled=1, LimitValue=5, ResetCadence='NONE', EnforcementMode='HARD'.
   Paid MONTHLY/YEARLY plans may use a plan-specific limit or NULL for unlimited.
   Free allowance is scoped to organisation/legal identity, never recruiter, job, calendar month, or device.
*/
