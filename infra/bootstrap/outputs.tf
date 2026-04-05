output "project_id" {
  value = google_project.this.project_id
}

output "state_bucket" {
  value = google_storage_bucket.tf_state.name
}

output "workload_identity_provider" {
  value = module.workload_identity.workload_identity_provider
}

output "service_account_email" {
  value = module.workload_identity.service_account_email
}
