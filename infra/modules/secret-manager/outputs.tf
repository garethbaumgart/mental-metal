output "secret_ids" {
  description = "Map of secret names to their Secret Manager secret IDs"
  value       = { for k, v in google_secret_manager_secret.this : k => v.secret_id }
}
