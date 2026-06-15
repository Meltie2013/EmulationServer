-- Adds Mangos Zero-compatible creature template/spawn tables.
-- EmulationServer extends creature with zoneId and areaId for startup, map-info, and reload diagnostics.

-- --------------------------------------------------------
--
-- Table structure for table `creature_template`
--

CREATE TABLE IF NOT EXISTS `creature_template` (
  `Entry` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `Name` varchar(100) NOT NULL DEFAULT '',
  `SubName` varchar(100) DEFAULT NULL,
  `MinLevel` tinyint(3) UNSIGNED NOT NULL DEFAULT 1,
  `MaxLevel` tinyint(3) UNSIGNED NOT NULL DEFAULT 1,
  `ModelId1` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `ModelId2` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `ModelId3` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `ModelId4` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `FactionAlliance` smallint(5) UNSIGNED NOT NULL DEFAULT 0,
  `FactionHorde` smallint(5) UNSIGNED NOT NULL DEFAULT 0,
  `Scale` float NOT NULL DEFAULT 1,
  `Family` tinyint(4) NOT NULL DEFAULT 0,
  `CreatureType` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `InhabitType` tinyint(3) UNSIGNED NOT NULL DEFAULT 3,
  `RegenerateStats` tinyint(3) UNSIGNED DEFAULT NULL,
  `RacialLeader` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `NpcFlags` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `UnitFlags` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `DynamicFlags` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `ExtraFlags` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `CreatureTypeFlags` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `SpeedWalk` float NOT NULL DEFAULT 1,
  `SpeedRun` float NOT NULL DEFAULT 1.14286,
  `UnitClass` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `Rank` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `HealthMultiplier` float NOT NULL DEFAULT 1,
  `PowerMultiplier` float NOT NULL DEFAULT 1,
  `DamageMultiplier` float NOT NULL DEFAULT 1,
  `DamageVariance` float NOT NULL DEFAULT 1,
  `ArmorMultiplier` float NOT NULL DEFAULT 1,
  `ExperienceMultiplier` float NOT NULL DEFAULT 1,
  `MinLevelHealth` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `MaxLevelHealth` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `MinLevelMana` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `MaxLevelMana` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `MinMeleeDmg` float NOT NULL DEFAULT 0,
  `MaxMeleeDmg` float NOT NULL DEFAULT 0,
  `MinRangedDmg` float NOT NULL DEFAULT 0,
  `MaxRangedDmg` float NOT NULL DEFAULT 0,
  `Armor` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `MeleeAttackPower` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `RangedAttackPower` smallint(5) UNSIGNED NOT NULL DEFAULT 0,
  `MeleeBaseAttackTime` int(10) UNSIGNED NOT NULL DEFAULT 2000,
  `RangedBaseAttackTime` int(10) UNSIGNED NOT NULL DEFAULT 2000,
  `DamageSchool` tinyint(4) NOT NULL DEFAULT 0,
  `MinLootGold` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `MaxLootGold` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `LootId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `PickpocketLootId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `SkinningLootId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `KillCredit1` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `KillCredit2` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `MechanicImmuneMask` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `SchoolImmuneMask` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `ResistanceHoly` smallint(5) NOT NULL DEFAULT 0,
  `ResistanceFire` smallint(5) NOT NULL DEFAULT 0,
  `ResistanceNature` smallint(5) NOT NULL DEFAULT 0,
  `ResistanceFrost` smallint(5) NOT NULL DEFAULT 0,
  `ResistanceShadow` smallint(5) NOT NULL DEFAULT 0,
  `ResistanceArcane` smallint(5) NOT NULL DEFAULT 0,
  `SpellListId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `PetSpellDataId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `MovementType` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `TrainerType` tinyint(4) NOT NULL DEFAULT 0,
  `TrainerSpell` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `TrainerClass` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `TrainerRace` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `TrainerTemplateId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `VendorTemplateId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `GossipMenuId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `EquipmentTemplateId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `Civilian` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `AIName` char(64) DEFAULT '',
  PRIMARY KEY (`Entry`),
  KEY `idx_name` (`Name`),
  KEY `idx_model1` (`ModelId1`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC COMMENT='Creature System';

-- --------------------------------------------------------
--
-- Table structure for table `creature`
--

CREATE TABLE IF NOT EXISTS `creature` (
  `guid` int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `id` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `map` smallint(5) UNSIGNED NOT NULL DEFAULT 0,
  `zoneId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Cached zone id for startup/map diagnostics.',
  `areaId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Cached area id for startup/map diagnostics.',
  `modelid` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `equipment_id` mediumint(9) NOT NULL DEFAULT 0,
  `position_x` float NOT NULL DEFAULT 0,
  `position_y` float NOT NULL DEFAULT 0,
  `position_z` float NOT NULL DEFAULT 0,
  `orientation` float NOT NULL DEFAULT 0,
  `spawntimesecs` int(10) UNSIGNED NOT NULL DEFAULT 120,
  `spawndist` float NOT NULL DEFAULT 5,
  `currentwaypoint` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `curhealth` int(10) UNSIGNED NOT NULL DEFAULT 1,
  `curmana` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `DeathState` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `MovementType` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`guid`),
  KEY `idx_map` (`map`),
  KEY `idx_id` (`id`),
  KEY `idx_zone_area` (`map`,`zoneId`,`areaId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC COMMENT='Creature System';


SET @add_creature_zoneId_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `creature` ADD COLUMN `zoneId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT \'Cached zone id for startup/map diagnostics.\' AFTER `map`',
    'SELECT 1'
  )
  FROM `information_schema`.`COLUMNS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'creature'
    AND `COLUMN_NAME` = 'zoneId'
);
PREPARE add_creature_zoneId_stmt FROM @add_creature_zoneId_sql;
EXECUTE add_creature_zoneId_stmt;
DEALLOCATE PREPARE add_creature_zoneId_stmt;

SET @add_creature_areaId_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `creature` ADD COLUMN `areaId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT \'Cached area id for startup/map diagnostics.\' AFTER `zoneId`',
    'SELECT 1'
  )
  FROM `information_schema`.`COLUMNS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'creature'
    AND `COLUMN_NAME` = 'areaId'
);
PREPARE add_creature_areaId_stmt FROM @add_creature_areaId_sql;
EXECUTE add_creature_areaId_stmt;
DEALLOCATE PREPARE add_creature_areaId_stmt;

SET @add_creature_zone_area_index_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `creature` ADD KEY `idx_zone_area` (`map`,`zoneId`,`areaId`)',
    'SELECT 1'
  )
  FROM `information_schema`.`STATISTICS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'creature'
    AND `INDEX_NAME` = 'idx_zone_area'
);
PREPARE add_creature_zone_area_index_stmt FROM @add_creature_zone_area_index_sql;
EXECUTE add_creature_zone_area_index_stmt;
DEALLOCATE PREPARE add_creature_zone_area_index_stmt;
