@echo off

call build.cmd

set dotNetBasePath=%windir%\Microsoft.NET\Framework
if exist %dotNetBasePath%64 set dotNetBasePath=%dotNetBasePath%64
for /R %dotNetBasePath% %%i in (*msbuild.exe) do set msbuild=%%i

cd msbuild.test
set target=msbuild.test.csproj

%msbuild% /v:q /t:Rebuild /p:Configuration=Release %target%

