-- Adds the in-game RBAC command permissions and command-group links.
-- Command ids start at 200 and are ordered alphabetically by command syntax.
-- Player accounts intentionally receive no command permissions.
-- Game Masters receive help plus map/map restart so they can perform the requested map reset workflow.

START TRANSACTION;

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
(217, 'Command: server shutdown')
ON DUPLICATE KEY UPDATE
    `name` = VALUES(`name`);

INSERT IGNORE INTO `rbac_default_permissions` (`secId`, `permissionId`) VALUES
(0, 192),
(1, 191),
(2, 190);

-- Clear previous command links before applying the new command tree.
DELETE FROM `rbac_linked_permissions`
WHERE `id` IN (195, 196, 197)
   OR `linkedId` BETWEEN 200 AND 217;

INSERT IGNORE INTO `rbac_linked_permissions` (`id`, `linkedId`) VALUES
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

COMMIT;
