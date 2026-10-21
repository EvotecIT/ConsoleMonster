@{
    SchemaVersion = 1
    Views = @(
        @{
            TypeName = 'EverydayViews.Server'
            View = 'Table'
            Property = @('Name', @{ Name = 'Health'; Expression = 'State'; Presenter = 'StatusWord' })
            GroupBy = 'Site'
            Group = 'EverydayServers'
        }
        @{
            TypeName = 'EverydayViews.OtherServer'
            View = 'Table'
            Property = @('Name', @{ Name = 'Health'; Expression = 'State'; Presenter = 'StatusWord' })
            GroupBy = 'Site'
            Group = 'EverydayServers'
        }
    )
}
