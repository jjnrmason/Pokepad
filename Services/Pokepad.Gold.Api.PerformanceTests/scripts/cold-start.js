import { expectStatus, post, productQuestion } from './common.js';

export const options = {
  scenarios: {
    cold_start: {
      executor: 'shared-iterations',
      vus: 1,
      iterations: 1,
    },
  },
  thresholds: {
    'http_req_duration{endpoint:search_cold_start}': ['p(95)<15000'],
    checks: ['rate>0.95'],
  },
};

export default function () {
  expectStatus(post('/v1/search', { question: productQuestion }, 'search_cold_start'), 200, 'cold search returned 200');
}
