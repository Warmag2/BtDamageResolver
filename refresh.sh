#!/bin/sh
cd BtDamageResolverInfrastructure
docker build --tag orleansdb -f ./postgresql/Dockerfile ./postgresql/
chmod -R o+rx dashboards/
chmod -R o+rx datasources/
docker build --tag resolvergrafana -f ./grafana/Dockerfile ./grafana/
docker build --tag resolverredis -f ./redis/Dockerfile ./redis/
#docker build --tag resolversdk -f ../CustomNugets/Dockerfile ../CustomNugets/
docker build --tag resolversdk -f ./sdk/Dockerfile ../
docker build --tag resolver -f ../BtDamageResolver/src/Silo/Dockerfile ../BtDamageResolver/
docker build --tag resolverdataimporter -f ../BtDamageResolver/tools/DataImporter/Dockerfile ../BtDamageResolver/
docker build --tag resolverclient -f ../BtDamageResolverClient/src/BlazorServer/Dockerfile ../BtDamageResolverClient/

docker-compose down
docker-compose up -d
cd ..
