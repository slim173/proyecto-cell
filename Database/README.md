# Base de Datos - CellShop ERP

## Configuración
- Motor: PostgreSQL 18.3
- Base de datos: `db_cell`
- Usuario: `postgres`
- Puerto: `5432`

## Ejecución

### Opción 1: psql desde terminal
```bash
psql -U postgres -d db_cell -f 01_schema.sql
psql -U postgres -d db_cell -f 02_verificacion.sql
```

### Opción 2: pgAdmin
1. Abrir pgAdmin → conectar a `db_cell`
2. Abrir Query Tool
3. Cargar y ejecutar `01_schema.sql`
4. Cargar y ejecutar `02_verificacion.sql` para verificar

## Tablas creadas
| Tabla | Descripción |
|---|---|
| `categorias` | Categorías de productos |
| `clientes` | Clientes de la tienda |
| `proveedores` | Proveedores de mercancía |
| `productos` | Catálogo de productos e inventario |
| `ventas` | Cabecera de ventas |
| `venta_detalles` | Líneas de cada venta |
| `reparaciones` | Órdenes de servicio técnico |
| `reparacion_detalles` | Piezas y servicios de cada reparación |
| `facturas` | Facturas emitidas (ventas y reparaciones) |
| `compras` | Compras a proveedores |
| `compra_detalles` | Líneas de cada compra |
| `inventario_movimientos` | Kardex de movimientos de stock |
| `email_logs` | Registro de emails enviados |
| `configuracion` | Parámetros del sistema |
| `numeracion_secuencias` | Control de numeración correlativa anual |

## Notas importantes
- **Sin SERIAL**: toda clave primaria usa `GENERATED ALWAYS AS IDENTITY`
- **Numeración automática**: vía función `generar_numero_*()` con secuencia anual
- **IVA**: 21% por defecto, almacenado como campo `porcentaje_iva` para trazabilidad
- **Email SMTP**: configurar `smtp_*` en tabla `configuracion` antes de arrancar
