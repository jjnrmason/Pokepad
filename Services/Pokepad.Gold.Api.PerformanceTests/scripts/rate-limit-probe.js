import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';
import { post, productQuestion } from './common.js';

const rateLimitedResponses = new Counter('rate_limited_responses');

export const options = {
  scenarios: {
    rate_limit_probe: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s', target: 15 },
        { duration: '30s', target: 15 },
        { duration: '30s', target: 5 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    'http_req_duration{endpoint:query_start}': ['p(95)<4000'],
    'http_reqs{endpoint:query_start}': ['count>0'],
    rate_limited_responses: ['count>0'],
    checks: ['rate>0.80'],
  },
};

export default function () {
  const response = post('/v1/query/start', { question: productQuestion }, 'query_start');

  check(response, {
    'query start returned 202 or 429': (r) => r.status === 202 || r.status === 429,
  });

  if (response.status === 429) {
    rateLimitedResponses.add(1);
  }

  sleep(1);
}
