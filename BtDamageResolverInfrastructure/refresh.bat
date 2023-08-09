cd postgresql
docker build --tag orleansdb .
cd ../grafana
chmod -R o+rx dashboards/
chmod -R o+rx datasources/
docker build --tag resolvergrafana .
cd ../redis
docker build --tag resolverredis .
cd ../../CustomNugets
docker build --tag resolversdk .
cd ../BtDamageResolver
docker build --tag resolver -f src/Silo/Dockerfile .
docker build --tag resolverdataimporter -f tools/DataImporter/Dockerfile .
cd ../BtDamageResolverClient
docker build --tag resolverclient -f src/BlazorServer/Dockerfile .
cd ../BtDamageResolverInfrastructure

docker-compose down
docker-compose up -d
