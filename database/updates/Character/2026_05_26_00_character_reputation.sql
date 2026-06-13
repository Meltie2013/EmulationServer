--
-- Add the character reputation table used by the WorldServer login reputation bootstrap.
--

CREATE TABLE IF NOT EXISTS `character_reputation` (
  `guid` int(11) unsigned NOT NULL DEFAULT 0 COMMENT 'The GUID of the character. (See character.guid).',
  `faction` int(11) unsigned NOT NULL DEFAULT 0 COMMENT 'The faction ID that the character has the given reputation in (See Faction.dbc).',
  `standing` int(11) NOT NULL DEFAULT 0 COMMENT 'The current reputation value that the character has.',
  `flags` int(11) NOT NULL DEFAULT 0 COMMENT 'This field is a bitmask containing flags that apply to the faction.',
  PRIMARY KEY (`guid`,`faction`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC COMMENT='Player System';
