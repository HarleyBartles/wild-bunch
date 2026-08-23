# Discord MCP Full Tool Map

This reference lists every tool exposed by the `@rayenking/discord-mcp` package used by the `discord` MCP server. Tools are marked **Authed** when the bot's current permissions (View Channels, Read Message History, Message Content Intent, Server Members Intent, Presence Intent) are enough to run them. **Non-authed** tools require your human partner to widen the bot's Discord permissions before use.

## Tool index

| Tool | Description | Required args | Status |
|---|---|---|---|
| `discord_read_messages` | Read the last N messages from a text channel. | `channel_id` | Authed |
| `discord_send_message` | Send a plain text message to a channel. | `channel_id`, `content` | Non-authed |
| `discord_reply_message` | Reply to a specific message in a channel. | `channel_id`, `message_id`, `content` | Non-authed |
| `discord_send_embed` | Send a rich embed message with title, description, color, fields, footer, image, thumbnail, author, URL, and timestamp. | `channel_id` | Non-authed |
| `discord_send_multiple_embeds` | Send up to 10 embeds in a single message. | `channel_id`, `embeds` | Non-authed |
| `discord_edit_message` | Edit a message sent by the bot. | `channel_id`, `message_id`, `content` | Non-authed |
| `discord_delete_message` | Delete a specific message from a channel. | `channel_id`, `message_id` | Non-authed |
| `discord_bulk_delete_messages` | Delete multiple messages at once (2-100, messages must be less than 14 days old). | `channel_id`, `count` | Non-authed |
| `discord_pin_message` | Pin or unpin a message in a channel. | `channel_id`, `message_id`, `pin` | Non-authed |
| `discord_fetch_pinned_messages` | List all pinned messages in a channel. | `channel_id` | Authed |
| `discord_add_reaction` | Add a reaction emoji to a message. | `channel_id`, `message_id`, `emoji` | Non-authed |
| `discord_remove_reactions` | Remove reactions from a message. No emoji = remove all. Emoji only = remove that emoji. Emoji + user_id = remove that user's reaction. | `channel_id`, `message_id` | Non-authed |
| `discord_get_reactions` | List users who reacted with a specific emoji on a message. | `channel_id`, `message_id`, `emoji` | Authed |
| `discord_search_messages` | Search messages in a channel by keyword (scans up to last 100 messages). | `channel_id`, `keyword` | Authed |
| `discord_forward_message` | Forward a message to another channel. | `channel_id`, `message_id`, `target_channel_id` | Non-authed |
| `discord_crosspost_message` | Publish a message in an announcement channel to all following channels. | `channel_id`, `message_id` | Non-authed |
| `discord_edit_embed` | Edit an embed message previously sent by the bot. Only provided fields are updated; omitted fields are removed. | `channel_id`, `message_id` | Non-authed |
| `discord_list_channels` | List all channels in a guild grouped by category. | `guild_id` | Authed |
| `discord_find_channel_by_name` | Find a channel by name in a guild (partial match supported). | `guild_id`, `name` | Authed |
| `discord_create_channel` | Create a text, voice channel or category in a guild. | `guild_id`, `name` | Non-authed |
| `discord_edit_channel` | Edit a channel's name, topic, slowmode, or NSFW flag. | `channel_id` | Non-authed |
| `discord_delete_channel` | Delete a channel. | `channel_id` | Non-authed |
| `discord_move_channel` | Move a channel into a category (or remove from category if category_id is omitted). | `channel_id` | Non-authed |
| `discord_clone_channel` | Clone a channel with its name, topic and permission overwrites. | `channel_id` | Non-authed |
| `discord_set_channel_position` | Set the display position of a channel within its category. | `channel_id`, `position` | Non-authed |
| `discord_follow_announcement_channel` | Follow an announcement channel so its messages are published to a target channel. | `source_channel_id`, `target_channel_id` | Non-authed |
| `discord_get_channel_permissions` | List all permission overwrites on a channel (per role and per member). | `channel_id` | Authed |
| `discord_set_role_permission` | Allow or deny specific permissions for a role on a channel. | `channel_id`, `role_id` | Non-authed |
| `discord_set_member_permission` | Allow or deny specific permissions for a single member on a channel. | `channel_id`, `user_id` | Non-authed |
| `discord_lock_channel_permissions` | Sync a channel's permissions with its parent category. | `channel_id` | Non-authed |
| `discord_reset_channel_permissions` | Remove ALL permission overwrites on a channel (reset to inherited). | `channel_id` | Non-authed |
| `discord_copy_permissions` | Copy all permission overwrites from one channel to another. | `source_channel_id`, `target_channel_id` | Non-authed |
| `discord_audit_permissions` | Generate a full permission audit report for a guild: who can access what on every channel. | `guild_id` | Authed |
| `discord_get_forum_channels` | List all forum channels in a guild. | `guild_id` | Authed |
| `discord_create_forum_channel` | Create a new forum channel in a guild. | `guild_id`, `name` | Non-authed |
| `discord_list_forum_threads` | List all threads (active and archived) in a forum channel. | `forum_channel_id` | Authed |
| `discord_create_forum_post` | Create a new post (thread) in a forum channel. | `forum_channel_id`, `title`, `content` | Non-authed |
| `discord_get_forum_post` | Get a forum post's details and its messages. | `thread_id` | Authed |
| `discord_reply_to_forum` | Reply to a forum post (send a message in a forum thread). | `thread_id`, `content` | Non-authed |
| `discord_delete_forum_post` | Delete (close) a forum post/thread. | `thread_id` | Non-authed |
| `discord_get_forum_tags` | Get the available tags for a forum channel. | `forum_channel_id` | Authed |
| `discord_set_forum_tags` | Set or update the available tags on a forum channel. | `forum_channel_id`, `tags` | Non-authed |
| `discord_update_forum_post` | Update a forum post's title, archived/locked status, or applied tags. | `thread_id` | Non-authed |
| `discord_list_members` | List guild members with their roles. | `guild_id` | Authed |
| `discord_get_member_info` | Get detailed info about a member: roles, permissions, join date, timeout status. | `guild_id`, `user_id` | Authed |
| `discord_search_members` | Search guild members by username or nickname. | `guild_id`, `query` | Authed |
| `discord_set_nickname` | Set or clear a member's nickname. | `guild_id`, `user_id`, `nickname` | Non-authed |
| `discord_list_roles` | List all roles in a guild with permissions and member count. | `guild_id` | Authed |
| `discord_create_role` | Create a new role in a guild. | `guild_id`, `name` | Non-authed |
| `discord_edit_role` | Edit an existing role (name, color, permissions, hoist, mentionable). | `guild_id`, `role_id` | Non-authed |
| `discord_delete_role` | Delete a role from a guild. | `guild_id`, `role_id` | Non-authed |
| `discord_add_role` | Assign a role to a member. | `guild_id`, `user_id`, `role_id` | Non-authed |
| `discord_remove_role` | Remove a role from a member. | `guild_id`, `user_id`, `role_id` | Non-authed |
| `discord_get_role_members` | List all members that have a specific role. | `guild_id`, `role_id` | Authed |
| `discord_set_role_position` | Change a role's position in the hierarchy. | `guild_id`, `role_id`, `position` | Non-authed |
| `discord_set_role_icon` | Set a custom icon or unicode emoji on a role (requires server boost level 2+). | `guild_id`, `role_id` | Non-authed |
| `discord_kick_member` | Kick a member from a guild. | `guild_id`, `user_id` | Non-authed |
| `discord_ban_member` | Ban a member from a guild. | `guild_id`, `user_id` | Non-authed |
| `discord_unban_member` | Unban a user from a guild. | `guild_id`, `user_id` | Non-authed |
| `discord_timeout_member` | Put a member in timeout (0 minutes to remove the timeout). | `guild_id`, `user_id`, `duration_minutes` | Non-authed |
| `discord_list_bans` | List all banned users in a guild. | `guild_id` | Non-authed |
| `discord_bulk_ban` | Ban multiple users at once (raid mitigation). | `guild_id`, `user_ids` | Non-authed |
| `discord_prune_members` | Remove inactive members. Use dry_run (default) to preview count first. | `guild_id`, `days` | Non-authed |
| `discord_create_webhook` | Create a webhook on a channel. | `channel_id`, `name` | Non-authed |
| `discord_send_webhook_message` | Send a message via a webhook using its ID and token. | `webhook_id`, `webhook_token` | Non-authed |
| `discord_edit_webhook` | Edit a webhook's name, avatar, or channel. | `webhook_id` | Non-authed |
| `discord_delete_webhook` | Delete a webhook. | `webhook_id` | Non-authed |
| `discord_list_webhooks` | List all webhooks for a channel or guild. Provide either channel_id or guild_id. |  | Non-authed |
| `discord_edit_webhook_message` | Edit a message previously sent by a webhook. | `webhook_id`, `webhook_token`, `message_id` | Non-authed |
| `discord_delete_webhook_message` | Delete a message sent by a webhook. | `webhook_id`, `webhook_token`, `message_id` | Non-authed |
| `discord_fetch_webhook_message` | Fetch a specific message sent by a webhook. | `webhook_id`, `webhook_token`, `message_id` | Non-authed |
| `discord_list_scheduled_events` | List all scheduled events in a guild. | `guild_id` | Authed |
| `discord_get_scheduled_event` | Get detailed info about a specific scheduled event. | `guild_id`, `event_id` | Authed |
| `discord_create_scheduled_event` | Create a scheduled event in a guild. Use entity_type 'VOICE' or 'STAGE_INSTANCE' with a channel_id, or 'EXTERNAL' with a location and scheduled_end_time. | `guild_id`, `name`, `entity_type`, `scheduled_start_time` | Non-authed |
| `discord_edit_scheduled_event` | Edit an existing scheduled event. Only provided fields are updated. | `guild_id`, `event_id` | Non-authed |
| `discord_delete_scheduled_event` | Delete a scheduled event. | `guild_id`, `event_id` | Non-authed |
| `discord_get_event_subscribers` | Get users who marked 'Interested' in a scheduled event. | `guild_id`, `event_id` | Authed |
| `discord_create_event_invite` | Create an invite URL linked to a scheduled event. | `guild_id`, `event_id` | Non-authed |
| `discord_list_invites` | List all active invites in a guild. | `guild_id` | Non-authed |
| `discord_get_invite` | Get details about a specific invite by its code. | `invite_code` | Non-authed |
| `discord_create_invite` | Create an invite link for a channel. | `channel_id` | Non-authed |
| `discord_delete_invite` | Delete (revoke) an invite by its code. | `invite_code` | Non-authed |
| `discord_list_channel_invites` | List all active invites for a specific channel. | `channel_id` | Non-authed |
| `discord_list_guilds` | List all Discord servers the bot is connected to. |  | Authed |
| `discord_get_guild_info` | Get detailed info about a guild: name, member count, channels, roles, boosts. | `guild_id` | Authed |
| `discord_get_server_stats` | Get server statistics: member count (humans vs bots), channels, roles, boost level. | `guild_id` | Authed |
| `discord_get_audit_log` | Fetch the guild audit log (who did what and when). | `guild_id` | Non-authed |
| `discord_get_membership_screening` | Get the current membership screening form (rules/questions new members must complete). | `guild_id` | Non-authed |
| `discord_update_membership_screening` | Update the membership screening form: set a description and rules/questions that new members must agree to before joining. | `guild_id` | Non-authed |
| `discord_send_dm` | Send a direct message to a user by their user ID. | `user_id`, `content` | Non-authed |
| `discord_read_dms` | Read the last N messages from a DM channel with a user. | `user_id` | Authed |
| `discord_list_dm_channels` | List all currently open/cached DM channels the bot has. |  | Authed |
| `discord_send_dm_embed` | Send a rich embed as a direct message to a user. | `user_id` | Non-authed |
| `discord_download_attachment` | Download a Discord attachment from a CDN URL to a local temp file. Only accepts URLs from cdn.discordapp.com or media.discordapp.net. | `url` | Authed |

## Authed vs non-authed by category

### Authed (read-only / safe with current permissions)

- `discord_read_messages` — Read the last N messages from a text channel. (required: `channel_id`)
- `discord_fetch_pinned_messages` — List all pinned messages in a channel. (required: `channel_id`)
- `discord_get_reactions` — List users who reacted with a specific emoji on a message. (required: `channel_id`, `message_id`, `emoji`)
- `discord_search_messages` — Search messages in a channel by keyword (scans up to last 100 messages). (required: `channel_id`, `keyword`)
- `discord_list_channels` — List all channels in a guild grouped by category. (required: `guild_id`)
- `discord_find_channel_by_name` — Find a channel by name in a guild (partial match supported). (required: `guild_id`, `name`)
- `discord_get_channel_permissions` — List all permission overwrites on a channel (per role and per member). (required: `channel_id`)
- `discord_audit_permissions` — Generate a full permission audit report for a guild: who can access what on every channel. (required: `guild_id`)
- `discord_get_forum_channels` — List all forum channels in a guild. (required: `guild_id`)
- `discord_list_forum_threads` — List all threads (active and archived) in a forum channel. (required: `forum_channel_id`)
- `discord_get_forum_post` — Get a forum post's details and its messages. (required: `thread_id`)
- `discord_get_forum_tags` — Get the available tags for a forum channel. (required: `forum_channel_id`)
- `discord_list_members` — List guild members with their roles. (required: `guild_id`)
- `discord_get_member_info` — Get detailed info about a member: roles, permissions, join date, timeout status. (required: `guild_id`, `user_id`)
- `discord_search_members` — Search guild members by username or nickname. (required: `guild_id`, `query`)
- `discord_list_roles` — List all roles in a guild with permissions and member count. (required: `guild_id`)
- `discord_get_role_members` — List all members that have a specific role. (required: `guild_id`, `role_id`)
- `discord_list_scheduled_events` — List all scheduled events in a guild. (required: `guild_id`)
- `discord_get_scheduled_event` — Get detailed info about a specific scheduled event. (required: `guild_id`, `event_id`)
- `discord_get_event_subscribers` — Get users who marked 'Interested' in a scheduled event. (required: `guild_id`, `event_id`)
- `discord_list_guilds` — List all Discord servers the bot is connected to. (required: )
- `discord_get_guild_info` — Get detailed info about a guild: name, member count, channels, roles, boosts. (required: `guild_id`)
- `discord_get_server_stats` — Get server statistics: member count (humans vs bots), channels, roles, boost level. (required: `guild_id`)
- `discord_read_dms` — Read the last N messages from a DM channel with a user. (required: `user_id`)
- `discord_list_dm_channels` — List all currently open/cached DM channels the bot has. (required: )
- `discord_download_attachment` — Download a Discord attachment from a CDN URL to a local temp file. Only accepts URLs from cdn.discordapp.com or media.discordapp.net. (required: `url`)

### Non-authed (require permission widening)

- `discord_send_message` — Send a plain text message to a channel. (required: `channel_id`, `content`)
- `discord_reply_message` — Reply to a specific message in a channel. (required: `channel_id`, `message_id`, `content`)
- `discord_send_embed` — Send a rich embed message with title, description, color, fields, footer, image, thumbnail, author, URL, and timestamp. (required: `channel_id`)
- `discord_send_multiple_embeds` — Send up to 10 embeds in a single message. (required: `channel_id`, `embeds`)
- `discord_edit_message` — Edit a message sent by the bot. (required: `channel_id`, `message_id`, `content`)
- `discord_delete_message` — Delete a specific message from a channel. (required: `channel_id`, `message_id`)
- `discord_bulk_delete_messages` — Delete multiple messages at once (2-100, messages must be less than 14 days old). (required: `channel_id`, `count`)
- `discord_pin_message` — Pin or unpin a message in a channel. (required: `channel_id`, `message_id`, `pin`)
- `discord_add_reaction` — Add a reaction emoji to a message. (required: `channel_id`, `message_id`, `emoji`)
- `discord_remove_reactions` — Remove reactions from a message. No emoji = remove all. Emoji only = remove that emoji. Emoji + user_id = remove that user's reaction. (required: `channel_id`, `message_id`)
- `discord_forward_message` — Forward a message to another channel. (required: `channel_id`, `message_id`, `target_channel_id`)
- `discord_crosspost_message` — Publish a message in an announcement channel to all following channels. (required: `channel_id`, `message_id`)
- `discord_edit_embed` — Edit an embed message previously sent by the bot. Only provided fields are updated; omitted fields are removed. (required: `channel_id`, `message_id`)
- `discord_create_channel` — Create a text, voice channel or category in a guild. (required: `guild_id`, `name`)
- `discord_edit_channel` — Edit a channel's name, topic, slowmode, or NSFW flag. (required: `channel_id`)
- `discord_delete_channel` — Delete a channel. (required: `channel_id`)
- `discord_move_channel` — Move a channel into a category (or remove from category if category_id is omitted). (required: `channel_id`)
- `discord_clone_channel` — Clone a channel with its name, topic and permission overwrites. (required: `channel_id`)
- `discord_set_channel_position` — Set the display position of a channel within its category. (required: `channel_id`, `position`)
- `discord_follow_announcement_channel` — Follow an announcement channel so its messages are published to a target channel. (required: `source_channel_id`, `target_channel_id`)
- `discord_set_role_permission` — Allow or deny specific permissions for a role on a channel. (required: `channel_id`, `role_id`)
- `discord_set_member_permission` — Allow or deny specific permissions for a single member on a channel. (required: `channel_id`, `user_id`)
- `discord_lock_channel_permissions` — Sync a channel's permissions with its parent category. (required: `channel_id`)
- `discord_reset_channel_permissions` — Remove ALL permission overwrites on a channel (reset to inherited). (required: `channel_id`)
- `discord_copy_permissions` — Copy all permission overwrites from one channel to another. (required: `source_channel_id`, `target_channel_id`)
- `discord_create_forum_channel` — Create a new forum channel in a guild. (required: `guild_id`, `name`)
- `discord_create_forum_post` — Create a new post (thread) in a forum channel. (required: `forum_channel_id`, `title`, `content`)
- `discord_reply_to_forum` — Reply to a forum post (send a message in a forum thread). (required: `thread_id`, `content`)
- `discord_delete_forum_post` — Delete (close) a forum post/thread. (required: `thread_id`)
- `discord_set_forum_tags` — Set or update the available tags on a forum channel. (required: `forum_channel_id`, `tags`)
- `discord_update_forum_post` — Update a forum post's title, archived/locked status, or applied tags. (required: `thread_id`)
- `discord_set_nickname` — Set or clear a member's nickname. (required: `guild_id`, `user_id`, `nickname`)
- `discord_create_role` — Create a new role in a guild. (required: `guild_id`, `name`)
- `discord_edit_role` — Edit an existing role (name, color, permissions, hoist, mentionable). (required: `guild_id`, `role_id`)
- `discord_delete_role` — Delete a role from a guild. (required: `guild_id`, `role_id`)
- `discord_add_role` — Assign a role to a member. (required: `guild_id`, `user_id`, `role_id`)
- `discord_remove_role` — Remove a role from a member. (required: `guild_id`, `user_id`, `role_id`)
- `discord_set_role_position` — Change a role's position in the hierarchy. (required: `guild_id`, `role_id`, `position`)
- `discord_set_role_icon` — Set a custom icon or unicode emoji on a role (requires server boost level 2+). (required: `guild_id`, `role_id`)
- `discord_kick_member` — Kick a member from a guild. (required: `guild_id`, `user_id`)
- `discord_ban_member` — Ban a member from a guild. (required: `guild_id`, `user_id`)
- `discord_unban_member` — Unban a user from a guild. (required: `guild_id`, `user_id`)
- `discord_timeout_member` — Put a member in timeout (0 minutes to remove the timeout). (required: `guild_id`, `user_id`, `duration_minutes`)
- `discord_list_bans` — List all banned users in a guild. (required: `guild_id`)
- `discord_bulk_ban` — Ban multiple users at once (raid mitigation). (required: `guild_id`, `user_ids`)
- `discord_prune_members` — Remove inactive members. Use dry_run (default) to preview count first. (required: `guild_id`, `days`)
- `discord_create_webhook` — Create a webhook on a channel. (required: `channel_id`, `name`)
- `discord_send_webhook_message` — Send a message via a webhook using its ID and token. (required: `webhook_id`, `webhook_token`)
- `discord_edit_webhook` — Edit a webhook's name, avatar, or channel. (required: `webhook_id`)
- `discord_delete_webhook` — Delete a webhook. (required: `webhook_id`)
- `discord_list_webhooks` — List all webhooks for a channel or guild. Provide either channel_id or guild_id. (required: )
- `discord_edit_webhook_message` — Edit a message previously sent by a webhook. (required: `webhook_id`, `webhook_token`, `message_id`)
- `discord_delete_webhook_message` — Delete a message sent by a webhook. (required: `webhook_id`, `webhook_token`, `message_id`)
- `discord_fetch_webhook_message` — Fetch a specific message sent by a webhook. (required: `webhook_id`, `webhook_token`, `message_id`)
- `discord_create_scheduled_event` — Create a scheduled event in a guild. Use entity_type 'VOICE' or 'STAGE_INSTANCE' with a channel_id, or 'EXTERNAL' with a location and scheduled_end_time. (required: `guild_id`, `name`, `entity_type`, `scheduled_start_time`)
- `discord_edit_scheduled_event` — Edit an existing scheduled event. Only provided fields are updated. (required: `guild_id`, `event_id`)
- `discord_delete_scheduled_event` — Delete a scheduled event. (required: `guild_id`, `event_id`)
- `discord_create_event_invite` — Create an invite URL linked to a scheduled event. (required: `guild_id`, `event_id`)
- `discord_list_invites` — List all active invites in a guild. (required: `guild_id`)
- `discord_get_invite` — Get details about a specific invite by its code. (required: `invite_code`)
- `discord_create_invite` — Create an invite link for a channel. (required: `channel_id`)
- `discord_delete_invite` — Delete (revoke) an invite by its code. (required: `invite_code`)
- `discord_list_channel_invites` — List all active invites for a specific channel. (required: `channel_id`)
- `discord_get_audit_log` — Fetch the guild audit log (who did what and when). (required: `guild_id`)
- `discord_get_membership_screening` — Get the current membership screening form (rules/questions new members must complete). (required: `guild_id`)
- `discord_update_membership_screening` — Update the membership screening form: set a description and rules/questions that new members must agree to before joining. (required: `guild_id`)
- `discord_send_dm` — Send a direct message to a user by their user ID. (required: `user_id`, `content`)
- `discord_send_dm_embed` — Send a rich embed as a direct message to a user. (required: `user_id`)
