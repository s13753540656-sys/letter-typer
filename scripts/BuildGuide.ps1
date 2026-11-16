param([Parameter(Mandatory=$true)][string]$Root)
$ErrorActionPreference = 'Stop'
$rootPath = (Resolve-Path -LiteralPath $Root).Path
$markdown = [IO.File]::ReadAllText((Join-Path $rootPath 'docs\USAGE.md'), [Text.Encoding]::UTF8)
$lines = $markdown.Replace("`r`n", "`n").Split([char]10)
function Convert-Inline([string]$Value) {
    $value = [Net.WebUtility]::HtmlEncode($Value)
    $value = [regex]::Replace($value, '!\[([^\]]*)\]\(([^)]+)\)', {
        param($match)
        $target = $match.Groups[2].Value
        if ($target -notmatch '^https?://') { $target = 'docs/' + $target }
        return '<img src="' + $target + '" alt="' + $match.Groups[1].Value + '" loading="lazy">'
    })
    $value = [regex]::Replace($value, '\[([^\]]+)\]\(([^)]+)\)', {
        param($match)
        $target = $match.Groups[2].Value
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -and $target -notmatch '^https?://') { return $match.Groups[1].Value }
        if ($target -notmatch '^(https?://|#)') { $target = 'docs/' + $target }
        return '<a href="' + $target + '">' + $match.Groups[1].Value + '</a>'
    })
    $value = [regex]::Replace($value, '`([^`]+)`', '<code>$1</code>')
    $value = [regex]::Replace($value, '\*\*(.+?)\*\*', '<strong>$1</strong>')
    return $value
}
$body = New-Object Text.StringBuilder
$toc = New-Object Text.StringBuilder
$section = 0
for ($index = 0; $index -lt $lines.Length; $index++) {
    $line = $lines[$index]
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line.StartsWith('```')) {
        $code = New-Object Text.StringBuilder
        $index++
        while ($index -lt $lines.Length -and !$lines[$index].StartsWith('```')) { [void]$code.AppendLine($lines[$index]); $index++ }
        [void]$body.AppendLine('<pre><code>' + [Net.WebUtility]::HtmlEncode($code.ToString()) + '</code></pre>')
    } elseif ($line -match '^(#{1,3})\s+(.+)$') {
        $level = $Matches[1].Length
        $title = Convert-Inline $Matches[2]
        $section++
        [void]$body.AppendLine("<h$level id=`"section-$section`">$title</h$level>")
        if ($level -eq 2) { [void]$toc.AppendLine("<li><a href=`"#section-$section`">$title</a></li>") }
    } elseif ($line.StartsWith('|')) {
        [void]$body.AppendLine('<div class="table-wrap"><table>')
        $row = 0
        while ($index -lt $lines.Length -and $lines[$index].StartsWith('|')) {
            $cells = $lines[$index].Trim().Trim('|').Split('|')
            if ($row -ne 1) {
                [void]$body.Append('<tr>')
                $tag = if ($row -eq 0) { 'th' } else { 'td' }
                foreach ($cell in $cells) { [void]$body.Append("<$tag>" + (Convert-Inline $cell.Trim()) + "</$tag>") }
                [void]$body.AppendLine('</tr>')
            }
            $row++; $index++
        }
        $index--; [void]$body.AppendLine('</table></div>')
    } elseif ($line -match '^(\d+\.\s+|-\s+)') {
        $ordered = $line -match '^\d+\.'
        $tag = if ($ordered) { 'ol' } else { 'ul' }
        $pattern = if ($ordered) { '^\d+\.\s+' } else { '^-\s+' }
        [void]$body.AppendLine("<$tag>")
        while ($index -lt $lines.Length -and $lines[$index] -match $pattern) {
            [void]$body.AppendLine('<li>' + (Convert-Inline ([regex]::Replace($lines[$index], $pattern, ''))) + '</li>')
            $index++
        }
        $index--; [void]$body.AppendLine("</$tag>")
    } else { [void]$body.AppendLine('<p>' + (Convert-Inline $line) + '</p>') }
}
$html = @'
<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src 'self' file:; style-src 'unsafe-inline'; connect-src 'none'; form-action 'none'; base-uri 'none'">
<title>LetterTyper 新手使用教程</title><style>
*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#f3f6fb;color:#17243a;font:18px/1.9 system-ui,'Microsoft YaHei UI',sans-serif}.layout{max-width:1260px;margin:auto;padding:28px;display:grid;grid-template-columns:260px minmax(0,1fr);gap:34px}nav{position:sticky;top:20px;align-self:start;background:#e6eef9;padding:20px;border-radius:12px;font-size:15px}nav ol{padding-left:24px}main{background:white;border:1px solid #dbe3ed;border-radius:16px;padding:36px;min-width:0}h1{font-size:32px;line-height:1.5}h2{font-size:25px;margin-top:48px;padding-top:12px;border-top:2px solid #e5ecf5;scroll-margin-top:15px}h3{font-size:21px;margin-top:32px}a{color:#1459bb;overflow-wrap:anywhere}strong{color:#143b6c}li{margin-bottom:10px}code{font-family:Consolas,monospace;background:#edf2f8;padding:2px 5px;border-radius:4px;overflow-wrap:anywhere}pre{padding:16px;background:#edf2f8;overflow:auto;font-size:15px}pre code{padding:0;white-space:pre}img{display:block;max-width:100%;height:auto;border:1px solid #c6d3e2;border-radius:8px;margin:24px auto}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:16px;margin:20px 0}th,td{border:1px solid #d1dce9;text-align:left;vertical-align:top;padding:12px}th{background:#edf3fc}footer{font-size:14px;color:#50617a;margin-top:35px}@media(max-width:900px){.layout{display:block;padding:12px}nav{position:static;margin-bottom:18px}main{padding:20px}body{font-size:17px}}@media print{nav{display:none}.layout{display:block;padding:0}main{border:0;padding:0}h2{break-after:avoid}img{max-height:600px}a{color:inherit}}
</style></head><body><div class="layout"><nav aria-label="教程目录"><strong>从第一步开始</strong><ol>__TOC__</ol><p><a href="examples/practice.html">打开本地练习页</a></p></nav><main>__BODY__<footer>此离线教程由 docs/USAGE.md 生成。没有远程脚本、外部字体或跟踪代码；点击下载和官方参考链接时才会离开本地文件。</footer></main></div></body></html>
'@
$html = $html.Replace('__TOC__', $toc.ToString()).Replace('__BODY__', $body.ToString())
[IO.File]::WriteAllText((Join-Path $rootPath '开始使用.html'), $html, [Text.UTF8Encoding]::new($false))
Write-Output 'Offline beginner guide generated from docs/USAGE.md.'
