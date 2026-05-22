using Dapper;
using Microsoft.Extensions.Logging;

namespace CellApi.Data;

public static class DbMigrator
{
    public static async Task RunAsync(DbConnectionFactory db, ILogger? logger = null)
    {
        using var conn = db.CreateConnection();

        var steps = new (string name, string sql)[]
        {
            ("garantias_table", @"
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
                )"),

            ("garantias_indexes", @"
                CREATE INDEX IF NOT EXISTS idx_garantias_cliente   ON garantias (cliente_id);
                CREATE INDEX IF NOT EXISTS idx_garantias_estado    ON garantias (estado);
                CREATE INDEX IF NOT EXISTS idx_garantias_fecha_fin ON garantias (fecha_fin)"),

            ("caja_sesiones_table", @"
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
                )"),

            ("caja_sesiones_indexes", @"
                CREATE INDEX IF NOT EXISTS idx_caja_sesiones_estado ON caja_sesiones (estado);
                CREATE INDEX IF NOT EXISTS idx_caja_sesiones_fecha  ON caja_sesiones (fecha_apertura DESC)"),

            ("caja_movimientos_table", @"
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
                )"),

            ("caja_movimientos_indexes", @"
                CREATE INDEX IF NOT EXISTS idx_caja_mov_sesion ON caja_movimientos (sesion_id);
                CREATE INDEX IF NOT EXISTS idx_caja_mov_fecha  ON caja_movimientos (fecha DESC)"),

            ("fn_numero_sesion_caja", @"
                CREATE OR REPLACE FUNCTION generar_numero_sesion_caja()
                RETURNS VARCHAR AS $$
                DECLARE
                    v_anio      INTEGER := EXTRACT(YEAR FROM NOW())::INTEGER;
                    v_secuencia INTEGER;
                BEGIN
                    v_secuencia := get_next_numero('caja_sesion', v_anio);
                    RETURN 'S-' || v_anio::TEXT || LPAD(v_secuencia::TEXT, 4, '0');
                END;
                $$ LANGUAGE plpgsql"),

            ("reparacion_imagenes_table", @"
                CREATE TABLE IF NOT EXISTS reparacion_imagenes (
                    id             INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    reparacion_id  INTEGER      NOT NULL REFERENCES reparaciones(id) ON DELETE CASCADE,
                    ruta_imagen    VARCHAR(500) NOT NULL,
                    nombre_archivo VARCHAR(300),
                    fecha          TIMESTAMP    NOT NULL DEFAULT NOW()
                )"),

            ("reparacion_imagenes_index", @"
                CREATE INDEX IF NOT EXISTS idx_rep_imagenes_reparacion ON reparacion_imagenes (reparacion_id)"),

            ("col_reparaciones_solucion",
                "ALTER TABLE reparaciones ADD COLUMN IF NOT EXISTS solucion TEXT"),

            ("col_reparaciones_recordatorio",
                "ALTER TABLE reparaciones ADD COLUMN IF NOT EXISTS recordatorio_enviado BOOLEAN NOT NULL DEFAULT false"),
        };

        foreach (var (name, sql) in steps)
        {
            try
            {
                await conn.ExecuteAsync(sql);
                logger?.LogInformation("[DbMigrator] OK: {Step}", name);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DbMigrator] ERROR en {Step}: {Message}", name, ex.Message);
                // Continúa con el siguiente paso — no aborta el arranque
            }
        }

        await SeedEmpresaConfigAsync(conn, logger);
        await SyncEnvVarsAsync(conn, logger);
    }

    // Inserta claves de empresa/config si todavía no existen en BD
    private static async Task SeedEmpresaConfigAsync(System.Data.IDbConnection conn, ILogger? logger)
    {
        var defaults = new (string clave, string valor, string desc)[]
        {
            ("empresa_nombre",    "CellShop",    "Nombre comercial"),
            ("empresa_cif",       "",            "CIF / NIF"),
            ("empresa_telefono",  "",            "Teléfono de contacto"),
            ("empresa_direccion", "",            "Dirección"),
            ("empresa_ciudad",    "",            "Ciudad"),
            ("empresa_cp",        "",            "Código postal"),
            ("empresa_email",     "",            "Email de empresa"),
            ("empresa_web",       "",            "Sitio web"),
            ("empresa_logo",      "",            "Logo (ruta relativa a wwwroot)"),
            ("iva_porcentaje",    "21",          "IVA por defecto (%)"),
            ("ticket_formato",    "a4",          "Formato de impresión (a4/ticket_80mm/ticket_58mm)"),
            ("ticket_mostrar_qr", "true",        "Mostrar QR en tickets"),
            ("ticket_clausula_reparacion", "",   "Cláusula de reparación en tickets"),
            ("ticket_clausula_recogida",   "",   "Condiciones de recogida en tickets"),
            ("factura_pie_texto", "",            "Pie de factura"),
            ("empresa_url_publica", "",          "URL pública (para QR en PDFs)"),
        };

        const string sql = @"
            INSERT INTO configuracion (clave, valor, descripcion)
            VALUES (@Clave, @Valor, @Desc)
            ON CONFLICT (clave) DO NOTHING";

        foreach (var (clave, valor, desc) in defaults)
        {
            try
            {
                await conn.ExecuteAsync(sql, new { Clave = clave, Valor = valor, Desc = desc });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DbMigrator] Error seed config {Clave}", clave);
            }
        }

        logger?.LogInformation("[DbMigrator] Seed configuracion OK");
    }

    // Sobreescribe claves de configuracion con variables de entorno de Railway/Docker
    private static async Task SyncEnvVarsAsync(System.Data.IDbConnection conn, ILogger? logger)
    {
        var vars = new Dictionary<string, string?>
        {
            // SMTP
            ["smtp_host"]       = Environment.GetEnvironmentVariable("SMTP_HOST"),
            ["smtp_puerto"]     = Environment.GetEnvironmentVariable("SMTP_PUERTO"),
            ["smtp_ssl"]        = Environment.GetEnvironmentVariable("SMTP_SSL"),
            ["smtp_usuario"]    = Environment.GetEnvironmentVariable("SMTP_USUARIO"),
            ["smtp_password"]   = Environment.GetEnvironmentVariable("SMTP_PASSWORD"),
            ["smtp_from_name"]  = Environment.GetEnvironmentVariable("SMTP_FROM_NAME"),
            ["smtp_from_email"] = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL"),
            // Empresa (Railway Variables)
            ["empresa_nombre"]    = Environment.GetEnvironmentVariable("EMPRESA_NOMBRE"),
            ["empresa_cif"]       = Environment.GetEnvironmentVariable("EMPRESA_CIF"),
            ["empresa_telefono"]  = Environment.GetEnvironmentVariable("EMPRESA_TELEFONO"),
            ["empresa_direccion"] = Environment.GetEnvironmentVariable("EMPRESA_DIRECCION"),
            ["empresa_ciudad"]    = Environment.GetEnvironmentVariable("EMPRESA_CIUDAD"),
            ["empresa_cp"]        = Environment.GetEnvironmentVariable("EMPRESA_CP"),
            ["empresa_email"]     = Environment.GetEnvironmentVariable("EMPRESA_EMAIL"),
        };

        const string sql = "UPDATE configuracion SET valor = @Valor WHERE clave = @Clave";

        foreach (var (clave, valor) in vars)
        {
            if (string.IsNullOrEmpty(valor)) continue;
            try
            {
                await conn.ExecuteAsync(sql, new { Clave = clave, Valor = valor });
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[DbMigrator] Error sync env var {Clave}", clave);
            }
        }
    }
}
