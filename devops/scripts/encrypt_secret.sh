#!/usr/bin/env bash

set -e
aws kms encrypt --key-id "alias/$1" --plaintext $(echo -n ''$2'' | base64) --output text --query CiphertextBlob