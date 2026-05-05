# DrugRegistry.API

DrugRegistry exposes read APIs for drugs and pharmacies.

## V2 API (current)

V2 is available under `/api/v2` and follows resource-oriented design with query filters.

### Drugs

1. `GET /api/v2/drugs`
   - List drugs with pagination.
   - Query params: `page` (default `0`), `size` (default `10`), `query` (search), `id` (repeatable id filter).
   - Example: `/api/v2/drugs?page=0&size=10`
   - Example: `/api/v2/drugs?query=paracetamol&page=0&size=10`
   - Example: `/api/v2/drugs?id={guid1}&id={guid2}`
2. `GET /api/v2/drugs/{id}`
   - Get a single drug by id.
3. `GET /api/v2/drugs/ean/{ean}`
   - Get a single drug by EAN code.

### Pharmacies

1. `GET /api/v2/pharmacies`
   - List pharmacies with pagination and optional filters.
   - Query params: `page` (default `0`), `size` (default `10`), `municipality`, `place`, `query`, `lon`, `lat`, `id` (repeatable id filter).
   - Example: `/api/v2/pharmacies?page=0&size=10`
   - Example: `/api/v2/pharmacies?query=zegin&page=0&size=10`
   - Example: `/api/v2/pharmacies?lon=21.433&lat=41.998&page=0&size=10`
   - Example: `/api/v2/pharmacies?id={guid1}&id={guid2}`
2. `GET /api/v2/pharmacies/{id}`
   - Get a single pharmacy by id.
3. `GET /api/v2/pharmacies/municipalities`
   - Get municipalities ordered by pharmacy frequency.
4. `GET /api/v2/pharmacies/municipalities/{municipality}/places`
   - Get places for a municipality ordered by pharmacy frequency.

### Response format

- Collection endpoints return:
  - `data`: list of resources
  - `totalCount`: total matching rows
  - `page`: current page (0-based)
  - `size`: page size
- Invalid inputs return RFC 7807 Problem Details.

## V1 API (deprecated)

Existing V1 paths under `/api/*` are still available for compatibility but are deprecated in favor of `/api/v2/*`.
