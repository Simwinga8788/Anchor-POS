Add-Type -AssemblyName System.Drawing

$src = "$PSScriptRoot\icon_v2.png"
$dest = "$PSScriptRoot\icon.ico"

if (Test-Path $src) {
    Write-Host "Converting $src to $dest..."
    
    # Load Image
    $img = [System.Drawing.Image]::FromFile($src)
    
    # Resize to 256x256 (Vista/Win10 standard)
    $bmp = New-Object System.Drawing.Bitmap($img, 256, 256)
    
    # Get HIcon
    $hIcon = $bmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    
    # Save
    $fileStream = new-object System.IO.FileStream($dest, [System.IO.FileMode]::Create)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    # Cleanup
    [System.Runtime.InteropServices.Marshal]::Exclude($icon) # Handle cleanup if needed, but Dispose works
    $icon.Dispose()
    $bmp.Dispose()
    $img.Dispose()
    
    Write-Host "Conversion complete. Created icon.ico"
}
else {
    Write-Error "Source file $src not found!"
}
