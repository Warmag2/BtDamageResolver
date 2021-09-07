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

