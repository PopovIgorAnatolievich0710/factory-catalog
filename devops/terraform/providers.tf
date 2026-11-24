provider "aws" {
  region = "eu-west-1"
}

provider "kubernetes" {}

provider "helm" {
  kubernetes {}
}
