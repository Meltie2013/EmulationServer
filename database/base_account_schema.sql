-- Host: 10.220.100.102:3306
-- Generation Time: May 24, 2026
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
-- Table structure for table `account`
--

CREATE TABLE `account` (
  `id` int(10) UNSIGNED NOT NULL,
  `username` varchar(32) NOT NULL DEFAULT '',
  `sha_pass_hash` varchar(40) NOT NULL DEFAULT '',
  `sessionkey` varchar(80) NOT NULL DEFAULT '',
  `v` text DEFAULT NULL,
  `s` text DEFAULT NULL,
  `email` varchar(255) NOT NULL DEFAULT '',
  `joindate` timestamp NOT NULL DEFAULT current_timestamp(),
  `last_ip` varchar(15) NOT NULL DEFAULT '0.0.0.0',
  `failed_logins` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `locked` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `last_login` timestamp NULL DEFAULT NULL,
  `active_realm_id` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `expansion` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `mutetime` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `locale` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
  `os` varchar(3) NOT NULL DEFAULT '',
  `playerBot` bit(1) NOT NULL DEFAULT b'0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `account_banned`
--

CREATE TABLE `account_banned` (
  `id` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `bandate` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `unbandate` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `bannedby` varchar(50) NOT NULL DEFAULT '',
  `banreason` varchar(255) NOT NULL DEFAULT '',
  `active` tinyint(3) UNSIGNED NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `ip_banned`
--

CREATE TABLE `ip_banned` (
  `ip` varchar(15) NOT NULL DEFAULT '0.0.0.0',
  `bandate` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `unbandate` bigint(20) UNSIGNED NOT NULL DEFAULT 0,
  `bannedby` varchar(50) NOT NULL DEFAULT '',
  `banreason` varchar(255) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `rbac_permissions`
--

CREATE TABLE `rbac_permissions` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(100) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `rbac_default_permissions`
--

CREATE TABLE `rbac_default_permissions` (
  `secId` int(10) NOT NULL DEFAULT 0,
  `permissionId` int(10) UNSIGNED NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `rbac_linked_permissions`
--

CREATE TABLE `rbac_linked_permissions` (
  `id` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `linkedId` int(10) UNSIGNED NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `rbac_account_permissions`
--

CREATE TABLE `rbac_account_permissions` (
  `accountId` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `permissionId` int(10) UNSIGNED NOT NULL DEFAULT 0,
  `granted` tinyint(1) NOT NULL DEFAULT 1,
  `realmId` int(11) NOT NULL DEFAULT -1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Base RBAC permissions.
-- Command permissions start at 200 so role/group permissions have room to grow below them.
--

INSERT INTO `rbac_permissions` (`id`, `name`) VALUES
(190, 'Administrator Permission'),
(191, 'Game Master Permission'),
(192, 'Player Permission'),
(195, 'Administrator Commands'),
(196, 'Game Master Commands'),
(197, 'Player Commands'),
(200, 'Command: account'),
(201, 'Command: account create'),
(202, 'Command: account delete'),
(203, 'Command: account remove permission'),
(204, 'Command: account set permission'),
(205, 'Command: ban'),
(206, 'Command: ban account'),
(207, 'Command: help'),
(208, 'Command: map'),
(209, 'Command: map info'),
(210, 'Command: map restart'),
(211, 'Command: map shutdown'),
(212, 'Command: map start'),
(213, 'Command: reload'),
(214, 'Command: reload rbac'),
(215, 'Command: server'),
(216, 'Command: server restart'),
(217, 'Command: server shutdown');

-- Default security-level templates.
-- Player accounts receive secId 0. Higher security templates are available for tools that assign elevated defaults later.
INSERT INTO `rbac_default_permissions` (`secId`, `permissionId`) VALUES
(0, 192),
(1, 191),
(2, 190);

-- Role inheritance and command groups.
INSERT INTO `rbac_linked_permissions` (`id`, `linkedId`) VALUES
(190, 191),
(190, 195),
(191, 192),
(191, 196),
(192, 197),
(195, 200),
(195, 201),
(195, 202),
(195, 203),
(195, 204),
(195, 205),
(195, 206),
(195, 208),
(195, 209),
(195, 210),
(195, 211),
(195, 212),
(195, 213),
(195, 214),
(195, 215),
(195, 216),
(195, 217),
(196, 207),
(196, 208),
(196, 210);

-- --------------------------------------------------------

--
-- Indexes for dumped tables
--

--
-- Indexes for table `account`
--
ALTER TABLE `account`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uk_account_username` (`username`),
  ADD KEY `idx_account_last_ip` (`last_ip`),
  ADD KEY `idx_account_active_realm_id` (`active_realm_id`);

--
-- Indexes for table `account_banned`
--
ALTER TABLE `account_banned`
  ADD PRIMARY KEY (`id`,`bandate`),
  ADD KEY `idx_account_banned_active` (`active`),
  ADD KEY `idx_account_banned_unbandate` (`unbandate`);

--
-- Indexes for table `ip_banned`
--
ALTER TABLE `ip_banned`
  ADD PRIMARY KEY (`ip`,`bandate`),
  ADD KEY `idx_ip_banned_unbandate` (`unbandate`);

--
-- Indexes for table `rbac_permissions`
--
ALTER TABLE `rbac_permissions`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uk_rbac_permissions_name` (`name`);

--
-- Indexes for table `rbac_default_permissions`
--
ALTER TABLE `rbac_default_permissions`
  ADD PRIMARY KEY (`secId`,`permissionId`),
  ADD KEY `idx_rbac_default_permission_id` (`permissionId`);

--
-- Indexes for table `rbac_linked_permissions`
--
ALTER TABLE `rbac_linked_permissions`
  ADD PRIMARY KEY (`id`,`linkedId`),
  ADD KEY `idx_rbac_linked_linked_id` (`linkedId`);

--
-- Indexes for table `rbac_account_permissions`
--
ALTER TABLE `rbac_account_permissions`
  ADD PRIMARY KEY (`accountId`,`permissionId`,`realmId`),
  ADD KEY `idx_rbac_account_permission_id` (`permissionId`),
  ADD KEY `idx_rbac_account_realm_id` (`realmId`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `account`
--
ALTER TABLE `account`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `account_banned`
--
ALTER TABLE `account_banned`
  ADD CONSTRAINT `fk_account_banned_account` FOREIGN KEY (`id`) REFERENCES `account` (`id`) ON DELETE CASCADE;

--
-- Constraints for table `rbac_default_permissions`
--
ALTER TABLE `rbac_default_permissions`
  ADD CONSTRAINT `fk_rbac_default_permissions_permission` FOREIGN KEY (`permissionId`) REFERENCES `rbac_permissions` (`id`) ON DELETE CASCADE;

--
-- Constraints for table `rbac_linked_permissions`
--
ALTER TABLE `rbac_linked_permissions`
  ADD CONSTRAINT `fk_rbac_linked_permissions_parent` FOREIGN KEY (`id`) REFERENCES `rbac_permissions` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_rbac_linked_permissions_child` FOREIGN KEY (`linkedId`) REFERENCES `rbac_permissions` (`id`) ON DELETE CASCADE;

--
-- Constraints for table `rbac_account_permissions`
--
ALTER TABLE `rbac_account_permissions`
  ADD CONSTRAINT `fk_rbac_account_permissions_account` FOREIGN KEY (`accountId`) REFERENCES `account` (`id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_rbac_account_permissions_permission` FOREIGN KEY (`permissionId`) REFERENCES `rbac_permissions` (`id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
