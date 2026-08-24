---
name: using-discord-mcp
description: Use when working with the Discord MCP server, choosing the right tool, or deciding whether the bot's current read-only permissions allow an action or require your human partner to widen them.
metadata:
  source-id: using-discord-mcp
  source-path: codex-marketplace/plugins/mcp-usage-pack/skills/using-discord-mcp/SKILL.md
  provenance-name: Using Discord MCP first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
    - Use when you need to read Discord messages, list servers/channels, search messages, or download attachments through the `discord` MCP server.
    - Use when you are unsure which Discord MCP tool to call.
    - Use when a task may require write, manage, or moderation privileges and you need to check whether the bot is allowed to run it.
  do_not_use_when:
    - Do not use when the bot has not been granted the required Discord permissions for a write/moderation action.
    - Do not use when another more specific skill owns the task.
license: MIT
---

# Using Discord MCP

Use this skill to pick the right `discord` MCP tool and to stay inside the bot's current read-only scope.

## Server context

- **MCP server name:** `discord`
- **Package:** `@rayenking/discord-mcp@1.0.2`
- **Default server (guild) ID:** `<your-guild-id>`
- **Current permissions:** View Channels, Read Message History, Message Content Intent, Server Members Intent, Presence Intent.
- **The bot is not an admin.** It cannot send messages, manage messages/channels/roles, kick/ban, create invites, or perform any other write or moderation action unless your human partner explicitly widens its permissions.

## Fast rules

1. **Read is safe; write is not.** If a tool only inspects data, the bot can run it. If a tool creates, updates, deletes, sends, or manages anything, stop and ask your human partner to add the Discord permission first.
2. **Use IDs, not names.** Discord tools accept snowflake IDs (`guild_id`, `channel_id`, `message_id`). Use `discord_list_channels` or `discord_find_channel_by_name` to resolve a channel name to an ID.
3. **The default `guild_id` is `<your-guild-id>`.** Use it for any tool that requires `guild_id` unless your human partner gives you a different one.

## Router

| Intent | Read first |
|---|---|
| Normal reading, listing, searching, or downloading attachments | [`references/authed-tool-map.md`](references/authed-tool-map.md) |
| A tool not in the authed map, or any write/moderation/server-management task | [`references/tool-map.md`](references/tool-map.md) |
| You are unsure whether the bot has permission | Ask your human partner before calling the tool. |

## Reading pattern

1. If you do not already have a `channel_id`, use `discord_list_channels` or `discord_find_channel_by_name` with `guild_id` `<your-guild-id>`.
2. Call `discord_read_messages` with `channel_id` and a small `limit`.
3. For each message, inspect `attachments[].url`. If a message contains an image or file, call `discord_download_attachment` with that URL to save it locally.

## Widening permission

If a task requires a non-authed tool, explain to your human partner exactly which Discord permission is needed and why, then wait for confirmation before calling the tool. Do not attempt a write call with the bot's current permissions.
