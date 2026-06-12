import { sleep } from 'k6';
import { expectStatus, get, post, productQuestion, semanticQuestion, startQuery, waitForQuery } from './common.js';

export const options = {
  scenarios: {
    baseline: {
      executor: 'constant-vus',
      vus: 1,
      duration: '60s',
    },
  },
  thresholds: {
    'http_req_duration{endpoint:health}': ['p(50)<50', 'p(95)<100'],
    'http_req_duration{endpoint:query_start}': ['p(50)<2000', 'p(95)<4000'],
    'http_req_duration{endpoint:query_status}': ['p(50)<200', 'p(95)<400'],
    'http_req_duration{endpoint:query_results}': ['p(50)<300', 'p(95)<500'],
    'http_req_duration{endpoint:search}': ['p(50)<5000', 'p(95)<12000'],
    'http_req_duration{endpoint:semantic_search}': ['p(50)<500', 'p(95)<1000'],
    checks: ['rate>0.95'],
  },
};

export function setup() {
  const executionId = startQuery();
  const completed = waitForQuery(executionId, 60);

  if (!completed) {
    throw new Error(`Query ${executionId} did not complete during setup`);
  }

  return { executionId };
}

export default function (data) {
  expectStatus(get('/v1/health', 'health'), 200, 'health returned 200');
  expectStatus(post('/v1/query/start', { question: productQuestion }, 'query_start'), 202, 'query start returned 202');
  expectStatus(get(`/v1/query/${data.executionId}/status`, 'query_status'), 200, 'query status returned 200');
  expectStatus(get(`/v1/query/${data.executionId}/results`, 'query_results'), 200, 'query results returned 200');
  expectStatus(post('/v1/search', { question: productQuestion }, 'search'), 200, 'search returned 200');
  expectStatus(post('/v1/semantic-search', { question: semanticQuestion, topK: 10, synthesise: false }, 'semantic_search'), 200, 'semantic search returned 200');

  sleep(1);
}
