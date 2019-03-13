@echo off
goto check_Permissions

:check_Permissions
    echo Checking user rights...

    net session >nul 2>&1
    if %errorLevel% == 0 (
        echo Success. Complete the association answering the message boxes.
        %~dp0\abupload.exe -register
        echo All done.
    ) else (
        echo Failure: Run the script again as administrator.
    )

    pause >nul