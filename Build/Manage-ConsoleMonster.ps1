Clear-Host
Import-Module "PSPublishModule" -Force

$Configuration = @{
    Information = @{
        ModuleName        = 'ConsoleMonster'
        #DirectoryProjects = 'C:\Support\GitHub'

        #DirectoryModulesCore = "$Env:USERPROFILE\Documents\PowerShell\Modules"
        #DirectoryModules     = "$Env:USERPROFILE\Documents\WindowsPowerShell\Modules"

        #LibrariesCore        = 'Lib\Core'
        #LibrariesDefault     = 'Lib\Default'

        Manifest          = @{
            # Version number of this module.
            ModuleVersion              = '0.0.X'
            # Supported PSEditions
            CompatiblePSEditions       = @('Desktop', 'Core')
            # ID used to uniquely identify this module
            GUID                       = '7ef2133c-55a5-47e6-809e-6010f6e65e09'
            # Author of this module
            Author                     = 'Przemyslaw Klys'
            # Company or vendor of this module
            CompanyName                = 'Evotec'
            # Copyright statement for this module
            Copyright                  = "(c) 2011 - $((Get-Date).Year) Przemyslaw Klys @ Evotec. All rights reserved."
            # Description of the functionality provided by this module
            Description                = 'Console Monster is a PowerShell module to build cool GUI applications using Spectre Console and Terminal GUI'
            # Minimum version of the Windows PowerShell engine required by this module
            PowerShellVersion          = '5.1'
            # Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
            Tags                       = @('Windows', 'MacOS', 'Linux')

            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2022/08/ConsoleMonster-1.png'

            ProjectUri                 = 'https://github.com/EvotecIT/ConsoleMonster'

            RequiredModules            = @(
                #@{ ModuleName = 'PSSharedGoods'; ModuleVersion = "Latest"; Guid = 'ee272aa8-baaa-4edf-9f45-b6d6f7d844fe' }
            )
            ExternalModuleDependencies = @(
                #"ActiveDirectory"
                #"GroupPolicy"
                #"DnsServer"
                #"DnsClient"
                #"CimCmdlets"
                #"NetTCPIP"
                #"Microsoft.PowerShell.Management"
                #"Microsoft.PowerShell.Security"
            )
        }
    }
    Options     = @{
        Merge             = @{
            Sort           = 'None'
            FormatCodePSM1 = @{
                Enabled           = $true
                RemoveComments    = $false
                FormatterSettings = @{
                    IncludeRules = @(
                        'PSPlaceOpenBrace',
                        'PSPlaceCloseBrace',
                        'PSUseConsistentWhitespace',
                        'PSUseConsistentIndentation',
                        'PSAlignAssignmentStatement',
                        'PSUseCorrectCasing'
                    )

                    Rules        = @{
                        PSPlaceOpenBrace           = @{
                            Enable             = $true
                            OnSameLine         = $true
                            NewLineAfter       = $true
                            IgnoreOneLineBlock = $true
                        }

                        PSPlaceCloseBrace          = @{
                            Enable             = $true
                            NewLineAfter       = $false
                            IgnoreOneLineBlock = $true
                            NoEmptyLineBefore  = $false
                        }

                        PSUseConsistentIndentation = @{
                            Enable              = $true
                            Kind                = 'space'
                            PipelineIndentation = 'IncreaseIndentationAfterEveryPipeline'
                            IndentationSize     = 4
                        }

                        PSUseConsistentWhitespace  = @{
                            Enable          = $true
                            CheckInnerBrace = $true
                            CheckOpenBrace  = $true
                            CheckOpenParen  = $true
                            CheckOperator   = $true
                            CheckPipe       = $true
                            CheckSeparator  = $true
                        }

                        PSAlignAssignmentStatement = @{
                            Enable         = $true
                            CheckHashtable = $true
                        }

                        PSUseCorrectCasing         = @{
                            Enable = $true
                        }
                    }
                }
            }
            FormatCodePSD1 = @{
                Enabled        = $true
                RemoveComments = $false
            }
            Integrate      = @{
                ApprovedModules = @('PSSharedGoods', 'PSWriteColor', 'Connectimo', 'PSUnifi', 'PSWebToolbox', 'PSMyPassword')
            }
        }
        Standard          = @{
            FormatCodePSM1 = @{

            }
            FormatCodePSD1 = @{
                Enabled = $true
                #RemoveComments = $true
            }
        }
        PowerShellGallery = @{
            ApiKey   = 'C:\Support\Important\PowerShellGalleryAPI.txt'
            FromFile = $true
        }
        GitHub            = @{
            ApiKey   = 'C:\Support\Important\GithubAPI.txt'
            FromFile = $true
            UserName = 'EvotecIT'
            #RepositoryName = 'PSWriteHTML'
        }
        Documentation     = @{
            Path       = 'Docs'
            PathReadme = 'Docs\Readme.md'
        }
        Style             = @{
            PSD1 = 'Minimal' # Native
        }
    }
    Steps       = @{
        BuildLibraries     = @{
            Enable        = $true # build once every time nuget gets updated
            Configuration = 'Release'
            Framework     = 'netstandard2.0', 'net472'
            ProjectName   = 'ConsoleMonster'
        }
        BuildModule        = @{  # requires Enable to be on to process all of that
            Enable                 = $true
            DeleteBefore           = $true
            Merge                  = $true
            MergeMissing           = $true
            LibrarySeparateFile    = $false
            LibraryDotSource       = $true
            ClassesDotSource       = $false
            SignMerged             = $true
            CreateFileCatalog      = $false # not working
            Releases               = $true
            ReleasesUnpacked       = $false
            RefreshPSD1Only        = $false
            DebugDLL               = $false
            #ResolveBinaryConflicts = $true # mostly for memory and other libraries
            ResolveBinaryConflicts = @{
                ProjectName = 'ConsoleMonster'
            }
        }
        BuildDocumentation = $false
        ImportModules      = @{
            Self            = $true
            RequiredModules = $false
            Verbose         = $false
        }
        PublishModule      = @{  # requires Enable to be on to process all of that
            Enabled      = $false
            Prerelease   = ''
            RequireForce = $false
            GitHub       = $false
        }
    }
}

New-PrepareModule -Configuration $Configuration