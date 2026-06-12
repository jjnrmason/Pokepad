# Pokepad Gold API performance tests

k6 scripts for measuring API latency and rate-limit behaviour.

## Prerequisites

- k6 installed locally
- `BASE_URL` set to the deployed API base URL, for example `https://abc123.execute-api.eu-west-2.amazonaws.com`
- `TOKEN` set to a Cognito ID token for a dedicated performance-test user

## Run

```bash
BASE_URL=https://<api-id>.execute-api.<region>.amazonaws.com \
TOKEN=<id-token> \
./run-perf-tests.sh
```

To run a single scenario:

```bash
BASE_URL=https://<api-id>.execute-api.<region>.amazonaws.com \
TOKEN=<id-token> \
k6 run scripts/baseline.js
```

All questions target product data to keep Athena scan costs predictable.
