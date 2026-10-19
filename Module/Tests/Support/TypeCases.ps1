# The input types the type sweep runs (ConsoleMonster.Types.Tests.ps1): dot-sourced at discovery (test cases) and in
# BeforeAll (the child script), since Pester 5 keeps the two phases apart.
$TypeCases = @(
        @{ Name = 'Hashtable'; Make = "@{ Name = 'alpha'; Count = 3; Enabled = `$true }" }
        @{ Name = 'Ordered'; Make = "[ordered] @{ Zulu = 1; Alpha = 2; Mike = 3 }" }
        @{ Name = 'GenericDictionary'; Make = "`$d = [System.Collections.Generic.Dictionary[string, object]]::new(); `$d['Name'] = 'alpha'; `$d['Count'] = 3; , `$d" }
        @{ Name = 'SortedDictionary'; Make = "`$d = [System.Collections.Generic.SortedDictionary[string, int]]::new(); `$d['b'] = 2; `$d['a'] = 1; , `$d" }
        @{ Name = 'NonStringKeys'; Make = "@{ 1 = 'one'; 2 = 'two'; [DayOfWeek]::Monday = 'start' }" }
        @{ Name = 'ListOfHashtables'; Make = "@{ Name = 'a'; Size = 1 }, @{ Name = 'b'; Size = 2 }" }
        @{ Name = 'NestedDictionary'; Make = "[pscustomobject] @{ Name = 'svc'; Settings = @{ Port = 8080; Tls = `$true } }" }
        @{ Name = 'PSCustomObject'; Make = "[pscustomobject] @{ Name = 'alpha'; Tags = @('a', 'b'); Owner = [pscustomobject] @{ Name = 'Jan'; Id = 7 } }" }
        @{ Name = 'ListAsOneObject'; Make = "`$l = [System.Collections.Generic.List[int]]::new(); `$l.AddRange([int[]] (1, 2, 3)); , `$l" }
        @{ Name = 'ArrayOfArrays'; Make = ", @(@(1, 2), @(3, 4))" }
        @{ Name = 'Strings'; Make = "'hello', 'world'" }
        @{ Name = 'DataRows'; Make = "`$t = [System.Data.DataTable]::new('t'); [void] `$t.Columns.Add('Name'); [void] `$t.Columns.Add('Size', [int]); [void] `$t.Rows.Add('a', 1); [void] `$t.Rows.Add('b', [DBNull]::Value); `$t.Rows" }
        @{ Name = 'XmlDocument'; Make = "[xml] '<config><server name=`"web01`" port=`"80`"/><server name=`"web02`" port=`"81`"/></config>'" }
        @{ Name = 'Json'; Make = "'{`"name`":`"app`",`"port`":8080,`"tags`":[`"a`",`"b`"]}' | ConvertFrom-Json" }
        @{ Name = 'Scalars'; Make = '[pscustomobject] @{ Enum = [DayOfWeek]::Monday; Guid = [guid] "0f8fad5b-d9cb-469f-a165-70867728950e"; Bytes = [byte[]] (1..200); Secret = (& { $s = [System.Security.SecureString]::new(); foreach ($ch in "NotARealSecret-123".ToCharArray()) { $s.AppendChar($ch) }; , $s }); When = [datetime] "2026-10-19 14:05" }' }
        @{ Name = 'SecureString'; Make = '$s = [System.Security.SecureString]::new(); foreach ($ch in "NotARealSecret-123".ToCharArray()) { $s.AppendChar($ch) }; , $s' }
        @{ Name = 'ThrowingGetter'; Make = "`$o = [pscustomobject] @{ Name = 'a' }; `$o | Add-Member ScriptProperty Boom { throw 'script getter failed' }; `$o" }
        @{ Name = 'SelfReference'; Make = "`$a = [pscustomobject] @{ Name = 'parent' }; `$b = [pscustomobject] @{ Name = 'child'; Parent = `$a }; `$a | Add-Member NoteProperty Child `$b; `$a" }
        @{ Name = 'Files'; Make = "Get-ChildItem -LiteralPath `$PSHOME -File | Select-Object -First 3" }
        @{ Name = 'Process'; Make = "Get-Process -Id `$PID" }
    )
