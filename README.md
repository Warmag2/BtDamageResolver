# BtDamageResolver
A combat resolver for the classic Battletech boardgame

## Usage

## Building BtDamageResolver

### Configuring your nuget source

To be able to use the nuget packages, add the following source to your Nuget.config:

    <add key="githubWarma" value="https://nuget.pkg.github.com/Warmag2/index.json" />

To be able to use the source, add the following personal access tokens with read-only access to my Github package repository:

    <packageSourceCredentials>
      <githubWarma>
        <add key="Username" value="Warmag2" />
        <add key="ClearTextPassword" value="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" />
      </githubWarma>
    </packageSourceCredentials>
    
I know this is a garbage solution, but I couldn't find any way to create a completely public Github nuget repository.
I will fix this later if I can get information on how to perform this feat.

## Creating the docker images

* Build all of your projects once. This will pull the nugets from the author's repository
* Copy the nugets to the `CustomNugets` folder in project root
* Go to the `BtDamageResolverInfrastructure` folder
* Run `refresh.sh`

This will create a compliation docker image, create all BtDamageResolver project docker images, create a postgresql image and a grafana image, and start all of them. Everything should work automatically, but might take some time. Once the images are running, a data import task will populate the entity databases once it reaches the server program, and after this, the web client should be available and fully operational on port 8787 of localhost.

The postgresql server container is available on port 65432, if you want to take a look.

## Shutting down

* Go to the `BtDamageResolverInfrastructure` folder
* Type `docker-compose down`

This should bring down everything. The volumes are persisted, should you

## Getting rid of all BtDamageResolver data

Normal docker prune methods should work once the system has been shut down. If you are sure that there is nothing you want to keep, you can also do the following:

* Go to the `BtDamageResolverInfrastructure` folder
* Run `prune.sh`

**Do note that the above will delete absolutely all dangling content in your docker environment.** All dangling images, networks, volumes etc. Only use it if you have nothing in your docker you want to spare.
