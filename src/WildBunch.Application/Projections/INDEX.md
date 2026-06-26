# Projections

Domain-event projectors and projection state built from the event stream.

## Key files

- [IDomainEventProjector.cs](IDomainEventProjector.cs) - Contract for domain-event projectors.
- [HudProjector.cs](HudProjector.cs) / [HudProjection.cs](HudProjection.cs) - HUD projection from events.
- [DiaryProjector.cs](DiaryProjector.cs) / [DiaryProjection.cs](DiaryProjection.cs) - Travel diary projection from events.
- [CaseFileViewProjector.cs](CaseFileViewProjector.cs) / [CaseFileViewProjection.cs](CaseFileViewProjection.cs) - Case file view projection from events.
- [FullAuditProjector.cs](FullAuditProjector.cs) / [FullAuditProjection.cs](FullAuditProjection.cs) - Full audit projection from events.
- [JournalLogProjector.cs](JournalLogProjector.cs) - Journal log projector.

Back to [WildBunch.Application/](../INDEX.md)
