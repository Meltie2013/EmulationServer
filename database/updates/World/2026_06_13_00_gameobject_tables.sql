-- Adds Mangos Zero-compatible game object template/spawn tables.
-- EmulationServer extends gameobject with zoneId and areaId for startup, map-info, and reload diagnostics.

CREATE TABLE IF NOT EXISTS `gameobject_template` (
  `entry` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Id of the gameobject template.',
  `type` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'GameObject type.',
  `displayId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Display model identifier for the object.',
  `name` varchar(100) NOT NULL DEFAULT '' COMMENT 'Object name.',
  `faction` smallint(5) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Object faction, if any.',
  `flags` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'GameObject flags.',
  `size` float NOT NULL DEFAULT 1 COMMENT 'Object scale.',
  `data0` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data1` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data2` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data3` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data4` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data5` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data6` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data7` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data8` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data9` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data10` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data11` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data12` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data13` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data14` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data15` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data16` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data17` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data18` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data19` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data20` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data21` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data22` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `data23` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `mingold` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  `maxgold` mediumint(8) UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`entry`),
  KEY `idx_type` (`type`),
  KEY `idx_displayId` (`displayId`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC COMMENT='Gameobject System';

CREATE TABLE IF NOT EXISTS `gameobject` (
  `guid` int(10) UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Unique game object spawn identifier.',
  `id` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'GameObject ID (See gameobject_template.entry).',
  `map` smallint(5) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Map id where the game object is located.',
  `zoneId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Cached zone id for startup/map diagnostics.',
  `areaId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Cached area id for startup/map diagnostics.',
  `position_x` float NOT NULL DEFAULT 0 COMMENT 'X position.',
  `position_y` float NOT NULL DEFAULT 0 COMMENT 'Y position.',
  `position_z` float NOT NULL DEFAULT 0 COMMENT 'Z position.',
  `orientation` float NOT NULL DEFAULT 0 COMMENT 'Orientation.',
  `rotation0` float NOT NULL DEFAULT 0 COMMENT 'Rotation axis value.',
  `rotation1` float NOT NULL DEFAULT 0 COMMENT 'Rotation axis value.',
  `rotation2` float NOT NULL DEFAULT 0 COMMENT 'Rotation axis value.',
  `rotation3` float NOT NULL DEFAULT 0 COMMENT 'Rotation axis value.',
  `spawntimesecs` int(11) NOT NULL DEFAULT 0 COMMENT 'Respawn time in seconds.',
  `animprogress` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Animation progress.',
  `state` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Spawn state.',
  PRIMARY KEY (`guid`),
  KEY `idx_map` (`map`),
  KEY `idx_id` (`id`),
  KEY `idx_zone_area` (`map`,`zoneId`,`areaId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci ROW_FORMAT=DYNAMIC COMMENT='Gameobject System';

SET @add_zoneId_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `gameobject` ADD COLUMN `zoneId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT \'Cached zone id for startup/map diagnostics.\' AFTER `map`',
    'SELECT 1'
  )
  FROM `information_schema`.`COLUMNS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'gameobject'
    AND `COLUMN_NAME` = 'zoneId'
);
PREPARE add_zoneId_stmt FROM @add_zoneId_sql;
EXECUTE add_zoneId_stmt;
DEALLOCATE PREPARE add_zoneId_stmt;

SET @add_areaId_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `gameobject` ADD COLUMN `areaId` mediumint(8) UNSIGNED NOT NULL DEFAULT 0 COMMENT \'Cached area id for startup/map diagnostics.\' AFTER `zoneId`',
    'SELECT 1'
  )
  FROM `information_schema`.`COLUMNS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'gameobject'
    AND `COLUMN_NAME` = 'areaId'
);
PREPARE add_areaId_stmt FROM @add_areaId_sql;
EXECUTE add_areaId_stmt;
DEALLOCATE PREPARE add_areaId_stmt;

SET @add_go_zone_area_index_sql := (
  SELECT IF(
    COUNT(*) = 0,
    'ALTER TABLE `gameobject` ADD KEY `idx_zone_area` (`map`,`zoneId`,`areaId`)',
    'SELECT 1'
  )
  FROM `information_schema`.`STATISTICS`
  WHERE `TABLE_SCHEMA` = DATABASE()
    AND `TABLE_NAME` = 'gameobject'
    AND `INDEX_NAME` = 'idx_zone_area'
);
PREPARE add_go_zone_area_index_stmt FROM @add_go_zone_area_index_sql;
EXECUTE add_go_zone_area_index_stmt;
DEALLOCATE PREPARE add_go_zone_area_index_stmt;
