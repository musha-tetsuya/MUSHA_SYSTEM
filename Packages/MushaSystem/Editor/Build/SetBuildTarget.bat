rem Unity�o�b�`���[�h���Ƀv���b�g�t�H�[���ˑ��R���p�C���������Ȃ��ꍇ������̂ŁA
rem .rsp�ł̃J�X�^��define�ݒ�ɁuUNITY_�v�������o�����߂̃o�b�`

@echo off

rem ��1�����F%1 = Unity�v���W�F�N�g�t�H���_�p�X
rem ��2�����F%2 = �v���b�g�t�H�[��

rem RSP�t�@�C���p�X
set RSP_FILE_PATH=%1\Assets\csc.rsp

rem RSP�ꎞ�t�@�C���p�X
set RSP_TEMP_FILE_PATH=rsp@temp.txt

rem �r���h�^�[�Q�b�g����
if %2 == PS4 (
    set PLATFORM=UNITY_PS4
) else if %2 == PS5 (
    set PLATFORM=UNITY_PS5
) else if %2 == Win (
    set PLATFORM=UNITY_STANDALONE_WIN
) else if %2 == Win64 (
    set PLATFORM=UNITY_STANDALONE_WIN
) else (
    echo ���Ή��v���b�g�t�H�[���F%2
    goto :eof
)

rem ������rsp�t�@�C������UNITY_�s������
call %1\Assets\Plugins\KG\Editor\Build\SetDefine.bat %1 UNITY_ false

rem �w���UNITY_��rsp�ɒǉ�
call %1\Assets\Plugins\KG\Editor\Build\SetDefine.bat %1 %PLATFORM% true