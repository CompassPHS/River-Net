@ECHO off
SET rootDir=%CD%
SET buildDir=%rootDir%\build
SET libBuildDir=%rootDir%\lib\build
SET psakeDir=%libBuildDir%\psake
SET psakeCommandFile=%psakeDir%\psake.cmd
SET defaultPsakeTaskFile=%buildDir%\default.ps1
SET defaultEnv=local
SET envList=[local, dev, qa, prod]
SET gruntWatchTaskName=watch

:UpdateSubmodules
    echo Updating GIT submodules
        git submodule update --init
        cd %libBuildDir%
        git checkout master
        git reset HEAD --hard
        git pull
        cd %rootDir%
    echo Done!
:End

IF [%1]==[help] GOTO Help
IF [%1]==[watch] GOTO Watch

:Build
    IF [%1] EQU [] (
        %psakeCommandFile% %defaultPsakeTaskFile% default
    ) ELSE (
        %psakeCommandFile% %defaultPsakeTaskFile% default -properties "@{'env'='%1%'}"
    )
:End

:Watch
    %psakeCommandFile% %defaultPsakeTaskFile% %gruntWatchTaskName%
:End

:Help
    ECHO Usage:
    ECHO     %0%
    ECHO     %0 % %%gruntWatchTaskName%%
    ECHO     %0 % %%envList%%
    ECHO     default: %0 % %%defaultEnv%%
:End