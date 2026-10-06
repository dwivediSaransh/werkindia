/* eslint-disable */
import * as types from './graphql';
import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';

/**
 * Map of all GraphQL operations in the project.
 *
 * This map has several performance disadvantages:
 * 1. It is not tree-shakeable, so it will include all operations in the project.
 * 2. It is not minifiable, so the string of a GraphQL query will be multiple times inside the bundle.
 * 3. It does not support dead code elimination, so it will add unused operations.
 *
 * Therefore it is highly recommended to use the babel or swc plugin for production.
 * Learn more about it here: https://the-guild.dev/graphql/codegen/plugins/presets/preset-client#reducing-bundle-size
 */
type Documents = {
  'query GetVersion {\n  version\n}': typeof types.GetVersionDocument;
  'query JobFamilies {\n  jobFamilies {\n    id\n    code\n    name\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery Roles($jobFamilyId: Long) {\n  roles(jobFamilyId: $jobFamilyId) {\n    id\n    canonicalName\n    slug\n    jobFamilyId\n    jobFamilyCode\n    jobFamilyName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    validFrom\n    validTo\n    createdAt\n  }\n}\n\nquery Specialisations($roleId: Long) {\n  specialisations(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    name\n    code\n    parentCode\n    parentName\n    isActive\n    createdAt\n  }\n}\n\nquery WorkContexts($roleId: Long) {\n  workContexts(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    code\n    name\n    isActive\n    createdAt\n  }\n}\n\nquery Skills {\n  skills {\n    id\n    code\n    name\n    description\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery TaxonomyVersions {\n  taxonomyVersions {\n    id\n    semanticVersion\n    status\n    releaseNotes\n    effectiveFrom\n    createdAt\n  }\n}\n\nquery Geographies($type: GeographyType, $search: String) {\n  geographies(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}\n\nquery GeographyHierarchy($type: GeographyType, $search: String) {\n  geographyHierarchy(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}': typeof types.JobFamiliesDocument;
  'query MyOrganisations {\n  myOrganisations {\n    id\n    legalName\n    tradeName\n    sector\n    employeeBand\n    organisationType\n    nic2025Code\n    verificationStatus\n    createdAt\n    role\n  }\n}\n\nmutation CreateOrganisation($input: CreateOrganisationInput!) {\n  createOrganisation(input: $input) {\n    isActive\n    organisationId\n    legalName\n    tradeName\n    sector\n    employeeBand\n    role\n    createdAt\n    errorCode\n    errorMessage\n  }\n}': typeof types.MyOrganisationsDocument;
  'mutation InviteMember($input: InviteMemberInput!) {\n  inviteMember(input: $input) {\n    isSuccess\n    invitationId\n    errorCode\n    errorMessage\n  }\n}': typeof types.InviteMemberDocument;
  'query PendingInvitations($organisationId: UUID!, $callerUserAccountId: UUID!) {\n  pendingInvitations(\n    organisationId: $organisationId\n    callerUserAccountId: $callerUserAccountId\n  ) {\n    id\n    organisationId\n    invitedByUserAccountId\n    inviteeIdentifierType\n    role\n    status\n    expiresAt\n    createdAt\n  }\n}': typeof types.PendingInvitationsDocument;
  'mutation RevokeInvitation($input: RevokeInvitationInput!) {\n  revokeInvitation(input: $input) {\n    isSuccess\n    errorCode\n    errorMessage\n  }\n}': typeof types.RevokeInvitationDocument;
};
const documents: Documents = {
  'query GetVersion {\n  version\n}': types.GetVersionDocument,
  'query JobFamilies {\n  jobFamilies {\n    id\n    code\n    name\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery Roles($jobFamilyId: Long) {\n  roles(jobFamilyId: $jobFamilyId) {\n    id\n    canonicalName\n    slug\n    jobFamilyId\n    jobFamilyCode\n    jobFamilyName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    validFrom\n    validTo\n    createdAt\n  }\n}\n\nquery Specialisations($roleId: Long) {\n  specialisations(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    name\n    code\n    parentCode\n    parentName\n    isActive\n    createdAt\n  }\n}\n\nquery WorkContexts($roleId: Long) {\n  workContexts(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    code\n    name\n    isActive\n    createdAt\n  }\n}\n\nquery Skills {\n  skills {\n    id\n    code\n    name\n    description\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery TaxonomyVersions {\n  taxonomyVersions {\n    id\n    semanticVersion\n    status\n    releaseNotes\n    effectiveFrom\n    createdAt\n  }\n}\n\nquery Geographies($type: GeographyType, $search: String) {\n  geographies(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}\n\nquery GeographyHierarchy($type: GeographyType, $search: String) {\n  geographyHierarchy(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}':
    types.JobFamiliesDocument,
  'query MyOrganisations {\n  myOrganisations {\n    id\n    legalName\n    tradeName\n    sector\n    employeeBand\n    organisationType\n    nic2025Code\n    verificationStatus\n    createdAt\n    role\n  }\n}\n\nmutation CreateOrganisation($input: CreateOrganisationInput!) {\n  createOrganisation(input: $input) {\n    isActive\n    organisationId\n    legalName\n    tradeName\n    sector\n    employeeBand\n    role\n    createdAt\n    errorCode\n    errorMessage\n  }\n}':
    types.MyOrganisationsDocument,
  'mutation InviteMember($input: InviteMemberInput!) {\n  inviteMember(input: $input) {\n    isSuccess\n    invitationId\n    errorCode\n    errorMessage\n  }\n}':
    types.InviteMemberDocument,
  'query PendingInvitations($organisationId: UUID!, $callerUserAccountId: UUID!) {\n  pendingInvitations(\n    organisationId: $organisationId\n    callerUserAccountId: $callerUserAccountId\n  ) {\n    id\n    organisationId\n    invitedByUserAccountId\n    inviteeIdentifierType\n    role\n    status\n    expiresAt\n    createdAt\n  }\n}':
    types.PendingInvitationsDocument,
  'mutation RevokeInvitation($input: RevokeInvitationInput!) {\n  revokeInvitation(input: $input) {\n    isSuccess\n    errorCode\n    errorMessage\n  }\n}':
    types.RevokeInvitationDocument,
};

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 *
 *
 * @example
 * ```ts
 * const query = graphql(`query GetUser($id: ID!) { user(id: $id) { name } }`);
 * ```
 *
 * The query argument is unknown!
 * Please regenerate the types.
 */
export function graphql(source: string): unknown;

/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'query GetVersion {\n  version\n}',
): (typeof documents)['query GetVersion {\n  version\n}'];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'query JobFamilies {\n  jobFamilies {\n    id\n    code\n    name\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery Roles($jobFamilyId: Long) {\n  roles(jobFamilyId: $jobFamilyId) {\n    id\n    canonicalName\n    slug\n    jobFamilyId\n    jobFamilyCode\n    jobFamilyName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    validFrom\n    validTo\n    createdAt\n  }\n}\n\nquery Specialisations($roleId: Long) {\n  specialisations(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    name\n    code\n    parentCode\n    parentName\n    isActive\n    createdAt\n  }\n}\n\nquery WorkContexts($roleId: Long) {\n  workContexts(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    code\n    name\n    isActive\n    createdAt\n  }\n}\n\nquery Skills {\n  skills {\n    id\n    code\n    name\n    description\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery TaxonomyVersions {\n  taxonomyVersions {\n    id\n    semanticVersion\n    status\n    releaseNotes\n    effectiveFrom\n    createdAt\n  }\n}\n\nquery Geographies($type: GeographyType, $search: String) {\n  geographies(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}\n\nquery GeographyHierarchy($type: GeographyType, $search: String) {\n  geographyHierarchy(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}',
): (typeof documents)['query JobFamilies {\n  jobFamilies {\n    id\n    code\n    name\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery Roles($jobFamilyId: Long) {\n  roles(jobFamilyId: $jobFamilyId) {\n    id\n    canonicalName\n    slug\n    jobFamilyId\n    jobFamilyCode\n    jobFamilyName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    validFrom\n    validTo\n    createdAt\n  }\n}\n\nquery Specialisations($roleId: Long) {\n  specialisations(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    name\n    code\n    parentCode\n    parentName\n    isActive\n    createdAt\n  }\n}\n\nquery WorkContexts($roleId: Long) {\n  workContexts(roleId: $roleId) {\n    id\n    roleId\n    roleCanonicalName\n    roleSlug\n    code\n    name\n    isActive\n    createdAt\n  }\n}\n\nquery Skills {\n  skills {\n    id\n    code\n    name\n    description\n    parentCode\n    parentName\n    taxonomyVersionId\n    taxonomyVersion\n    isActive\n    createdAt\n  }\n}\n\nquery TaxonomyVersions {\n  taxonomyVersions {\n    id\n    semanticVersion\n    status\n    releaseNotes\n    effectiveFrom\n    createdAt\n  }\n}\n\nquery Geographies($type: GeographyType, $search: String) {\n  geographies(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}\n\nquery GeographyHierarchy($type: GeographyType, $search: String) {\n  geographyHierarchy(type: $type, search: $search) {\n    id\n    type\n    code\n    name\n    parentId\n    parentCode\n    parentName\n    latitude\n    longitude\n    createdAt\n  }\n}'];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'query MyOrganisations {\n  myOrganisations {\n    id\n    legalName\n    tradeName\n    sector\n    employeeBand\n    organisationType\n    nic2025Code\n    verificationStatus\n    createdAt\n    role\n  }\n}\n\nmutation CreateOrganisation($input: CreateOrganisationInput!) {\n  createOrganisation(input: $input) {\n    isActive\n    organisationId\n    legalName\n    tradeName\n    sector\n    employeeBand\n    role\n    createdAt\n    errorCode\n    errorMessage\n  }\n}',
): (typeof documents)['query MyOrganisations {\n  myOrganisations {\n    id\n    legalName\n    tradeName\n    sector\n    employeeBand\n    organisationType\n    nic2025Code\n    verificationStatus\n    createdAt\n    role\n  }\n}\n\nmutation CreateOrganisation($input: CreateOrganisationInput!) {\n  createOrganisation(input: $input) {\n    isActive\n    organisationId\n    legalName\n    tradeName\n    sector\n    employeeBand\n    role\n    createdAt\n    errorCode\n    errorMessage\n  }\n}'];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'mutation InviteMember($input: InviteMemberInput!) {\n  inviteMember(input: $input) {\n    isSuccess\n    invitationId\n    errorCode\n    errorMessage\n  }\n}',
): (typeof documents)['mutation InviteMember($input: InviteMemberInput!) {\n  inviteMember(input: $input) {\n    isSuccess\n    invitationId\n    errorCode\n    errorMessage\n  }\n}'];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'query PendingInvitations($organisationId: UUID!, $callerUserAccountId: UUID!) {\n  pendingInvitations(\n    organisationId: $organisationId\n    callerUserAccountId: $callerUserAccountId\n  ) {\n    id\n    organisationId\n    invitedByUserAccountId\n    inviteeIdentifierType\n    role\n    status\n    expiresAt\n    createdAt\n  }\n}',
): (typeof documents)['query PendingInvitations($organisationId: UUID!, $callerUserAccountId: UUID!) {\n  pendingInvitations(\n    organisationId: $organisationId\n    callerUserAccountId: $callerUserAccountId\n  ) {\n    id\n    organisationId\n    invitedByUserAccountId\n    inviteeIdentifierType\n    role\n    status\n    expiresAt\n    createdAt\n  }\n}'];
/**
 * The graphql function is used to parse GraphQL queries into a document that can be used by GraphQL clients.
 */
export function graphql(
  source: 'mutation RevokeInvitation($input: RevokeInvitationInput!) {\n  revokeInvitation(input: $input) {\n    isSuccess\n    errorCode\n    errorMessage\n  }\n}',
): (typeof documents)['mutation RevokeInvitation($input: RevokeInvitationInput!) {\n  revokeInvitation(input: $input) {\n    isSuccess\n    errorCode\n    errorMessage\n  }\n}'];

export function graphql(source: string) {
  return (documents as any)[source] ?? {};
}

export type DocumentType<TDocumentNode extends DocumentNode<any, any>> =
  TDocumentNode extends DocumentNode<infer TType, any> ? TType : never;
