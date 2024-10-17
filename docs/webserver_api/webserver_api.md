# Webserver Api

## Overview

This document will specify funcions that the webserver will provide that can be called to collect data from our search engine

## Conventions

- All API endpoints begin with `/api`.
- Keys followed by a `?` are optional and may not be present unless explicitly guaranteed by a function call.
- Values enclosed in `[]` denote arrays, which may contain multiple elements.
- Values enclosed in `<>` denote placeholders that should be replaced with actual values.
- Keys with non-primitive value types are specified elsewhere in this document; in such cases, there will be a hyperlink in the description.

## Table of Contents

- [Webserver API Specification](#webserver-api)
  - [Overview](#overview)
  - [Conventions](#conventions)
  - [Table of Contents](#table-of-contents)
- [Enpoints](#endpoints)

## Endpoints

### `/query?query=<query_string>&max_results=<result_count>` (GET)

This endpoint is used to retreve query results from the webserver. It returns a [Document JSON](#document-json-specification) array that has at most result_count elements. The array will be empty if no entries are found

## JSON Specification

### Document JSON

A Query JSON describes the payload that the webserver will package the search engine result into

```json
{
	"id": string,
	"title": string,
	"description": string,
	"summary": string,
	"url": string,
	"author": string,
	"ranking": int
}
```

| Object              | Definition                                       |
| ------------------- | ------------------------------------------------ |
| id         | A unique identifier for this document  |
| title      | The title of this document|
| description| A brief description of this document |
| summary    | A brief summary of the document |
| url        | a link to the actual document |
| author     | the author of the document |
| ranking    | the ranking of this document in terms of relevance to the query  |



