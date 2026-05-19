-- ============================================================
-- SISTEMA ERP - CELL SHOP (ESPAÑA)
-- Base de datos: db_cell
-- Motor: PostgreSQL 18.3
-- Autor: CellSolution
-- Fecha: 2024
-- ============================================================
-- INSTRUCCIONES:
--   Conectarse a db_cell y ejecutar este script completo.
--   psql -U postgres -d db_cell -f 01_schema.sql
-- ============================================================

-- Asegurar extensión para UUID si se necesita en futuro
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================
-- 1. CATEGORIAS
-- ============================================================
CREATE TABLE IF NOT EXISTS categorias (
    id          INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre      VARCHAR(100) NOT NULL,
    descripcion TEXT,
    activo      BOOLEAN     NOT NULL DEFAULT true,
    fecha_creacion TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 2. CLIENTES
-- ============================================================
CREATE TABLE IF NOT EXISTS clientes (
    id             INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre         VARCHAR(150) NOT NULL,
    apellidos      VARCHAR(150),
    email          VARCHAR(200) NOT NULL,
    telefono       VARCHAR(20),
    direccion      TEXT,
    ciudad         VARCHAR(100),
    codigo_postal  VARCHAR(10),
    nif            VARCHAR(20),
    activo         BOOLEAN      NOT NULL DEFAULT true,
    observaciones  TEXT,
    fecha_creacion TIMESTAMP    NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP,
    CONSTRAINT uq_clientes_email UNIQUE (email)
);

CREATE INDEX IF NOT EXISTS idx_clientes_email    ON clientes (email);
CREATE INDEX IF NOT EXISTS idx_clientes_nif      ON clientes (nif);
CREATE INDEX IF NOT EXISTS idx_clientes_nombre   ON clientes (nombre, apellidos);
CREATE INDEX IF NOT EXISTS idx_clientes_activo   ON clientes (activo);

-- ============================================================
-- 3. PROVEEDORES
-- ============================================================
CREATE TABLE IF NOT EXISTS proveedores (
    id             INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre         VARCHAR(150) NOT NULL,
    email          VARCHAR(200),
    telefono       VARCHAR(20),
    direccion      TEXT,
    ciudad         VARCHAR(100),
    codigo_postal  VARCHAR(10),
    cif            VARCHAR(20),
    activo         BOOLEAN      NOT NULL DEFAULT true,
    observaciones  TEXT,
    fecha_creacion TIMESTAMP    NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_proveedores_nombre ON proveedores (nombre);
CREATE INDEX IF NOT EXISTS idx_proveedores_activo ON proveedores (activo);

-- ============================================================
-- 4. PRODUCTOS
-- ============================================================
CREATE TABLE IF NOT EXISTS productos (
    id             INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo         VARCHAR(50),
    nombre         VARCHAR(200)  NOT NULL,
    descripcion    TEXT,
    categoria_id   INTEGER       REFERENCES categorias(id) ON DELETE SET NULL,
    precio_venta   NUMERIC(10,2) NOT NULL DEFAULT 0,
    costo          NUMERIC(10,2) NOT NULL DEFAULT 0,
    stock          INTEGER       NOT NULL DEFAULT 0,
    stock_minimo   INTEGER       NOT NULL DEFAULT 0,
    unidad_medida  VARCHAR(30)   DEFAULT 'unidad',
    activo         BOOLEAN       NOT NULL DEFAULT true,
    fecha_creacion TIMESTAMP     NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP,
    CONSTRAINT uq_productos_codigo UNIQUE (codigo),
    CONSTRAINT chk_precio_venta CHECK (precio_venta >= 0),
    CONSTRAINT chk_costo CHECK (costo >= 0),
    CONSTRAINT chk_stock CHECK (stock >= 0)
);

CREATE INDEX IF NOT EXISTS idx_productos_codigo      ON productos (codigo);
CREATE INDEX IF NOT EXISTS idx_productos_nombre      ON productos (nombre);
CREATE INDEX IF NOT EXISTS idx_productos_categoria   ON productos (categoria_id);
CREATE INDEX IF NOT EXISTS idx_productos_activo      ON productos (activo);
CREATE INDEX IF NOT EXISTS idx_productos_stock_bajo  ON productos (stock, stock_minimo) WHERE activo = true;

-- ============================================================
-- 5. VENTAS
-- ============================================================
CREATE TABLE IF NOT EXISTS ventas (
    id               INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero_venta     VARCHAR(20)   NOT NULL,
    cliente_id       INTEGER       NOT NULL REFERENCES clientes(id),
    fecha            TIMESTAMP     NOT NULL DEFAULT NOW(),
    base_imponible   NUMERIC(10,2) NOT NULL DEFAULT 0,
    porcentaje_iva   NUMERIC(5,2)  NOT NULL DEFAULT 21.00,
    importe_iva      NUMERIC(10,2) NOT NULL DEFAULT 0,
    total            NUMERIC(10,2) NOT NULL DEFAULT 0,
    estado           VARCHAR(20)   NOT NULL DEFAULT 'pendiente',
    metodo_pago      VARCHAR(50)   DEFAULT 'efectivo',
    observaciones    TEXT,
    factura_enviada  BOOLEAN       NOT NULL DEFAULT false,
    fecha_creacion   TIMESTAMP     NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP,
    CONSTRAINT uq_ventas_numero UNIQUE (numero_venta),
    CONSTRAINT chk_ventas_estado CHECK (estado IN ('pendiente','cobrada','anulada')),
    CONSTRAINT chk_ventas_total CHECK (total >= 0)
);

CREATE INDEX IF NOT EXISTS idx_ventas_cliente        ON ventas (cliente_id);
CREATE INDEX IF NOT EXISTS idx_ventas_fecha          ON ventas (fecha DESC);
CREATE INDEX IF NOT EXISTS idx_ventas_estado         ON ventas (estado);
CREATE INDEX IF NOT EXISTS idx_ventas_numero         ON ventas (numero_venta);

-- ============================================================
-- 6. VENTA_DETALLES
-- ============================================================
CREATE TABLE IF NOT EXISTS venta_detalles (
    id              INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    venta_id        INTEGER       NOT NULL REFERENCES ventas(id) ON DELETE CASCADE,
    producto_id     INTEGER       REFERENCES productos(id) ON DELETE SET NULL,
    descripcion     VARCHAR(300)  NOT NULL,
    cantidad        INTEGER       NOT NULL DEFAULT 1,
    precio_unitario NUMERIC(10,2) NOT NULL DEFAULT 0,
    subtotal        NUMERIC(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT chk_venta_det_cantidad CHECK (cantidad > 0),
    CONSTRAINT chk_venta_det_precio   CHECK (precio_unitario >= 0)
);

CREATE INDEX IF NOT EXISTS idx_venta_detalles_venta    ON venta_detalles (venta_id);
CREATE INDEX IF NOT EXISTS idx_venta_detalles_producto ON venta_detalles (producto_id);

-- ============================================================
-- 7. REPARACIONES
-- ============================================================
CREATE TABLE IF NOT EXISTS reparaciones (
    id                     INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero_orden           VARCHAR(20)   NOT NULL,
    cliente_id             INTEGER       NOT NULL REFERENCES clientes(id),
    dispositivo            VARCHAR(200)  NOT NULL,
    marca                  VARCHAR(100),
    modelo                 VARCHAR(100),
    imei                   VARCHAR(20),
    descripcion_falla      TEXT          NOT NULL,
    observaciones_tecnico  TEXT,
    estado                 VARCHAR(30)   NOT NULL DEFAULT 'recibido',
    prioridad              VARCHAR(20)   NOT NULL DEFAULT 'normal',
    precio_estimado        NUMERIC(10,2),
    precio_final           NUMERIC(10,2),
    base_imponible         NUMERIC(10,2),
    porcentaje_iva         NUMERIC(5,2)  DEFAULT 21.00,
    importe_iva            NUMERIC(10,2),
    total                  NUMERIC(10,2),
    tecnico_asignado       VARCHAR(150),
    fecha_recepcion        TIMESTAMP     NOT NULL DEFAULT NOW(),
    fecha_estimada_entrega DATE,
    fecha_entrega_real     TIMESTAMP,
    factura_enviada        BOOLEAN       NOT NULL DEFAULT false,
    fecha_creacion         TIMESTAMP     NOT NULL DEFAULT NOW(),
    fecha_modificacion     TIMESTAMP,
    CONSTRAINT uq_reparaciones_numero UNIQUE (numero_orden),
    CONSTRAINT chk_rep_estado    CHECK (estado IN ('recibido','diagnosticado','en_reparacion','reparado','entregado','no_reparable')),
    CONSTRAINT chk_rep_prioridad CHECK (prioridad IN ('baja','normal','alta','urgente'))
);

CREATE INDEX IF NOT EXISTS idx_reparaciones_cliente  ON reparaciones (cliente_id);
CREATE INDEX IF NOT EXISTS idx_reparaciones_estado   ON reparaciones (estado);
CREATE INDEX IF NOT EXISTS idx_reparaciones_fecha    ON reparaciones (fecha_recepcion DESC);
CREATE INDEX IF NOT EXISTS idx_reparaciones_numero   ON reparaciones (numero_orden);

-- ============================================================
-- 8. REPARACION_DETALLES
-- ============================================================
CREATE TABLE IF NOT EXISTS reparacion_detalles (
    id              INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reparacion_id   INTEGER       NOT NULL REFERENCES reparaciones(id) ON DELETE CASCADE,
    producto_id     INTEGER       REFERENCES productos(id) ON DELETE SET NULL,
    descripcion     VARCHAR(300)  NOT NULL,
    cantidad        INTEGER       NOT NULL DEFAULT 1,
    precio_unitario NUMERIC(10,2) NOT NULL DEFAULT 0,
    subtotal        NUMERIC(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT chk_rep_det_cantidad CHECK (cantidad > 0),
    CONSTRAINT chk_rep_det_precio   CHECK (precio_unitario >= 0)
);

CREATE INDEX IF NOT EXISTS idx_rep_detalles_reparacion ON reparacion_detalles (reparacion_id);
CREATE INDEX IF NOT EXISTS idx_rep_detalles_producto   ON reparacion_detalles (producto_id);

-- ============================================================
-- 9. FACTURAS
-- ============================================================
CREATE TABLE IF NOT EXISTS facturas (
    id               INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero_factura   VARCHAR(20)   NOT NULL,
    venta_id         INTEGER       REFERENCES ventas(id) ON DELETE RESTRICT,
    reparacion_id    INTEGER       REFERENCES reparaciones(id) ON DELETE RESTRICT,
    cliente_id       INTEGER       NOT NULL REFERENCES clientes(id),
    fecha_emision    DATE          NOT NULL DEFAULT CURRENT_DATE,
    base_imponible   NUMERIC(10,2) NOT NULL DEFAULT 0,
    porcentaje_iva   NUMERIC(5,2)  NOT NULL DEFAULT 21.00,
    importe_iva      NUMERIC(10,2) NOT NULL DEFAULT 0,
    total            NUMERIC(10,2) NOT NULL DEFAULT 0,
    pdf_path         VARCHAR(500),
    anulada          BOOLEAN       NOT NULL DEFAULT false,
    motivo_anulacion TEXT,
    fecha_creacion   TIMESTAMP     NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_facturas_numero   UNIQUE (numero_factura),
    -- Permite: venta, reparación, o ninguno (factura manual)
    -- Prohíbe: ambos a la vez
    CONSTRAINT chk_facturas_origen  CHECK (
        venta_id IS NULL OR reparacion_id IS NULL
    )
);

CREATE INDEX IF NOT EXISTS idx_facturas_numero      ON facturas (numero_factura);
CREATE INDEX IF NOT EXISTS idx_facturas_cliente     ON facturas (cliente_id);
CREATE INDEX IF NOT EXISTS idx_facturas_venta       ON facturas (venta_id);
CREATE INDEX IF NOT EXISTS idx_facturas_reparacion  ON facturas (reparacion_id);
CREATE INDEX IF NOT EXISTS idx_facturas_fecha       ON facturas (fecha_emision DESC);

-- ============================================================
-- 10. COMPRAS
-- ============================================================
CREATE TABLE IF NOT EXISTS compras (
    id               INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    numero_compra    VARCHAR(20)   NOT NULL,
    proveedor_id     INTEGER       NOT NULL REFERENCES proveedores(id),
    fecha            TIMESTAMP     NOT NULL DEFAULT NOW(),
    total            NUMERIC(10,2) NOT NULL DEFAULT 0,
    estado           VARCHAR(20)   NOT NULL DEFAULT 'pendiente',
    observaciones    TEXT,
    fecha_creacion   TIMESTAMP     NOT NULL DEFAULT NOW(),
    fecha_modificacion TIMESTAMP,
    CONSTRAINT uq_compras_numero UNIQUE (numero_compra),
    CONSTRAINT chk_compras_estado CHECK (estado IN ('pendiente','recibida','anulada'))
);

CREATE INDEX IF NOT EXISTS idx_compras_proveedor ON compras (proveedor_id);
CREATE INDEX IF NOT EXISTS idx_compras_fecha     ON compras (fecha DESC);
CREATE INDEX IF NOT EXISTS idx_compras_estado    ON compras (estado);

-- ============================================================
-- 11. COMPRA_DETALLES
-- ============================================================
CREATE TABLE IF NOT EXISTS compra_detalles (
    id              INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    compra_id       INTEGER       NOT NULL REFERENCES compras(id) ON DELETE CASCADE,
    producto_id     INTEGER       NOT NULL REFERENCES productos(id),
    cantidad        INTEGER       NOT NULL DEFAULT 1,
    costo_unitario  NUMERIC(10,2) NOT NULL DEFAULT 0,
    subtotal        NUMERIC(10,2) NOT NULL DEFAULT 0,
    CONSTRAINT chk_compra_det_cantidad CHECK (cantidad > 0),
    CONSTRAINT chk_compra_det_costo    CHECK (costo_unitario >= 0)
);

CREATE INDEX IF NOT EXISTS idx_compra_detalles_compra   ON compra_detalles (compra_id);
CREATE INDEX IF NOT EXISTS idx_compra_detalles_producto ON compra_detalles (producto_id);

-- ============================================================
-- 12. INVENTARIO_MOVIMIENTOS (KARDEX)
-- ============================================================
CREATE TABLE IF NOT EXISTS inventario_movimientos (
    id               INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    producto_id      INTEGER      NOT NULL REFERENCES productos(id),
    tipo             VARCHAR(20)  NOT NULL,
    cantidad         INTEGER      NOT NULL,
    stock_anterior   INTEGER      NOT NULL,
    stock_posterior  INTEGER      NOT NULL,
    referencia_tipo  VARCHAR(20),
    referencia_id    INTEGER,
    observaciones    TEXT,
    usuario          VARCHAR(100) DEFAULT 'sistema',
    fecha            TIMESTAMP    NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_inv_tipo CHECK (tipo IN ('entrada','salida','ajuste'))
);

CREATE INDEX IF NOT EXISTS idx_inv_mov_producto   ON inventario_movimientos (producto_id);
CREATE INDEX IF NOT EXISTS idx_inv_mov_fecha      ON inventario_movimientos (fecha DESC);
CREATE INDEX IF NOT EXISTS idx_inv_mov_tipo       ON inventario_movimientos (tipo);
CREATE INDEX IF NOT EXISTS idx_inv_mov_referencia ON inventario_movimientos (referencia_tipo, referencia_id);

-- ============================================================
-- 13. EMAIL_LOGS
-- ============================================================
CREATE TABLE IF NOT EXISTS email_logs (
    id               INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    destinatario     VARCHAR(200) NOT NULL,
    asunto           VARCHAR(300) NOT NULL,
    cuerpo           TEXT,
    tipo             VARCHAR(50),
    referencia_tipo  VARCHAR(30),
    referencia_id    INTEGER,
    estado           VARCHAR(20)  NOT NULL DEFAULT 'pendiente',
    error_mensaje    TEXT,
    intentos         INTEGER      NOT NULL DEFAULT 0,
    fecha_envio      TIMESTAMP,
    fecha_creacion   TIMESTAMP    NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_email_estado CHECK (estado IN ('pendiente','enviado','error'))
);

CREATE INDEX IF NOT EXISTS idx_email_logs_estado      ON email_logs (estado);
CREATE INDEX IF NOT EXISTS idx_email_logs_tipo        ON email_logs (tipo);
CREATE INDEX IF NOT EXISTS idx_email_logs_fecha       ON email_logs (fecha_creacion DESC);
CREATE INDEX IF NOT EXISTS idx_email_logs_referencia  ON email_logs (referencia_tipo, referencia_id);

-- ============================================================
-- 14. CONFIGURACION
-- ============================================================
CREATE TABLE IF NOT EXISTS configuracion (
    id          INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    clave       VARCHAR(100) NOT NULL,
    valor       TEXT,
    descripcion TEXT,
    CONSTRAINT uq_configuracion_clave UNIQUE (clave)
);

-- ============================================================
-- 15. SECUENCIAS DE NUMERACIÓN
-- ============================================================
-- Tabla para control de numeración correlativa anual
CREATE TABLE IF NOT EXISTS numeracion_secuencias (
    id          INTEGER     GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    tipo        VARCHAR(20) NOT NULL,   -- venta, factura, reparacion, compra
    anio        INTEGER     NOT NULL,
    ultimo      INTEGER     NOT NULL DEFAULT 0,
    CONSTRAINT uq_numeracion_tipo_anio UNIQUE (tipo, anio)
);

-- ============================================================
-- FUNCIONES AUXILIARES
-- ============================================================

-- Función para obtener/incrementar el siguiente número correlativo
CREATE OR REPLACE FUNCTION get_next_numero(p_tipo VARCHAR, p_anio INTEGER)
RETURNS INTEGER AS $$
DECLARE
    v_ultimo INTEGER;
BEGIN
    INSERT INTO numeracion_secuencias (tipo, anio, ultimo)
    VALUES (p_tipo, p_anio, 1)
    ON CONFLICT (tipo, anio)
    DO UPDATE SET ultimo = numeracion_secuencias.ultimo + 1
    RETURNING ultimo INTO v_ultimo;

    RETURN v_ultimo;
END;
$$ LANGUAGE plpgsql;

-- Función para generar número de venta: V-YYYYNNNN
CREATE OR REPLACE FUNCTION generar_numero_venta()
RETURNS VARCHAR AS $$
DECLARE
    v_anio    INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
    v_secuencia INTEGER;
BEGIN
    v_secuencia := get_next_numero('venta', v_anio);
    RETURN 'V-' || v_anio::TEXT || LPAD(v_secuencia::TEXT, 4, '0');
END;
$$ LANGUAGE plpgsql;

-- Función para generar número de factura: F-YYYY-NNNN
CREATE OR REPLACE FUNCTION generar_numero_factura()
RETURNS VARCHAR AS $$
DECLARE
    v_anio    INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
    v_secuencia INTEGER;
BEGIN
    v_secuencia := get_next_numero('factura', v_anio);
    RETURN 'F-' || v_anio::TEXT || '-' || LPAD(v_secuencia::TEXT, 4, '0');
END;
$$ LANGUAGE plpgsql;

-- Función para generar número de reparación: R-YYYYNNNN
CREATE OR REPLACE FUNCTION generar_numero_reparacion()
RETURNS VARCHAR AS $$
DECLARE
    v_anio    INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
    v_secuencia INTEGER;
BEGIN
    v_secuencia := get_next_numero('reparacion', v_anio);
    RETURN 'R-' || v_anio::TEXT || LPAD(v_secuencia::TEXT, 4, '0');
END;
$$ LANGUAGE plpgsql;

-- Función para generar número de compra: C-YYYYNNNN
CREATE OR REPLACE FUNCTION generar_numero_compra()
RETURNS VARCHAR AS $$
DECLARE
    v_anio    INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
    v_secuencia INTEGER;
BEGIN
    v_secuencia := get_next_numero('compra', v_anio);
    RETURN 'C-' || v_anio::TEXT || LPAD(v_secuencia::TEXT, 4, '0');
END;
$$ LANGUAGE plpgsql;

-- ============================================================
-- DATOS SEMILLA (SEED DATA)
-- ============================================================

-- Configuración de la empresa
INSERT INTO configuracion (clave, valor, descripcion) VALUES
('empresa_nombre',       'CellShop',                    'Nombre comercial de la empresa'),
('empresa_cif',          'B12345678',                   'CIF de la empresa'),
('empresa_direccion',    'Calle Mayor, 1',               'Dirección fiscal'),
('empresa_ciudad',       'Madrid',                       'Ciudad'),
('empresa_cp',           '28001',                        'Código postal'),
('empresa_telefono',     '+34 91 000 00 00',             'Teléfono de contacto'),
('empresa_email',        'info@cellshop.es',             'Email de la empresa'),
('empresa_web',          'www.cellshop.es',              'Sitio web'),
('iva_porcentaje',       '21',                           'Porcentaje de IVA general (%)'),
('smtp_host',            'smtp.gmail.com',               'Servidor SMTP'),
('smtp_puerto',          '587',                          'Puerto SMTP'),
('smtp_ssl',             'true',                         'Usar SSL/TLS'),
('smtp_usuario',         'tucorreo@gmail.com',           'Usuario SMTP'),
('smtp_password',        'tupassword',                   'Contraseña SMTP (app password)'),
('smtp_from_name',       'CellShop',                     'Nombre del remitente'),
('smtp_from_email',      'tucorreo@gmail.com',           'Email del remitente'),
('factura_pie_texto',    'Gracias por su confianza. CellShop - Servicio Técnico Oficial', 'Texto pie de factura'),
('pdf_directorio',       'wwwroot/facturas',             'Directorio para guardar PDFs')
ON CONFLICT (clave) DO NOTHING;

-- Categorías
INSERT INTO categorias (nombre, descripcion) VALUES
('Smartphones',         'Teléfonos inteligentes y accesorios'),
('Fundas y Protectores','Fundas, protectores de pantalla y carcasas'),
('Cargadores',          'Cargadores, cables y adaptadores'),
('Baterías',            'Baterías de repuesto para dispositivos'),
('Pantallas',           'Pantallas y displays de repuesto'),
('Piezas de reparación','Componentes internos para reparación'),
('Accesorios',          'Auriculares, soportes y otros accesorios'),
('Tablets',             'Tablets y accesorios para tablets')
ON CONFLICT DO NOTHING;

-- Proveedor de ejemplo
INSERT INTO proveedores (nombre, email, telefono, direccion, ciudad, codigo_postal, cif) VALUES
('TechDistrib S.L.',    'pedidos@techdistrib.es', '+34 93 111 22 33', 'Polígono Industrial Norte, Nave 5', 'Barcelona', '08001', 'B98765432'),
('MobilePartes S.A.',   'ventas@mobilepartes.es', '+34 91 222 33 44', 'Calle del Comercio, 15',            'Madrid',    '28010', 'A11223344')
ON CONFLICT DO NOTHING;

-- Productos de ejemplo
INSERT INTO productos (codigo, nombre, descripcion, categoria_id, precio_venta, costo, stock, stock_minimo) VALUES
('PROD-001', 'Pantalla iPhone 13',           'Display OLED original para iPhone 13',              5, 189.99, 95.00,  10, 3),
('PROD-002', 'Batería Samsung Galaxy A52',   'Batería de repuesto 4500mAh para Galaxy A52',       4,  34.99, 15.00,  25, 5),
('PROD-003', 'Funda silicona iPhone 14',     'Funda de silicona premium para iPhone 14',          2,  19.99,  6.00,  50, 10),
('PROD-004', 'Cristal templado universal',   'Protector de pantalla cristal templado 9H',         2,   9.99,  2.50, 100, 20),
('PROD-005', 'Cable USB-C 1m',               'Cable de carga rápida USB-C a USB-C 65W',           3,  14.99,  4.00,  40, 10),
('PROD-006', 'Cargador USB-C 45W',           'Cargador de pared USB-C 45W carga rápida',          3,  29.99, 12.00,  20, 5),
('PROD-007', 'Conector de carga iPhone',     'Puerto Lightning de repuesto para iPhone',          6,  24.99, 10.00,  15, 3),
('PROD-008', 'Herramienta apertura móvil',   'Kit completo de herramientas para reparación',      6,  39.99, 18.00,   8, 2),
('PROD-009', 'Pantalla Samsung Galaxy S21',  'Display AMOLED original para Galaxy S21',           5, 219.99,110.00,   6, 2),
('PROD-010', 'Auriculares Bluetooth',        'Auriculares inalámbricos TWS con estuche',          7,  49.99, 20.00,  30, 5)
ON CONFLICT (codigo) DO NOTHING;

-- Cliente de ejemplo
INSERT INTO clientes (nombre, apellidos, email, telefono, direccion, ciudad, codigo_postal, nif) VALUES
('Juan',     'García López',    'juan.garcia@email.es',    '+34 666 111 222', 'Calle Alcalá, 25, 3º A',   'Madrid',    '28009', '12345678A'),
('María',    'Martínez Ruiz',   'maria.martinez@email.es', '+34 677 333 444', 'Avda. Diagonal, 100, 2º B', 'Barcelona', '08008', '87654321B'),
('Carlos',   'López Fernández', 'carlos.lopez@email.es',   '+34 655 555 666', 'Plaza Mayor, 5, 1º',        'Valencia',  '46001', '11223344C')
ON CONFLICT (email) DO NOTHING;

-- ============================================================
-- VISTAS ÚTILES
-- ============================================================

-- Vista: Ventas con datos del cliente
CREATE OR REPLACE VIEW v_ventas AS
SELECT
    v.id,
    v.numero_venta,
    v.fecha,
    v.estado,
    v.metodo_pago,
    v.base_imponible,
    v.porcentaje_iva,
    v.importe_iva,
    v.total,
    v.factura_enviada,
    c.id          AS cliente_id,
    c.nombre      AS cliente_nombre,
    c.apellidos   AS cliente_apellidos,
    c.email       AS cliente_email,
    c.nif         AS cliente_nif,
    f.numero_factura
FROM ventas v
JOIN clientes c ON c.id = v.cliente_id
LEFT JOIN facturas f ON f.venta_id = v.id;

-- Vista: Reparaciones con datos del cliente
CREATE OR REPLACE VIEW v_reparaciones AS
SELECT
    r.id,
    r.numero_orden,
    r.estado,
    r.prioridad,
    r.dispositivo,
    r.marca,
    r.modelo,
    r.descripcion_falla,
    r.precio_estimado,
    r.precio_final,
    r.total,
    r.tecnico_asignado,
    r.fecha_recepcion,
    r.fecha_estimada_entrega,
    r.fecha_entrega_real,
    r.factura_enviada,
    c.id        AS cliente_id,
    c.nombre    AS cliente_nombre,
    c.apellidos AS cliente_apellidos,
    c.email     AS cliente_email,
    c.telefono  AS cliente_telefono
FROM reparaciones r
JOIN clientes c ON c.id = r.cliente_id;

-- Vista: Productos con stock bajo
CREATE OR REPLACE VIEW v_productos_stock_bajo AS
SELECT
    p.id,
    p.codigo,
    p.nombre,
    p.stock,
    p.stock_minimo,
    (p.stock_minimo - p.stock) AS unidades_faltantes,
    cat.nombre AS categoria
FROM productos p
LEFT JOIN categorias cat ON cat.id = p.categoria_id
WHERE p.activo = true
  AND p.stock <= p.stock_minimo
ORDER BY (p.stock_minimo - p.stock) DESC;

-- Vista: Dashboard - resumen ventas del mes actual
CREATE OR REPLACE VIEW v_dashboard_ventas_mes AS
SELECT
    COUNT(*)                      AS total_ventas,
    COALESCE(SUM(total), 0)       AS importe_total,
    COALESCE(SUM(importe_iva), 0) AS iva_total,
    COALESCE(AVG(total), 0)       AS ticket_medio
FROM ventas
WHERE DATE_TRUNC('month', fecha) = DATE_TRUNC('month', NOW())
  AND estado != 'anulada';

-- Vista: Kardex (movimientos con nombre de producto)
CREATE OR REPLACE VIEW v_kardex AS
SELECT
    im.id,
    im.fecha,
    im.tipo,
    im.cantidad,
    im.stock_anterior,
    im.stock_posterior,
    im.referencia_tipo,
    im.referencia_id,
    im.observaciones,
    im.usuario,
    p.id     AS producto_id,
    p.codigo AS producto_codigo,
    p.nombre AS producto_nombre
FROM inventario_movimientos im
JOIN productos p ON p.id = im.producto_id;

-- ============================================================
-- FIN DEL SCRIPT
-- ============================================================
