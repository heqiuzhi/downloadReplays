@echo off

..\..\Protogen\protogen -s:..\ -i:"my_dota_gcmessages_common.proto" -o:"..\..\protoClass\MyMatch.cs" -t:csharp -ns:"DownloadDota2Replay"

pause 
