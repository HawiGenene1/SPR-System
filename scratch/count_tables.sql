SELECT 'Roles' AS tbl, count(*) FROM "Roles" UNION ALL
SELECT 'Users', count(*) FROM "Users" UNION ALL
SELECT 'Vehicles', count(*) FROM "Vehicles" UNION ALL
SELECT 'ParkingAreas', count(*) FROM "ParkingAreas" UNION ALL
SELECT 'ParkingSlots', count(*) FROM "ParkingSlots" UNION ALL
SELECT 'Reservations', count(*) FROM "Reservations" UNION ALL
SELECT 'Payments', count(*) FROM "Payments" UNION ALL
SELECT 'QRCodes', count(*) FROM "QRCodes" UNION ALL
SELECT 'ParkingHistories', count(*) FROM "ParkingHistories" UNION ALL
SELECT 'AuditLogs', count(*) FROM "AuditLogs" UNION ALL
SELECT 'Notifications', count(*) FROM "Notifications" UNION ALL
SELECT '__EFMigrationsHistory', count(*) FROM "__EFMigrationsHistory";
