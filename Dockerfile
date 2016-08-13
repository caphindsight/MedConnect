FROM ubuntu:16.04
MAINTAINER Cap. Hindsight <hindsight@yandex.ru>

RUN apt-get update
RUN apt-get install -y mono-complete nuget

COPY . /MedConnect

WORKDIR /MedConnect
RUN nuget restore
RUN xbuild /p:Configuration=Release

WORKDIR /MedConnect/MedConnectBot/bin/Release
CMD mono MedConnectBot.exe

