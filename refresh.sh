#!/bin/sh
cd infra
docker build --tag resolverredis -f ./redis/Dockerfile ./redis/
docker build --tag resolversdk -f ./sdk/Dockerfile ../
docker build --tag resolver -f ../BtDamageResolver/src/Silo/Dockerfile ../
docker build --tag resolverdataimporter -f ../BtDamageResolver/tools/DataImporter/Dockerfile ../
docker build --tag resolverdataexporter -f ../BtDamageResolver/tools/DataExporter/Dockerfile ../
docker build --tag resolverclient -f ../BtDamageResolverClient/src/BlazorServer/Dockerfile ../

docker compose down
docker compose up -d
cd ..
