$lib = '..\lib\build'
$libUtils = $($lib + '\utils')
Include $($libUtils + '\utility-functions.ps1')
Include $($libUtils + '\find-and-replace.ps1')
Include $($libUtils + '\utility-tasks.ps1')
Include $($libUtils + '\general-tasks.ps1')

properties {
    $rootDir = Get-PrevDir $PWD
    $project = @{
        Name = Get-ProjectName $rootDir
        DefaultEnv = 'local'
    }
    $env = $project.DefaultEnv
    $projectDirs = @{
        Root = "$rootDir"
        Main = "$rootDir\$($project.Name)"
        Build =  "$rootDir\build"
        LibBuild = "$lib"
    }
    $projectDirs.Add("Templates", "$($projectDirs.Build)\templates")
    $projectDirs.Add("Variables", "$($projectDirs.Build)\variables")
    $projectFiles = @{
        Template = @{
            Input = "$($projectDirs.Templates)\web.config.tmpl"
            Output = "$($projectDirs.Root)\web.config"
        }
        Solution = "$($projectDirs.Root)\$($project.Name).sln"
    }
    $msbuildConfig = @{
        Target = 'Build'
        Verbosity = 'Minimal'
    }
    $messages = @{
        Default = @{
            Complete = 'Complete... Press any key to continue...'
            Error = 'There was a console error. See details above.'
        }
    }
}

task default -Depends Build-WebConfig, Build-Solution, Install-NPM, Install-Bower, Compile-Grunt

task Build-Solution -Depends Build-WebConfig {
    Exec { msbuild $projectFiles.Solution /maxcpucount /nologo /target:"$($msbuildConfig.Target)" /verbosity:"$($msbuildConfig.Verbosity)" } $messages.Default.Error
}

task Watch -Depends Watch-Grunt
