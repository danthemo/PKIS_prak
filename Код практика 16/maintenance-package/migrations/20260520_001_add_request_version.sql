DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'maintenance_requests'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'maintenance_requests'
          AND column_name = 'version'
    ) THEN
        ALTER TABLE public.maintenance_requests
            ADD COLUMN version integer NOT NULL DEFAULT 1;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'maintenance_requests'
    ) THEN
        CREATE INDEX IF NOT EXISTS ix_maintenance_requests_id_version
            ON public.maintenance_requests (id, version);

        COMMENT ON COLUMN public.maintenance_requests.version
            IS 'Optimistic concurrency version for maintenance request updates.';
    END IF;
END $$;
