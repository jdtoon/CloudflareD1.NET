import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: '🏠 Local & Remote',
    description: (
      <>
        Develop locally with SQLite, deploy to Cloudflare D1. Switch between
        modes with a single configuration change. No cloud setup needed for development.
      </>
    ),
  },
  {
    title: '⚡ Full-Featured',
    description: (
      <>
        Complete D1 API implementation including queries, batch operations, transactions,
        time travel, and database management. Everything you need in one package.
      </>
    ),
  },
  {
    title: '🛠️ Developer Friendly',
    description: (
      <>
        Strong typing, async/await, dependency injection, comprehensive logging,
        and extensive documentation. Built with .NET best practices in mind.
      </>
    ),
  },
];

function Feature({title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
