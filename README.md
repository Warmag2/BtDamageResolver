# BtDamageResolver

A combat resolver for the classic Battletech boardgame

## Usage

Youtube video: https://www.youtube.com/watch?v=27BzMTPgLZc

TODO: Write something actually useful here.

## Building BtDamageResolver

### Creating and running the docker images

* Pull this repository into a Linux/Unix server with docker support installed
* Go to the `BtDamageResolverInfrastructure` folder found in repository root
* Copy or rename the `.env_sample` file to just `.env`
* Type a secure password into the password environment variable in the `.env` file
* Go back to repository root
* Run `refresh.sh`

This will first create a compilation image with the Nuget files required by the client application, create all BtDamageResolver project docker images, create a postgresql image, redis image and a grafana image, and start all of them. Everything should work automatically, but might take some time. Once all the images are running and the server program can be reached, a data import task will launch and populate the entity databases with initial data.

After the import data succeeds (should take a few seconds) the web client should be available and fully operational on port 8787 of localhost.

### Troubleshooting

> Unexpected characters in the .env file

This can be caused by Docker Desktop. Disabling "Docker Compose V2" from the settings of Docker Desktop might fix this.

> You are toying with BtDamageResolverClient but can't find the NuGet files

If you downloaded the NuGet packages from GitHub manually, be sure that you renamed them correctly. The name should also contain the version number.
If you did not do this, you can run `build_producenugets.bat` (in Windows) to produce the nugets and copy them to a CustomNugets directory. Make sure to add this to your `NuGet.config`.

> Unhandled exception rendering component: Cannot assign requested address (localhost:8080)

In some setups, Docker does not allow accessing localhost addresses. You can replace "localhost" with "host.docker.internal".

### Services installed

The following services are now available and running in containers:

* BtDamageResolver, which is not exposed to the outside world
* BtDamageResolverClient on port 8787
* PostgreSQL on port 65432
* Grafana on port 63000
* Redis on port 63790

Only the BtDamageResolverClient needs to be accessed by players, and it does not need a password. To access the rest of these services, use the password and username you set in the `.env` file.
For users who do not need to access these services from the outside world, I would recommend firewalling all of them except the aforementioned BtDamageResolverClient.

To edit the external ports or adjust any service settings, edit the `docker-compose.yml` file in the `BtDamageResolverInfrastructure` folder.

### Shutting down and starting up

To shut down the system, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose down`

This should bring down everything. The volumes are persisted, should you want to bring up the system later.

To start the system at any later time, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose up -d`

Currently there is a bug where accessing the client before the data import task finishes (after bringing the system up) fails. You must wait for the task to finish before starting to use the system. This is related to Redis not persisting properly and the backend not being able to recover if it doesn't immediately find the data it needs. It would probably be easy to fix but I haven't bothered yet because it does so little harm.

### Getting rid of all BtDamageResolver data

Normal docker prune methods should work once the system has been shut down. If you are sure that there is nothing you want to keep, you can also do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Run `prune.sh`

**Do note that the above will delete absolutely all dangling content in your docker environment.** All dangling images, networks, volumes etc. Only use it if you have nothing in your docker you want to spare. It is included only because I want this to be usable by someone who doesn't know shit about docker and has only installed it to run BtDamageResolver.

### Using the GitHub Nuget Feed

The easiest way is to develop a client would be to use the nuget feed provided by Github to build the project and not worry about anything else. To accomplish this, one can add the following source to your Nuget.config:

    <add key="githubWarma" value="https://nuget.pkg.github.com/Warmag2/index.json" />

To be able to use the NuGet source, use the following credential block in its entirety or add the contained `githubWarma` source to your credentials block:

    <packageSourceCredentials>
      <githubWarma>
        <add key="Username" value="Warmag2" />
        <add key="ClearTextPassword" value="INSERT_YOUR_READ_ONLY_NUGET_FEED_PAT_HERE" />
      </githubWarma>
    </packageSourceCredentials>
