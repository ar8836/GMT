CREATE TABLE IF NOT EXISTS registros_pendientes (
    id uuid PRIMARY KEY,
    email text NOT NULL,
    datos_json text NOT NULL,
    tipo_registro text NOT NULL,
    expires_at timestamptz NOT NULL,
    s3key text
);