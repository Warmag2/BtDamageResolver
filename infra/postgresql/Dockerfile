FROM postgres:latest
WORKDIR /docker-entrypoint-initdb.d
COPY ["scripts/", "."]
RUN /bin/sh -c "chmod o+rx /docker-entrypoint-initdb.d/*.sql"
EXPOSE 5432
