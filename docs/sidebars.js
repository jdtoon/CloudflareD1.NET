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
      ],
    },
    {
      type: 'category',
      label: 'Database Migrations',
      items: [
        'migrations/overview',
      ],
    },
    {
      type: 'category',
      label: 'Code-First',
      items: [
        'code-first/overview',
      ],
    },
    {
      type: 'category',
      label: 'LINQ Package',
      items: [
        'linq/intro',
        'linq/installation',
        'linq/iqueryable',
        'linq/query-builder',
        'linq/groupby',
        'linq/having',
        'linq/joins',
        'linq/set-operations',
        'linq/existence-checks',
        'linq/async-streaming',
        'linq/expression-trees',
        'linq/entity-mapping',
      ],
    },
  ],
};

export default sidebars;
