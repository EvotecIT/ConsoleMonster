Import-Module $PSScriptRoot\..\ConsoleMonster.psd1 -Force

New-SpectreImage -FilePath "$PSScriptRoot\Images\Kulek.jpg"
New-SpectreImage -FilePath "$PSScriptRoot\Images\PrzemyslawKlysAndKulkozaurr.jpg"
New-SpectreImage -Uri 'https://upload.wikimedia.org/wikipedia/en/2/21/Web_of_Spider-Man_Vol_1_129-1.png'