@echo off
set dotNetBasePath=%windir%\Microsoft.NET\Framework
if exist %dotNetBasePath%64 set dotNetBasePath=%dotNetBasePath%64
for /R %dotNetBasePath% %%i in (*msbuild.exe) do set msbuild=%%i

set target=msbuild.sln

rem %msbuild% /t:Rebuild /p:Configuration=Debug %target%
%msbuild% /v:q /t:Rebuild /p:Configuration=Release %target%

