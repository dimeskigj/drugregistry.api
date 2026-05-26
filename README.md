# DrugRegistry.API

DrugRegistry exposes public read APIs for drugs and pharmacies.

## Running With Docker Compose

Create a `.env` file from `.env.example`, set a real `POSTGRES_PASSWORD`, then start the stack:

```sh
docker compose up --build -d
```

Compose startup order:

1. `db` starts Postgres and waits for `pg_isready`.
2. `migrations` runs an idempotent EF migration bundle.
3. `api` starts only after migrations complete successfully.

The migration bundle is safe to run on every `docker compose up`; it only applies pending migrations.

Required/important environment variables:

- `POSTGRES_PASSWORD`: required, no default.
- `POSTGRES_USER`: default `drugregistry`.
- `POSTGRES_DB`: default `drugdb`.
- `POSTGRES_DATA_PATH`: default `../files/postgres-data`.
- `API_PORT`: default `8080`.
- `CORS_ALLOWED_ORIGINS`: comma-separated browser origins allowed to call the API. Empty means no browser CORS origins are allowed.
- `FORWARDED_HEADERS_KNOWN_PROXIES`: comma-separated proxy IP addresses allowed to supply `X-Forwarded-*` headers. Leave empty when the API is exposed directly.
- `FORWARDED_HEADERS_KNOWN_NETWORKS`: comma-separated proxy CIDR networks allowed to supply `X-Forwarded-*` headers. Leave empty when the API is exposed directly.
- `DATA_INGESTION_RUN_BOOTSTRAP_ON_STARTUP`: default `false`. Set to `true` only when you intentionally want startup to trigger scraping/seeding for empty tables.

## Local Development

The API requires `ConnectionStrings:Database` at startup. For local `dotnet run` or IDE launch, store it in user secrets instead of committing a developer password:

```sh
dotnet user-secrets set --project DrugRegistry.API "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=drugdb;Username=drugregistry;Password=<your-local-password>"
```

## API Docs And Health

- Scalar API docs: `GET /docs`
- OpenAPI JSON: `GET /openapi/v2.json`, `GET /openapi/v1.json`
- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`

Swagger UI and Swashbuckle are not used.

## V2 API

V2 is available under `/api/v2` and is the current public API.

### Drugs

1. `GET /api/v2/drugs`
   - Query params: `page`, `size`, `query`, repeatable `id`.
   - Examples: `/api/v2/drugs?page=0&size=10`, `/api/v2/drugs?query=paracetamol`, `/api/v2/drugs?id={guid1}&id={guid2}`.
2. `GET /api/v2/drugs/{id}`
3. `GET /api/v2/drugs/ean/{ean}`

### Pharmacies

1. `GET /api/v2/pharmacies`
   - Query params: `page`, `size`, `municipality`, `place`, `query`, `lon`, `lat`, repeatable `id`.
   - Examples: `/api/v2/pharmacies?query=zegin`, `/api/v2/pharmacies?lon=21.433&lat=41.998`, `/api/v2/pharmacies?id={guid1}&id={guid2}`.
2. `GET /api/v2/pharmacies/{id}`
3. `GET /api/v2/pharmacies/municipalities`
4. `GET /api/v2/pharmacies/municipalities/{municipality}/places`

Collection endpoints return `data`, `totalCount`, `page`, and `size`. Invalid inputs return RFC 7807 Problem Details.

## Public Limits

Request limits:

- `page`: `0..500`, default `0`.
- `size`: `1..100`, default `10`.
- `query`: `2..200` trimmed characters.
- `id`: at most `50` repeated ids per request.
- `municipality`: at most `100` trimmed characters.
- `place`: at most `100` trimmed characters.
- `ean`: at most `32` trimmed characters.
- `lon`: finite number in `-180..180`.
- `lat`: finite number in `-90..90`.
- `lon` and `lat` must be provided together.

Rate limits are per client IP with no queueing:

- Public API endpoints: `120` requests per minute.
- Scalar/OpenAPI docs: `30` requests per minute.
- Health endpoints: `60` requests per minute.

Cache TTLs:

- List/search endpoints: `2 minutes`.
- Detail endpoints by id/EAN: `10 minutes`.
- Municipality/place lookup endpoints: `30 minutes`.
- Health endpoints and errors are not cached.

Fuzzy search remains in memory. Query, page, and size limits are enforced before fuzzy search runs.

## V1 API

Existing V1 paths under `/api/*` remain available for compatibility, but they are deprecated in favor of `/api/v2/*`. V1 is also rate-limited and validates the same public limits where applicable.
