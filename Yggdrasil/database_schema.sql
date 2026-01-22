ALTER DATABASE CHARACTER SET utf8mb4;


CREATE TABLE `acct` (
    `accountId` int unsigned NOT NULL AUTO_INCREMENT,
    `username` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `password` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `secondPassword` varchar(255) CHARACTER SET utf8mb4 NULL,
    `email` varchar(100) CHARACTER SET utf8mb4 NULL,
    `uniId` int unsigned NULL,
    `char1` int NULL,
    `char2` int NULL,
    `char3` int NULL,
    `char4` int NULL,
    `lastChar` int NULL,
    `premium` int NOT NULL,
    `cash` bigint NOT NULL,
    `silk` int NOT NULL,
    `level` int NOT NULL,
    CONSTRAINT `PK_acct` PRIMARY KEY (`accountId`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `servers` (
    `serverId` int NOT NULL AUTO_INCREMENT,
    `name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `ip` varchar(45) CHARACTER SET utf8mb4 NULL,
    `port` int NOT NULL,
    CONSTRAINT `PK_servers` PRIMARY KEY (`serverId`)
) CHARACTER SET=utf8mb4;


CREATE TABLE `chars` (
    `characterId` int NOT NULL AUTO_INCREMENT,
    `accountId` int unsigned NOT NULL,
    `charName` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `model` smallint unsigned NOT NULL,
    `level` tinyint unsigned NOT NULL,
    `partner` int NULL,
    `map` int unsigned NOT NULL,
    `x` int NOT NULL,
    `y` int NOT NULL,
    `hp` int NOT NULL,
    `ds` int NOT NULL,
    `money` bigint NOT NULL,
    `inventory` blob NULL,
    `warehouse` blob NULL,
    `archive` blob NULL,
    CONSTRAINT `PK_chars` PRIMARY KEY (`characterId`),
    CONSTRAINT `FK_chars_acct_accountId` FOREIGN KEY (`accountId`) REFERENCES `acct` (`accountId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE TABLE `digimon` (
    `digimonId` int NOT NULL AUTO_INCREMENT,
    `characterId` int NULL,
    `digiName` varchar(50) CHARACTER SET utf8mb4 NULL,
    `digiModel` int unsigned NOT NULL,
    `level` tinyint unsigned NOT NULL,
    `size` smallint unsigned NOT NULL,
    `hp` int NOT NULL,
    `ds` int NOT NULL,
    `exp` bigint NOT NULL,
    `digiSlot` tinyint unsigned NOT NULL,
    `evolutions` blob NULL,
    CONSTRAINT `PK_digimon` PRIMARY KEY (`digimonId`),
    CONSTRAINT `FK_digimon_chars_characterId` FOREIGN KEY (`characterId`) REFERENCES `chars` (`characterId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;


CREATE UNIQUE INDEX `IX_acct_username` ON `acct` (`username`);


CREATE INDEX `IX_chars_accountId` ON `chars` (`accountId`);


CREATE UNIQUE INDEX `IX_chars_charName` ON `chars` (`charName`);


CREATE INDEX `IX_digimon_characterId` ON `digimon` (`characterId`);


