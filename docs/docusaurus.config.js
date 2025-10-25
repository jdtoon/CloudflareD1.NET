// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'CloudflareD1.NET',
  tagline: 'Complete .NET adapter for Cloudflare D1 database',
  favicon: 'img/favicon.svg',

  // Set the production url of your site here
  url: 'https://jdtoon.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/CloudflareD1.NET/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'jdtoon', // Usually your GitHub org/user name.
  projectName: 'CloudflareD1.NET', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          // Please change this to your repo.
          editUrl:
            'https://github.com/jdtoon/CloudflareD1.NET/tree/main/docs/',
        },
        blog: false, // Disable blog
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/docusaurus-social-card.svg',
      navbar: {
        title: 'CloudflareD1.NET',
        logo: {
          alt: 'CloudflareD1.NET Logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'tutorialSidebar',
            position: 'left',
            label: 'Documentation',
          },
          {
            href: 'https://github.com/jdtoon/CloudflareD1.NET',
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {
                label: 'Getting Started',
                to: '/docs/intro',
              },
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'GitHub Discussions',
                href: 'https://github.com/jdtoon/CloudflareD1.NET/discussions',
              },
              {
                label: 'Issues',
                href: 'https://github.com/jdtoon/CloudflareD1.NET/issues',
              },
            ],
          },
          {
            title: 'More',
            items: [
              {
                label: 'NuGet',
                href: 'https://www.nuget.org/packages/CloudflareD1.NET',
              },
              {
                label: 'GitHub',
                href: 'https://github.com/jdtoon/CloudflareD1.NET',
              },
            ],
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} CloudflareD1.NET. Built with Docusaurus.`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
        additionalLanguages: ['csharp', 'json', 'bash'],
      },
    }),
};

export default config;

