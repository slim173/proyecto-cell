-- ============================================================
-- Migración: permite facturas manuales (sin venta ni reparación)
-- Ejecutar una sola vez contra db_cell:
--   psql -U postgres -d db_cell -f 07_fix_facturas_manual.sql
-- ============================================================

ALTER TABLE facturas DROP CONSTRAINT IF EXISTS chk_facturas_origen;

ALTER TABLE facturas ADD CONSTRAINT chk_facturas_origen CHECK (
    -- Permite: venta, reparación, o ninguno (factura manual)
    -- Prohíbe: ambos a la vez
    venta_id IS NULL OR reparacion_id IS NULL
);
