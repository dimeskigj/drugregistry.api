# DrugRegistry.API

This document contains the API specification for **DrugRegistry**, which is a platform for querying information about drugs and pharmacies.

## Paths
The following paths are available:

### **/api/drugs**

This endpoint allows users to list all drugs with pagination. It accepts the following parameters:

- **page** (optional): An integer representing the page number of the results (default: 0).
- **size** (optional): An integer representing the number of results to show per page (default: 10, max: 20).

**Response**: Returns a paged result of drugs ordered by generic name.

---

### **/api/drugs/search**

This endpoint allows users to search for drugs based on a query string. It accepts the following parameters:

- **query**: A string representing the search query.
- **page** (optional): An integer representing the page number of the search results.
- **size** (optional): An integer representing the number of results to show per page.

**Response**: Returns a paged result of drugs.

---

### **/api/drugs/by-ids**

This endpoint allows users to retrieve drugs by their unique identifiers (IDs). It accepts the following parameters:

- **ids**: A list of GUIDs representing the IDs of the drugs to retrieve.

**Response**: Returns a list of drugs corresponding to the provided IDs.

---

### **/api/pharmacies/by-location**

This endpoint allows users to search for pharmacies based on their geographical location. It accepts the following parameters:

- **lon**: A double representing the longitude of the user's location.
- **lat**: A double representing the latitude of the user's location.
- **page** (optional): An integer representing the page number of the search results.
- **size** (optional): An integer representing the number of results to show per page.
- **municipality** (optional): A string representing the municipality of the pharmacy.
- **place** (optional): A string representing the place of the pharmacy.

**Response**: Returns a paged result of pharmacies located near the provided coordinates.

---

### **/api/pharmacies/search**

This endpoint allows users to search for pharmacies based on a search query related to their name or address. It accepts the following parameters:

- **query**: A string representing the search query.
- **page** (optional): An integer representing the page number of the search results.
- **size** (optional): An integer representing the number of results to show per page.
- **municipality** (optional): A string representing the municipality of the pharmacy.
- **place** (optional): A string representing the place of the pharmacy.

**Response**: Returns a paged result of pharmacies matching the search query.

---

### **/api/pharmacies/municipalities-by-frequency**

This endpoint allows users to query the municipalities with the highest number of pharmacies. It does not accept any parameters.

**Response**: Returns a list of municipality names ordered by the frequency of pharmacies.

---

### **/api/pharmacies/places-by-frequency**

This endpoint allows users to query the places within a municipality that have the highest number of pharmacies. It accepts the following parameter:

- **municipality**: A string representing the municipality to query.

**Response**: Returns a list of places within the specified municipality, ordered by the frequency of pharmacies.

---

## Components

The following components are used in the API:

- **PagedResult**: A paged result containing a list of drugs or pharmacies and pagination information.

---
