import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import angular from 'angular-eslint';

export default tseslint.config(
  { ignores: ['dist/**', '.angular/**', 'node_modules/**'] },
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
  ...angular.configs.tsRecommended,
  {
    files: ['**/*.ts'],
    processor: angular.processInlineTemplates,
    rules: { '@angular-eslint/prefer-inject': 'off', '@typescript-eslint/no-unused-vars': 'off' },
  },
  { files: ['**/*.html'], extends: [...angular.configs.templateRecommended] },
);
