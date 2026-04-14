## ADDED Requirements

### Requirement: User aggregate supports daily close-out log

The `User` aggregate SHALL expose an owned collection `DailyCloseOutLogs` of `DailyCloseOutLog` entries. Each entry SHALL contain `Date` (DateOnly, unique per user), `ClosedAtUtc` (DateTime), `ConfirmedCount` (int), `DiscardedCount` (int), and `RemainingCount` (int). The aggregate SHALL provide a `RecordDailyCloseOut(DateOnly date, int confirmed, int discarded, int remaining)` method that creates or overwrites the entry for the given date with the supplied counts and `ClosedAtUtc = DateTime.UtcNow`, and raises a `DailyCloseOutRecorded` domain event. The aggregate SHALL provide a read-only accessor `IReadOnlyList<DailyCloseOutLog> DailyCloseOutLogs` and a `GetCloseOutLog(DateOnly date)` lookup. Counts MUST be non-negative.

#### Scenario: Record close-out for a new date

- **WHEN** RecordDailyCloseOut(date: 2026-04-14, confirmed: 3, discarded: 1, remaining: 0) is called on a User with no prior log entry for that date
- **THEN** a new DailyCloseOutLog entry is added with the supplied counts and ClosedAtUtc set to now, and a DailyCloseOutRecorded event is raised

#### Scenario: Idempotent overwrite for the same date

- **WHEN** RecordDailyCloseOut is called twice for the same date with different counts
- **THEN** the existing entry's counts and ClosedAtUtc are overwritten with the latest values, and only one entry exists for that date

#### Scenario: Negative count rejected

- **WHEN** RecordDailyCloseOut is called with a negative confirmed, discarded, or remaining count
- **THEN** the method throws a domain validation exception and no entry is created

#### Scenario: Logs scoped to user

- **WHEN** User A and User B each record close-outs for the same date
- **THEN** each user's DailyCloseOutLogs collection contains only their own entry

### Requirement: User profile response includes most-recent close-out

The `GET /api/me` endpoint response SHALL include a `lastCloseOutAtUtc` field set to the `ClosedAtUtc` of the most-recent `DailyCloseOutLog` entry, or null if the user has never closed out.

#### Scenario: User has prior close-outs

- **WHEN** an authenticated user with at least one DailyCloseOutLog entry sends GET /api/me
- **THEN** the response includes lastCloseOutAtUtc equal to the most-recent entry's ClosedAtUtc

#### Scenario: User has never closed out

- **WHEN** an authenticated user with no DailyCloseOutLog entries sends GET /api/me
- **THEN** the response includes lastCloseOutAtUtc as null
