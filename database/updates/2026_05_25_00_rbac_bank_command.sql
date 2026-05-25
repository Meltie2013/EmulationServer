-- Adds the .bank command permission without renumbering the existing command ids.
-- This command is intended for administrator/game-master testing of inventory,
-- bag, and bank-slot update fields while banker creatures are not implemented yet.

START TRANSACTION;

INSERT INTO `rbac_permissions` (`id`, `name`) VALUES
(218, 'Command: bank')
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`);

INSERT IGNORE INTO `rbac_linked_permissions` (`id`, `linkedId`) VALUES
(195, 218),
(196, 218);

COMMIT;
