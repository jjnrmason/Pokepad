import { sleep } from 'k6';
import { expectStatus, get, post, productQuestion } from './common.js';

export const options = {
  scenarios: {
    async_flow: {
      executor: 'constant-vus',
      vus: 5,
      duration: '3m',
    },
  },
  thresholds: {
    'http_req_duration{endpoint:query_start}': ['p(50)<2000', 'p(95)<4000'],
    'http_req_duration{endpoint:query_status}': ['p(50)<200', 'p(95)<400'],
    'http_req_duration{endpoint:query_results}': ['p(50)<300', 'p(95)<500'],
    checks: ['rate>0.95'],
  },
};

export default function () {
  const startResponse = post('/v1/query/start', { question: productQuestion }, 'query_start');
  expectStatus(startResponse, 202, 'query start returned 202');

  if (startResponse.status !== 202) {
    return;
  }

  const executionId = startResponse.json('executionId');
  if (!executionId) {
    return;
  }

  for (let i = 0; i < 30; i += 1) {
    const statusResponse = get(`/v1/query/${executionId}/status`, 'query_status');
    expectStatus(statusResponse, 200, 'query status returned 200');

    if (statusResponse.json('status') === 'SUCCEEDED') {
      expectStatus(get(`/v1/query/${executionId}/results`, 'query_results'), 200, 'query results returned 200');
      return;
    }

    sleep(1);
  }
}
