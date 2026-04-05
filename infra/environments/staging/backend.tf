terraform {
  backend "gcs" {
    bucket = "mental-metal-tf-state"
    prefix = "staging"
  }
}
