# BtDamageResolver
A combat resolver for the classic Battletech boardgame

## Usage

## Building BtDamageResolver

### Configuring your nuget source

The idea is, that one would use the nuget feed provided by Github to build the project. To accomplish this, add the following source to your Nuget.config:

    <add key="githubWarma" value="https://nuget.pkg.github.com/Warmag2/index.json" />

To be able to use the source, add the following credential block in its entirety or add the `githubWarma` source to your credentials block:

    <packageSourceCredentials>
      <githubWarma>
        <add key="Username" value="Warmag2" />
        <add key="ClearTextPassword" value="INSERT_READ_ONLY_PAT_HERE" />
      </githubWarma>
    </packageSourceCredentials>

Mail me or contact me in IRC (Ircnet/Warma), and I will give you the read-only PAT so you can read the feed and get the nugets. I'd just put it here, but Github watches the commits I push into this repository, and if they contain the hash for the PAT, the PAT will be immediately revoked. I know this is a garbage solution, but I couldn't find any way to create a completely public nuget repository here in Github - I guess this service isn't meant for actually allowing others to access your repositories or something.

I will fix this later if someone tells me how to do it properly.

### Creating the docker images

* Pull this repository into a linux/unix server with docker support installed
* Run `producenugets.bat` or `producenugets.sh` in repository root
  - If you do not have a build system installed, or do not have access to my nuget feed (see above), download the nugets manually instead. You can do this from the sidebar in the Github repository page. After downloading, move them to the `CustomNugets` folder in repository root.
* Go to the `BtDamageResolverInfrastructure` folder found in repository root
* Run `refresh.sh`

This will create a compliation docker image, create all BtDamageResolver project docker images, create a postgresql image and a grafana image, and start all of them. Everything should work automatically, but might take some time. Once all the images are running and the server program can be reached, a data import task will launch and populate the entity databases with initial data. After this, the web client should be available and fully operational on port 8787 of localhost.

The postgresql server container is available on port 65432, if you want to take a look.

To edit the ports or adjust , edit the `docker-compose.yml` file in the `BtDamageResolverInfrastructure` folder.

### Shutting down and starting up

To shut down the system, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose down`

This should bring down everything. The volumes are persisted, should you want to bring up the system later.

To start the system at any later time, do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Type `docker-compose up -d`

### Getting rid of all BtDamageResolver data

Normal docker prune methods should work once the system has been shut down. If you are sure that there is nothing you want to keep, you can also do the following:

* Go to the `BtDamageResolverInfrastructure` folder in repository root
* Run `prune.sh`

**Do note that the above will delete absolutely all dangling content in your docker environment.** All dangling images, networks, volumes etc. Only use it if you have nothing in your docker you want to spare. It is included only because I want this to be usable by someone who doesn't know shit about docker and has only installed it to run BtDamageResolver.
