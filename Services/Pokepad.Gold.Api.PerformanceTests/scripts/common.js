import http from 'k6/http';
import { check, sleep } from 'k6';

export const productQuestion = 'Show me products in the electronics category';
export const semanticQuestion = 'Show me waterproof hiking boots under 100';

export function baseUrl() {
  const value = __ENV.BASE_URL;
  if (!value) {
    throw new Error('BASE_URL is required');
  }

  return value.replace(/\/$/, '');
}

export function headers() {
  const token = __ENV.TOKEN;
  if (!token) {
    throw new Error('TOKEN is required');
  }

  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
}

export function get(path, endpoint) {
  return http.get(`${baseUrl()}${path}`, {
    headers: headers(),
    tags: { endpoint },
  });
}

export function post(path, body, endpoint) {
  return http.post(`${baseUrl()}${path}`, JSON.stringify(body), {
    headers: headers(),
    tags: { endpoint },
  });
}

export function expectStatus(response, expected, name) {
  check(response, {
    [name]: (r) => r.status === expected,
  });
}

export function startQuery() {
  const response = post('/v1/query/start', { question: productQuestion }, 'query_start');
  expectStatus(response, 202, 'query start returned 202');
  return response.json('executionId');
}

export function waitForQuery(executionId, timeoutSeconds = 30) {
  const startedAt = Date.now();

  while ((Date.now() - startedAt) / 1000 < timeoutSeconds) {
    const response = get(`/v1/query/${executionId}/status`, 'query_status');
    expectStatus(response, 200, 'query status returned 200');

    if (response.json('status') === 'SUCCEEDED') {
      return true;
    }

    sleep(1);
  }

  return false;
}
