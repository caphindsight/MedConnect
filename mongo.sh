#! /usr/bin/env bash

CMD="$1"
DIR="$(pwd)/mongo"

case "$CMD" in

"mongo-dev")
mkdir -p mongo/{db,db_config}
docker run -it --rm --name=medconnect_mongo -v "${DIR}/db:/data/db" -v "${DIR}/db_conf:/data/configdb" -p 127.0.0.1:32000:27017 mongo
;;

"admin-dev")
docker run -it --rm --name=medconnect_mongo_admin -p 127.0.0.1:1234:1234 --link medconnect_mongo:mongo adicom/admin-mongo
;;

"*")
echo "Usage: bash mongo.sh <mongo-dev/admin-dev>" >&2

esac

