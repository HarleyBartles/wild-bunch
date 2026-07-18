# Worktree and branch policy

## Read when

Read before repository work, worktree creation, branching, scratch use, a PR,
or publication decision.

## Portable location algorithm

Use this exact Git-derived algorithm. Future tooling must consume this
algorithm rather than deriving repository locations from the Python source
file, process directory, or arbitrary parent searches.

~~~text
superproject = git -C <start-path> rev-parse --show-superproject-working-tree
current-checkout = git -C <start-path> rev-parse --show-toplevel
common-git = git -C <current-checkout> rev-parse --path-format=absolute --git-common-dir
checkout-git = git -C <current-checkout> rev-parse --path-format=absolute --git-dir
main-checkout = parent(common-git)
repository-name = basename(main-checkout)
external-worktree-root = parent(main-checkout) / "_agent-worktrees" / repository-name
external-scratch-root = parent(main-checkout) / "_agent-scratch" / repository-name / branch-name
~~~

Run the superproject command first from the actual start path. Any non-empty
result is a submodule: reject it unconditionally. Do not allow a shared-checkout
override or fallback inference to bypass that rejection.

The absolute common Git directory is anchored by Git to the active repository.
Its parent is the main checkout for both a normal checkout and a linked
worktree, including when the start path is nested below either checkout. Do not
join a relative `--git-common-dir` result to an already-resolved checkout, and
do not derive these locations from `Path(__file__)` or the process working
directory.

When `checkout-git` and `common-git` resolve to the same path, the worker is in
the shared main checkout. Mutation must refuse that checkout by default. An
explicit `--allow-shared-checkout` may continue only after current human
approval and a prominent warning; it changes no path calculation and never
permits a submodule.

## Fresh-main gate

Before editing files:

1. Fetch the required remote base, normally current `origin/main`.
2. Record the starting main SHA.
3. Create the task branch from that SHA, or update the existing task branch
   onto that SHA before continuing.
4. Do not implement from a stale base.
5. If the base cannot be fetched or the branch cannot be updated safely, stop
   with the exact blocker.

## Worktree isolation and verification gate

Use a dedicated linked worktree for repo mutation unless the user explicitly
authorizes the shared checkout override. Before mutation, record:

- current checkout and Git-derived main checkout;
- task branch and base commit;
- `git status --short` and any pre-existing dirty state;
- the external worktree and scratch roots selected under local repository
  policy.

Verify the checkout before editing:

~~~text
git -C <current-checkout> rev-parse --show-toplevel
git -C <current-checkout> rev-parse --path-format=absolute --git-dir
git -C <current-checkout> rev-parse --path-format=absolute --git-common-dir
git -C <current-checkout> branch --show-current
git -C <current-checkout> status --short
git worktree list --porcelain
~~~

The top level must equal the intended linked worktree, the branch must equal
the task branch, the worktree list must register that path, and edited files
must resolve below it. Preserve and report pre-existing dirty state.

## Worktree stop signs

Stop before mutation when:

- the checkout is a submodule;
- the checkout is shared and no explicit approved override exists;
- the current top level, registered worktree, branch, or file paths point at a
  different checkout;
- current main cannot be fetched or the task branch cannot be updated;
- the target repository or base is ambiguous;
- moving or replacing pre-existing dirty work would be required.

Correct location mismatches before continuing. Do not create a nested worktree
or silently switch to the main checkout.

## Worker boundary

Use a dedicated worktree and task branch unless the user explicitly authorizes
another route. Before mutation, record the checkout, branch, base commit, and
initial status; preserve pre-existing dirty state. Keep disposable artifacts
under external_scratch_root, never inside the repository. Scratch is external,
per-repository, per-branch, disposable, and never durable custody.

Fetch the required base, make a focused commit, push the branch, and open a PR
unless direct-main work is explicitly authorized. Local edits, test logs, and
commit hashes are not publication proof; GitHub-visible PR or authorized
direct-main evidence is.

## Branch and PR gate

Use a task branch. The normal publication sequence is edit, validate, commit,
push, open or update a PR into the approved base, then verify the published
head and remote checks. Direct-main mutation requires explicit current
authorization. Do not substitute local credentials workarounds for the
repository's declared GitHub publication route.
