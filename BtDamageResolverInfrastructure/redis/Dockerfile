FROM redis:latest
RUN mkdir /conf
COPY ["redis.conf", "/conf/redis.conf"]
RUN chown --recursive redis:redis /conf
EXPOSE 6379
CMD [ "redis-server", "/conf/redis.conf", "--requirepass $REDIS_PASSWORD" ]
