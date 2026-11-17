$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot/../Assets/_Project/Scripts/Survival/HudLayout.cs"
function Assert-Equal($actual, $expected, $name) {
    if ($actual -ne $expected) { throw "$name : expected $expected, got $actual" }
}
Assert-Equal ([Evolve.Survival.HudLayout]::ClampCenter(-30, 300, 1280, 12)) 162 'Left edge'
Assert-Equal ([Evolve.Survival.HudLayout]::ClampCenter(1500, 300, 1280, 12)) 1118 'Right edge'
Assert-Equal ([Evolve.Survival.HudLayout]::ClampCenter(640, 300, 1280, 12)) 640 'Visible anchor'
Assert-Equal ([Evolve.Survival.HudLayout]::ClampCenter(-30, 400, 300, 12)) 150 'Oversized panel centers'
Assert-Equal ([Evolve.Survival.HudLayout]::Toggle('Skills', 'Skills')) 'None' 'Repeated key closes'
Assert-Equal ([Evolve.Survival.HudLayout]::Toggle('Skills', 'Organs')) 'Organs' 'Switch replaces menu'
Write-Output 'PASS: 6 HUD viewport and menu checks'
