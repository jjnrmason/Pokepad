import json
import time
import boto3
import pg8000.native


def on_event(event, context):
    if event["RequestType"] in ("Create", "Update"):
        _init_database(event["ResourceProperties"])
    return {"PhysicalResourceId": "db-init"}


def _init_database(props):
    sm = boto3.client("secretsmanager")
    secret = json.loads(sm.get_secret_value(SecretId=props["SecretArn"])["SecretString"])

    conn = _connect_with_retry(props["Host"], int(props["Port"]), props["Database"], secret)

    conn.run("CREATE EXTENSION IF NOT EXISTS vector")
    conn.run(
        """
        CREATE TABLE IF NOT EXISTS products_embeddings (
            id         BIGSERIAL PRIMARY KEY,
            product_id TEXT      NOT NULL UNIQUE,
            embedding  vector(1536),
            metadata   JSONB
        )
        """
    )
    conn.run(
        "CREATE INDEX IF NOT EXISTS products_embeddings_hnsw_idx "
        "ON products_embeddings USING hnsw (embedding vector_cosine_ops)"
    )
    conn.close()


def _connect_with_retry(host, port, database, secret, retries=10, delay=30):
    for attempt in range(retries):
        try:
            return pg8000.native.Connection(
                host=host,
                port=port,
                database=database,
                user=secret["username"],
                password=secret["password"],
            )
        except Exception:
            if attempt == retries - 1:
                raise
            time.sleep(delay)
