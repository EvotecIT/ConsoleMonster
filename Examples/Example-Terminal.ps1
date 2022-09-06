Import-Module .\ConsoleMonster.psd1 -Force

New-Terminal {
    New-TerminalWindow -Title 'Test' {
        New-TerminalText -Text 'Special Text' -Width 20 -X 20
        New-TerminalButton -Text "Quit" -X 2 -Y 20 -Command {

        }
        New-TerminalRadioGroup -Text "one", 'Two', 'Three' -X 10 -Y 30


        New-TerminalCheckBox -Text "CheckBox1" -X 10 -Y 50
        New-TerminalCheckBox -Text "CheckBox2" -X 10 -Y 60
        New-TerminalCheckBox -Text "CheckBox3" -X 10 -Y 70

    }
}