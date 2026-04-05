output "connection_uri" {
  description = "PostgreSQL connection string"
  value       = "postgres://${neon_role.this.name}:${neon_role.this.password}@${neon_endpoint.this.host}/${neon_database.this.name}?sslmode=require"
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
