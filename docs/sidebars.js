/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */

// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  tutorialSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Usage',
      items: [
        'usage/local-mode',
        'usage/remote-mode',
        'usage/dependency-injection',
        'usage/query-execution',
        'usage/batch-operations',
      ],
    },
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'api/d1client',
        'api/d1options',
        'api/models',
        'api/exceptions',
      ],
    },
    {
      type: 'category',
      label: 'Advanced',
      items: [
        'advanced/time-travel',
        'advanced/database-management',
        'advanced/error-handling',
      ],
    },
    'examples',
    'faq',
  ],
};

export default sidebars;
