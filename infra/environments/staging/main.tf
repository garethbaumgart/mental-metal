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

  neon_org_id     = var.neon_org_id
  neon_project_id = var.neon_project_id
  neon_branch_id  = var.neon_branch_id
  database_name   = "mentalmetalstaging"
  role_name       = "mentalmetalstaging"
}

# --- Secret Manager ---

module "secrets" {
  source = "../../modules/secret-manager"

  project_id               = var.project_id
  region                   = var.region
  secret_names             = ["STAGING_DATABASE_URL"]
  accessor_service_account = google_service_account.cloud_run.email
}

# --- Cloud Run ---

module "cloud_run" {
  source = "../../modules/cloud-run"

  project_id              = var.project_id
  region                  = var.region
  service_name            = "mental-metal-staging"
  image                   = var.image
  secret_ids              = { "DATABASE_URL" = "STAGING_DATABASE_URL" }
  runtime_service_account = google_service_account.cloud_run.email
  allow_public_access     = true
}
