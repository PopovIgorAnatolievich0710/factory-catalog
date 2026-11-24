param([string]$KMSKeyAlias, [string]$encryptedSecret)
$ErrorActionPreference = "Stop"
$encryptedBytes = [System.Convert]::FromBase64String($encryptedSecret)
$encryptedMemoryStreamToDecrypt = New-Object System.IO.MemoryStream($encryptedBytes,0,$encryptedBytes.Length)
$decryptedMemoryStream = Invoke-KMSDecrypt -Region "eu-west-1" -KeyId "alias/$KMSKeyAlias" -CiphertextBlob $encryptedMemoryStreamToDecrypt
$decrypt_plaintext = [System.Text.Encoding]::UTF8.GetString($decryptedMemoryStream.Plaintext.ToArray())
Write-Output $decrypt_plaintext
