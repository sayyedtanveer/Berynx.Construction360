SELECT to_regclass('public.m11_receipt') AS receipt_table,
       to_regclass('public.m11_receipt_allocation') AS allocation_table,
       to_regclass('public.m11_promise_to_pay') AS promise_table,
       to_regclass('public.m11_receivable_event') AS event_table;
SELECT COUNT(*) AS invalid_draft_allocations
FROM m11_receipt r
JOIN (SELECT "TenantId","ReceiptId",SUM("Amount") AS allocated
      FROM m11_receipt_allocation GROUP BY "TenantId","ReceiptId") a
  ON a."TenantId"=r."TenantId" AND a."ReceiptId"=r."ReceiptId"
WHERE a.allocated > r."Amount";
SELECT "ModuleCode","IsCore","IsActive" FROM platform_module WHERE "ModuleCode"='M11';
SELECT "PermissionCode" FROM m02_permission_catalog WHERE "PermissionCode" LIKE 'm11.%';
