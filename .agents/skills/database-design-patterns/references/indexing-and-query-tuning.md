# Indexing and query tuning

Use this reference when planning indexes or tuning queries. Start by examining
the query plan and the predicates; then choose indexes that cover the most
selective predicates and avoid redundant or unused indexes.

This guidance is first-party synthesis supported by the BCcampus textbook
chapter on indexing, Markus Winand's *Use The Index, Luke*, the Red Book, and
PostgreSQL/SQLite documentation for engine-specific examples. Do not copy
proprietary material; state the principles and point to the sources for depth.
