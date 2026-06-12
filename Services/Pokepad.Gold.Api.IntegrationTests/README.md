# Pokepad.Gold.Api.IntegrationTests

End-to-end integration tests that run against a **deployed** Pokepad environment over real HTTP. They exercise the full request path through API Gateway, Cognito auth, OpenAI SQL generation, Athena, and DynamoDB query tracking — they are not unit tests.

## Configuration

The tests read their target environment from environment variables:

| Variable | Description |
|---|---|
| `POKEPAD_API_BASE_URL` | Base URL of the deployed API, e.g. `https://abc123.execute-api.eu-west-2.amazonaws.com` |
| `POKEPAD_PRIMARY_USER_TOKEN` | Cognito JWT for the primary test user |
| `POKEPAD_SECONDARY_USER_TOKEN` | Cognito JWT for a second test user, used for the cross-user 403 scenarios |

Tokens can be obtained via the Cognito `InitiateAuth` API using the `USER_PASSWORD_AUTH` flow.

## Running

All tests are tagged `[Category("Integration")]` so they are excluded from the normal test run:

```sh
# normal CI run — integration tests excluded
dotnet test --filter "TestCategory!=Integration"

# integration run against a deployed environment
dotnet test Services/Pokepad.Gold.Api.IntegrationTests --filter "TestCategory=Integration"
```

## Cleanup

Tests are safe to run repeatedly: query-tracking records in DynamoDB expire automatically via the 24 h TTL, so no manual teardown is required.
