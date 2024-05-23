#!/bin/sh
ServerPath='/lib/systemd/system'
FileName='miniftp.service'

chmod +x $FileName
chmod +x miniftp
chmod +x RemoveServer.sh

cp $FileName $ServerPath/$FileName

systemctl enable $FileName
systemctl start $FileName
systemctl status $FileName
echo 'init end'
