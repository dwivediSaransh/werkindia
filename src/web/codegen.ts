import type { CodegenConfig } from '@graphql-codegen/cli';

const config: CodegenConfig = {
  schema: './schema.graphql',
  generates: {
    './src/app/core/graphql/generated/': {
      preset: 'client',
      presetConfig: {
        client: {
          addTypename: false,
        },
        fragmentMasking: false,
      },
      documents: [
        'src/app/features/candidate/configure-apollo-angular-and-typed-graphql-code-generation/*.graphql',
        'src/app/**/*.graphql',
      ],
    },
  },
};

export default config;
