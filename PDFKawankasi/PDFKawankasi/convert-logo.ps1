# PowerShell script to convert SVG logo to PNG files for app icons
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$svgPath = Join-Path $PSScriptRoot "Assets\PDF Editor\Kawankasi\PDF Kawankasi logo.svg"
$assetsPath = Join-Path $PSScriptRoot "Assets"

# Define the sizes needed for the app
$sizes = @(
    @{Name="Square44x44Logo"; Size=44},
    @{Name="Square71x71Logo"; Size=71},
    @{Name="Square150x150Logo"; Size=150},
    @{Name="Wide310x150Logo"; Width=310; Height=150},
    @{Name="Square310x310Logo"; Size=310},
    @{Name="StoreLogo"; Size=50},
    @{Name="SplashScreen"; Size=620},
    @{Name="icon"; Size=256}  # For title bar - will be scaled down
)

Write-Host "Converting SVG logo to PNG files..." -ForegroundColor Cyan

foreach ($icon in $sizes) {
    $width = if ($icon.Width) { $icon.Width } else { $icon.Size }
    $height = if ($icon.Height) { $icon.Height } else { $icon.Size }
    $outputPath = Join-Path $assetsPath "$($icon.Name).png"
    
    try {
        # Load SVG using SharpVectors (since it's in the project dependencies)
        $uri = [System.Uri]::new($svgPath)
        $svgDocument = [SharpVectors.Converters.SvgImageExtension]::LoadDocument($uri)
        
        # Create render target
        $renderTarget = New-Object System.Windows.Media.Imaging.RenderTargetBitmap(
            $width, $height, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
        
        # Create drawing visual
        $drawingVisual = New-Object System.Windows.Media.DrawingVisual
        $drawingContext = $drawingVisual.RenderOpen()
        
        # Render SVG
        $svgRenderer = New-Object SharpVectors.Renderers.Wpf.WpfDrawingRenderer
        $svgRenderer.Window = $svgDocument
        $drawing = $svgRenderer.Drawing
        $drawingContext.DrawDrawing($drawing)
        $drawingContext.Close()
        
        # Render to bitmap
        $renderTarget.Render($drawingVisual)
        
        # Save as PNG
        $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
        $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($renderTarget))
        
        $fileStream = [System.IO.File]::Create($outputPath)
        $encoder.Save($fileStream)
        $fileStream.Close()
        
        Write-Host "✓ Created: $($icon.Name).png ($width x $height)" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Failed to create $($icon.Name).png: $_" -ForegroundColor Red
    }
}

Write-Host "`nConversion complete!" -ForegroundColor Cyan
