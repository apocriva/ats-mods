@echo off

powershell -command "Compress-Archive -Path 'Against the Storm\*.*' -DestinationPath 'AgainstTheStormExtension.zip' -Force
