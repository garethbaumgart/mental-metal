terraform {
  required_version = ">= 1.5"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 6.0"
    }
  }
}

resource "google_secret_manager_secret" "this" {
  for_each = toset(var.secret_names)

  project   = var.project_id
  secret_id = each.value

  replication {
    user_managed {
      replicas {
        location = var.region
      }
    }
  }

  # Secret values are managed out-of-band (via CI/CD or gcloud) to avoid
  # storing sensitive data in Terraform state.
  lifecycle {
    ignore_changes = [labels]
  }
}

resource "google_secret_manager_secret_iam_member" "accessor" {
  for_each = toset(var.secret_names)

  project   = var.project_id
  secret_id = google_secret_manager_secret.this[each.value].secret_id
  role      = "roles/secretmanager.secretAccessor"
  member    = "serviceAccount:${var.accessor_service_account}"
}
