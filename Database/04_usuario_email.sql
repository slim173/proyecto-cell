-- Añadir columna email a usuarios
ALTER TABLE usuarios
    ADD COLUMN IF NOT EXISTS email VARCHAR(150);
