/* eslint-disable */
/** Internal type. DO NOT USE DIRECTLY. */
type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
/** Internal type. DO NOT USE DIRECTLY. */
export type Incremental<T> =
  T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
import { TypedDocumentNode as DocumentNode } from '@graphql-typed-document-node/core';
export type CreateOrganisationInput = {
  creatorUserAccountId: unknown;
  employeeBand?: string | null | undefined;
  legalName: string;
  nic2025Code?: string | null | undefined;
  organisationType?: string | null | undefined;
  sector?: EmployerSectorInput | null | undefined;
  tradeName?: string | null | undefined;
};

export type EmployerSectorInput = 'Government' | 'Household' | 'Ngo' | 'Private' | 'Psu';

export type GeographyType = 'City' | 'Country' | 'District' | 'Locality' | 'Pin' | 'State';

export type InviteMemberInput = {
  expiresAt: unknown;
  invitedByUserAccountId: unknown;
  inviteeIdentifierHash: Array<number>;
  inviteeIdentifierType: string;
  organisationId: unknown;
  role: string;
  tokenHash: Array<number>;
};

export type RevokeInvitationInput = {
  invitationId: unknown;
  revokedByUserAccountId: unknown;
};

export type GetVersionQueryVariables = Exact<{ [key: string]: never }>;

export type GetVersionQuery = { version: string };

export type JobFamiliesQueryVariables = Exact<{ [key: string]: never }>;

export type JobFamiliesQuery = {
  jobFamilies: Array<{
    id: unknown;
    code: string;
    name: string;
    parentCode: string | null;
    parentName: string | null;
    taxonomyVersionId: unknown;
    taxonomyVersion: string;
    isActive: boolean;
    createdAt: unknown;
  }>;
};

export type RolesQueryVariables = Exact<{
  jobFamilyId?: unknown;
}>;

export type RolesQuery = {
  roles: Array<{
    id: unknown;
    canonicalName: string;
    slug: string;
    jobFamilyId: unknown;
    jobFamilyCode: string;
    jobFamilyName: string;
    taxonomyVersionId: unknown;
    taxonomyVersion: string;
    isActive: boolean;
    validFrom: unknown;
    validTo: unknown;
    createdAt: unknown;
  }>;
};

export type SpecialisationsQueryVariables = Exact<{
  roleId?: unknown;
}>;

export type SpecialisationsQuery = {
  specialisations: Array<{
    id: unknown;
    roleId: unknown;
    roleCanonicalName: string;
    roleSlug: string;
    name: string;
    code: string;
    parentCode: string | null;
    parentName: string | null;
    isActive: boolean;
    createdAt: unknown;
  }>;
};

export type WorkContextsQueryVariables = Exact<{
  roleId?: unknown;
}>;

export type WorkContextsQuery = {
  workContexts: Array<{
    id: unknown;
    roleId: unknown;
    roleCanonicalName: string;
    roleSlug: string;
    code: string;
    name: string;
    isActive: boolean;
    createdAt: unknown;
  }>;
};

export type SkillsQueryVariables = Exact<{ [key: string]: never }>;

export type SkillsQuery = {
  skills: Array<{
    id: unknown;
    code: string;
    name: string;
    description: string | null;
    parentCode: string | null;
    parentName: string | null;
    taxonomyVersionId: unknown;
    taxonomyVersion: string;
    isActive: boolean;
    createdAt: unknown;
  }>;
};

export type TaxonomyVersionsQueryVariables = Exact<{ [key: string]: never }>;

export type TaxonomyVersionsQuery = {
  taxonomyVersions: Array<{
    id: unknown;
    semanticVersion: string;
    status: string;
    releaseNotes: string | null;
    effectiveFrom: unknown;
    createdAt: unknown;
  }>;
};

export type GeographiesQueryVariables = Exact<{
  type?: GeographyType | null | undefined;
  search?: string | null | undefined;
}>;

export type GeographiesQuery = {
  geographies: Array<{
    id: string;
    type: string;
    code: string | null;
    name: string;
    parentId: string | null;
    parentCode: string | null;
    parentName: string | null;
    latitude: number | null;
    longitude: number | null;
    createdAt: unknown;
  }>;
};

export type GeographyHierarchyQueryVariables = Exact<{
  type?: GeographyType | null | undefined;
  search?: string | null | undefined;
}>;

export type GeographyHierarchyQuery = {
  geographyHierarchy: Array<{
    id: string;
    type: string;
    code: string | null;
    name: string;
    parentId: string | null;
    parentCode: string | null;
    parentName: string | null;
    latitude: number | null;
    longitude: number | null;
    createdAt: unknown;
  }>;
};

export type MyOrganisationsQueryVariables = Exact<{ [key: string]: never }>;

export type MyOrganisationsQuery = {
  myOrganisations: Array<{
    id: string;
    legalName: string;
    tradeName: string | null;
    sector: string | null;
    employeeBand: string | null;
    organisationType: string | null;
    nic2025Code: string | null;
    verificationStatus: string;
    createdAt: unknown;
    role: string;
  }>;
};

export type CreateOrganisationMutationVariables = Exact<{
  input: CreateOrganisationInput;
}>;

export type CreateOrganisationMutation = {
  createOrganisation: {
    isActive: boolean;
    organisationId: string | null;
    legalName: string | null;
    tradeName: string | null;
    sector: string | null;
    employeeBand: string | null;
    role: string | null;
    createdAt: unknown;
    errorCode: string | null;
    errorMessage: string | null;
  };
};

export type InviteMemberMutationVariables = Exact<{
  input: InviteMemberInput;
}>;

export type InviteMemberMutation = {
  inviteMember: {
    isSuccess: boolean;
    invitationId: string | null;
    errorCode: string | null;
    errorMessage: string | null;
  };
};

export type PendingInvitationsQueryVariables = Exact<{
  organisationId: unknown;
  callerUserAccountId: unknown;
}>;

export type PendingInvitationsQuery = {
  pendingInvitations: Array<{
    id: string;
    organisationId: string;
    invitedByUserAccountId: string;
    inviteeIdentifierType: string;
    role: string;
    status: string;
    expiresAt: unknown;
    createdAt: unknown;
  }>;
};

export type RevokeInvitationMutationVariables = Exact<{
  input: RevokeInvitationInput;
}>;

export type RevokeInvitationMutation = {
  revokeInvitation: { isSuccess: boolean; errorCode: string | null; errorMessage: string | null };
};

export const GetVersionDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'GetVersion' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [{ kind: 'Field', name: { kind: 'Name', value: 'version' } }],
      },
    },
  ],
} as unknown as DocumentNode<GetVersionQuery, GetVersionQueryVariables>;
export const JobFamiliesDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'JobFamilies' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'jobFamilies' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersionId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersion' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<JobFamiliesQuery, JobFamiliesQueryVariables>;
export const RolesDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Roles' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'jobFamilyId' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Long' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'roles' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'jobFamilyId' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'jobFamilyId' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'canonicalName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'slug' } },
                { kind: 'Field', name: { kind: 'Name', value: 'jobFamilyId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'jobFamilyCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'jobFamilyName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersionId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersion' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'validFrom' } },
                { kind: 'Field', name: { kind: 'Name', value: 'validTo' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<RolesQuery, RolesQueryVariables>;
export const SpecialisationsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Specialisations' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'roleId' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Long' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'specialisations' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'roleId' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'roleId' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleCanonicalName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleSlug' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SpecialisationsQuery, SpecialisationsQueryVariables>;
export const WorkContextsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'WorkContexts' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'roleId' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'Long' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'workContexts' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'roleId' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'roleId' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleCanonicalName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'roleSlug' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<WorkContextsQuery, WorkContextsQueryVariables>;
export const SkillsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Skills' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'skills' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'description' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersionId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'taxonomyVersion' } },
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<SkillsQuery, SkillsQueryVariables>;
export const TaxonomyVersionsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'TaxonomyVersions' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'taxonomyVersions' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'semanticVersion' } },
                { kind: 'Field', name: { kind: 'Name', value: 'status' } },
                { kind: 'Field', name: { kind: 'Name', value: 'releaseNotes' } },
                { kind: 'Field', name: { kind: 'Name', value: 'effectiveFrom' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<TaxonomyVersionsQuery, TaxonomyVersionsQueryVariables>;
export const GeographiesDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'Geographies' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'type' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'GeographyType' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'search' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'geographies' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'type' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'type' } },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'search' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'search' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'latitude' } },
                { kind: 'Field', name: { kind: 'Name', value: 'longitude' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GeographiesQuery, GeographiesQueryVariables>;
export const GeographyHierarchyDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'GeographyHierarchy' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'type' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'GeographyType' } },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'search' } },
          type: { kind: 'NamedType', name: { kind: 'Name', value: 'String' } },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'geographyHierarchy' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'type' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'type' } },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'search' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'search' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'type' } },
                { kind: 'Field', name: { kind: 'Name', value: 'code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'name' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'parentName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'latitude' } },
                { kind: 'Field', name: { kind: 'Name', value: 'longitude' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<GeographyHierarchyQuery, GeographyHierarchyQueryVariables>;
export const MyOrganisationsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'MyOrganisations' },
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'myOrganisations' },
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'legalName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tradeName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'sector' } },
                { kind: 'Field', name: { kind: 'Name', value: 'employeeBand' } },
                { kind: 'Field', name: { kind: 'Name', value: 'organisationType' } },
                { kind: 'Field', name: { kind: 'Name', value: 'nic2025Code' } },
                { kind: 'Field', name: { kind: 'Name', value: 'verificationStatus' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
                { kind: 'Field', name: { kind: 'Name', value: 'role' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<MyOrganisationsQuery, MyOrganisationsQueryVariables>;
export const CreateOrganisationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'CreateOrganisation' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'CreateOrganisationInput' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'createOrganisation' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'isActive' } },
                { kind: 'Field', name: { kind: 'Name', value: 'organisationId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'legalName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'tradeName' } },
                { kind: 'Field', name: { kind: 'Name', value: 'sector' } },
                { kind: 'Field', name: { kind: 'Name', value: 'employeeBand' } },
                { kind: 'Field', name: { kind: 'Name', value: 'role' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorMessage' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<CreateOrganisationMutation, CreateOrganisationMutationVariables>;
export const InviteMemberDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'InviteMember' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'InviteMemberInput' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'inviteMember' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'isSuccess' } },
                { kind: 'Field', name: { kind: 'Name', value: 'invitationId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorMessage' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<InviteMemberMutation, InviteMemberMutationVariables>;
export const PendingInvitationsDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'query',
      name: { kind: 'Name', value: 'PendingInvitations' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'organisationId' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'UUID' } },
          },
        },
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'callerUserAccountId' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'UUID' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'pendingInvitations' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'organisationId' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'organisationId' } },
              },
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'callerUserAccountId' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'callerUserAccountId' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'id' } },
                { kind: 'Field', name: { kind: 'Name', value: 'organisationId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'invitedByUserAccountId' } },
                { kind: 'Field', name: { kind: 'Name', value: 'inviteeIdentifierType' } },
                { kind: 'Field', name: { kind: 'Name', value: 'role' } },
                { kind: 'Field', name: { kind: 'Name', value: 'status' } },
                { kind: 'Field', name: { kind: 'Name', value: 'expiresAt' } },
                { kind: 'Field', name: { kind: 'Name', value: 'createdAt' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<PendingInvitationsQuery, PendingInvitationsQueryVariables>;
export const RevokeInvitationDocument = {
  kind: 'Document',
  definitions: [
    {
      kind: 'OperationDefinition',
      operation: 'mutation',
      name: { kind: 'Name', value: 'RevokeInvitation' },
      variableDefinitions: [
        {
          kind: 'VariableDefinition',
          variable: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
          type: {
            kind: 'NonNullType',
            type: { kind: 'NamedType', name: { kind: 'Name', value: 'RevokeInvitationInput' } },
          },
        },
      ],
      selectionSet: {
        kind: 'SelectionSet',
        selections: [
          {
            kind: 'Field',
            name: { kind: 'Name', value: 'revokeInvitation' },
            arguments: [
              {
                kind: 'Argument',
                name: { kind: 'Name', value: 'input' },
                value: { kind: 'Variable', name: { kind: 'Name', value: 'input' } },
              },
            ],
            selectionSet: {
              kind: 'SelectionSet',
              selections: [
                { kind: 'Field', name: { kind: 'Name', value: 'isSuccess' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorCode' } },
                { kind: 'Field', name: { kind: 'Name', value: 'errorMessage' } },
              ],
            },
          },
        ],
      },
    },
  ],
} as unknown as DocumentNode<RevokeInvitationMutation, RevokeInvitationMutationVariables>;
