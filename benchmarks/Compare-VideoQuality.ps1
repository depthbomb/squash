param(
    [Parameter(Mandatory)]
    [string] $Reference,

    [Parameter(Mandatory)]
    [string] $Baseline,

    [Parameter(Mandatory)]
    [string] $Candidate,

    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source
$ffprobe = (Get-Command ffprobe -ErrorAction Stop).Source
$resolvedReference = (Resolve-Path -LiteralPath $Reference).Path
$resolvedBaseline = (Resolve-Path -LiteralPath $Baseline).Path
$resolvedCandidate = (Resolve-Path -LiteralPath $Candidate).Path

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$resolvedOutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

function Get-MediaRecord {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $probeJson = & $ffprobe `
        -v error `
        -show_entries 'format=duration,size,bit_rate,format_name:stream=index,codec_type,codec_name,profile,width,height,pix_fmt,avg_frame_rate,duration,bit_rate,channels,channel_layout,color_range,color_space,color_transfer,color_primaries,chroma_location' `
        -of json `
        $Path
    if ($LASTEXITCODE -ne 0) {
        throw "FFprobe failed for $Path."
    }

    return [ordered]@{
        path = $Path
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
        bytes = (Get-Item -LiteralPath $Path).Length
        probe = $probeJson | ConvertFrom-Json
    }
}

function Measure-Quality {
    param(
        [Parameter(Mandatory)]
        [string] $Label,

        [Parameter(Mandatory)]
        [string] $DistortedPath
    )

    $logName = "$Label-vmaf.json"
    $filter = "[0:v]setpts=PTS-STARTPTS[dist];[1:v]setpts=PTS-STARTPTS[ref];[dist][ref]libvmaf=log_fmt=json:log_path=${logName}:feature=name=psnr|name=float_ssim|name=float_ms_ssim|name=ciede|name=cambi"

    Push-Location $resolvedOutputDirectory
    try {
        & $ffmpeg `
            -hide_banner `
            -loglevel error `
            -i $DistortedPath `
            -i $resolvedReference `
            -lavfi $filter `
            -an `
            -f null `
            NUL
        if ($LASTEXITCODE -ne 0) {
            throw "FFmpeg quality measurement failed for $Label."
        }
    }
    finally {
        Pop-Location
    }

    $logPath = Join-Path $resolvedOutputDirectory $logName
    $metrics = Get-Content -Raw -LiteralPath $logPath | ConvertFrom-Json

    return [ordered]@{
        media = Get-MediaRecord -Path $DistortedPath
        pooled_metrics = $metrics.pooled_metrics
        frames = $metrics.frames.Count
        metric_log = $logPath
    }
}

$ffmpegVersion = (& $ffmpeg -hide_banner -version | Select-Object -First 1)
$summary = [ordered]@{
    created_at = (Get-Date).ToString('o')
    ffmpeg = $ffmpegVersion
    reference = Get-MediaRecord -Path $resolvedReference
    baseline = Measure-Quality -Label 'baseline' -DistortedPath $resolvedBaseline
    candidate = Measure-Quality -Label 'candidate' -DistortedPath $resolvedCandidate
}
$summaryPath = Join-Path $resolvedOutputDirectory 'comparison.json'

$summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $summaryPath -Encoding utf8

Write-Output $summaryPath
