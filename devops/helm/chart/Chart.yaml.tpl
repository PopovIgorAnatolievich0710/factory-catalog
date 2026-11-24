apiVersion: v2
description: A Helm chart for Kubernetes
name: "${project_name}"
version: 1.0.0
dependencies:
  - name: postgresql
    version: 12.11.2
    condition: postgresql.enabled
    #repository: https://charts.bitnami.com/bitnami
    #fix for https://github.com/bitnami/charts/issues/10833
    repository: "file://./dependencies/postgresql"
