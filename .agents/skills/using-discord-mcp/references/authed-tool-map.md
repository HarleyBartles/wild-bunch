# Discord MCP Authed Tool Map

These are the `discord` MCP tools that work with the bot's current read-only permissions. Use this as the default map for normal reading, discovery, and attachment download tasks. If a task needs a tool not on this list, stop and ask your human partner to widen the bot's Discord permissions, then open `references/tool-map.md`.

| Tool | Description | Required args |
|---|---|---|
| `discord_read_messages` | Read the last N messages from a text channel. | `channel_id` |
| `discord_fetch_pinned_messages` | List all pinned messages in a channel. | `channel_id` |
| `discord_get_reactions` | List users who reacted with a specific emoji on a message. | `channel_id`, `message_id`, `emoji` |
| `discord_search_messages` | Search messages in a channel by keyword (scans up to last 100 messages). | `channel_id`, `keyword` |
| `discord_list_channels` | List all channels in a guild grouped by category. | `guild_id` |
| `discord_find_channel_by_name` | Find a channel by name in a guild (partial match supported). | `guild_id`, `name` |
| `discord_get_channel_permissions` | List all permission overwrites on a channel (per role and per member). | `channel_id` |
| `discord_audit_permissions` | Generate a full permission audit report for a guild: who can access what on every channel. | `guild_id` |
| `discord_get_forum_channels` | List all forum channels in a guild. | `guild_id` |
| `discord_list_forum_threads` | List all threads (active and archived) in a forum channel. | `forum_channel_id` |
| `discord_get_forum_post` | Get a forum post's details and its messages. | `thread_id` |
| `discord_get_forum_tags` | Get the available tags for a forum channel. | `forum_channel_id` |
| `discord_list_members` | List guild members with their roles. | `guild_id` |
| `discord_get_member_info` | Get detailed info about a member: roles, permissions, join date, timeout status. | `guild_id`, `user_id` |
| `discord_search_members` | Search guild members by username or nickname. | `guild_id`, `query` |
| `discord_list_roles` | List all roles in a guild with permissions and member count. | `guild_id` |
| `discord_get_role_members` | List all members that have a specific role. | `guild_id`, `role_id` |
| `discord_list_scheduled_events` | List all scheduled events in a guild. | `guild_id` |
| `discord_get_scheduled_event` | Get detailed info about a specific scheduled event. | `guild_id`, `event_id` |
| `discord_get_event_subscribers` | Get users who marked 'Interested' in a scheduled event. | `guild_id`, `event_id` |
| `discord_list_guilds` | List all Discord servers the bot is connected to. |  |
| `discord_get_guild_info` | Get detailed info about a guild: name, member count, channels, roles, boosts. | `guild_id` |
| `discord_get_server_stats` | Get server statistics: member count (humans vs bots), channels, roles, boost level. | `guild_id` |
| `discord_read_dms` | Read the last N messages from a DM channel with a user. | `user_id` |
| `discord_list_dm_channels` | List all currently open/cached DM channels the bot has. |  |
| `discord_download_attachment` | Download a Discord attachment from a CDN URL to a local temp file. Only accepts URLs from cdn.discordapp.com or media.discordapp.net. | `url` |

## Common patterns

### Read messages from a text channel

1. Use `discord_list_channels` or `discord_find_channel_by_name` with the guild ID from the `DISCORD_GUILD_ID` environment variable (or call `discord_list_guilds` to discover the target server).
2. Call `discord_read_messages` with `channel_id` and `limit`.
3. If a message has attachments, use `discord_download_attachment` with `attachments[].url`.

### Search and reactions

- `discord_search_messages` (`channel_id`, `keyword`) scans the last 100 messages.
- `discord_get_reactions` (`channel_id`, `message_id`, `emoji`) lists users for a reaction.

### Forum and members

- `discord_get_forum_channels` lists forum channels; `discord_list_forum_threads` and `discord_get_forum_post` read forum content.
- `discord_list_members`, `discord_get_member_info`, and `discord_search_members` read member data (Server Members Intent is enabled).
