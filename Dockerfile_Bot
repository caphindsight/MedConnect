FROM mono
MAINTAINER Cap. Hindsight <hindsight@yandex.ru>

COPY . /MedConnect

WORKDIR /MedConnect
RUN nuget restore
RUN xbuild /p:Configuration=Release

WORKDIR /MedConnect/MedConnectBot/bin/Release
CMD mono MedConnectBot.exe

