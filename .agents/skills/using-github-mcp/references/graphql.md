# GraphQL

Use this when REST or `gh` does not expose the exact GitHub object you need, such as nested reads, bulk collections, or objects that only exist in GraphQL (e.g., review threads, project item fields).

## Running GraphQL

Use `gh api graphql --input <file>.json` with a JSON body of `{"query": "...", "variables": {...}}`.

In PowerShell, `--input <file>.json` is the reliable form; `-f query='...'` is fragile when the query contains spaces, quotes, or braces.

## Common queries

### Repository overview

```graphql
query {
  repository(owner: "OWNER", name: "REPO") {
    id
    defaultBranchRef { name }
    description
    isPrivate
    pushedAt
  }
}
```

### Pull request state and head SHA

```graphql
query {
  repository(owner: "OWNER", name: "REPO") {
    pullRequest(number: N) {
      id
      headRefOid
      url
      state
      mergeable
      isDraft
      author { login }
    }
  }
}
```

### Issue list with labels

```graphql
query {
  repository(owner: "OWNER", name: "REPO") {
    issues(first: 25, states: OPEN) {
      nodes {
        number
        title
        state
        author { login }
        labels(first: 10) { nodes { name } }
      }
    }
  }
}
```

### Review threads on a PR

```graphql
query {
  repository(owner: "OWNER", name: "REPO") {
    pullRequest(number: N) {
      reviewThreads(first: 100) {
        nodes {
          id
          isResolved
          comments(first: 10) {
            nodes {
              id
              author { login }
              body
            }
          }
        }
      }
    }
  }
}
```

## Common mutations

### Resolve a review thread

```graphql
mutation {
  resolveReviewThread(input: {threadId: "PRRT_..."}) {
    thread { id isResolved }
  }
}
```

### Add a reply to a review thread

```graphql
mutation {
  addPullRequestReviewThreadReply(input: {pullRequestReviewThreadId: "PRRT_...", body: "..."}) {
    comment { id body }
  }
}
```

## Notes

- GraphQL is the only route for `PullRequestReviewThread` reads and for resolving review threads.
- REST review comment IDs (from `#discussion_r<id>`) are numeric; GraphQL thread node IDs are `PRRT_...`.
- Use GraphQL when you need nested or paginated collections that REST returns as separate calls.
- Keep mutations explicit and authorized; classify any tool call as `read_only` or `mutation` before calling it.
