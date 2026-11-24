param([string]$KMSKeyAlias, [string]$plainText)
$ErrorActionPreference = "Stop"
[byte[]]$byteArray = [System.Text.Encoding]::UTF8.GetBytes($plainText)
$memoryStream = New-Object System.IO.MemoryStream($byteArray,0,$byteArray.Length)
$encryptedMemoryStream = Invoke-KMSEncrypt -Region "eu-west-1" -KeyId "alias/$KMSKeyAlias" -Plaintext $memoryStream
$base64encrypted = [System.Convert]::ToBase64String($encryptedMemoryStream.CiphertextBlob.ToArray())
Write-Output $base64encrypted