#!/bin/sh
ServerPath='/lib/systemd/system'
FileName='miniftp.service'

systemctl stop $FileName
systemctl disable $FileName
systemctl daemon-reload
systemctl status $FileName

rm $ServerPath/$FileName

echo systemctl status $FileName
