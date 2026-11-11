import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import angular from 'angular-eslint';

const FEATURES = [
  'fleet',
  'maintenance',
  'service-orders',
  'administration',
  'data-import',
  'reports',
  'simulators',
];

const featureRules = FEATURES.map((feature) => ({
  files: [`src/app/features/${feature}/**/*.ts`],
  rules: {
    'no-restricted-imports': [
      'error',
      {
        patterns: FEATURES.filter(
          (other) => other !== feature && !(feature === 'fleet' && other === 'maintenance')
        ).map((other) => ({
          group: [`**/features/${other}`, `**/features/${other}/**`],
          message: `Feature '${feature}' cannot import from feature '${other}'.`,
        })),
      },
    ],
  },
}));

export default tseslint.config(
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
    },
  },
  {
    files: ['src/app/shared/**/*.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['**/features/**', '**/core/api/**'],
              message: 'Shared components cannot import from features or core/api.',
            },
          ],
        },
      ],
    },
  },
  ...featureRules,
  {
    files: ['**/*.html'],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {},
  }
);

