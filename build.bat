@echo Off
set config=%1
if "%config%" == "" (
    set config=Release
)

set version=
if not "%BuildCounter%" == "" (
   set packversionsuffix=--version-suffix ci-%BuildCounter%
)

REM Detect MSBuild 15.0 path
if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
    set msbuild="%programfiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
)
if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
    set msbuild="%programfiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
)

if exist "%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
    set msbuild="%programfiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
)

if exist "%programfiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set msbuild="%programfiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)


REM Restore
call dotnet restore MyGet.sln --disable-parallel
if not "%errorlevel%"=="0" goto failure

REM Build
REM - Option 1: Run dotnet build for every source folder in the project
REM   e.g. call dotnet build <path> --configuration %config%
REM - Option 2: Let msbuild handle things and build the solution
call "%msbuild%" MyGet.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
REM call dotnet build --configuration %config%
if not "%errorlevel%"=="0" goto failure

REM Package
mkdir %cd%\..\artifacts
call dotnet pack MyLibrary --configuration %config% %packversionsuffix% --output %cd%\..\artifacts
if not "%errorlevel%"=="0" goto failure

:success
exit 0

:failure
exit -1

msbuild Microsoft.FeatureManagement.Plus\Microsoft.FeatureManagement.Plus.csproj  /t:pack /p:IncludeSymbols=true /p:SymbolPackageFormat=snupkg -fl -flp:logfile=MyProjectOutput.log;verbosity=diagnostic