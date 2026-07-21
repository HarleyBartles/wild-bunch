# GitHub surface map

This is the at-a-glance inventory. Use the intent files for the actual tool guidance.

## MCP server tools

### Context

- `get_me` — Get information about the authenticated user.
- `get_teams` — Get a list of teams for an organization.
- `get_team_members` — Get a list of members for a team.

### Repositories

- `search_repositories` — Search for repositories.
- `get_file_contents` — Get the contents of a file in a repository.
- `list_commits` — List commits in a repository.
- `search_code` — Search code in a repository.
- `get_commit` — Get a single commit.
- `list_branches` — List branches in a repository.
- `list_tags` — List git tags in a repository.
- `get_tag` — Get a single tag.
- `list_releases` — List releases in a repository.
- `get_latest_release` — Get the latest release of a repository.
- `get_release_by_tag` — Get a release by its tag name.
- `create_or_update_file` — Create or update a file in a repository.
- `create_repository` — Create a new repository.
- `fork_repository` — Fork a repository.
- `create_branch` — Create a new branch.
- `push_files` — Push files to a repository.
- `delete_file` — Delete a file from a repository.
- `list_starred_repositories` — List starred repositories for the authenticated user.
- `star_repository` — Star a repository.
- `unstar_repository` — Unstar a repository.
- `get_repository_tree` — Get the tree of a repository.

### Issues

- `issue_read` — Read GitHub issues.
- `search_issues` — Search for issues.
- `list_issues` — List issues.
- `list_issue_types` — List available issue types.
- `issue_write` — Create or update GitHub issues.
- `add_issue_comment` — Add a comment to an issue or pull request.
- `sub_issue_write` — Create or update sub-issues.

### Pull requests

- `pull_request_read` — Read GitHub pull requests.
- `list_pull_requests` — List pull requests in a repository.
- `search_pull_requests` — Search for pull requests.
- `merge_pull_request` — Merge a pull request.
- `update_pull_request_branch` — Update a pull request branch.
- `create_pull_request` — Create a new pull request.
- `update_pull_request` — Update an existing pull request.
- `pull_request_review_write` — Write a pull request review.
- `add_comment_to_pending_review` — Add a comment to a pending review.
- `add_reply_to_pull_request_comment` — Add a reply to a pull request comment.

### Labels

- `list_labels` — List labels for a repository.
- `get_label` — Get a single label.
- `create_label` — Create a new label.
- `update_label` — Update an existing label.
- `delete_label` — Delete a label.

### Actions

- `actions_get` — Get details of GitHub Actions resources.
- `actions_list` — List GitHub Actions workflows in a repository.
- `actions_run_trigger` — Trigger GitHub Actions workflow runs.

### Notifications

- `list_notifications` — List GitHub notifications for the authenticated user.
- `get_notification_details` — Get details for a specific notification.
- `dismiss_notification` — Dismiss a notification.
- `mark_all_notifications_read` — Mark all notifications as read.
- `manage_notification_subscription` — Manage a notification subscription.
- `manage_repository_notification_subscription` — Manage a repository notification subscription.

### Projects

- `list_projects` — List projects for a repository or organization.
- `get_project` — Get a single project.
- `create_project` — Create a new project.
- `update_project` — Update an existing project.
- `delete_project` — Delete a project.
- `list_project_fields` — List fields for a project.
- `list_project_items` — List items for a project.
- `get_project_item` — Get a single project item.
- `create_project_item` — Create a new project item.
- `update_project_item` — Update an existing project item.
- `delete_project_item` — Delete an existing project item.

### Security, quality, and dependabot

- `get_code_scanning_alert` — Get a single code scanning alert.
- `list_code_scanning_alerts` — List code scanning alerts for a repository.
- `get_dependabot_alert` — Get a single Dependabot alert.
- `list_dependabot_alerts` — List Dependabot alerts in a repository.
- `get_secret_scanning_alert` — Get a single secret scanning alert.
- `list_secret_scanning_alerts` — List secret scanning alerts for a repository.
- `list_security_advisories` — List security advisories.
- `get_security_advisory` — Get a single security advisory.

### Discussions

- `list_discussions` — List discussions for a repository or organisation.
- `get_discussion` — Get a single discussion.
- `get_discussion_comments` — Get comments for a discussion.
- `list_discussion_categories` — List discussion categories with their id and name.

### Gists

- `list_gists` — List gists for a user.
- `get_gist` — Get the content of a gist.

### Dynamic

- `enable_toolset` — Enables a specified toolset.
- `list_available_toolsets` — List all available toolsets.
- `get_toolset_tools` — Lists the capabilities enabled with a specified toolset.

### Copilot

- `assign_copilot_to_issue` — Assign a Copilot to an issue.
- `request_copilot_review` — Request a Copilot review for a pull request.
- `get_copilot_space` — Get a Copilot Space.
- `list_copilot_spaces` — List Copilot Spaces.

### Users and orgs

- `search_users` — Search for users.
- `search_orgs` — Search for organizations.
- `list_stargazers` — List stargazers for a repository.

### GitHub support docs search (remote only)

- `github_support_docs_search` — Retrieve documentation to answer GitHub product and support questions.

## gh CLI

### Authentication and context

- `gh auth status` — Check current authentication state.
- `gh auth token` — Print the auth token for the active account.
- `gh auth login` — Authenticate with a GitHub host.
- `gh auth logout` — Log out of a GitHub host.

### Repository read and search

- `gh repo view` — View a repository.
- `gh repo list` — List repositories owned by a user or organization.
- `gh search repos` — Search for repositories.
- `gh search code` — Search code.

### Pull requests

- `gh pr view` — View a pull request.
- `gh pr list` — List pull requests.
- `gh pr create` — Create a pull request.
- `gh pr checkout` — Check out a pull request branch.
- `gh pr merge` — Merge a pull request.
- `gh pr comment` — Add a comment to a pull request.
- `gh pr review` — Review a pull request.
- `gh pr checks` — Show CI status for a pull request.
- `gh pr diff` — View a pull request diff.

### Issues

- `gh issue view` — View an issue.
- `gh issue list` — List issues.
- `gh issue create` — Create an issue.
- `gh issue comment` — Add a comment to an issue.
- `gh issue close` — Close an issue.

### API and scripting

- `gh api` — Make authenticated GitHub API calls.
- `gh api graphql` — Make authenticated GitHub GraphQL calls.
- `gh run list` — List recent workflow runs.
- `gh run view` — View a workflow run.
- `gh release list` — List releases.
- `gh release view` — View a release.

## REST API

- `GET /repos/{owner}/{repo}` — Repository metadata.
- `GET /repos/{owner}/{repo}/contents/{path}` — File contents.
- `GET /repos/{owner}/{repo}/commits` — Commit list.
- `GET /repos/{owner}/{repo}/git/trees/{ref}` — Git tree.
- `GET /repos/{owner}/{repo}/pulls` — Pull requests.
- `GET /repos/{owner}/{repo}/pulls/{pull_number}` — Single pull request.
- `GET /repos/{owner}/{repo}/pulls/{pull_number}/comments` — Review comments.
- `GET /repos/{owner}/{repo}/issues` — Issues.
- `GET /repos/{owner}/{repo}/issues/{issue_number}/comments` — Issue comments.
- `POST /repos/{owner}/{repo}/pulls/{pull_number}/reviews` — Submit a PR review.
- `POST /repos/{owner}/{repo}/issues/{issue_number}/comments` — Add an issue or PR comment.
- `PUT /repos/{owner}/{repo}/contents/{path}` — Create or update a file.
- `DELETE /repos/{owner}/{repo}/contents/{path}` — Delete a file.
- `POST /repos/{owner}/{repo}/git/refs` — Create a ref.
- `PATCH /repos/{owner}/{repo}/git/refs/{ref}` — Update a ref.
- `GET /repos/{owner}/{repo}/actions/runs` — Workflow runs.

## GraphQL

- Query `repository(owner, name)` for repo metadata, PR list, issue list, refs, and releases.
- Query `repository.pullRequest(number)` for `headRefOid`, `state`, `mergeable`, `reviewThreads`, and `comments`.
- Query `PullRequestReviewThread` nodes for threaded review comments and `isResolved`.
- Mutation `resolveReviewThread(input)` to resolve a review thread.
- Mutation `addPullRequestReviewThreadReply(input)` to reply in a review thread.

## Native git

- `git log` — Commit history.
- `git show` — Commit or object details.
- `git branch` — Local branches.
- `git tag` — Tags.
- `git remote` — Remotes.
- `git fetch` — Fetch remote refs.
- `git push` — Push refs.
- `git worktree` — Linked worktrees.
- `git status` — Working tree status.
