# Voice-card contract

Use a voice card only when the user supplies text for the current task or
states explicit preferences. The card records bounded, observable tendencies;
it is not a personality, identity, culture, or authorship profile.

## Derivation boundary

1. Confirm the supplied text or preferences are authorised for the current
   task and record that basis.
2. Observe tendencies only across the supplied sample: sentence range,
   directness, vocabulary register, tolerated fragments, rhetorical devices,
   formatting norms, and explicit prefer/avoid choices.
3. State represented genres, audiences, sample count, derivation date, and
   limitations. Small or narrow samples warrant narrow claims.
4. Store the tendencies, not sentences or excerpts. Set `source_retained` to
   `false`; do not copy the sample into cards, logs, fixtures, or repository
   assets.
5. Apply the card only inside its declared task, storage, and distribution
   boundary. Public availability alone is not permission to ship an imitation
   profile.

Keep provenance fields coupled:

- `synthetic_default` uses `synthetic_example`, `synthetic_fixture`, zero
  samples, and `no_source_storage`;
- `current_task_text` uses `current_task`, `current_task_user`, at least one
  sample, and `no_source_storage`;
- `explicit_preferences` uses `current_task`, `explicit_user_preference`, zero
  text samples, and `no_source_storage`.

All three bases require `source_retained: false`.

The machine-readable contract is
`references/profiles/voice/voice-card.schema.json`. The shipped
`default-voice-card.json` is a synthetic neutral example, not a real person's
profile.

## Application

Prefer positive guidance: name what the prose tends to do and which choices
the user explicitly prefers. Preserve a distinctive device unless a
higher-authority factual, safety, accessibility, project-style, or clarity need
requires change. If the sample does not support a tendency, omit it or mark the
card limited rather than guessing.

## Do not infer or retain

- identity, demographics, personality, culture, health, politics, or intent;
- authorship or detector conclusions;
- a private source corpus, sentence bank, embeddings, or recoverable excerpts;
- tendencies beyond the supplied genres, audiences, or task.
