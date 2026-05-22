using Dapper;

namespace CellApi.Data;

public static class DbMigrator
{
    public static async Task RunAsync(DbConnectionFactory db)
    {
        using var conn = db.CreateConnection();

        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS garantias (
                id                   INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                numero_garantia      VARCHAR(20),
                tipo                 VARCHAR(20)   NOT NULL DEFAULT 'venta',
                referencia_id        INTEGER       NOT NULL DEFAULT 0,
                cliente_id           INTEGER       NOT NULL REFERENCES clientes(id),
                producto_descripcion VARCHAR(300)  NOT NULL,
                fecha_inicio         TIMESTAMP     NOT NULL DEFAULT NOW(),
                fecha_fin            TIMESTAMP     NOT NULL,
                meses                INTEGER       NOT NULL DEFAULT 12,
                estado               VARCHAR(20)   NOT NULL DEFAULT 'activa',
                observaciones        TEXT,
                fecha_creacion       TIMESTAMP     NOT NULL DEFAULT NOW(),
                CONSTRAINT uq_garantias_numero  UNIQUE (numero_garantia),
                CONSTRAINT chk_garantias_tipo   CHECK (tipo   IN ('venta','reparacion')),
                CONSTRAINT chk_garantias_estado CHECK (estado IN ('activa','reclamada','vencida','anulada'))
            );

            CREATE INDEX IF NOT EXISTS idx_garantias_cliente   ON garantias (cliente_id);
            CREATE INDEX IF NOT EXISTS idx_garantias_estado    ON garantias (estado);
            CREATE INDEX IF NOT EXISTS idx_garantias_fecha_fin ON garantias (fecha_fin);

            CREATE TABLE IF NOT EXISTS caja_sesiones (
                id                INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                numero_sesion     VARCHAR(20),
                fecha_apertura    TIMESTAMP     NOT NULL DEFAULT NOW(),
                fecha_cierre      TIMESTAMP,
                efectivo_apertura NUMERIC(10,2) NOT NULL DEFAULT 0,
                efectivo_cierre   NUMERIC(10,2),
                total_efectivo    NUMERIC(10,2) NOT NULL DEFAULT 0,
                total_tarjeta     NUMERIC(10,2) NOT NULL DEFAULT 0,
                total_otros       NUMERIC(10,2) NOT NULL DEFAULT 0,
                diferencia        NUMERIC(10,2),
                estado            VARCHAR(20)   NOT NULL DEFAULT 'abierta',
                usuario_apertura  VARCHAR(150),
                usuario_cierre    VARCHAR(150),
                observaciones     TEXT,
                fecha_creacion    TIMESTAMP     NOT NULL DEFAULT NOW(),
                CONSTRAINT chk_caja_estado CHECK (estado IN ('abierta','cerrada'))
            );

            CREATE INDEX IF NOT EXISTS idx_caja_sesiones_estado ON caja_sesiones (estado);
            CREATE INDEX IF NOT EXISTS idx_caja_sesiones_fecha  ON caja_sesiones (fecha_apertura DESC);

            CREATE TABLE IF NOT EXISTS caja_movimientos (
                id              INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                sesion_id       INTEGER       NOT NULL REFERENCES caja_sesiones(id) ON DELETE CASCADE,
                tipo            VARCHAR(20)   NOT NULL DEFAULT 'entrada',
                concepto        VARCHAR(300)  NOT NULL,
                importe         NUMERIC(10,2) NOT NULL DEFAULT 0,
                metodo_pago     VARCHAR(50),
                referencia_tipo VARCHAR(50),
                referencia_id   INTEGER,
                usuario         VARCHAR(150),
                fecha           TIMESTAMP     NOT NULL DEFAULT NOW(),
                CONSTRAINT chk_caja_mov_tipo CHECK (tipo IN ('entrada','salida','venta','devolucion'))
            );

            CREATE INDEX IF NOT EXISTS idx_caja_mov_sesion ON caja_movimientos (sesion_id);
            CREATE INDEX IF NOT EXISTS idx_caja_mov_fecha  ON caja_movimientos (fecha DESC);

            CREATE OR REPLACE FUNCTION generar_numero_sesion_caja()
            RETURNS VARCHAR AS $$
            DECLARE
                v_anio      INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
                v_secuencia INTEGER;
            BEGIN
                v_secuencia := get_next_numero('caja_sesion', v_anio);
                RETURN 'S-' || v_anio::TEXT || LPAD(v_secuencia::TEXT, 4, '0');
            END;
            $$ LANGUAGE plpgsql;

            CREATE TABLE IF NOT EXISTS reparacion_imagenes (
                id             INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                reparacion_id  INTEGER      NOT NULL REFERENCES reparaciones(id) ON DELETE CASCADE,
                ruta_imagen    VARCHAR(500) NOT NULL,
                nombre_archivo VARCHAR(300),
                fecha          TIMESTAMP    NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS idx_rep_imagenes_reparacion ON reparacion_imagenes (reparacion_id);

            ALTER TABLE reparaciones ADD COLUMN IF NOT EXISTS solucion TEXT;
            ALTER TABLE reparaciones ADD COLUMN IF NOT EXISTS recordatorio_enviado BOOLEAN NOT NULL DEFAULT false;
        ");

        await SyncSmtpConfigAsync(conn);
    }

    private static async Task SyncSmtpConfigAsync(System.Data.IDbConnection conn)
    {
        var vars = new Dictionary<string, string?>
        {
            ["smtp_host"]       = Environment.GetEnvironmentVariable("SMTP_HOST"),
            ["smtp_puerto"]     = Environment.GetEnvironmentVariable("SMTP_PUERTO"),
            ["smtp_ssl"]        = Environment.GetEnvironmentVariable("SMTP_SSL"),
            ["smtp_usuario"]    = Environment.GetEnvironmentVariable("SMTP_USUARIO"),
            ["smtp_password"]   = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            ["smtp_from_name"]  = Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
            ["smtp_from_email"] = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
        };

        foreach (var (clave, valor) in vars)
        {
            if (string.IsNullOrEmpty(valor)) continue;
            await conn.ExecuteAsync(
                "UPDATE configuracion SET valor = @Valor WHERE clave = @Clave",
                new { Clave = clave, Valor = valor });
        }
    }
}
