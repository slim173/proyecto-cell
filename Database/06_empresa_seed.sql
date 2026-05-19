-- Datos reales de la empresa (FixPro, San Pedro del Pinatar)
-- Se ejecuta con INSERT ... ON CONFLICT UPDATE para no pisar valores
-- ya personalizados si existe la clave.

INSERT INTO configuracion (clave, valor, descripcion) VALUES
  ('empresa_nombre',    'FixPro Reparaciones',          'Nombre comercial'),
  ('empresa_cif',       'B-12345678',                   'CIF / NIF'),
  ('empresa_telefono',  '+34 968 000 000',              'Teléfono de contacto'),
  ('empresa_direccion', 'Calle Mayor 15',                'Dirección'),
  ('empresa_ciudad',    'San Pedro del Pinatar',        'Ciudad'),
  ('empresa_cp',        '30740',                        'Código postal'),
  ('empresa_email',     'info@fixpro.es',               'Email de empresa'),
  ('empresa_web',       'www.fixpro.es',                'Sitio web'),
  ('iva_porcentaje',    '21',                           'IVA por defecto (%)'),
  ('factura_pie_texto', 'Gracias por confiar en FixPro Reparaciones · +34 968 000 000', 'Pie de factura'),
  ('wa_msg_entrada',
   '¡Hola {nombre}! 🔧 Hemos recibido tu {dispositivo} en FixPro Reparaciones. Orden #{orden}. Te avisamos en cuanto esté listo. ¡Gracias!',
   'WA — entrada registrada'),
  ('wa_msg_listo',
   '¡Hola {nombre}! ✅ Tu {dispositivo} ya está listo para recoger. Orden #{orden} · Total: {total}€. Pasa cuando quieras por FixPro Reparaciones. ¡Hasta pronto!',
   'WA — listo para recoger'),
  ('wa_msg_recordatorio',
   'Hola {nombre} 📱, te recordamos que tienes el {dispositivo} (Orden #{orden}) pendiente de recoger en FixPro Reparaciones. ¿Cuándo puedes pasarte?',
   'WA — recordatorio de recogida')
ON CONFLICT (clave) DO UPDATE SET
  valor       = EXCLUDED.valor,
  descripcion = EXCLUDED.descripcion;
