@echo off
for /F %%z in ('dir obj /ad /s /b') do echo %%z && rd /s /q %%z
for /F %%z in ('dir bin /ad /s /b') do echo %%z && rd /s /q %%z
echo Batchfile %0 is complete
pause