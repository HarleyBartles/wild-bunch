# Attachments, images, and docs help

Use this when the task is about files attached to Linear or helper surfaces around docs and images.

## Tools

| Tool | Use when | Required params | Optional params |
| --- | --- | --- | --- |
| `prepare_attachment_upload` | Prepare a signed upload for an existing issue. | `issue`, `filename`, `contentType`, `size` | `subtitle`, `title` |
| `create_attachment_from_upload` | Finalize an upload after the raw bytes have been PUT to the signed URL. | `assetUrl`, `issue` | `subtitle`, `title` |
| `create_attachment` | Tiny-file fallback when direct upload is not practical. | `base64Content`, `contentType`, `filename`, `issue` | `subtitle`, `title` |
| `get_attachment` | Read an attachment by ID. | `id` | None |
| `delete_attachment` | Remove an attachment by ID. | `id` | None |
| `extract_images` | Pull image references out of markdown content. | `markdown` | None |
| `search_documentation` | Search Linear documentation for feature or usage guidance. | `query` | `page` |

## Upload flow

1. Call `prepare_attachment_upload`.
2. PUT the raw bytes to the signed upload URL.
3. Call `create_attachment_from_upload`.

## Notes

- `create_attachment` is the deprecated fallback; prefer the prepare/PUT/finalize flow.
- `search_documentation` is a helper surface when the agent needs Linear docs, not a data mutation.
