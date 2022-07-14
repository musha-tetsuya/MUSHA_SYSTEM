rem Unityバッチモード時にプラットフォーム依存コンパイルが効かない場合があるので、
rem .rspでのカスタムdefine設定に「UNITY_」を書き出すためのバッチ

@echo off

rem 第1引数：%1 = Unityプロジェクトフォルダパス
rem 第2引数：%2 = プラットフォーム

rem RSPファイルパス
set RSP_FILE_PATH=%1\Assets\csc.rsp

rem RSP一時ファイルパス
set RSP_TEMP_FILE_PATH=rsp@temp.txt

rem ビルドターゲット判定
if %2 == PS4 (
    set PLATFORM=UNITY_PS4
) else if %2 == PS5 (
    set PLATFORM=UNITY_PS5
) else if %2 == Win (
    set PLATFORM=UNITY_STANDALONE_WIN
) else if %2 == Win64 (
    set PLATFORM=UNITY_STANDALONE_WIN
) else (
    echo 未対応プラットフォーム：%2
    goto :eof
)

rem 既存のrspファイルからUNITY_行を除去
call %1\Assets\Plugins\KG\Editor\Build\SetDefine.bat %1 UNITY_ false

rem 指定のUNITY_をrspに追加
call %1\Assets\Plugins\KG\Editor\Build\SetDefine.bat %1 %PLATFORM% true