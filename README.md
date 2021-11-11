# BtDamageResolver

A combat resolver for the classic Battletech boardgame

## Usage

TODO: Write this, or create Youtube videos or something

## Building BtDamageResolver

### Creating and running the docker images

* Pull this repository into a linux/unix server with docker support installed
* Run `build_producenugets.bat` or `build_producenugets.sh` in repository root depending on whether you are a Windows or Unix user. This will create the Nuget files required by the client implementation and put them in the `CustomNugets` subdirectory where the build system needs them to be.
  - If you do not have a build system installed, or do not have access to my nuget feed, download the nugets manually instead. You can do this from the sidebar in the Github repository page. After downloading, manually move them to the `CustomNugets` folder in repository root.
* Go to the `BtDamageResolverInfrastructure` folder found in repository root
* Copy or rename the `.env_sample` file to just `.env`
* Type a secure password into the password environment variable in the `.env` file.
* Run `refresh.sh`

This will create a compliation docker image, create all BtDamageResolver project docker images, create a postgresql image, redis image and a grafana image, and start all of them. Everything should work automatically, but might take some time. Once all the images are running and the server program can be reached, a data import task will launch and populate the entity databases with initial data.

After the import data succeeds (should take a few seconds) the web client should be available and fully operational on port 8787 of localhost.

### Services installed

The following services are now available and running in containers:

* BtDamageResolver, which is not exposed to the outside world
* BtDamageResolverClient on port 8787
* PostgreSQL on port 65432
* Grafana on port 63000
* Redis on port 63790

To access these services, use the password and username you set in the `.env` file.

To edit the external ports or adjust any service settings, edit the `docker-compose.yml` file in the `BtDamageResolverInfrastructure` folder.

### Shutting down and starting up

To shut down the system, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose down`

This should bring down everything. The volumes are persisted, should you want to bring up the system later.

To start the system at any later time, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose up -d`

### Getting rid of all BtDamageResolver data

Normal docker prune methods should work once the system has been shut down. If you are sure that there is nothing in your docker installation that you want to keep, you can also do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Run `prune.sh`

**Do note that the above will delete absolutely all dangling content in your docker environment.** All dangling images, networks, volumes etc. Only use it if you have nothing in your docker you want to spare. It is included only because I want this to be usable by someone who doesn't know shit about docker and has only installed it to run BtDamageResolver.

### Using the GitHub Nuget Feed

The easiest way is to develop a client would be to use the nuget feed provided by Github to build the project and not worry about anything else. To accomplish this, one can add the following source to your Nuget.config:

    <add key="githubWarma" value="https://nuget.pkg.github.com/Warmag2/index.json" />

To be able to use the source, add the following credential block in its entirety or add the `githubWarma` source to your credentials block:

    <packageSourceCredentials>
      <githubWarma>
        <add key="Username" value="Warmag2" />
        <add key="ClearTextPassword" value="INSERT_READ_ONLY_NUGET_FEED_PAT_HERE" />
      </githubWarma>
    </packageSourceCredentials>

Unfortunately Github watches the commits I push into this repository, and if they contain the hash for the PAT, the PAT will be immediately revoked. Therefore I cannot share it in this readme directly. I guess this service isn't meant for actually allowing others to access your feeds or repositories or something.

To circumvent this, mail me or contact me in IRC (Ircnet/Warma), and I will give you the read-only PAT so you can read the feed and get those nugets.

I will fix this later if someone tells me how to do it properly.
