param([string]$Source = "$PSScriptRoot/../Assets/_Project/Scripts/Survival/HudLayout.cs")
$ErrorActionPreference = 'Stop'
Add-Type -Path $Source
function Equal($actual, $expected, $name) { if ($actual -ne $expected) { throw "$name : expected $expected, got $actual" } }
Equal ([Evolve.Survival.HudCatalog]::Matches('Bộc phát gai', 'Gai lưng', 'boc phat')) $true 'Vietnamese name search'
Equal ([Evolve.Survival.HudCatalog]::Matches('Lao mạnh', 'Chân cơ bắp', 'chan co')) $true 'Organ search'
Equal ([Evolve.Survival.HudCatalog]::Matches('Lao mạnh', 'Chân', 'mat')) $false 'No match'
Equal ([Evolve.Survival.HudCatalog]::PageCount(103,8)) 13 'Large catalog'
Equal ([Evolve.Survival.HudCatalog]::ClampPage(99,103,8)) 12 'Last page'
Equal ([Evolve.Survival.HudCatalog]::ClampPage(-1,0,8)) 0 'Empty catalog'
Equal ([Evolve.Survival.HudCatalog]::SpikeTotal(6,5,2)) 30 'Spike level 2'
Equal ([Evolve.Survival.HudCatalog]::SpikeTotal(6,5,3)) 63 'Spike level 3'
Equal ([Evolve.Survival.HudCatalog]::SpikeTotal(8,4,3)) 72 'Real asset values'
Write-Output 'PASS: 9 catalog and damage checks'
foreach($viewport in @(@(1280,800),@(1366,768),@(1920,1080),@(800,600))) {
    $width=$viewport[0];$height=$viewport[1]
    $hudScale=[Math]::Max(.1,[Math]::Min(1,[Math]::Min($width/960,$height/600)))
    $menuScale=[Evolve.Survival.HudCatalog]::MenuScale($width,$height,$hudScale)
    $menuBottom=52+650*$menuScale
    $secondaryTop=$height-196*$hudScale
    if($menuBottom -gt $secondaryTop-12) {throw "Menu covers HUD at $width x $height"}
}
Write-Output 'PASS: menu leaves expanded HUD visible at 4 viewport sizes'
