# Ticket: Container observability — resource metrics + log investigation in Grafana

Status: PLANNED (not yet implemented)
Origin: Opus 4.7 review, section C6 (Logging / observability). Deferred from the review pass to a standalone feature ticket.

## Summary

Add two observability features to the existing Grafana stack, covering the four
runtime containers `redis`, `resolverpostgres` (postgres), `resolver` (Silo), and
`resolverclient` (BlazorServer):

1. **Resource metrics** — combined CPU% and memory line graphs per container, added
   to the existing `dashboard_resolver_events.json` dashboard alongside the current
   player/game-update panels.
2. **Log ingestion + investigation** — collect stdout/stderr (and the app's own
   structured logs) from the same four containers into a queryable store, with a
   Grafana dashboard to browse/filter/search them.

## Hard constraint: low CPU overhead

The stack is hosted on a low-power ARM box. The overriding design constraint is that
these features must add **near-flat, predictable background CPU**, not continuous
scraping/indexing load. This rules out the otherwise-standard heavy components:

- **cAdvisor is explicitly out.** It continuously walks every container's cgroup tree
  and derives metrics; this is exactly the kind of constant background CPU we must
  avoid. (Prometheus scraping itself is cheap — cAdvisor's collection is the cost.)
- A **full ELK / heavyweight log pipeline is out** for the same reason (continuous
  ingestion + indexing overhead).

---

## Part 1 — Resource metrics (CPU / memory per container)

### Proposed approach (lowest overhead, reuses existing Postgres datasource)

A tiny periodic poller that reuses the Postgres datasource Grafana already uses:

- A small service (or ~30-line script in a minimal container) runs
  `docker stats --no-stream` (or reads the Docker API `/containers/{id}/stats?stream=false`)
  every **15–30s**, then exits until the next interval. One cgroup read per interval,
  no persistent scraper.
- Insert rows `(timestamp, container_name, cpu_pct, mem_used_bytes, mem_limit_bytes)`
  into a new Postgres table.
- Grafana reads from the **same existing Postgres datasource** — add panels to
  `dashboard_resolver_events.json` next to the player/game-update counts. No
  Prometheus, no second datasource, no cAdvisor.

### Proposed schema (Postgres)

```sql
CREATE TABLE IF NOT EXISTS container_stats (
    ts             timestamptz NOT NULL DEFAULT now(),
    container_name text        NOT NULL,
    cpu_pct        double precision,
    mem_used_bytes bigint,
    mem_limit_bytes bigint
);
CREATE INDEX IF NOT EXISTS ix_container_stats_ts ON container_stats (ts);
CREATE INDEX IF NOT EXISTS ix_container_stats_name_ts ON container_stats (container_name, ts);
```

### Retention

- Add a periodic cleanup so the table does not grow unbounded, e.g. a scheduled
  `DELETE FROM container_stats WHERE ts < now() - interval '30 days';` (interval TBD).
- Decide retention window (suggest 14–30 days) before implementation.

### Security / wiring

- The poller needs **read-only** access to the Docker socket:
  `/var/run/docker.sock:ro`. Note this is a privileged mount — confine the poller image
  to a minimal base and a single-purpose script.
- New compose service on `resolvernet`; `depends_on` the four target containers (no
  health gating needed — it tolerates targets being absent).
- Consider running it under the `profiles`/restart conventions already used in
  `infra/docker-compose.yml` (poller should be long-lived with its own sleep loop, or a
  one-shot on a cron/timer — decide during implementation).

### Open questions

- Poll interval (15s vs 30s vs 60s) — trade resolution vs overhead.
- Long-lived sleep-loop container vs external cron invoking `docker compose run`.
- Whether to also capture network I/O / block I/O columns from `docker stats`.

---

## Part 2 — Log ingestion + investigation dashboard

Collect logs from the same four containers (`redis`, `resolverpostgres`, `resolver`,
`resolverclient`) into a queryable store and add a Grafana dashboard to browse, filter
by container/level/time, and full-text search.

### Options (decide during implementation; overhead is the deciding factor)

**Option A — Postgres log table (reuses existing datasource, lowest new infra).**
- App services already have a `FaemiyahLogger` capable of writing to the database; extend
  / reuse it to land structured log rows (timestamp, container/service, level, category,
  message, exception) in a Postgres table.
- For `redis` and `postgres` (which don't use FaemiyahLogger), a lightweight tail-and-insert
  shipper, or scrape their container stdout periodically, into the same table.
- Grafana log panels query Postgres directly (same datasource as Part 1 and the existing
  dashboard). Full-text via `tsvector`/`ILIKE` (index appropriately).
- Pros: no new datasource, one storage system, simplest ops. Cons: Postgres is not a
  purpose-built log store; need retention + indexing discipline; high-volume logging could
  pressure the DB.

**Option B — Grafana Loki + a lightweight agent (Grafana Alloy / Promtail).**
- Loki is materially lighter than ELK (index-on-labels, stores compressed chunks). A single
  Alloy/Promtail agent tails the four containers' stdout and ships to Loki.
- Add a Loki datasource in Grafana and a log-exploration dashboard.
- Pros: purpose-built log UX (LogQL, live tail, label filters). Cons: adds Loki + an agent
  (more memory/CPU than Option A; still far less than ELK). Must validate overhead on the
  ARM host before committing.

### Recommendation to evaluate first

Start with **Option A** (Postgres) for parity with Part 1 and zero new datasource, and only
escalate to **Option B (Loki)** if log volume or query ergonomics make the Postgres approach
painful. Measure overhead either way.

### Schema sketch (Option A)

```sql
CREATE TABLE IF NOT EXISTS container_logs (
    ts          timestamptz NOT NULL DEFAULT now(),
    container_name text     NOT NULL,
    level       text,
    category    text,
    message     text,
    exception   text
);
CREATE INDEX IF NOT EXISTS ix_container_logs_ts ON container_logs (ts);
CREATE INDEX IF NOT EXISTS ix_container_logs_name_ts ON container_logs (container_name, ts);
-- full-text search index TBD (tsvector on message/exception)
```

### Retention

- Logs are higher-volume than metrics — pick a shorter window (suggest 7–14 days) and a
  scheduled cleanup delete. Confirm before implementation.

### Open questions

- Postgres (Option A) vs Loki (Option B) — gate on a quick overhead measurement.
- How to capture `redis` / `postgres` container stdout uniformly (shipper vs periodic scrape).
- Log level filtering at source to cap volume (don't ship Debug in production).
- PII / secrets scrubbing before persistence.

---

## Acceptance criteria

- [ ] `container_stats` populated for all four containers at the chosen interval, with
      retention cleanup in place.
- [ ] Existing `dashboard_resolver_events.json` gains combined CPU% and memory panels for
      redis / postgres / resolver / resolverclient, served from the existing Postgres datasource.
- [ ] Logs from all four containers are ingested into the chosen store with retention.
- [ ] A Grafana dashboard allows browsing/filtering/searching those logs by container, level,
      and time range.
- [ ] Measured background CPU overhead on the ARM host is confirmed acceptable (no continuous
      high-CPU scraper; cAdvisor and ELK explicitly avoided).

## Notes

- Both parts deliberately reuse the single existing Grafana Postgres datasource where possible
  to avoid new moving parts.
- This ticket supersedes the C6 review bullets about missing OpenTelemetry/Prometheus/metrics
  dashboards and missing log aggregation; those were intentionally deferred here rather than
  built into the review remediation.
