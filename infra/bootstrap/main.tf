terraform {
  required_version = ">= 1.5"

  backend "gcs" {
    bucket = "mental-metal-tf-state"
    prefix = "bootstrap"
  }

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 6.0"
    }
  }
}

locals {
  project_id = "mental-metal"
}

resource "google_project" "this" {
  name            = "Mental Metal"
  project_id      = local.project_id
  org_id          = var.org_id
  billing_account = var.billing_account
}

resource "google_project_service" "apis" {
  for_each = toset([
    "run.googleapis.com",
    "artifactregistry.googleapis.com",
    "secretmanager.googleapis.com",
    "iam.googleapis.com",
    "cloudresourcemanager.googleapis.com",
  ])

  project = google_project.this.project_id
  service = each.value

  disable_dependent_services = false
  disable_on_destroy         = false
}

resource "google_storage_bucket" "tf_state" {
  name     = "mental-metal-tf-state"
  project  = google_project.this.project_id
  location = var.region

  versioning {
    enabled = true
  }

  uniform_bucket_level_access = true

  depends_on = [google_project_service.apis]
}

# Grant GitHub Actions service account access to the TF state bucket
resource "google_storage_bucket_iam_member" "tf_state_admin" {
  bucket = google_storage_bucket.tf_state.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${module.workload_identity.service_account_email}"
}

# --- Workload Identity Federation ---
# Must be bootstrapped locally so GitHub Actions can authenticate to GCP

module "workload_identity" {
  source = "../modules/workload-identity"

  project_id  = google_project.this.project_id
  github_repo = "garethbaumgart/mental-metal"

  depends_on = [google_project_service.apis]
}
