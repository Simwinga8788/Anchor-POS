Add-Type -AssemblyName System.Drawing

$src = "$PSScriptRoot\icon.png"
$dest = "$PSScriptRoot\icon.ico"

if (Test-Path $src) {
    Write-Host "Converting $src to $dest..."
    $bitmap = [System.Drawing.Bitmap]::FromFile($src)
    # Resize to standard icon sizes if needed, but Hicon method works for simple cases
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    
    $file = New-Object System.IO.FileStream($dest, [System.IO.FileMode]::Create)
    $icon.Save($file)
    $file.Close()
    
    $bitmap.Dispose()
    $icon.Dispose()
    Write-Host "Conversion complete."
}
else {
    Write-Error "Source file $src not found!"
}
