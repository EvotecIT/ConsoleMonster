# Read by ConsoleMonster when it is loaded; plain data, no code. The module itself never imports ConsoleMonster.
@{
    SchemaVersion = 1
    Views         = @(
        @{
            TypeName = 'Contoso.Server'
            View     = 'Table'
            Property = @(
                'Name'
                @{ Name = 'Health'; Expression = 'State'; Presenter = 'StatusWord' }
                'Uptime'
            )
            GroupBy  = 'Site'
        }
    )
}