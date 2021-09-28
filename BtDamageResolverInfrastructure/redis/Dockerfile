FROM redis:latest
RUN mkdir /conf
COPY ["redis.conf", "/conf/redis.conf"]
RUN chown --recursive redis:redis /conf
CMD [ "redis-server", "/conf/redis.conf" ]
