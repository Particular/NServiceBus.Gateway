# Engineering context

## About this repository

This repository implements the NServiceBus Gateway for sending messages between sites.

- `src/` — implementation and tests
- `.github/workflows/` — CI pipelines

## Start here

- [Component documentation](https://docs.particular.net/nservicebus/gateway/) — how sites are configured and how the Gateway handles duplicate messages
- [Repository README](../README.md) — how to run the tests locally
- [Contributing](https://docs.particular.net/platform/contributing) — contribution process

## Decisions and rationale

- [Architecture and design decisions](decisions/)

### Decisions recorded in pull requests

- Register Gateway persistence through features so downstream persisters can install their infrastructure — [#776](https://github.com/Particular/NServiceBus.Gateway/pull/776)

Keep this index current when a canonical source is added, replaced, or retired; link, do not copy.
