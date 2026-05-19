-- ============================================================
-- SCRIPT DE VERIFICACIÓN
-- Ejecutar después de 01_schema.sql para confirmar que todo
-- quedó correctamente instalado.
-- ============================================================

-- 1. Verificar tablas creadas
SELECT
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns
     WHERE table_schema = 'public'
       AND columns.table_name = t.table_name) AS columnas
FROM information_schema.tables t
WHERE table_schema = 'public'
  AND table_type = 'BASE TABLE'
ORDER BY table_name;

-- 2. Verificar vistas
SELECT viewname FROM pg_views WHERE schemaname = 'public' ORDER BY viewname;

-- 3. Verificar funciones
SELECT proname, pronargs FROM pg_proc
WHERE pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'public')
ORDER BY proname;

-- 4. Verificar datos semilla
SELECT 'configuracion'           AS tabla, COUNT(*) AS registros FROM configuracion
UNION ALL
SELECT 'categorias',             COUNT(*) FROM categorias
UNION ALL
SELECT 'proveedores',            COUNT(*) FROM proveedores
UNION ALL
SELECT 'productos',              COUNT(*) FROM productos
UNION ALL
SELECT 'clientes',               COUNT(*) FROM clientes
UNION ALL
SELECT 'ventas',                 COUNT(*) FROM ventas
UNION ALL
SELECT 'reparaciones',           COUNT(*) FROM reparaciones
UNION ALL
SELECT 'inventario_movimientos', COUNT(*) FROM inventario_movimientos
UNION ALL
SELECT 'email_logs',             COUNT(*) FROM email_logs
UNION ALL
SELECT 'facturas',               COUNT(*) FROM facturas
ORDER BY tabla;

-- 5. Probar función de numeración
SELECT generar_numero_venta()       AS prueba_venta;
SELECT generar_numero_factura()     AS prueba_factura;
SELECT generar_numero_reparacion()  AS prueba_reparacion;
SELECT generar_numero_compra()      AS prueba_compra;

-- 6. Verificar productos con stock bajo
SELECT * FROM v_productos_stock_bajo;

-- ============================================================
-- RESULTADO ESPERADO:
-- - 14 tablas creadas (sin SERIAL en ninguna)
-- - 4 vistas creadas (v_ventas, v_reparaciones, v_productos_stock_bajo, v_kardex, etc.)
-- - 4 funciones + get_next_numero
-- - Datos: 18 configs, 8 categorías, 2 proveedores, 10 productos, 3 clientes
-- ============================================================
