# Scratch workspace policy

## Scope

This policy covers the off-repo `_agent-scratch` directory used for
plan-scoped, branch-scoped, and task-scoped temporary files.

## Layout

```text
_agent-scratch/
  <repo-name>/
    <branch-name>/
      <plan-or-task-basename>/
        ...
```

The top level of `_agent-scratch` may only contain folders named after the
repositories that use it. Each repo folder may only contain folders named
after in-flight branches or active tasks. Leaf contents are disposable
scratch for that task.

## Naming

- `<repo-name>` is the basename of the repository's main checkout directory.
- `<branch-name>` is the git branch name the scratch belongs to.
- `<plan-or-task-basename>` is the base name of the plan or task file without
  extension.

## Canonical branch sanitization

Branch names are used as filesystem directory names, so the following characters
are mapped to `-` by all producers and consumers of scratch paths:

```text
: \ ? * " < > | /
```

This set matches the default Windows/URL forbidden characters plus the path
separators `/` and `\`. All implementations (Bash, PowerShell, and Python
scripts) must use this exact character set so slash-branches such as `feature/x`
are consistently stored as `feature-x`.

## Validation

The `repo-standards` validator checks the local `_agent-scratch` root against
this layout. It reports any file or folder that is not a repo-name folder, and
any repo folder that contains entries not matching a branch or task.

## Cleanup

When a branch is merged and its worktree is removed, its scratch directory is
`delete_now` unless another active task or plan still references it.
