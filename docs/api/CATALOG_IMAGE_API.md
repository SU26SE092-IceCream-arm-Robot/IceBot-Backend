# Catalog Image API

This document owns catalog-image upload, replacement, removal, and delivery contracts for management callers and customer runtime reads.

## Search Keywords

`catalog image`, `product image`, `product variant image`, `multipart upload`, `Cloudinary`, `Idempotency-Key`, `expectedRevision`, `If-Match`

## Management Routes

```text
PUT    /api/v1/management/product-templates/{productId}/image
DELETE /api/v1/management/product-templates/{productId}/image
PUT    /api/v1/management/product-templates/{productId}/variants/{variantId}/image
DELETE /api/v1/management/product-templates/{productId}/variants/{variantId}/image

PUT    /api/v1/management/organizations/{organizationId}/products/{productId}/image
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/image
PUT    /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/image
DELETE /api/v1/management/organizations/{organizationId}/products/{productId}/variants/{variantId}/image
```

`PUT` is both initial upload and replacement. It uses `multipart/form-data` with `file`, `expectedRevision`, and optional `altText`. `DELETE` requires an `If-Match` revision. Every mutation requires a caller-generated `Idempotency-Key` header.

For a repeated key with the same normalized operation, owner, expected revision, and file fingerprint, the API performs no additional provider upload or deletion and returns the owner's current authoritative projection. It does not retain a historical response snapshot. Reusing a key with a changed operation or payload returns `409`. A stale owner revision returns `409` before external storage is called.

## Image Validation And Delivery

The backend accepts configured image MIME types, verifies image signatures and header dimensions, and rejects files outside the configured byte and dimension bounds. It creates public delivery URLs only after Cloudinary has accepted the upload.

Management and runtime responses expose only purpose-specific card/detail delivery URLs and alt text. They never expose Cloudinary credentials, provider public IDs, provider asset IDs, provider version identifiers, or cleanup state to customer runtime callers.

`RootFolder` is the complete generated path prefix. For example, `icebot/production` produces `icebot/production/organizations/{organizationId}/products/{productId}/{assetId}`; callers must not append a separate environment segment.

## Cleanup

Replacing or removing an image releases the old asset and creates a durable cleanup task. A background worker rechecks active product, variant, and production-package snapshot references before it deletes the provider asset. Provider failure is retried with bounded backoff and does not roll back the authoritative catalog mutation.

## Related Docs

- [Management API Surface](MANAGEMENT_API_SURFACE.md)
- [Catalog Runtime Menu Flow](../flows/CATALOG_RUNTIME_MENU_FLOW.md)
- [Deployment Configuration](../operations/DEPLOYMENT_CONFIG.md)
