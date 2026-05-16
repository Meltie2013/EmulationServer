
-- EmulationServer auth database schema
-- Matches the current RealmServer account/auth repository implementation.
-- Realms are intentionally configuration/internal-packet driven, so no realmlist table is included here.

CREATE TABLE IF NOT EXISTS `account` (
    `id` INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `username` VARCHAR(32) NOT NULL DEFAULT '',
    `sha_pass_hash` VARCHAR(40) NOT NULL DEFAULT '',
    `gmlevel` TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `sessionkey` VARCHAR(80) NOT NULL DEFAULT '',
    `v` TEXT NULL,
    `s` TEXT NULL,
    `email` VARCHAR(255) NOT NULL DEFAULT '',
    `joindate` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_ip` VARCHAR(15) NOT NULL DEFAULT '0.0.0.0',
    `failed_logins` INT UNSIGNED NOT NULL DEFAULT 0,
    `locked` TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `last_login` TIMESTAMP NULL DEFAULT NULL,
    `active_realm_id` INT UNSIGNED NOT NULL DEFAULT 0,
    `expansion` TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `mutetime` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `locale` TINYINT UNSIGNED NOT NULL DEFAULT 0,
    `os` VARCHAR(3) NOT NULL DEFAULT '',
    `playerBot` BIT(1) NOT NULL DEFAULT b'0',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uk_account_username` (`username`),
    KEY `idx_account_last_ip` (`last_ip`),
    KEY `idx_account_active_realm_id` (`active_realm_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `account_banned` (
    `id` INT UNSIGNED NOT NULL DEFAULT 0,
    `bandate` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `unbandate` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `bannedby` VARCHAR(50) NOT NULL DEFAULT '',
    `banreason` VARCHAR(255) NOT NULL DEFAULT '',
    `active` TINYINT UNSIGNED NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`, `bandate`),
    KEY `idx_account_banned_active` (`active`),
    KEY `idx_account_banned_unbandate` (`unbandate`),
    CONSTRAINT `fk_account_banned_account`
        FOREIGN KEY (`id`) REFERENCES `account` (`id`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `ip_banned` (
    `ip` VARCHAR(15) NOT NULL DEFAULT '0.0.0.0',
    `bandate` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `unbandate` BIGINT UNSIGNED NOT NULL DEFAULT 0,
    `bannedby` VARCHAR(50) NOT NULL DEFAULT '',
    `banreason` VARCHAR(255) NOT NULL DEFAULT '',
    PRIMARY KEY (`ip`, `bandate`),
    KEY `idx_ip_banned_unbandate` (`unbandate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Optional helper view for quick account inspection while testing.
CREATE OR REPLACE VIEW `account_login_status` AS
SELECT
    `account`.`id`,
    `account`.`username`,
    `account`.`gmlevel`,
    `account`.`last_ip`,
    `account`.`last_login`,
    `account`.`failed_logins`,
    `account`.`locked`,
    CASE
        WHEN `account_banned`.`id` IS NULL THEN 0
        ELSE 1
    END AS `is_banned`
FROM `account`
LEFT JOIN `account_banned`
    ON `account_banned`.`id` = `account`.`id`
   AND `account_banned`.`active` = 1
   AND (`account_banned`.`unbandate` = `account_banned`.`bandate` OR `account_banned`.`unbandate` > UNIX_TIMESTAMP());

-- Account creation is expected to be done through RealmServer console commands:
--   account add <username> <password> [email] [gmlevel]
--   account remove <username>
--
-- Example after starting RealmServer:
--   account add test test
