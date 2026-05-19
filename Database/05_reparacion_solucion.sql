-- Migración 05: Campo solución en reparaciones
ALTER TABLE reparaciones ADD COLUMN IF NOT EXISTS solucion TEXT;

-- Índice para búsqueda por IMEI (detección de reingresos)
CREATE INDEX IF NOT EXISTS idx_reparaciones_imei ON reparaciones (imei) WHERE imei IS NOT NULL AND imei <> '';
