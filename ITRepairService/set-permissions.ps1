$path = 'C:\inetpub1\IT_Service'

# Create logs directory if not exists
if (-not (Test-Path "$path\logs")) {
    New-Item -Path "$path\logs" -ItemType Directory -Force | Out-Null
    Write-Host "Logs directory created"
}

# Set IIS permissions
$acl = Get-Acl $path

$rule1 = New-Object System.Security.AccessControl.FileSystemAccessRule('IIS_IUSRS', 'Read,Write,Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
$acl.SetAccessRule($rule1)

$rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule('NETWORK SERVICE', 'Read,Write,Modify', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
$acl.SetAccessRule($rule2)

Set-Acl -Path $path -AclObject $acl

Write-Host "IIS permissions set successfully for $path"