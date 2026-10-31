# BtDamageResolver

A combat resolver for the classic Battletech boardgame

## Usage

Youtube video: https://www.youtube.com/watch?v=27BzMTPgLZc

BtDamageResolver is a multiplayer web tool for resolving combat damage in the classic BattleTech tabletop boardgame. A shared game state lets multiple players in the same game see each other's units and the resulting damage reports in real time, so the whole table can follow through a combat round without doing any dice rolling or table referencing by hand.

Players define their units, weapons and firing solutions, and the resolver computes attacks against shared targets -- rolling to hit, determining hit locations, applying damage and through-armor criticals and motive hits. The outcome of the weapons and melee phases are presented on per-unit paper dolls, from which users can apply the damage to their paper unit sheets, which are still required for play.

The system is built as an Orleans silo (the `BtDamageResolver` server, which holds the game logic and state) plus a Blazor Server web client (`BtDamageResolverClient`), backed by an external PostgreSQL database for Orleans clustering/state storage and Redis for entity data and event bus. The whole stack runs from docker-compose as described below.

## Building BtDamageResolver

### Building the SDK

In the past, this was self-contained, but nowadays I have multiple services that run on the same stack. You need the dev container which hs all the necessary libraries and build tools.

Get it at: https://github.com/Warmag2/Faemiyah.Library

### Running the application with docker

* Pull this repository into a Linux/Unix server with docker support installed
* Go to the `infra` folder found in repository root
* Copy or rename the `.env_sample` file to just `.env`
* Type a secure password into the password environment variable in the `.env` file
* Make sure a PostgreSQL database named `resolver` is reachable at the host configured in `infra/docker-compose.yml` (by default `host.docker.internal:5432`).
* Go back to repository root
* Run `refresh.sh`

This will first create a compilation image with the Nuget files required by the client application, create all BtDamageResolver project docker images and a redis image, and start all of them. The silo creates the required Orleans schema automatically on first start, so an empty database with full rights for the configured user is sufficient. For local development you can start one with the bundled dev container (see [Hosting a development PostgreSQL container](#hosting-a-development-postgresql-container).

Everything should work automatically, but might take some time. Once all the images are running and the server program can be reached, a data import task will launch and populate the entity databases with initial data. After the import data succeeds (should take a few seconds) the web client should be available and fully operational on port 8787 of localhost.

Currently there is a bug where accessing the client before the data import task finishes will crash the client. This is related to Redis still being empty and the backend not being able to recover if it doesn't immediately find the data it needs. It would probably be easy to fix this, but I haven't bothered yet because the malfunction window is very small.

### Hosting a development PostgreSQL container

BtDamageResolver connects to an external PostgreSQL database; it is no longer bundled into the main stack. For local development, `infra/docker-compose.dev.yml` can spin up a throwaway PostgreSQL container that works out of the box do this before running the last step of the above guide (i.e. before running  `refresh.sh`):

* Go to the `infra` folder in repository root
* Run `docker compose -f docker-compose.dev.yml up -d`

This starts a PostgreSQL instance on port 5432 with an empty database named `resolver`, owned by the user from your `.env`. The silo creates the Orleans schema automatically on first start, so no manual script execution is required.

To shut the dev database down while keeping its data volume, run `docker compose -f docker-compose.dev.yml down`. Add `-v` to also delete the stored data.

### Troubleshooting

> Unable to connect to the database

On some Docker hosts, containers cannot resolve `host.docker.internal` by default. If the resolver container cannot reach the database, add the following to the `resolver` service in `docker-compose.yml`:

    extra_hosts:
      - "host.docker.internal:host-gateway"

> Unexpected characters in the .env file

This can be caused by Docker Desktop. Disabling "Docker Compose V2" from the settings of Docker Desktop might fix this.

> You are toying with BtDamageResolverClient but can't find the NuGet files

If you downloaded the NuGet packages from GitHub manually, be sure that you renamed them correctly. The name should also contain the version number.

As an easier option (in Windows), you can also run `build_producenugets.bat` to create the nugets yourself. After this, make sure that the `CustomNugets` directory in repository root is added to your `NuGet.config`.

> Unhandled exception rendering component: Cannot assign requested address (localhost:8080)

In some setups, Docker does not allow accessing localhost addresses. You can replace "localhost" with "host.docker.internal".

### Services installed

The following services are now available and running in containers:

* BtDamageResolver, which is not exposed to the outside world
* BtDamageResolverClient on port 8787
* Redis on port 63790

Only the BtDamageResolverClient needs to be accessed by players, and it does not need a password. To access the rest of these services, use the password and username you set in the `.env` file.
For users who do not need to access these services from the outside world, I would recommend firewalling all of them except the aforementioned BtDamageResolverClient.

To edit the external ports or adjust any service settings, edit the `docker-compose.yml` file in the `infra` folder.

### Shutting down and starting up

To shut down the system, do the following:

* Go to the `infra` folder in repository root
* `docker-compose down`
* (If dev container is in use) `docker compose -f docker-compose.dev.yml down`

This should bring down everything. Any necessary volumes are persisted, should you want to bring up the system later.

To start the system at any later time, follow the initial instructions in #[Running the application with docker](running-the-application-with-docker) or do the following:

* Go to the `infra` folder in repository root
* (If dev container is in use) `docker compose -f docker-compose.dev.yml up -d`
* `docker-compose up -d`

### Getting rid of all BtDamageResolver data

Normal docker prune methods should work once the system has been shut down. If you are sure that there is nothing you want to keep, you can also do the following:

* Go to the `infra` folder in repository root
* Run `prune.sh`

**Do note that the above will delete absolutely all dangling content in your docker environment.** All dangling images, networks, volumes etc. Only use it if you have nothing in your docker you want to spare. It is included only because I want this to be usable by someone who doesn't know anything about docker and has only installed it to run BtDamageResolver.

### Using the Nugets (Api and CompressionLZMA)

Building the project from the docker files creates a build container with the newest nugets and builds the client in that container, so as a regular user, it is not necessary to care about this.

In other words, the nugets are only necessary if you want to make your own client or work on developing the existing client. Compiling the BtDamageResolver project produces them. You can also run `build_producenugets.bat` which copies them into `\CustomNugets` in repository root.
