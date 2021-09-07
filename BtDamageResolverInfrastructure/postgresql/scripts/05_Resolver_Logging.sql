-- This table defines Resolver logging data for game actor activations
CREATE TABLE ResolverLogGame
(
    Id SERIAL PRIMARY KEY,
    EventTime TIMESTAMP(3) NOT NULL,
    GameId INT NOT NULL,
    ActionType SMALLINT NOT NULL,
    ActionData INT NOT NULL
);

CREATE INDEX ResolverLogGame_EventTime ON ResolverLogGame (EventTime);
CREATE INDEX ResolverLogGame_ActionType_EventTime ON ResolverLogGame (ActionType, EventTime);

-- This table defines Resolver logging data for user actor activations
CREATE TABLE ResolverLogPlayer
(
    Id SERIAL PRIMARY KEY,
    EventTime TIMESTAMP(3) NOT NULL,
    PlayerId INT NOT NULL,
    ActionType SMALLINT NOT NULL,
    ActionData INT NOT NULL
);

CREATE INDEX ResolverLogPlayer_EventTime ON ResolverLogPlayer (EventTime);
CREATE INDEX ResolverLogPlayer_ActionType_EventTime ON ResolverLogPlayer (ActionType, EventTime);

-- This table defines Resolver logging data for unit actor activations
CREATE TABLE ResolverLogUnit
(
    Id SERIAL PRIMARY KEY,
    EventTime TIMESTAMP(3) NOT NULL,
    UnitId INT NOT NULL,
    ActionType SMALLINT NOT NULL,
    ActionData INT NOT NULL
);

CREATE INDEX ResolverLogUnit_EventTime ON ResolverLogUnit (EventTime);
CREATE INDEX ResolverLogUnit_ActionType_EventTime ON ResolverLogUnit (ActionType, EventTime);