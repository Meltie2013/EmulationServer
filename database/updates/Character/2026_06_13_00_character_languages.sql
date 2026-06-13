--
-- Character language knowledge backfill.
-- Ensures existing characters have the Vanilla language spells and language skill rows
-- required for the chat language dropdown and same-faction comprehension.
--

CREATE TABLE IF NOT EXISTS `character_skills` (
  `guid` int(11) UNSIGNED NOT NULL COMMENT 'Global Unique Identifier.',
  `skill` mediumint(9) UNSIGNED NOT NULL COMMENT 'Skill identifier.',
  `value` mediumint(9) UNSIGNED NOT NULL COMMENT 'Current skill value.',
  `max` mediumint(9) UNSIGNED NOT NULL COMMENT 'Maximum skill value.',
  PRIMARY KEY (`guid`, `skill`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Skill System' ROW_FORMAT=DYNAMIC;

-- Base faction languages.
INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 98, 300, 300 FROM `characters` WHERE `race` IN (1, 3, 4, 7)
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 109, 300, 300 FROM `characters` WHERE `race` IN (2, 5, 6, 8)
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

-- Race languages.
INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 111, 300, 300 FROM `characters` WHERE `race` = 3
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 113, 300, 300 FROM `characters` WHERE `race` = 4
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 673, 300, 300 FROM `characters` WHERE `race` = 5
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 115, 300, 300 FROM `characters` WHERE `race` = 6
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 313, 300, 300 FROM `characters` WHERE `race` = 7
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

INSERT INTO `character_skills` (`guid`, `skill`, `value`, `max`)
SELECT `guid`, 315, 300, 300 FROM `characters` WHERE `race` = 8
ON DUPLICATE KEY UPDATE
  `value` = GREATEST(`value`, VALUES(`value`)),
  `max` = GREATEST(`max`, VALUES(`max`));

-- Base faction language spells.
INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 668, 1, 0 FROM `characters` WHERE `race` IN (1, 3, 4, 7);

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 669, 1, 0 FROM `characters` WHERE `race` IN (2, 5, 6, 8);

-- Race language spells.
INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 672, 1, 0 FROM `characters` WHERE `race` = 3;

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 671, 1, 0 FROM `characters` WHERE `race` = 4;

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 17737, 1, 0 FROM `characters` WHERE `race` = 5;

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 670, 1, 0 FROM `characters` WHERE `race` = 6;

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 7340, 1, 0 FROM `characters` WHERE `race` = 7;

INSERT IGNORE INTO `character_spell` (`guid`, `spell`, `active`, `disabled`)
SELECT `guid`, 7341, 1, 0 FROM `characters` WHERE `race` = 8;
