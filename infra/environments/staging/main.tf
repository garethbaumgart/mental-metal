terraform {
  required_version = ">= 1.5"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 6.0"
    }
    neon = {
      source  = "kislerdm/neon"
      version = "~> 0.13"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

provider "neon" {}

# --- Runtime Service Account ---

resource "google_service_account" "cloud_run" {
  project      = var.project_id
  account_id   = "mental-metal-staging-run"
  display_name = "Mental Metal Staging Cloud Run"
}

# --- Artifact Registry ---

module "artifact_registry" {
  source = "../../modules/artifact-registry"

  project_id    = var.project_id
  region        = var.region
  repository_id = "mental-metal"
}

# --- NeonDB ---

module "neondb" {
  source = "../../modules/neondb"

  neon_org_id            = var.neon_org_id
  neon_project_id        = var.neon_project_id
  neon_branch_id         = var.neon_branch_id
  neon_endpoint_host     = var.neon_endpoint_host
  database_name          = "mentalmetalstaging"
  neondb_owner_password  = var.neondb_owner_password
}

# --- Secret Manager ---

module "secrets" {
  source = "../../modules/secret-manager"

  project_id               = var.project_id
  region                   = var.region
  secret_names             = ["STAGING_DATABASE_URL", "STAGING_JWT_SECRET"]
  accessor_service_account = google_service_account.cloud_run.email
}

# --- DataProtection Keys Bucket ---
# Persists ASP.NET Core DataProtection keys outside the Cloud Run container so
# OAuth state cookies issued by one container instance can be validated by
# another after a cold start (#75 Bug 4).

resource "google_storage_bucket" "data_protection_keys" {
  project                     = var.project_id
  name                        = "${var.project_id}-mental-metal-staging-dp-keys"
  location                    = var.region
  force_destroy               = false
  uniform_bucket_level_access = true
  public_access_prevention    = "enforced"

  versioning {
    enabled = true
  }
}

# Least-privilege access: the repository needs to list + read existing key
# objects and create new ones on key rotation. It never needs to delete keys
# (stale keys are harmless and can be pruned administratively), so we split
# the grants rather than using the broader roles/storage.objectAdmin.
resource "google_storage_bucket_iam_member" "data_protection_keys_viewer" {
  bucket = google_storage_bucket.data_protection_keys.name
  role   = "roles/storage.objectViewer"
  member = "serviceAccount:${google_service_account.cloud_run.email}"
}

resource "google_storage_bucket_iam_member" "data_protection_keys_creator" {
  bucket = google_storage_bucket.data_protection_keys.name
  role   = "roles/storage.objectCreator"
  member = "serviceAccount:${google_service_account.cloud_run.email}"
}

# --- Cloud Run ---

module "cloud_run" {
  source = "../../modules/cloud-run"

  project_id              = var.project_id
  region                  = var.region
  service_name            = "mental-metal-staging"
  image                   = var.image
  secret_ids              = {
    "DATABASE_URL" = "STAGING_DATABASE_URL"
    "Jwt__Secret"  = "STAGING_JWT_SECRET"
  }
  env_vars = {
    "DataProtection__BucketName" = google_storage_bucket.data_protection_keys.name
  }
  runtime_service_account = google_service_account.cloud_run.email
  allow_public_access     = true
}
