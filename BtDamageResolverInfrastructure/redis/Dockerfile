FROM redis:latest
RUN mkdir /conf
RUN mkdir /data
COPY ["redis.conf", "/conf/redis.conf"]
RUN chown --recursive redis:redis /conf
RUN chown --recursive redis:redis /data
EXPOSE 6379
CMD redis-server /conf/redis.conf --requirepass $REDIS_PASSWORD
