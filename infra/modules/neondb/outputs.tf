output "connection_uri" {
  description = "PostgreSQL connection string"
  value       = "postgres://neondb_owner:${urlencode(var.neondb_owner_password)}@${local.endpoint_host}/${neon_database.this.name}?sslmode=require"
  sensitive   = true
}

output "project_id" {
  description = "Neon project ID"
  value       = local.project_id
}

output "branch_id" {
  description = "Neon branch ID"
  value       = local.branch_id
}
