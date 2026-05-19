-- ============================================================
-- MIGRACIÓN 03: Módulo de autenticación + Imágenes reparación
-- Ejecutar DESPUÉS de 01_schema.sql
-- ============================================================

-- ── Tabla usuarios ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS usuarios (
    id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre           VARCHAR(100)  NOT NULL,
    username         VARCHAR(50)   NOT NULL UNIQUE,
    password_hash    VARCHAR(255)  NOT NULL,
    activo           BOOLEAN       NOT NULL DEFAULT true,
    fecha_creacion   TIMESTAMPTZ   NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_usuarios_username ON usuarios(username);

-- ── Usuario inicial ─────────────────────────────────────────
-- Nombre: Dj-Had cont
-- Username: maaroufi
-- Contraseña: 638373510  → BCrypt hash (work factor 12)
INSERT INTO usuarios (nombre, username, password_hash)
VALUES (
    'Dj-Had cont',
    'maaroufi',
    '$2a$12$DDetI3Ffxrim9mf.Dku2IuiDaqc4yNqEWtibMdDK1O11mVXYEhk2G'
)
ON CONFLICT (username) DO NOTHING;

-- ── Tabla imágenes de reparación ────────────────────────────
CREATE TABLE IF NOT EXISTS reparacion_imagenes (
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reparacion_id   INTEGER      NOT NULL REFERENCES reparaciones(id) ON DELETE CASCADE,
    ruta_imagen     VARCHAR(500) NOT NULL,
    nombre_archivo  VARCHAR(255),
    fecha           TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_reparacion_imagenes_rep ON reparacion_imagenes(reparacion_id);

-- ── Verificación ────────────────────────────────────────────
SELECT 'usuarios' AS tabla, COUNT(*) FROM usuarios
UNION ALL
SELECT 'reparacion_imagenes', COUNT(*) FROM reparacion_imagenes;
