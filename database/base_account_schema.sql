-- Host: 10.220.100.102:3306
-- Generation Time: May 23, 2026 at 12:13 PM
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
  `gmlevel` tinyint(3) UNSIGNED NOT NULL DEFAULT 0,
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
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
