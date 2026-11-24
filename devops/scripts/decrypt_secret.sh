#!/usr/bin/env bash

set -e
aws kms decrypt --key-id "alias/$1" --ciphertext-blob ''$2'' --output text --query Plaintext | base64 --decode




