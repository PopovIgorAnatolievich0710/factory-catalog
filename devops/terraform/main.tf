terraform {
  required_version = ">=0.14.7"
  required_providers {
    aws = {
      version = "5.7.0"
    }
    kubernetes = {
      version = "2.21.1"
    }
    helm = {
      version = "2.10.1"
    }
    local = {
      version = "2.4.0"
    }
    null = {
      version = "3.2.1"
    }
  }
}
