-- 1. Check all Foreign Keys in the database
SELECT
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
ORDER BY tc.table_name, kcu.column_name;

-- 2. End-to-end joined record showing customer -> vehicle -> area -> slot -> reservation -> payment -> qr -> history
SELECT 
    u."FullName" AS customer,
    v."LicensePlate" AS plate,
    pa."Name" AS facility,
    ps."SlotNumber" AS slot,
    r."ReservationNumber" AS res_num,
    r."TotalPrice" AS price,
    r."Status" AS res_status,
    p."Amount" AS paid_amount,
    p."Status" AS payment_status,
    qr."Code" AS qr_token,
    ph."CheckInTime" AS entry_time,
    ph."CheckOutTime" AS exit_time,
    att_in."FullName" AS entry_attendant,
    att_out."FullName" AS exit_attendant
FROM "Reservations" r
JOIN "Users" u ON r."UserId" = u."Id"
JOIN "Vehicles" v ON r."VehicleId" = v."Id"
JOIN "ParkingSlots" ps ON r."ParkingSlotId" = ps."Id"
JOIN "ParkingAreas" pa ON ps."ParkingAreaId" = pa."Id"
LEFT JOIN "Payments" p ON p."ReservationId" = r."Id"
LEFT JOIN "QRCodes" qr ON qr."ReservationId" = r."Id"
LEFT JOIN "ParkingHistories" ph ON ph."ReservationId" = r."Id"
LEFT JOIN "Users" att_in ON ph."AttendantCheckInId" = att_in."Id"
LEFT JOIN "Users" att_out ON ph."AttendantCheckOutId" = att_out."Id"
WHERE ph."Id" IS NOT NULL
ORDER BY ph."CheckInTime" DESC
LIMIT 3;
