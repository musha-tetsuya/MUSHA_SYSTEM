rem .rsp��define��ǉ�/�������邽�߂̃o�b�`

@echo off

rem ��1�����F%1 = Unity�v���W�F�N�g�t�H���_�p�X
rem ��2�����F%2 = �ݒ肵����define
rem ��3�����F%3 = true=�ǉ�, false=����

rem RSP�t�@�C���p�X
set RSP_FILE_PATH=%1\Assets\csc.rsp

rem RSP�ꎞ�t�@�C���p�X
set RSP_TEMP_FILE_PATH=rsp@temp.txt

rem RSP�t�@�C���������Ȃ�
if exist %RSP_FILE_PATH% (

    rem �Ώۍs���폜���Ĉꎞ�t�@�C���ɕۑ�
    find /v "-define:%2" <%RSP_FILE_PATH% >%RSP_TEMP_FILE_PATH%

    rem �ꎞ�t�@�C�����e��RSP�t�@�C���ɏ㏑��
    move /y %RSP_TEMP_FILE_PATH% %RSP_FILE_PATH%
)

rem define�ǉ�����Ȃ�
if "%3" == "true" (
    
    rem RSP�t�@�C���ɒǉ�Define���L��
    echo -define:%2>>%RSP_FILE_PATH%
)
