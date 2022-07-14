rem .rspにdefineを追加/除去するためのバッチ

@echo off

rem 第1引数：%1 = Unityプロジェクトフォルダパス
rem 第2引数：%2 = 設定したいdefine
rem 第3引数：%3 = true=追加, false=除去

rem RSPファイルパス
set RSP_FILE_PATH=%1\Assets\csc.rsp

rem RSP一時ファイルパス
set RSP_TEMP_FILE_PATH=rsp@temp.txt

rem RSPファイルが既存なら
if exist %RSP_FILE_PATH% (

    rem 対象行を削除して一時ファイルに保存
    find /v "-define:%2" <%RSP_FILE_PATH% >%RSP_TEMP_FILE_PATH%

    rem 一時ファイル内容をRSPファイルに上書き
    move /y %RSP_TEMP_FILE_PATH% %RSP_FILE_PATH%
)

rem define追加するなら
if "%3" == "true" (
    
    rem RSPファイルに追加Defineを記入
    echo -define:%2>>%RSP_FILE_PATH%
)
