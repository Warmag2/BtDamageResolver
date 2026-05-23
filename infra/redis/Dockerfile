FROM redis:latest
RUN mkdir /conf
RUN mkdir /db
COPY ["redis.conf", "/conf/redis.conf"]
RUN chown --recursive redis:redis /conf
RUN chown --recursive redis:redis /db
EXPOSE 6379
CMD redis-server /conf/redis.conf --requirepass $REDIS_PASSWORD
