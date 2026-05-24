-- Host: 10.220.100.102:3306
-- Generation Time: May 23, 2026 at 12:15 PM
-- Server version: 10.6.22-MariaDB-0ubuntu0.22.04.1
-- PHP Version: 8.1.2-1ubuntu2.23

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

-- --------------------------------------------------------

--
-- Table structure for table `characters`
--

CREATE TABLE `characters` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The character global unique identifier.',
  `account` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The account ID in which this character resides (See account.id) in the realm db.',
  `name` varchar(12) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL DEFAULT '' COMMENT 'The name of the character.',
  `race` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The race of the character.',
  `class` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The Class Id of the character (See chrclasses.dbc).',
  `gender` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The Sex/Gender of the character.',
  `level` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The current level of the designated player.',
  `xp` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The total amount of xp that the signified player has.',
  `money` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This is the amount of copper the character possesses.',
  `playerBytes` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This defines the physical attributes of the character.',
  `playerBytes2` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This defines the facial hair physical attribute of the character.',
  `playerFlags` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'These are the player flags, such as GM mode on or off.',
  `position_x` float NOT NULL DEFAULT 0 COMMENT 'The x position of the character''s location.',
  `position_y` float NOT NULL DEFAULT 0 COMMENT 'The y position of the character''s location.',
  `position_z` float NOT NULL DEFAULT 0 COMMENT 'The z position of the character''s location.',
  `map` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The map ID the character is in (See maps.dbc)',
  `orientation` float NOT NULL DEFAULT 0 COMMENT 'The orientation the character is facing. (North = 0.0, South = 3.14159)',
  `taximask` longtext CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL,
  `online` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Records whether the character is online (1) or offline (0).',
  `cinematic` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Boolean 1 or 0 controlling whether the start cinematic has been shown or not.',
  `totaltime` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The total time that the character has been active in the world.',
  `leveltime` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The total time the character has spent in the world at the current level.',
  `logout_time` bigint(20) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The time when the character last logged out, measured in Unix time.',
  `is_logout_resting` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Boolean 1 or 0 controlling if the character is currently in a resting zone.',
  `rest_bonus` float NOT NULL DEFAULT 0 COMMENT 'This is the rest bonus for the character.',
  `resettalents_cost` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The cost for the character to reset its talents, measured in copper.',
  `resettalents_time` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `trans_x` float NOT NULL DEFAULT 0 COMMENT 'The X coordinate of the character on the transport it is travelling on.',
  `trans_y` float NOT NULL DEFAULT 0 COMMENT 'The Y coordinate of the character on the transport it is travelling on.',
  `trans_z` float NOT NULL DEFAULT 0 COMMENT 'The Z coordinate of the character on the transport it is travelling on.',
  `trans_o` float NOT NULL DEFAULT 0 COMMENT 'The orientation of the character on the transport it is travelling on.',
  `transguid` bigint(20) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This is the transprt the character is currently travelling on.',
  `extra_flags` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'These flags control certain player specific attributes, mostly GM features.',
  `stable_slots` tinyint(1) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The number of stable slots the player has available. Maximum is 2.',
  `at_login` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The status of the character.',
  `zone` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The zone ID the character is in.',
  `death_expire_time` bigint(20) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Time when a character can be resurrected.',
  `taxi_path` text CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'Stores the players current taxi path (TaxiPath.dbc) if logged off while on one.',
  `honor_highest_rank` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This shows the highest honor rank ever earned.',
  `honor_standing` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `stored_honor_rating` float NOT NULL DEFAULT 0 COMMENT 'This is the current amount of honor points.',
  `stored_dishonorable_kills` int(11) NOT NULL DEFAULT 0 COMMENT 'The is the total dishonorable kills made by the character.',
  `stored_honorable_kills` int(11) NOT NULL DEFAULT 0 COMMENT 'The is the total honorable kills made by the character.',
  `watchedFaction` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The faction whose rep is being followed on the experience bar.',
  `drunk` smallint(5) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This represents the alcoholic strength of the drink.',
  `health` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The character''s health when logging out.',
  `power1` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'If a mana user, then this is a character''s mana level when logging out.',
  `power2` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'If a warrior, then this is a character''s rage level when logging out.',
  `power3` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This is a hunter pet''s focus level.',
  `power4` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'If a rogue, then this is a character''s energy level when logging out.',
  `power5` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This is the current active pet''s happiness level.',
  `exploredZones` longtext CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'This is a record of all areas discovered by the character.',
  `equipmentCache` longtext CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'This is a record of all items that are currently equipped.',
  `ammoId` int(10) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This is the item_template Entry ID of the ammo currently equipped.',
  `actionBars` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'This represents which action bars are currently active.',
  `deleteInfos_Account` int(11) UNSIGNED DEFAULT NULL COMMENT 'This is the account number from the characters table.',
  `deleteInfos_Name` varchar(12) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'The is the name of the character being deleted.',
  `deleteDate` bigint(20) UNSIGNED DEFAULT NULL COMMENT 'This is the date the character was deleted,',
  `createdDate` bigint(20) NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_homebind`
--

CREATE TABLE `character_homebind` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The GUID (Global Unique Identifier) of the character. (See character.guid).',
  `map` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The Map Identifier where the character gets teleported to. (See Maps.dbc).',
  `zone` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The Zone Identifier where the character gets teleported to.',
  `position_x` float NOT NULL DEFAULT 0 COMMENT 'The x position where the character gets teleported to.',
  `position_y` float NOT NULL DEFAULT 0 COMMENT 'The y position where the character gets teleported to.',
  `position_z` float NOT NULL DEFAULT 0 COMMENT 'The z position where the character gets teleported to.'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_inventory`
--

CREATE TABLE `character_inventory` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The GUID (Global Unique Identifier) of the character. (See character.guid).',
  `bag` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'If it isn''t 0, then it is the bag''s item GUID (Global Unique Identifier).',
  `slot` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The slot is the slot in the bag where the item is.',
  `item` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The item''s GUID. (See item_instance.guid).',
  `item_template` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The item''s template entry (Item Identifier). (See item_template.entry).'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `item_instance`
--

CREATE TABLE `item_instance` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The GUID of the item. This number is unique for each item instance.',
  `owner_guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'The GUID of the character who has ownership of this item. (See character.guid)',
  `data` longtext CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'Much like the playerbytes fields in the characters table.',
  `text` longtext CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci DEFAULT NULL COMMENT 'The Name of the Item'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Item System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_action`
--

CREATE TABLE `character_action` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Global Unique Identifier.',
  `button` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Action bar button index.',
  `action` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Spell, item, macro, or action identifier.',
  `type` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Action type. Vanilla uses 0 for spell, 64 for macro, and 128 for item.',
  PRIMARY KEY (`guid`,`button`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Action Bar System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_aura`
--

CREATE TABLE `character_aura` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Global Unique Identifier.',
  `caster_guid` bigint(20) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Full caster Global Unique Identifier.',
  `item_guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Item instance that applied the aura, if any.',
  `spell` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Spell identifier.',
  `effect_mask` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Active effect mask.',
  `recalculate_mask` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Effects requiring recalculation.',
  `stackcount` tinyint(3) UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Aura stack count.',
  `remaincharges` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Remaining charges.',
  `basepoints0` int(11) NOT NULL DEFAULT 0,
  `basepoints1` int(11) NOT NULL DEFAULT 0,
  `basepoints2` int(11) NOT NULL DEFAULT 0,
  `periodictime0` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `periodictime1` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `periodictime2` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `maxduration` int(11) NOT NULL DEFAULT 0,
  `remaintime` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`guid`,`caster_guid`,`item_guid`,`spell`,`effect_mask`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Aura System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_reputation`
--

CREATE TABLE `character_reputation` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Global Unique Identifier.',
  `faction` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Faction identifier.',
  `standing` int(11) NOT NULL DEFAULT 0 COMMENT 'Reputation standing value.',
  `flags` int(11) NOT NULL DEFAULT 0 COMMENT 'Reputation flags.',
  PRIMARY KEY (`guid`,`faction`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Reputation System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_skills`
--

CREATE TABLE `character_skills` (
  `guid` int(11) UNSIGNED NOT NULL COMMENT 'Global Unique Identifier.',
  `skill` mediumint(9) UNSIGNED NOT NULL COMMENT 'Skill identifier.',
  `value` mediumint(9) UNSIGNED NOT NULL COMMENT 'Current skill value.',
  `max` mediumint(9) UNSIGNED NOT NULL COMMENT 'Maximum skill value.',
  PRIMARY KEY (`guid`,`skill`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Skill System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_spell`
--

CREATE TABLE `character_spell` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Global Unique Identifier.',
  `spell` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Spell identifier.',
  `active` tinyint(3) UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Whether the spell is active.',
  `disabled` tinyint(3) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Whether the spell is disabled.',
  PRIMARY KEY (`guid`,`spell`),
  KEY `idx_spell` (`spell`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Spell System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_stats`
--

CREATE TABLE `character_stats` (
  `guid` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Global Unique Identifier, low part.',
  `maxhealth` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower1` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower2` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower3` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower4` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower5` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower6` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `maxpower7` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `strength` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `agility` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `stamina` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `intellect` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `spirit` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `armor` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resHoly` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resFire` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resNature` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resFrost` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resShadow` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `resArcane` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `blockPct` float NOT NULL DEFAULT 0,
  `dodgePct` float NOT NULL DEFAULT 0,
  `parryPct` float NOT NULL DEFAULT 0,
  `critPct` float NOT NULL DEFAULT 0,
  `rangedCritPct` float NOT NULL DEFAULT 0,
  `attackPower` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `rangedAttackPower` int(10) UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`guid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Stats System' ROW_FORMAT=DYNAMIC;

-- --------------------------------------------------------

--
-- Table structure for table `character_tutorial`
--

CREATE TABLE `character_tutorial` (
  `account` int(11) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Realm account identifier.',
  `tut0` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut1` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut2` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut3` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut4` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut5` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut6` int(11) UNSIGNED NOT NULL DEFAULT 0,
  `tut7` int(11) UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (`account`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Player Tutorial System' ROW_FORMAT=DYNAMIC;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `characters`
--
ALTER TABLE `characters`
  ADD PRIMARY KEY (`guid`),
  ADD KEY `idx_account` (`account`),
  ADD KEY `idx_online` (`online`),
  ADD KEY `idx_name` (`name`);

--
-- Indexes for table `character_homebind`
--
ALTER TABLE `character_homebind`
  ADD PRIMARY KEY (`guid`);

--
-- Indexes for table `character_inventory`
--
ALTER TABLE `character_inventory`
  ADD PRIMARY KEY (`item`),
  ADD KEY `idx_guid` (`guid`);

--
-- Indexes for table `item_instance`
--
ALTER TABLE `item_instance`
  ADD PRIMARY KEY (`guid`),
  ADD KEY `idx_owner_guid` (`owner_guid`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
