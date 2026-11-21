param([string]$Source, [string]$Out)
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$W = 1024.0
$cx = 512.0; $cy = 430.0

$fs = [IO.File]::OpenRead($Source)
$dec = [Windows.Media.Imaging.BitmapDecoder]::Create($fs, 'PreservePixelFormat', 'OnLoad')
$frame = $dec.Frames[0]; $fs.Close()
$side = [Math]::Min($frame.PixelWidth, $frame.PixelHeight)
$x0 = [int](($frame.PixelWidth - $side) / 2)
$crop = New-Object Windows.Media.Imaging.CroppedBitmap($frame, (New-Object Windows.Int32Rect($x0, 0, $side, $side)))

function C([byte]$a, [byte]$r, [byte]$g, [byte]$b) { [Windows.Media.Color]::FromArgb($a, $r, $g, $b) }
function Radial($stops) {
    $br = New-Object Windows.Media.RadialGradientBrush
    $br.MappingMode = 'Absolute'; $br.Center = New-Object Windows.Point($cx, $cy); $br.GradientOrigin = $br.Center
    $br.RadiusX = 380; $br.RadiusY = 380
    foreach ($s in $stops) { $br.GradientStops.Add((New-Object Windows.Media.GradientStop($s[0], $s[1]))) }
    $br
}

$dv = New-Object Windows.Media.DrawingVisual
$dc = $dv.RenderOpen()
$full = New-Object Windows.Rect(0, 0, $W, $W)

# 1. The city at night.
$dc.DrawImage($crop, $full)

# 2. Darken everything outside the light (the light pool stays clear).
$dc.DrawRectangle((Radial @(
    @((C 0 0 0 0), 0.0), @((C 0 0 0 0), 0.45), @((C 175 4 6 14), 0.85), @((C 215 2 3 10), 1.0))), $null, $full)

# 3. Warm cursor light: soft core fading out.
$dc.DrawRectangle((Radial @(
    @((C 150 255 236 200), 0.0), @((C 95 255 214 160), 0.22), @((C 40 255 190 120), 0.5), @((C 0 255 180 110), 0.75))), $null, $full)

# 4. Mouse cursor arrow at the centre of the light.
$arrow = [Windows.Media.Geometry]::Parse('M 0,0 L 0,92 L 22,71 L 37,106 L 52,100 L 37,65 L 66,65 Z')
$dc.PushTransform([Windows.Media.TranslateTransform]::new($cx - 14, $cy - 30))
$shadow = $arrow.Clone(); $shadow.Transform = New-Object Windows.Media.TranslateTransform(5, 6)
$dc.DrawGeometry((New-Object Windows.Media.SolidColorBrush((C 120 0 0 0))), $null, $shadow)
$pen = New-Object Windows.Media.Pen((New-Object Windows.Media.SolidColorBrush((C 255 20 20 28))), 5); $pen.LineJoin = 'Round'
$dc.DrawGeometry([Windows.Media.Brushes]::White, $pen, $arrow)
$dc.Pop()

# 5. Title band at the bottom.
$band = New-Object Windows.Media.LinearGradientBrush((C 0 0 0 0), (C 235 0 0 6), 90)
$dc.DrawRectangle($band, $null, (New-Object Windows.Rect(0, 700, $W, 324)))

$culture = [Globalization.CultureInfo]::InvariantCulture
function Text($s, $size, $weight, $brush) {
    $tf = New-Object Windows.Media.Typeface((New-Object Windows.Media.FontFamily('Segoe UI')), 'Normal', $weight, 'Normal')
    New-Object Windows.Media.FormattedText($s, $culture, 'LeftToRight', $tf, $size, $brush, 1.0)
}
$warm = New-Object Windows.Media.SolidColorBrush((C 255 255 214 150))
$title = Text 'Better Cursor Light' 104 'Bold' ([Windows.Media.Brushes]::White)
$sub = Text 'Light up the shadows, day and night' 44 'SemiBold' $warm
$dc.DrawText((Text 'Better Cursor Light' 104 'Bold' (New-Object Windows.Media.SolidColorBrush((C 160 0 0 0)))), (New-Object Windows.Point((($W - $title.Width) / 2 + 4), 798)))
$dc.DrawText($title, (New-Object Windows.Point((($W - $title.Width) / 2), 794)))
$dc.DrawText($sub, (New-Object Windows.Point((($W - $sub.Width) / 2), 932)))
$dc.Close()

$rtb = New-Object Windows.Media.Imaging.RenderTargetBitmap(1024, 1024, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
$rtb.Render($dv)
$enc = New-Object Windows.Media.Imaging.PngBitmapEncoder
$enc.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($rtb))
$o = [IO.File]::Create($Out); $enc.Save($o); $o.Close()
"saved $Out"
