param (
    [string]$PathToExe
)

$dlStorageRoot = "$env:USERPROFILE\.dlaas"
[Environment]::SetEnvironmentVariable("DL_STORAGE_ROOT", $dlStorageRoot, "Machine")

$chiaRoot = "$env:USERPROFILE\.chia\mainnet"
if ([string]::IsNullOrEmpty($env:CHIA_ROOT)) {
    [Environment]::SetEnvironmentVariable("CHIA_ROOT", $chiaRoot, "Machine")
}

& "$PathToExe" install

