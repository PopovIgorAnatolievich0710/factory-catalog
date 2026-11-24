{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
!!! You SHOULD replace 'scaffolding' with your own unique namespace
*/}}
{{- define "scaffolding.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
!!! You SHOULD replace 'scaffolding' with your own unique namespace
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
*/}}
{{- define "scaffolding.fullname" -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
ADD FQN for each component
*/}}

{{/*
Create a fully qualified Server name.
!!! You SHOULD replace 'scaffolding' with your own unique namespace
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
*/}}
{{/*
{{- define "scaffolding.server.fullname" -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- printf "%s-%s-%s" .Release.Name $name .Values.server.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
*/}}
